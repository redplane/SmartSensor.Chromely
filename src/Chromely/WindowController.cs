// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using Chromely.Browser;
using Chromely.Core;
using Chromely.Core.Configuration;
using Chromely.Core.Host;
using Chromely.Core.Network;
using Chromely.Core.Providers;

namespace Chromely
{
    public partial class WindowController : ChromelyWindowController
    {
        protected IChromelyRequestSchemeProvider _requestSchemeProvider;

        protected IChromiumDownloadUrlBuilder _downloadUrlBuilder;

        public WindowController(
            IChromiumDownloadUrlBuilder downloadUrlBuilder,
            IChromelyWindow window, 
                                IChromelyNativeHost nativeHost, 
                                IChromelyConfiguration config, 
                                IChromelyRouteProvider routeProvider, 
                                IChromelyRequestTaskRunner requestTaskRunner, 
                                IChromelyCommandTaskRunner commandTaskRunner,
                                IChromelyRequestSchemeProvider requestSchemeProvider,
                                ChromelyHandlersResolver handlersResolver)
            : base(window, nativeHost, config, routeProvider, requestTaskRunner, commandTaskRunner, handlersResolver)
        {

	        _downloadUrlBuilder = downloadUrlBuilder;

            // WindowController.NativeWindow
            _nativeHost.HostCreated += OnWindowCreated;
            _nativeHost.HostMoving += OnWindowMoving;
            _nativeHost.HostSizeChanged += OnWindowSizeChanged;
            _nativeHost.HostClose += OnWindowClose;

            _requestSchemeProvider = requestSchemeProvider;
        }
    }
}
