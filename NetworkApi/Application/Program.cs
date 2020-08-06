/*
 * .NET core entry point.
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;
using System.IO;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SensateService.NetworkApi.Application
{
	public class Program
	{
		public static string GetAppSettings()
		{
			return Environment.GetEnvironmentVariable("SENSATE_NETWORKAPI_APPSETTINGS") ?? "appsettings.json";
		}

		public static void Main(string[] args)
		{
			IWebHost wh;

			Console.WriteLine($"Starting NetworkApi using {Version.VersionString}");
			wh = BuildWebHost(args);
			wh.Run();
		}

		private static IWebHost BuildWebHost(string[] args)
		{
			var conf = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("hosting.json")
						.Build();

			var wh = WebHost.CreateDefaultBuilder(args)
				.UseConfiguration(conf)
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) => {
					config.AddJsonFile(GetAppSettings(), optional: false, reloadOnChange: true);
					config.AddEnvironmentVariables();
				})
				.ConfigureLogging((hostingContext, logging) => {
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
					logging.AddConsole();
					logging.AddDebug();
				})
				.UseStartup<Startup>()
				.ConfigureKestrel((ctx, opts) => {
					opts.AllowSynchronousIO = true;
				});

			return wh.Build();
		}
	}
}

