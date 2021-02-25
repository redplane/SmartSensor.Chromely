using System.Threading.Tasks;
using Chromely.Core.Configuration;
using Chromely.Core.Models;

namespace Chromely.Core.Providers
{
	public interface IChromiumDownloadUrlBuilder
	{
		#region Methods

		Task<BuiltChromiumDownloadUrl> BuildDownloadUrlAsync(IChromelyConfiguration configuration);

		#endregion
	}
}