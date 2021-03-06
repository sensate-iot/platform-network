/*
 * MongoDB measurement repository implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Contexts;
using SensateIoT.Platform.Network.DataAccess.Extensions;

namespace SensateIoT.Platform.Network.DataAccess.Repositories
{
	public class MeasurementRepository : IMeasurementRepository
	{
		private const int MeasurementBucketSize = 500;

		private readonly IMongoCollection<MeasurementBucket> m_buckets;
		private readonly ILogger<MeasurementRepository> m_logger;

		public MeasurementRepository(MongoDBContext context, ILogger<MeasurementRepository> logger)
		{
			this.m_logger = logger;
			this.m_buckets = context.Measurements;
		}

		#region Measurement creation
		private static UpdateDefinition<MeasurementBucket> CreateBucketUpdate(
			ObjectId sensor, ICollection<Measurement> measurements)
		{
			var first = measurements.ElementAt(0);
			var last = measurements.ElementAt(measurements.Count - 1);

			var ubuilder = Builders<MeasurementBucket>.Update;
			var update = ubuilder.PushEach(x => x.Measurements, measurements)
				.SetOnInsert(x => x.Timestamp, DateTime.UtcNow.ThisHour())
				.SetOnInsert(x => x.SensorId, sensor)
				.SetOnInsert(x => x.First, first.Timestamp)
				.Set(x => x.Last, last.Timestamp)
				.Inc(x => x.Count, measurements.Count);

			return update;
		}

		public async Task StoreAsync(IDictionary<ObjectId, List<Measurement>> measurements, CancellationToken ct)
		{
			var updates = new List<UpdateOneModel<MeasurementBucket>>();
			var total = 0L;

			foreach(var kvpair in measurements) {
				var fbuilder = Builders<MeasurementBucket>.Filter;

				var filter = fbuilder.Eq(x => x.Timestamp, DateTime.UtcNow.ThisHour()) &
							 fbuilder.Eq(x => x.SensorId, kvpair.Key) &
							 fbuilder.Lt(x => x.Count, MeasurementBucketSize);

				for(var idx = 0; idx < kvpair.Value.Count;) {
					var sublist = kvpair.Value.GetRange(idx,
														Math.Min(MeasurementBucketSize, kvpair.Value.Count - idx));
					var update = CreateBucketUpdate(kvpair.Key, sublist);

					var upsert = new UpdateOneModel<MeasurementBucket>(filter, update) {
						IsUpsert = true
					};

					updates.Add(upsert);
					idx += sublist.Count;
					total += idx;
				}
			}

			this.m_logger.LogDebug("Measurements stored: " + total);

			var opts = new BulkWriteOptions {
				IsOrdered = false,
				BypassDocumentValidation = true
			};

			try {
				await this.m_buckets.BulkWriteAsync(updates, opts, ct).ConfigureAwait(false);
			} catch(Exception ex) {
				this.m_logger.LogWarning("Unable to store measurements in MongoDB: {message}.", ex.Message);
				this.m_logger.LogWarning("Full exception: {exception}.", ex);
				throw new DataException("Unable to store measurements.", ex);
			}
		}

		public async Task DeleteBySensorId(ObjectId sensorId, CancellationToken ct = default)
		{
			try {
				FilterDefinition<MeasurementBucket> fd;

				fd = Builders<MeasurementBucket>.Filter.Eq(x => x.SensorId, sensorId);
				await this.m_buckets.DeleteManyAsync(fd, ct).ConfigureAwait(false);
			} catch(MongoException ex) {
				this.m_logger.LogWarning(ex.Message);
			}
		}

		public async Task DeleteBySensorId(IEnumerable<ObjectId> sensorIds, CancellationToken ct = default)
		{
			var filter = Builders<MeasurementBucket>.Filter.In(x => x.SensorId, sensorIds);
			await this.m_buckets.DeleteManyAsync(filter, ct).ConfigureAwait(false);
		}

		#endregion
	}
}
