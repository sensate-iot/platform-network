﻿/*
 * .NET core services startup.
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using SensateService.ApiCore.Init;
using SensateService.ApiCore.Middleware;
using SensateService.Common.Config.Config;
using SensateService.Infrastructure.Sql;
using SensateService.Init;

namespace SensateService.Api.DataApi.Application
{
	public class Startup
	{
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration, IWebHostEnvironment environment)
		{
			this._configuration = configuration;
			this._env = environment;
		}

		// ReSharper disable once UnusedMember.Global
		public void ConfigureServices(IServiceCollection services)
		{
			var cache = new CacheConfig();
			var db = new DatabaseConfig();
			var auth = new AuthenticationConfig();
			var sys = new SystemConfig();

			this._configuration.GetSection("System").Bind(sys);
			this._configuration.GetSection("Authentication").Bind(auth);
			this._configuration.GetSection("Cache").Bind(cache);
			this._configuration.GetSection("Database").Bind(db);

			services.AddCors();
			services.AddPostgres(db.PgSQL.ConnectionString, db.Network.ConnectionString);
			services.AddDocumentStore(db.MongoDB.ConnectionString, db.MongoDB.DatabaseName, db.MongoDB.MaxConnections);
			services.AddIdentityFramwork(auth);
			services.AddReverseProxy(sys);
			services.AddAuthorizationProxy(sys);

			if(cache.Enabled) {
				services.AddCacheStrategy(cache, db);
			}

			/* Add repositories */
			services.AddSqlRepositories(cache.Enabled);
			services.AddDocumentRepositories(cache.Enabled);
			services.AddSensorServices();

			services.AddSwaggerGen(c => {
				c.SwaggerDoc("v1", new OpenApiInfo {
					Title = "Sensate IoT Data API - Version 1",
					Version = "v1"
				});

				c.AddSecurityDefinition("key", new OpenApiSecurityScheme {
					In = ParameterLocation.Query,
					Name = "key",
					Type = SecuritySchemeType.ApiKey,
					Description = "API key needed to access the endpoints."
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Name = "key",
							Type = SecuritySchemeType.ApiKey,
							In = ParameterLocation.Query,
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "key"
							},
						},
						new string[] {}
					}
				});
			});

			services.AddRouting();
			services.AddControllers().AddNewtonsoftJson();

			services.AddLogging((logging) => {
				logging.AddConsole();

				if(this._env.IsDevelopment()) {
					logging.AddDebug();
				}
			});
		}

		// ReSharper disable once UnusedMember.Global
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider sp)
		{
			app.UseForwardedHeaders();
			app.UseRouting();

			app.UseCors(p => {
				p.SetIsOriginAllowed(host => true)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			});

			using(var scope = sp.CreateScope()) {
				var ctx = scope.ServiceProvider.GetRequiredService<SensateSqlContext>();
				ctx.Database.EnsureCreated();
				ctx.Database.Migrate();
			}

			if(env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseSwagger(c => {
				c.RouteTemplate = "data/swagger/{documentName}/swagger.json";
			});

			app.UseSwaggerUI(c => {
				c.SwaggerEndpoint("/data/swagger/v1/swagger.json", "Sensate Data API v1");
				c.RoutePrefix = "data/swagger";
			});

			app.UseMiddleware<ApiKeyValidationMiddleware>();
			app.UseMiddleware<RequestLoggingMiddleware>();
			app.UseEndpoints(ep => { ep.MapControllers(); });
		}
	}
}
