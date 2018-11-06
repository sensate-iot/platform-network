/*
 * Abstract measurement repository
 *
 * @author: Michel Megens
 * @email:  dev@bietje.net
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using SensateService.Models;

namespace SensateService.Infrastructure.Repositories
{
	public interface ISensorRepository
	{
		void Create(Sensor sensor);
		Task CreateAsync(Sensor sensor);
		Sensor Get(string id);
		void Remove(string secret);
		void Update(Sensor obj);

		Task<IEnumerable<Sensor>> GetAsync(SensateUser user);
		Task<Sensor> GetAsync(string id);
		Task<long> CountAsync(SensateUser user = null);
		Task RemoveAsync(string id);
		Task UpdateAsync(Sensor sensor);
	}
}
