﻿/*
 * Measurement store implementation. The measurement store acts
 * as a write through storage controller, which means that data
 * isn't cached locally.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SensateService.Crypto;
using SensateService.Enums;
using SensateService.Helpers;
using SensateService.Infrastructure.Events;
using SensateService.Infrastructure.Repositories;
using SensateService.Models;
using SensateService.Models.Json.In;

namespace SensateService.Infrastructure.Storage
{
	using ParsedMeasurementEntry = Tuple<RequestMethod, RawMeasurement, string>;

	public class MeasurementStore : AbstractMeasurementStore
	{
		public static event OnMeasurementReceived MeasurementReceived;

		private readonly ISensorRepository _sensors;
		private readonly IMeasurementRepository _measurements;
		private readonly IUserRepository _users;
		private readonly ISensorStatisticsRepository _stats;

		public MeasurementStore(
			IHashAlgorithm algo,
			IUserRepository users,
			ISensorRepository sensors,
			IMeasurementRepository measurements,
			ISensorStatisticsRepository stats,
			ILogger<MeasurementStore> logger
		) : base(algo, logger)
		{
			this._sensors = sensors;
			this._measurements = measurements;
			this._users = users;
			this._stats = stats;
		}

		public override async Task StoreAsync(string raw, RequestMethod method)
		{
			Measurement m;
			Sensor sensor;
			SensateUser user;
			var obj = JsonConvert.DeserializeObject<RawMeasurement>(raw);

			sensor = await this._sensors.GetAsync(obj.CreatedById).AwaitBackground();

			if(sensor == null) {
				return;
			}

			user = await this._users.GetAsync(sensor.Owner).AwaitBackground();

			if(user == null || user.BillingLockout) {
				return;
			}

			m = this.AuthorizeMeasurement(sensor, user, new ParsedMeasurementEntry(method, obj, raw));
			var measurement_worker = this._measurements.StoreAsync(sensor, m);
			var stats_worker = this._stats.IncrementAsync(sensor, method);
			var events_worker = InvokeMeasurementReceivedAsync(sensor, m);

			await Task.WhenAll(measurement_worker, stats_worker, events_worker).AwaitBackground();
		}

		private static async Task InvokeMeasurementReceivedAsync(Sensor sender, Measurement m)
		{
			Delegate[] delegates;
			MeasurementReceivedEventArgs args;

			if(MeasurementReceived == null)
				return;

			delegates = MeasurementReceived.GetInvocationList();

			if(delegates.Length <= 0)
				return;

			args = new MeasurementReceivedEventArgs {
				CancellationToken = CancellationToken.None,
				Measurement = m,
				Sensor = sender
			};

			await MeasurementReceived.Invoke(sender, args).AwaitBackground();
		}
	}
}
