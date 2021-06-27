/*
 * Filesystem blob service implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Extensions.Options;

using SensateIoT.Platform.Network.Adapters.Abstract;
using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.Adapters.Blobs
{
	public class FilesystemBlobService : IBlobService
	{
		private readonly string m_path;

		private string BasePath => $"{this.m_path}{Path.DirectorySeparatorChar}";

		public FilesystemBlobService(IOptions<BlobOptions> options)
		{
			this.m_path = options.Value.StoragePath;
		}

		public async Task<byte[]> ReadAsync(Blob blob, CancellationToken ct)
		{
			return await Task.Run(() => {
				var path = $"{blob.Path}{Path.DirectorySeparatorChar}{blob.FileName}";
				return File.ReadAllBytes(path);
			}, ct);
		}

		public async Task RemoveAsync(Blob blob, CancellationToken ct)
		{
			await Task.Run(() => {
				var path = $"{blob.Path}{Path.DirectorySeparatorChar}{blob.FileName}";

				if(File.Exists(path)) {
					File.Delete(path);
				}
			}, ct);
		}

		public async Task RemoveAsync(string sensorId, CancellationToken ct)
		{
			await Task.Run(() => {
				var path = $"{this.BasePath}{sensorId}";
				if(Directory.Exists(path)) {
					Directory.Delete(path, true);
				}
			}, ct);
		}
	}
}
