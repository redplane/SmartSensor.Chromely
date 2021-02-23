using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromely.Core.Configuration;
using Chromely.Core.Infrastructure;
using Chromely.Core.Providers;
using Chromely.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xilium.CefGlue;
using Xilium.CefGlue.Interop;

namespace Chromely.Providers
{
	public class DefaultChromiumDownloadUrlBuilder : IChromiumDownloadUrlBuilder
	{
		#region Properties

		protected readonly HttpClient _httpClient;

		protected readonly string _baseUrl;

		#endregion

		#region Constructor

		public DefaultChromiumDownloadUrlBuilder(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_baseUrl = "https://cef-builds.spotifycdn.com";
		}

		#endregion

		#region Methods

		public virtual async Task<string> BuildDownloadUrlAsync(IChromelyConfiguration configuration)
		{
			// Get the current runtime architecture.
			var runtimeArchitecture = GetRuntimeArchitecture();

			// Get the expected cef build number.
			//var expectedCefBuildNumber = GetExpectedCefBuildNumber();
			var szCefVersion = GetCefVersion();
			var szChromiumVersion = GetChromiumVersion();

			// Format the architecture.
			var szArchitecture = runtimeArchitecture.ToString()
				.Replace("X64", "64")
				.Replace("X86", "32");

			// Get the operating system.
			var szOperatingSystem = configuration.Platform.ToString().ToLowerInvariant();
			szArchitecture = $"{szOperatingSystem}{szArchitecture}";

			// Download available cef builds.
			var availableCefBuilds = DownloadAvailableCefBuildAsync().Result;
			if (availableCefBuilds == null || availableCefBuilds.Count < 1)
				throw new Exception("Unable to find matched CEF version.");

			if (!availableCefBuilds.ContainsKey(szArchitecture))
				throw new Exception($"Unable to find CEF version for {szArchitecture}");

			var availableVersions = availableCefBuilds[szArchitecture];
			if (availableVersions == null || availableVersions.Versions == null || availableVersions.Versions.Length < 1)
				throw new Exception($"Cannot find any cef versions for {szArchitecture}");

			var availableVersion = availableVersions.Versions
				.FirstOrDefault(version =>
					version.CefVersion.Equals(szCefVersion) && version.ChromiumVersion.Equals(szChromiumVersion)
					                                        && version.Channel.Equals("stable"));

			if (availableVersion == null)
				throw new Exception($"Cannot find any downloadable version. Expected CEF version: {szCefVersion} | Chromium version: {szChromiumVersion}");

			return $"cef_binary_{availableVersion.CefVersion}_{szArchitecture}_minimal.tar.br2";
		}

		#endregion

		#region Internal methods

		protected virtual Architecture GetRuntimeArchitecture()
		{
			return RuntimeInformation.ProcessArchitecture;
		}

		protected virtual string GetCefVersion()
		{
			return libcef.CEF_VERSION;
		}

		protected virtual string GetChromiumVersion()
		{
			return
				$"{libcef.CHROME_VERSION_MAJOR}.{libcef.CHROME_VERSION_MINOR}.{libcef.CHROME_VERSION_BUILD}.{libcef.CHROME_VERSION_PATCH}";
		}

		protected virtual async Task<IDictionary<string, CefBuildViewModel>> DownloadAvailableCefBuildAsync(CancellationToken cancellationToken = default)
		{
			var indexUrl = $"https://cef-builds.spotifycdn.com/index.json";
			var httpResponseMessage = await _httpClient.GetAsync(indexUrl, cancellationToken);

			if (!httpResponseMessage.IsSuccessStatusCode)
				throw new Exception("Unable to download CEF builds list.");

			var httpContent = httpResponseMessage.Content;
			if (httpContent == null)
				throw new Exception("Unable to download CEF builds list.");

			var szContent = await httpContent.ReadAsStringAsync();
			var jsonSerializerSettings = new JsonSerializerSettings();
			var contractResolver = new DefaultContractResolver();
			contractResolver.NamingStrategy = new SnakeCaseNamingStrategy();
			jsonSerializerSettings.ContractResolver = contractResolver;
			var model = JsonConvert.DeserializeObject<Dictionary<string, CefBuildViewModel>>(szContent, jsonSerializerSettings);
			return model;
		}

		#endregion
	}
}