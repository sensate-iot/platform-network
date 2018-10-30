/*
 * Helper methods to create new database connections.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using SensateService.Converters;
using SensateService.Infrastructure.Document;
using SensateService.Infrastructure.Sql;

namespace SensateService.Init
{
	public static class DatabaseInitExtensions
	{
		public static void AddDocumentStore(this IServiceCollection services, string conn, string db)
		{
			services.Configure<MongoDBSettings>(options => {
				options.DatabaseName = db;
				options.ConnectionString = conn;
			});

			BsonSerializer.RegisterSerializationProvider(new BsonDecimalSerializationProvider());
			services.AddScoped<SensateContext>();
		}

		public static void AddPostgres(this IServiceCollection services, string conn)
		{
			services.AddEntityFrameworkNpgsql()
					.AddDbContext<SensateSqlContext>(options => {
				options.UseNpgsql(conn);
			}, ServiceLifetime.Scoped);
		}
	}
}
