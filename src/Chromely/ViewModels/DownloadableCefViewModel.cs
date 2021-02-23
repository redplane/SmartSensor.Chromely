using System.Text.Json.Serialization;

namespace Chromely.ViewModels
{
	public class DownloadableCefViewModel
	{
		#region Properties

		/// <summary>
		/// Cef version
		/// </summary>
		public string CefVersion { get; set; }

		/// <summary>
		/// Whether version is stable or still in beta.
		/// </summary>
		public string Channel { get; set; }

		/// <summary>
		/// Chromium version
		/// </summary>
		public string ChromiumVersion { get; set; }

		#endregion
	}
}