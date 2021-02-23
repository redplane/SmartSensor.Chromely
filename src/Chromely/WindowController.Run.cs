﻿// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using System.IO;
using Chromely.Browser;
using Chromely.Core.Infrastructure;
using Chromely.Core.Logging;
using Chromely.Loader;
using Chromely.NativeHosts;
using Microsoft.Extensions.Logging;
using Xilium.CefGlue;

namespace Chromely
{
    public partial class WindowController
    {
        private CefSettings _settings;

        /// <summary>
        /// Runs the application.
        /// This call does not return until the application terminates
        /// or an error is occured.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>   
        /// <returns>
        ///  0 successfully run application - now terminated
        ///  1 on internal exception (see log for more information).
        /// </returns>
        public override int Run(string[] args)
        {
            try
            {
                return RunInternal(args);
            }
            catch (Exception exception)
            {
                Logger.Instance.Log.LogError(exception, "HostBase:Run");
                return 1;
            }
        }

        public override void Quit()
        {
            NativeHost_Quit();
        }

        protected virtual CefApp CreateApp() {
            return new CefBrowserApp(_config, _requestSchemeProvider, _handlersResolver);
        }

        protected int RunInternal(string[] args)
        {
            MacHostRuntime.LoadNativeHostFile(_config);

            _config.ChromelyVersion = CefRuntime.ChromeVersion;

            // Load chromium files.
            var tempFiles = CefBinariesLoader.Load(_config, _downloadUrlBuilder);

            CefRuntime.EnableHighDpiSupport();

            _settings = new CefSettings
            {
                MultiThreadedMessageLoop = (_config.Platform == ChromelyPlatform.Windows && !_config.WindowOptions.UseOnlyCefMessageLoop),
                LogSeverity = CefLogSeverity.Info,
                LogFile = "logs\\chromely.cef_" + DateTime.Now.ToString("yyyyMMdd") + ".log",
                ResourcesDirPath = _config.AppExeLocation
            };

            _settings.LocalesDirPath = Path.Combine(_settings.ResourcesDirPath, "locales");
            _settings.RemoteDebuggingPort = 20480;
            _settings.Locale = "en-US";
            _settings.NoSandbox = true;

            var argv = args;
            if (CefRuntime.Platform != CefRuntimePlatform.Windows)
            {
                argv = new string[args.Length + 1];
                Array.Copy(args, 0, argv, 1, args.Length);
                argv[0] = "-";
            }

            // Update configuration settings
            _settings.Update(_config.CustomSettings);

            // For Windows- if MultiThreadedMessageLoop is overriden in Setting using CustomSettings, then
            // It is assumed that the developer way not be aware of IWindowOptions - UseOnlyCefMessageLoop
            if (_config.Platform == ChromelyPlatform.Windows)
            {
                _config.WindowOptions.UseOnlyCefMessageLoop = !_settings.MultiThreadedMessageLoop;
            }
            
            // Set DevTools url
            string devtoolsUrl = _config.DevToolsUrl;
            if (string.IsNullOrWhiteSpace(devtoolsUrl))
            {
                _config.DevToolsUrl = $"http://127.0.0.1:{_settings.RemoteDebuggingPort}";
            }
            else
            {
                Uri uri = new Uri(devtoolsUrl);
                if (uri.Port <= 80)
                {
                    _config.DevToolsUrl = $"{devtoolsUrl}:{_settings.RemoteDebuggingPort}";
                }
            }

            ResolveHandlers();

            var mainArgs = new CefMainArgs(argv);
            CefApp app = CreateApp();

            if (ClientAppUtils.ExecuteProcess(_config.Platform, argv))
            {
                // CEF applications have multiple sub-processes (render, plugin, GPU, etc)
                // that share the same executable. This function checks the command-line and,
                // if this is a sub-process, executes the appropriate logic.
                var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
                if (exitCode >= 0)
                {
                    // The sub-process has completed so return here.
                    Logger.Instance.Log.LogInformation($"Sub process executes successfully with code: {exitCode}");
                    return exitCode;
                }
            }

            CefRuntime.Initialize(mainArgs, _settings, app, IntPtr.Zero);

            _window.RegisterHandlers();
            RegisterDefaultSchemeHandlers();
            RegisterCustomSchemeHandlers();

            CefBinariesLoader.DeleteTempFiles(tempFiles);

            _window.Init(_settings);

            MacHostRuntime.EnsureNativeHostFileExists(_config);

            NativeHost_CreateAndShowWindow();

            NativeHost_Run();

            CefRuntime.Shutdown();

            NativeHost_Quit();

            return 0;
        }
    }
}
