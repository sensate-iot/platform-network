/*
 * Measurement model
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

using MongoDB.Bson.Serialization.Attributes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SensateService.Models
{
	using DataPointMap = IDictionary<string, DataPoint>;

	[Serializable]
	public class Measurement : ISerializable
	{
		[BsonRequired]
		public IDictionary<string, DataPoint> Data { get;set; }
		[BsonRequired]
		public DateTime CreatedAt {get;set;}

		public Measurement()
		{
		}

		protected Measurement(SerializationInfo info, StreamingContext context)
		{
			this.Data = info.GetValue("Data", typeof(DataPointMap)) as DataPointMap;
			this.CreatedAt = info.GetDateTime("CreatedAt");
		}

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		public static bool TryParseData(JToken data, out DataPointMap output)
		{
			DataPointMap datapoints;

			if(data == null) {
				output = null;
				return false;
			}

			try {
				datapoints = data.ToObject<DataPointMap>();
			} catch(JsonSerializationException) {
				output = null;
				return false;
			}

			output = datapoints;
			return true;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Data", this.Data, typeof(DataPointMap));
			info.AddValue("CreatedAt", this.CreatedAt);
		}
	}
}
