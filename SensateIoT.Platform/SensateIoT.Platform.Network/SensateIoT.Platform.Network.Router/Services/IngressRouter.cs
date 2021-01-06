﻿/*
 * Sensate IoT gRPC router service.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Google.Protobuf;
using Grpc.Core;
using Prometheus;

using SensateIoT.Platform.Network.Common.Collections.Abstract;
using SensateIoT.Platform.Network.Common.Converters;
using SensateIoT.Platform.Network.Contracts.RPC;

namespace SensateIoT.Platform.Network.Router.Services
{
	public class IngressRouter : Contracts.Services.IngressRouter.IngressRouterBase
	{
		private readonly IMessageQueue m_queue;
		private readonly ILogger<IngressRouter> m_logger;
		private readonly Counter m_measurementRequests;
		private readonly Counter m_messageRequests;

		public IngressRouter(IMessageQueue queue, ILogger<IngressRouter> logger)
		{
			this.m_queue = queue;
			this.m_logger = logger;
			this.m_measurementRequests = Metrics.CreateCounter("router_measurement_requests_total", "Total amount of measurement routing requests.");
			this.m_messageRequests = Metrics.CreateCounter("router_message_requests_total", "Total amount of message routing requests.");
		}

		public override Task<RoutingResponse> EnqueueMeasurement(Contracts.DTO.Measurement request,
																 ServerCallContext context)
		{
			RoutingResponse response;

			try {
				var dto = MeasurementProtobufConverter.Convert(request);

				this.m_queue.Add(dto);
				this.m_measurementRequests.Inc();

				response = new RoutingResponse {
					Count = 1,
					Message = "Measurements queued.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			} catch(FormatException) {
				this.m_logger.LogWarning("Received messages from an invalid sensor (ID): {sensorID}", request.SensorID);

				response = new RoutingResponse {
					Count = 0,
					Message = "Measurements not queued. Invalid sensor ID.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			}

			return Task.FromResult(response);
		}

		public override Task<RoutingResponse> EnqueueMessage(Contracts.DTO.TextMessage request,
															 ServerCallContext context)
		{
			RoutingResponse response;

			try {
				var dto = MessageProtobufConverter.Convert(request);

				this.m_queue.Add(dto);
				this.m_messageRequests.Inc();

				response = new RoutingResponse {
					Count = 1,
					Message = "Message queued.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			} catch(FormatException) {
				this.m_logger.LogWarning("Received messages from an invalid sensor (ID): {sensorID}", request.SensorID);

				response = new RoutingResponse {
					Count = 0,
					Message = "Messages not queued. Invalid sensor ID.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			}

			return Task.FromResult(response);
		}

		public override Task<RoutingResponse> EnqueueBulkMeasurements(Contracts.DTO.MeasurementData request,
																	  ServerCallContext context)
		{
			RoutingResponse response;

			try {
				var dto = MeasurementProtobufConverter.Convert(request).ToList();

				this.m_queue.AddRange(dto);
				this.m_measurementRequests.Inc();

				response = new RoutingResponse {
					Count = request.Measurements.Count,
					Message = "Messages queued.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			} catch(FormatException ex) {
				this.m_logger.LogWarning("Received messages from an invalid sensor. Exception: {exception}", ex);

				response = new RoutingResponse {
					Count = 0,
					Message = "Messages not queued. Invalid sensor ID.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			}

			return Task.FromResult(response);
		}

		public override Task<RoutingResponse> EnqueueBulkMessages(Contracts.DTO.TextMessageData request,
																  ServerCallContext context)
		{
			RoutingResponse response;

			try {
				var dto = MessageProtobufConverter.Convert(request).ToList();

				this.m_queue.AddRange(dto);
				this.m_messageRequests.Inc();

				response = new RoutingResponse {
					Count = request.Messages.Count,
					Message = "Messages queued.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			} catch(FormatException ex) {
				this.m_logger.LogWarning("Received messages from an invalid sensor. Exception: {exception}", ex);

				response = new RoutingResponse {
					Count = 0,
					Message = "Messages not queued. Invalid sensor ID.",
					ResponseID = ByteString.CopyFrom(Guid.NewGuid().ToByteArray())
				};
			}

			return Task.FromResult(response);
		}
	}
}
