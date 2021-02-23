// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Chromely.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Chromely.Core.Infrastructure
{
    /// <summary>
    /// This class provides operating system and runtime information
    /// used to host the application.
    /// </summary>
    public static class ChromelyRuntime
    {
        /// <summary>
        /// Gets the runtime the application is running on.
        /// </summary>
        public static ChromelyPlatform Platform
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return ChromelyPlatform.MacOSX;
                    
                    case PlatformID.Unix:
                    case (PlatformID)128:   // Framework (1.0 and 1.1) didn't include any PlatformID value for Unix, so Mono used the value 128.
                        return IsRunningOnMac()
                        ? ChromelyPlatform.MacOSX
                        : ChromelyPlatform.Linux;

                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.Xbox:
                        return ChromelyPlatform.Windows;

                    default:
                        return ChromelyPlatform.NotSupported;
                }
                
            }
        }

        private static bool IsRunningOnMac()
        {
            try
            {
                var osName = Environment.OSVersion.VersionString;
                if (osName.ToLower().Contains("darwin")) return true;
                if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist")) return true;
            }
            catch {}

            return false;
        }
    }
}