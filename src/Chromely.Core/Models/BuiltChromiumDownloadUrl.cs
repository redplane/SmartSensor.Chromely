namespace Chromely.Core.Models
{
	public class BuiltChromiumDownloadUrl
	{
		#region Properties

		public string FullUrl { get; private set; }

		public string FileName { get; private set; }

		#endregion

		#region Constructor

		public BuiltChromiumDownloadUrl(string fullUrl, string fileName)
		{
			FullUrl = fullUrl;
			FileName = fileName;
		}

		#endregion
	}
}