/*
 * Abstract document repository
 *
 * @author: Michel Megens
 * @email:  dev@bietje.net
 */

using System;
using System.Threading.Tasks;

using MongoDB.Bson;

namespace SensateService.Infrastructure.Document
{
	public abstract class AbstractDocumentRepository<TKey, T> : IRepository<TKey, T> where T : class
	{
		private readonly SensateContext context;

		public AbstractDocumentRepository(SensateContext context)
		{
			this.context = context;
		}

		public abstract void Commit(T obj);
		public abstract Task CommitAsync(T obj);
		public abstract void Create(T obj);
		public abstract T GetById(TKey id);

		protected virtual ObjectId GenerateId(DateTime ts)
		{
			return ObjectId.GenerateNewId(ts);
		}

		protected virtual ObjectId GenerateId()
		{
			return this.GenerateId(DateTime.Now);
		}

		public abstract Task CreateAsync(T obj);
		public abstract Task DeleteAsync(TKey id);
		public abstract void Update(T obj);
		public abstract void Delete(TKey id);
	}
}