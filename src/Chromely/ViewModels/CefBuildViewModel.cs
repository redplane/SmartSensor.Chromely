using System.Text.Json.Serialization;

namespace Chromely.ViewModels
{
	public class CefBuildViewModel
	{
		#region Properties

		/// <summary>
		/// Version of cef builds.
		/// </summary>
		public DownloadableCefViewModel[] Versions { get; set; }

		#endregion
	}
}