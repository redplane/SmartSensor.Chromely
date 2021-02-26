using System.Threading.Tasks;
using Chromely.Core.Configuration;
using Chromely.Core.Models;

namespace Chromely.Core.Providers
{
	public interface IChromiumDownloadUrlBuilder
	{
		#region Methods

		/// <summary>
		/// Build CEF download url.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		Task<BuiltChromiumDownloadUrl> BuildDownloadUrlAsync(IChromelyConfiguration configuration);

		#endregion
	}
}