/*
 * MongoDB settings.
 *
 * @author: Michel Megens
 * @email:  dev@bietje.net
 */

namespace SensateService.Models.Database
{
	public class MongoDBSettings
	{
		public string ConnectionString { get; set; }
		public string DatabaseName { get; set; }
	}
}
