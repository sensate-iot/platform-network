﻿/*
 * Load test router client.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading.Tasks;

using Grpc.Core;

using SensateIoT.Platform.Router.Contracts.Services;

namespace SensateIoT.Platform.Network.LoadTest.RouterTest
{
	public class RouterClient
	{
		private readonly Channel m_channel;

		public RouterClient(string hostname, ushort port)
		{
			this.m_channel = new Channel(hostname, Convert.ToInt32(port), ChannelCredentials.Insecure);
		}

		public async Task RunAsync(MeasurementGenerator generator, int count)
		{
			var router = new IngressRouter.IngressRouterClient(this.m_channel);

			while(true) {
				var measurements = generator.GenerateMeasurementData(count);
				await router.EnqueueBulkMeasurementsAsync(measurements, Metadata.Empty);
			}
		}
	}
}
