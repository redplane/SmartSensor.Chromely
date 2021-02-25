﻿// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System.Collections.Generic;

namespace Chromely.Core.Network
{
    public interface IChromelyRouteProvider
    {
        #region Properties

        Dictionary<string, RequestActionRoute> ActionRouteDictionary { get; }

        Dictionary<string, CommandActionRoute> CommandRouteDictionary { get; }

        #endregion

        #region Methods

        void RegisterAllRoutes(List<ChromelyController> controllers);

        void RegisterActionRoute(string key, RequestActionRoute route);

        void RegisterCommandRoute(string key, CommandActionRoute command);

        void RegisterActionRoutes(Dictionary<string, RequestActionRoute> newRouteDictionary);

        void RegisterCommandRoutes(Dictionary<string, CommandActionRoute> newCommandDictionary);

        RequestActionRoute GetActionRoute(string method, string routeUrl);

        CommandActionRoute GetCommandRoute(string commandPath);

        bool IsActionRouteAsync(string method, string routeUrl);

        #endregion
    }
}
