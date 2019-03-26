/*
 * Cached measurement repository
 *
 * @author: Michel Megens
 * @email:  dev@bietje.net
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SensateService.Helpers;
using SensateService.Infrastructure.Cache;
using SensateService.Models;

namespace SensateService.Infrastructure.Document
{
	public class CachedMeasurementRepository : MeasurementRepository
	{
		private readonly ICacheStrategy<string> _cache;

		public CachedMeasurementRepository(
			SensateContext context,
			ILogger<MeasurementRepository> logger,
			ICacheStrategy<string> cache) : base(context, logger)
		{
			_cache = cache;
		}

		public override async Task DeleteAsync(string id)
		{
			var tasks = new[] {
				_cache.RemoveAsync(id),
				base.DeleteAsync(id)
			};

			await Task.WhenAll(tasks).AwaitBackground();
		}

		private async Task CacheDataAsync(string key, object data, int tmo, bool slide = true)
		{
			if(key == null || data == null)
				return;

			await this._cache.SerializeAsync(key, data, tmo, slide).AwaitBackground();
		}

		public override async Task DeleteBySensorAsync(Sensor sensor)
		{
			string key;

			key = $"Measurements::{sensor.InternalId.ToString()}";
			var tasks = new[] {
				this._cache.RemoveAsync(key),
				base.DeleteBySensorAsync(sensor)
			};

			await Task.WhenAll(tasks).AwaitBackground();
		}

		public override async Task DeleteBetweenAsync(Sensor sensor, DateTime start, DateTime end)
		{
			string key;

			key = $"{sensor.InternalId}::{start.ToString(CultureInfo.InvariantCulture)}::{end.ToString(CultureInfo.InvariantCulture)}";
			var tasks = new[] {
                this._cache.RemoveAsync(key),
                base.DeleteBetweenAsync(sensor, start, end)
			};

			await Task.WhenAll(tasks).AwaitBackground();
		}

		public override async Task<IEnumerable<Measurement>> GetMeasurementsBySensorAsync(Sensor sensor)
		{
			string key;

			key = $"Measurements::{sensor.InternalId.ToString()}";
			return await TryGetMeasurementsAsync(key, x => x.SensorId == sensor.InternalId, CacheTimeout.TimeoutMedium.ToInt() ).AwaitBackground();
		}

		private async Task<IEnumerable<Measurement>> TryGetMeasurementsAsync(string key, Expression<Func<MeasurementBucket, bool>> expression, int tmo)
		{
			IEnumerable<Measurement> measurements = null;

			if(key != null)
				measurements = await _cache.DeserializeAsync<IEnumerable<Measurement>>(key);

			if(measurements != null)
				return measurements;

			measurements = await base.GetMeasurementsAsync(expression).AwaitBackground();
			await this.CacheDataAsync(key, measurements, tmo, false).AwaitBackground();
			return measurements;

		}

		public override async Task<IEnumerable<Measurement>> GetAfterAsync(Sensor sensor, DateTime pit)
		{
			string key;
			IEnumerable<Measurement> measurements;

			key = $"{sensor.InternalId}::after::{pit.ToString(CultureInfo.InvariantCulture)}";
			measurements = await _cache.DeserializeAsync<IEnumerable<Measurement>>(key);

			if(measurements != null)
				return measurements;

			measurements = await base.GetAfterAsync(sensor, pit).AwaitBackground();
			await this.CacheDataAsync(key, measurements, CacheTimeout.TimeoutMedium.ToInt(), false).AwaitBackground();
			return measurements;

		}

		public override async Task<IEnumerable<Measurement>> GetBetweenAsync(Sensor sensor, DateTime start, DateTime end)
		{
			string key;
			IEnumerable<Measurement> measurements;

			key = $"{sensor.InternalId}::{start.ToString(CultureInfo.InvariantCulture)}::{end.ToString(CultureInfo.InvariantCulture)}";
			measurements = await _cache.DeserializeAsync<IEnumerable<Measurement>>(key);

			if(measurements != null)
				return measurements;

			measurements = await base.GetBetweenAsync(sensor, start, end).AwaitBackground();
			await CacheDataAsync(key, measurements, CacheTimeout.Timeout.ToInt()).AwaitBackground();
			return measurements;


		}

		public override async Task<IEnumerable<Measurement>> GetBeforeAsync(Sensor sensor, DateTime pit)
		{
			string key;
			IEnumerable<Measurement> measurements;

			key = $"{sensor.InternalId}::before::{pit.ToString(CultureInfo.InvariantCulture)}";
			measurements = await this._cache.DeserializeAsync<IEnumerable<Measurement>>(key).AwaitBackground();

			if(measurements != null)
				return null;

			measurements = await base.GetBeforeAsync(sensor, pit).AwaitBackground();
			await this.CacheDataAsync(key, measurements, CacheTimeout.Timeout.ToInt()).AwaitBackground();
			return measurements;
		}
	}
}
