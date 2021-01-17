﻿// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;

namespace Chromely.Core.Network
{
    /// <summary>
    /// The Chromely request.
    /// </summary>
    public class ChromelyRequest : IChromelyRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChromelyRequest"/> class.
        /// </summary>
        public ChromelyRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromelyRequest"/> class.
        /// </summary>
        /// <param name="routeUrl">
        /// The route path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="postData">
        /// The post data.
        /// </param>
        public ChromelyRequest(string routeUrl, IDictionary<string, string> parameters, object postData)
        {
            Id = Guid.NewGuid().ToString();
            RouteUrl = routeUrl;
            Parameters = parameters;
            PostData = postData;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromelyRequest"/> class.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="routeUrl">
        /// The route path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="postData">
        /// The post data.
        /// </param>
        public ChromelyRequest(string id, string routeUrl, IDictionary<string, string> parameters, object postData)
        {
            Id = id;
            RouteUrl = routeUrl;
            Parameters = parameters;
            PostData = postData;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromelyRequest"/> class.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="method">Request method</param>
        /// <param name="routeUrl">
        /// The route path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="postData">
        /// The post data.
        /// </param>
        /// <param name="rawJson">
        /// The raw json.
        /// </param>
        public ChromelyRequest(string id, string method, string routeUrl, IDictionary<string, string> parameters, object postData, string rawJson)
        {
            Id = id;
            Method = method;
            RouteUrl = routeUrl;
            Parameters = parameters;
            PostData = postData;
            RawJson = rawJson;
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Method of request.
        /// </summary>
        public string Method { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the route path.
        /// </summary>
        public string RouteUrl { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the post data.
        /// </summary>
        public object PostData { get; set; }

        /// <summary>
        /// Gets or sets the raw json.
        /// Only used for CefGlue Generic Message Routing requests.
        /// </summary>
        public string RawJson { get; set; }
    }
}
