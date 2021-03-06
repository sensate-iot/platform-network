/*
 * Unit tests for MemoryCache.TryRemove().
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SensateIoT.Platform.Network.Common.Caching.Memory;

namespace SensateIoT.Platform.Network.Tests.Caching.Memory
{
	[TestClass]
	public class TryRemoveEntryTests
	{
		[TestMethod]
		public void RemoveDoesDeleteAfterScan()
		{
			var cache = new MemoryCache<int, int>();

			cache.Add(1, 2);
			cache.TryRemove(1);
			cache.RemoveScheduledEntries();
			Thread.Yield();

			Assert.AreEqual(0, cache.Count);
		}

		[TestMethod]
		public void CannotRemoveNullKey()
		{
			var cache = new MemoryCache<string, int>();
			Assert.ThrowsException<ArgumentNullException>(() => cache.Remove(null));
		}

		[TestMethod]
		public void CannotRemoveNonExistingKey()
		{
			var cache = new MemoryCache<int, int>();

			cache.Add(1, 2);
			Assert.IsFalse(cache.TryRemove(2));
		}

		[TestMethod]
		public void CannotRemovePreviouslyRemovedKey()
		{
			var cache = new MemoryCache<int, int>();

			cache.Add(1, 2);
			Assert.IsTrue(cache.TryRemove(1));
			Assert.IsFalse(cache.TryRemove(1));
		}


		[TestMethod]
		public void CanRemoveExpiredItem()
		{
			var cache = new MemoryCache<int, int>(MemoryCache<int, int>.DefaultCapacity, TimeSpan.FromMilliseconds(100));

			cache.Add(1, 2);
			Thread.Sleep(150);
			Assert.IsTrue(cache.TryRemove(1));
			cache.RemoveScheduledEntries();

			Assert.AreEqual(0, cache.Count);
		}

		[TestMethod]
		public void DeletionScanRunsOncePerTimeUnit()
		{
			var cache = new MemoryCache<int, int>();

			cache.Add(1, 1);
			Assert.AreEqual(1, cache.Count);
			cache.Remove(1);

			Thread.Sleep(10);
			Assert.AreEqual(0, cache.Count);

			cache.Add(1, 1);
			Assert.AreEqual(1, cache.Count);
			cache.Remove(1);
			Thread.Sleep(10);
			Assert.AreEqual(1, cache.Count);
		}
	}
}
