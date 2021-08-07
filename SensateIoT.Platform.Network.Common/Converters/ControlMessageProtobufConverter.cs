﻿/*
 * Protobuf control message converter.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Linq;

using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson;

using SensateIoT.Platform.Network.Data.Abstract;
using SensateIoT.Platform.Network.Data.DTO;

namespace SensateIoT.Platform.Network.Common.Converters
{
	public static class ControlMessageProtobufConverter
	{
		public static ControlMessage Convert(Router.Contracts.DTO.ControlMessage message)
		{
			return new ControlMessage {
				Timestamp = message.Timestamp?.ToDateTime() ?? DateTime.UtcNow,
				Data = message.Data,
				Secret = "",
				SensorId = ObjectId.Parse(message.SensorID),
				Destination = (ControlMessageType)message.Destination
			};
		}

		public static IEnumerable<ControlMessage> Convert(Router.Contracts.DTO.ControlMessageData message)
		{
			return message.Messages.Select(Convert);
		}

		public static Router.Contracts.DTO.ControlMessage Convert(ControlMessage message)
		{
			return new Router.Contracts.DTO.ControlMessage {
				Data = message.Data,
				Destination = System.Convert.ToInt32(message.Destination),
				SensorID = message.SensorID.ToString(),
				Timestamp = Timestamp.FromDateTime(message.PlatformTimestamp),
				Secret = message.Secret
			};
		}
	}
}
