﻿/*
 * MQTT measurement handler.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Google.Protobuf;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Prometheus;

using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

using SensateIoT.Platform.Network.Common.Converters;
using SensateIoT.Platform.Network.Common.Helpers;
using SensateIoT.Platform.Network.Common.MQTT;
using SensateIoT.Platform.Router.Contracts.DTO;
using SensateIoT.Platform.Network.Data.DTO;
using SensateIoT.Platform.Network.TriggerService.Abstract;
using SensateIoT.Platform.Network.TriggerService.Config;
using SensateIoT.Platform.Network.TriggerService.DTO;

using Convert = System.Convert;
using DataPoint = SensateIoT.Platform.Network.Data.DTO.DataPoint;

namespace SensateIoT.Platform.Network.TriggerService.MQTT
{
	[UsedImplicitly]
	public class MqttBulkNumberTriggerHandler : IMqttHandler
	{
		private readonly IServiceProvider m_provider;
		private readonly ILogger<MqttBulkNumberTriggerHandler> logger;
		private readonly IDataPointMatchingService m_matcher;
		private readonly IInternalMqttClient m_client;
		private readonly string m_eventTopic;

		private readonly Counter m_measurementCounter;
		private readonly Counter m_matchCounter;
		private readonly Histogram m_duration;

		public MqttBulkNumberTriggerHandler(IServiceProvider provider,
											IDataPointMatchingService matcher,
											IInternalMqttClient client,
											IOptions<MqttConfig> options,
											ILogger<MqttBulkNumberTriggerHandler> logger)
		{
			this.m_provider = provider;
			this.logger = logger;
			this.m_matcher = matcher;
			this.m_client = client;
			this.m_eventTopic = options.Value.InternalBroker.TriggerEventTopic;

			this.m_matchCounter = Metrics.CreateCounter("triggerservice_measurements_matched_total", "Total amount of measurements that matched a trigger.");
			this.m_measurementCounter = Metrics.CreateCounter("triggerservice_measurements_received_total", "Total amount of measurements received.");
			this.m_duration = Metrics.CreateHistogram("triggerservice_measurement_handle_duration_seconds", "Histogram of measurement handling duration.");
		}

		private IEnumerable<InternalBulkMeasurements> Decompress(string data)
		{
			var bytes = Convert.FromBase64String(data);
			using var to = new MemoryStream();
			using var from = new MemoryStream(bytes);
			using var gzip = new GZipStream(@from, CompressionMode.Decompress);

			gzip.CopyTo(to);
			var final = to.ToArray();
			var protoMeasurements = MeasurementData.Parser.ParseFrom(final);
			var measurements =
				from measurement in protoMeasurements.Measurements
				group measurement by measurement.SensorID into g
				select new InternalBulkMeasurements {
					SensorID = ObjectId.Parse(g.Key),
					Measurements = g.Select(m => new SingleMeasurement {
						Data = m.Datapoints.ToDictionary(p => p.Key, p => new DataPoint {
							Accuracy = p.Accuracy,
							Precision = p.Precision,
							Unit = p.Unit,
							Value = Convert.ToDecimal(p.Value),
						}),
						Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(m.Longitude, m.Latitude)),
						PlatformTime = m.PlatformTime.ToDateTime(),
						Timestamp = m.Timestamp.ToDateTime()
					}).ToList()
				};

			this.logger.LogInformation("Received {count} measurements.", protoMeasurements.Measurements.Count);
			return measurements;
		}

		public async Task OnMessageAsync(string topic, string message, CancellationToken ct)
		{
			using(this.m_duration.NewTimer()) {
				await this.HandleMessageAsync(message).ConfigureAwait(false);
			}
		}

		private async Task HandleMessageAsync(string message)
		{
			this.logger.LogDebug("Measurement received.");

			var measurements = this.Decompress(message).ToList();

			this.m_measurementCounter.Inc(measurements.Count);
			var tasks = measurements.Select(this.HandleMeasurement).ToList();
			var results = await Task.WhenAll(tasks).ConfigureAwait(false);

			var data = new TriggerEventData();

			foreach(var triggerLists in results) {
				foreach(var triggerEvents in triggerLists) {
					data.Events.AddRange(triggerEvents);
				}
			}

			await this.PublishAsync(data).ConfigureAwait(false);
			this.logger.LogDebug("Measurement handled.");
		}

		private Task<List<TriggerEvent>[]> HandleMeasurement(InternalBulkMeasurements measurements)
		{
			using var scope = this.m_provider.CreateScope();
			var triggersdb = scope.ServiceProvider.GetRequiredService<ITriggerActionCache>();
			var actions = triggersdb.Lookup(measurements.SensorID);
			var exec = scope.ServiceProvider.GetRequiredService<ITriggerActionExecutionService>();
			var tasks = new List<Task<List<TriggerEvent>>>();

			if(actions == null) {
				return Task.FromResult<List<TriggerEvent>[]>(null);
			}

			foreach(var m in measurements.Measurements) {
				foreach(var keyValuePair in m.Data) {
					this.m_matchCounter.Inc();
					var matched = this.m_matcher.Match(keyValuePair.Key, keyValuePair.Value, actions).ToList();
					tasks.Add(ExecuteActionsAsync(exec, matched, keyValuePair.Value, m));
				}
			}

			return Task.WhenAll(tasks);
		}

		private static async Task<List<TriggerEvent>> ExecuteActionsAsync(ITriggerActionExecutionService exec, IEnumerable<TriggerAction> actions, DataPoint dp, SingleMeasurement m)
		{
			var tasks = new List<Task>();
			var events = new List<TriggerEvent>();

			foreach(var action in actions) {
				var result = Replace(action.Message, dp, m);

				tasks.Add(exec.ExecuteAsync(action, result));
				events.Add(TriggerActionEventConverter.Convert(action));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
			return events;
		}

		private static string Replace(string message, DataPoint dp, SingleMeasurement m)
		{
			string precision;
			string accuracy;
			var body = message.Replace("$value", dp.Value.ToString(CultureInfo.InvariantCulture));

			body = body.Replace("$unit", dp.Unit);

			precision = dp.Precision != null ? dp.Precision.Value.ToString(CultureInfo.InvariantCulture) : "";
			accuracy = dp.Accuracy != null ? dp.Accuracy.Value.ToString(CultureInfo.InvariantCulture) : "";
			var lon = m.Location.Coordinates.Longitude.ToString(CultureInfo.InvariantCulture);
			var lat = m.Location.Coordinates.Latitude.ToString(CultureInfo.InvariantCulture);

			body = body.Replace("$timestamp", m.Timestamp.ToString("O"));
			body = body.Replace("$precision", precision);
			body = body.Replace("$accuracy", accuracy);
			body = body.Replace("$lon", lon);
			body = body.Replace("$lat", lat);

			return body;
		}

		private async Task PublishAsync(IMessage protoEvents)
		{
			await using var measurementStream = new MemoryStream();
			protoEvents.WriteTo(measurementStream);
			var data = measurementStream.ToArray().Compress();
			await this.m_client.PublishOnAsync(this.m_eventTopic, data, false).ConfigureAwait(false);
		}
	}
}

