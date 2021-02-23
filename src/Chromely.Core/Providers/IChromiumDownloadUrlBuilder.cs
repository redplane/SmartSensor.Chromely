using System.Threading.Tasks;
using Chromely.Core.Configuration;

namespace Chromely.Core.Providers
{
	public interface IChromiumDownloadUrlBuilder
	{
		#region Methods

		Task<string> BuildDownloadUrlAsync(IChromelyConfiguration configuration);

		#endregion
	}
}