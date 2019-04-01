﻿/*
 * Options for the internal MQTT service.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

namespace SensateService.Services.Settings
{
	public class InternalMqttServiceOptions
	{
		public string Host {get;set;}
		public int Port {get;set;}
		public bool Ssl {get;set;}
		public string Id {get;set;}
		public string Username {get;set;}
		public string Password {get;set;}
		public string InternalMeasurementTopic { get; set; }
		public string InternalBulkMeasurementTopic { get; set; }
	}
}