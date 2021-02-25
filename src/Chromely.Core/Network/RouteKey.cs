// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

namespace Chromely.Core.Network
{
    public static class RouteKey
    {
        #region Methods

        public static string CreateRequestKey(string method, string url)
        {
            url = url?.Trim().TrimStart('/');
            var key = string.IsNullOrWhiteSpace(method)
                ? $"action_{url}".Replace("/", "_").Replace("\\", "_") 
                : $"action_{method}_{url}".Replace("/", "_").Replace("\\", "_");

            return key.ToLower();
        }

        public static string CreateCommandKey(string url)
        {
            url = url?.Trim().TrimStart('/');
            var routeKey = $"commmand_{url}".Replace("/", "_").Replace("\\", "_");
            return routeKey.ToLower();
        }

        #endregion
    }
}
