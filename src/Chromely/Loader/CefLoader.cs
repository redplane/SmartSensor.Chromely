// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Chromely.Core.Configuration;
using Chromely.Core.Infrastructure;
using Chromely.Core.Logging;
using Chromely.Core.Models;
using Chromely.Core.Providers;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Xilium.CefGlue;
// ReSharper disable MemberCanBePrivate.Global

namespace Chromely.Loader
{
	/// <summary>
	/// Loads the necessary CEF runtime files from cef-builds.spotifycdn.com
	/// Inherits detailed version information from cefbuilds/index page.
	/// Note:
	/// Keep this class in a separate nuget package
	/// due to additional reference to ICSharpCode.SharpZipLib.
	/// Not everyone will be glad about this. 
	/// </summary>
	public class CefLoader
	{
		private static string MacOSConfigFile = "Info.plist";

		private static string MacOSDefaultAppName = "Chromium Embedded Framework";

		/// <summary>
		/// Gets or sets the timeout for the CEF download in minutes.
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
		public int DownloadTimeoutMinutes { get; set; } = 10;

		/// <summary>
		/// Download CEF runtime files.
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static void Download(IChromelyConfiguration chromelyConfiguration, IChromiumDownloadUrlBuilder downloadUrlBuilder)
		{
			var loader = new CefLoader(chromelyConfiguration.Platform);
			try
			{
				var watch = new Stopwatch();
				watch.Start();

				// Get the download url.
				var builtUrl = downloadUrlBuilder.BuildDownloadUrlAsync(chromelyConfiguration)
					.Result;

				//loader.GetDownloadUrl();
				if (!loader.ParallelDownload(builtUrl))
				{
					loader.Download(builtUrl.FullUrl);
				}
				Logger.Instance.Log.LogInformation($"CefLoader: Download took {watch.ElapsedMilliseconds}ms");
				watch.Restart();
				loader.DecompressArchive();
				Logger.Instance.Log.LogInformation($"CefLoader: Decompressing archive took {watch.ElapsedMilliseconds}ms");
				watch.Restart();
				loader.CopyFilesToAppDirectory(builtUrl.FileName);
				Logger.Instance.Log.LogInformation($"CefLoader: Copying files took {watch.ElapsedMilliseconds}ms");
			}
			catch (Exception ex)
			{
				Logger.Instance.Log.LogError("CefLoader: " + ex.Message);
				throw;
			}
			finally
			{
				if (!string.IsNullOrEmpty(loader._tempBz2File))
				{
					File.Delete(loader._tempBz2File);
				}

				if (!string.IsNullOrEmpty(loader._tempTarStream))
				{
					File.Delete(loader._tempTarStream);
				}

				if (!string.IsNullOrEmpty(loader._tempTarFile))
				{
					File.Delete(loader._tempTarFile);
				}
				if (!string.IsNullOrEmpty(loader._tempDirectory) && Directory.Exists(loader._tempDirectory))
				{
					Directory.Delete(loader._tempDirectory, true);
				}
			}
		}


		private readonly ChromelyPlatform _platform;

		private readonly string _tempTarStream;
		private readonly string _tempBz2File;
		private readonly string _tempTarFile;
		private readonly string _tempDirectory;

		private long _downloadLength;

		private readonly int _numberOfParallelDownloads;
		private int _lastPercent;

		private CefLoader(ChromelyPlatform platform)
		{
			_platform = platform;

			_lastPercent = 0;
			_numberOfParallelDownloads = Environment.ProcessorCount;

			_tempTarStream = Path.GetTempFileName();
			_tempBz2File = Path.GetTempFileName();
			_tempTarFile = Path.GetTempFileName();
			_tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		}

		public static void SetMacOSAppName(IChromelyConfiguration config)
		{
			if (config.Platform == ChromelyPlatform.MacOSX)
			{
				Task.Run(() =>
				{
					try
					{
						var appName = config.AppName;
						if (string.IsNullOrWhiteSpace(appName))
						{
							appName = Assembly.GetEntryAssembly()?.GetName().Name;
						}

						var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
						var pInfoFile = Path.Combine(appDirectory, MacOSConfigFile);
						if (File.Exists(pInfoFile))
						{
							var pInfoFileText = File.ReadAllText(pInfoFile);
							pInfoFileText = pInfoFileText.Replace(MacOSDefaultAppName, appName);
							File.WriteAllText(MacOSConfigFile, pInfoFileText);
						}
					}
					catch { }
				});
			}
		}

		private class Range
		{
			public long Start { get; set; }
			public long End { get; set; }
		}
		private bool ParallelDownload(BuiltChromiumDownloadUrl builtDownloadUrl)
		{
			try
			{
				var webRequest = WebRequest.Create(builtDownloadUrl.FullUrl);
				webRequest.Method = "HEAD";
				using (var webResponse = webRequest.GetResponse())
				{
					_downloadLength = long.Parse(webResponse.Headers.Get("Content-Length"));
				}

				Logger.Instance.Log.LogInformation($"CefLoader: Parallel download {builtDownloadUrl.FileName}, {_downloadLength / (1024 * 1024)}MB");

				// Calculate ranges  
				var readRanges = new List<Range>();
				for (var chunk = 0; chunk < _numberOfParallelDownloads - 1; chunk++)
				{
					var range = new Range()
					{
						Start = chunk * (_downloadLength / _numberOfParallelDownloads),
						End = ((chunk + 1) * (_downloadLength / _numberOfParallelDownloads)) - 1
					};
					readRanges.Add(range);
				}
				readRanges.Add(new Range()
				{
					Start = readRanges.Any() ? readRanges.Last().End + 1 : 0,
					End = _downloadLength - 1
				});

				// Parallel download
				var tempFilesDictionary = new ConcurrentDictionary<long, string>();

				Parallel.ForEach(readRanges, new ParallelOptions() { MaxDegreeOfParallelism = _numberOfParallelDownloads }, readRange =>
				{
					var httpWebRequest = WebRequest.Create(builtDownloadUrl.FullUrl) as HttpWebRequest;
					// ReSharper disable once PossibleNullReferenceException
					httpWebRequest.Method = "GET";
					httpWebRequest.Timeout = (int)TimeSpan.FromMinutes(DownloadTimeoutMinutes).TotalMilliseconds;
					httpWebRequest.AddRange(readRange.Start, readRange.End);
					using (var httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
					{
						var tempFilePath = Path.GetTempFileName();
						Logger.Instance.Log.LogInformation($"CefLoader: Load {tempFilePath} ({readRange.Start}..{readRange.End})");
						using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
						{
							httpWebResponse?.GetResponseStream()?.CopyTo(fileStream);
							tempFilesDictionary.TryAdd(readRange.Start, tempFilePath);
						}
					}
				});

				// Merge to single file
				if (File.Exists(_tempBz2File))
				{
					File.Delete(_tempBz2File);
				}
				using (var destinationStream = new FileStream(_tempBz2File, FileMode.Append))
				{
					foreach (var tempFile in tempFilesDictionary.OrderBy(b => b.Key))
					{
						var tempFileBytes = File.ReadAllBytes(tempFile.Value);
						destinationStream.Write(tempFileBytes, 0, tempFileBytes.Length);
						File.Delete(tempFile.Value);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Logger.Instance.Log.LogError("CefLoader.ParallelDownload: " + ex.Message);
			}

			return false;
		}

		private void Download(string downloadUrl)
		{
			using var client = new WebClient();
			if (File.Exists(_tempBz2File))
				File.Delete(_tempBz2File);

			Logger.Instance.Log.LogInformation($"CefLoader: Loading {_tempBz2File}");
			client.DownloadFile(downloadUrl, _tempBz2File);
		}

		private void DecompressArchive()
		{
			Logger.Instance.Log.LogInformation("CefLoader: Decompressing BZ2 archive");
			using var tarStream = new FileStream(_tempTarStream, FileMode.Create, FileAccess.ReadWrite);
			using (var inStream = new FileStream(_tempBz2File, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				BZip2.Decompress(inStream, tarStream, false);
			}

			Logger.Instance.Log.LogInformation("CefLoader: Decompressing TAR archive");
			tarStream.Seek(0, SeekOrigin.Begin);
			var tar = TarArchive.CreateInputTarArchive(tarStream, Encoding.UTF8);
			tar.ProgressMessageEvent += (archive, entry, message) =>
			{
				Logger.Instance.Log.LogInformation("CefLoader: Extracting " + entry.Name);
			};
				
			Directory.CreateDirectory(_tempDirectory);
			tar.ExtractContents(_tempDirectory);
		}

		private void CopyFilesToAppDirectory(string folderName)
		{
			Logger.Instance.Log.LogInformation("CefLoader: Copy files to application directory");
			// now we have all files in the temporary directory
			// we have to copy the 'Release' folder to the application directory
			var srcPathRelease = Path.Combine(_tempDirectory, folderName, "Release");
			var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (_platform != ChromelyPlatform.MacOSX)
			{
				CopyDirectory(srcPathRelease, appDirectory);

				var srcPathResources = Path.Combine(_tempDirectory, folderName, "Resources");
				CopyDirectory(srcPathResources, appDirectory);
			}

			if (_platform != ChromelyPlatform.MacOSX) return;

			var cefFrameworkFolder = Path.Combine(srcPathRelease, "Chromium Embedded Framework.framework");

			// rename Chromium Embedded Framework to libcef.dylib and copy to destination folder
			var frameworkFile = Path.Combine(cefFrameworkFolder, "Chromium Embedded Framework");
			var libcefFile = Path.Combine(appDirectory, "libcef.dylib");
			var libcefdylibInfo = new FileInfo(frameworkFile);
			libcefdylibInfo.CopyTo(libcefFile, true);

			// Copy Libraries files
			var librariesFolder = Path.Combine(cefFrameworkFolder, "Libraries");
			CopyDirectory(librariesFolder, appDirectory);

			// Copy Resource files
			var resourcesFolder = Path.Combine(cefFrameworkFolder, "Resources");
			CopyDirectory(resourcesFolder, appDirectory);
		}

		private static void CopyDirectory(string sourceDirName, string destDirName)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dirInfo = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dirInfo.GetDirectories();

			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			var files = dirInfo.GetFiles();
			foreach (var file in files)
			{
				var tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, true);
			}

			foreach (var subDir in dirs)
			{
				var tempPath = Path.Combine(destDirName, subDir.Name);
				CopyDirectory(subDir.FullName, tempPath);
			}
		}
	}
}
