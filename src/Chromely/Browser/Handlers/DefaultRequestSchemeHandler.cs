// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Chromely.Core;
using Chromely.Core.Infrastructure;
using Chromely.Core.Logging;
using Chromely.Core.Network;
using Microsoft.Extensions.Logging;
using Xilium.CefGlue;

namespace Chromely.Browser
{
    public class DefaultRequestSchemeHandler : CefResourceHandler
    {
        #region Properties

        protected readonly IChromelyRequestSchemeProvider _requestSchemeProvider;

        protected readonly IChromelyRequestTaskRunner _requestTaskRunner;

        protected readonly IChromelyRouteProvider _routeProvider;

        protected readonly IChromelySerializerUtil _serializerUtil;

        protected IChromelyResponse _chromelyResponse;

        protected bool _completed;

        protected byte[] _responseBytes;

        protected int _totalBytesRead;

        #endregion

        #region Constructor

        public DefaultRequestSchemeHandler(IChromelyRouteProvider routeProvider,
            IChromelyRequestSchemeProvider requestSchemeProvider, IChromelyRequestTaskRunner requestTaskRunner,
            IChromelySerializerUtil serializerUtil)
        {
            _routeProvider = routeProvider;
            _requestSchemeProvider = requestSchemeProvider;
            _requestTaskRunner = requestTaskRunner;
            _serializerUtil = serializerUtil;
        }

        #endregion

        #region Methods

        [Obsolete]
        protected override bool ProcessRequest(CefRequest request, CefCallback callback)
        {
            var isSchemeRegistered = _requestSchemeProvider?.IsSchemeRegistered(request.Url);
            if (isSchemeRegistered == null || !isSchemeRegistered.Value)
            {
                Logger.Instance.Log.LogWarning($"Url {request.Url} is not of a registered custom scheme.");
                callback.Dispose();
                return false;
            }

            var uri = new Uri(request.Url);
            var path = uri.LocalPath;

            var isRequestAsync = _routeProvider.IsActionRouteAsync(path);
            if (isRequestAsync)
                ProcessRequestAsync(path, request, callback);
            else
                ProcessRequest(path, request, callback);

            return true;
        }

        protected override void GetResponseHeaders(CefResponse response, out long responseLength,
            out string redirectUrl)
        {
            // unknown content-length
            // no-redirect
            responseLength = -1;
            redirectUrl = null;

            try
            {
                var status = _chromelyResponse != null
                    ? (HttpStatusCode)_chromelyResponse.Status
                    : HttpStatusCode.BadRequest;
                var errorStatus = _chromelyResponse != null ? _chromelyResponse.Data.ToString() : "Not Found";

                var headers = response.GetHeaderMap();
                headers.Add("Cache-Control", "private");
                headers.Add("Access-Control-Allow-Origin", "*");
                headers.Add("Access-Control-Allow-Methods", "*");
                headers.Add("Access-Control-Allow-Headers", "Content-Type");
                headers.Add("Content-Type", "application/json; charset=utf-8");
                response.SetHeaderMap(headers);

                response.Status = (int)status;
                response.MimeType = "application/json";
                response.StatusText = status == HttpStatusCode.OK ? "OK" : errorStatus;
            }
            catch (Exception exception)
            {
                Logger.Instance.Log.LogError(exception, exception.Message);
            }
        }

        [Obsolete]
        protected override bool ReadResponse(Stream response, int bytesToRead, out int bytesRead, CefCallback callback)
        {
            var currBytesRead = 0;

            try
            {
                if (_completed)
                {
                    bytesRead = 0;
                    _totalBytesRead = 0;
                    _responseBytes = null;
                    return false;
                }

                if (_responseBytes != null)
                {
                    currBytesRead = Math.Min(_responseBytes.Length - _totalBytesRead, bytesToRead);
                    response.Write(_responseBytes, _totalBytesRead, currBytesRead);
                    _totalBytesRead += currBytesRead;

                    if (_totalBytesRead >= _responseBytes.Length) _completed = true;
                }
                else
                {
                    bytesRead = 0;
                    _completed = true;
                }
            }
            catch (Exception exception)
            {
                Logger.Instance.Log.LogError(exception, exception.Message);
            }

            bytesRead = currBytesRead;
            return true;
        }

        protected override void Cancel()
        {
        }

        protected override bool Open(CefRequest request, out bool handleRequest, CefCallback callback)
        {
            handleRequest = false;
            return false;
        }

        protected override bool Skip(long bytesToSkip, out long bytesSkipped, CefResourceSkipCallback callback)
        {
            bytesSkipped = 0;
            return true;
        }

        protected override bool Read(IntPtr dataOut, int bytesToRead, out int bytesRead,
            CefResourceReadCallback callback)
        {
            bytesRead = -1;
            return false;
        }

        private static string GetPostData(CefRequest request)
        {
            var postDataElements = request?.PostData?.GetElements();
            if (postDataElements == null || postDataElements.Length == 0) return string.Empty;

            var dataElement = postDataElements[0];

            switch (dataElement.ElementType)
            {
                case CefPostDataElementType.Empty:
                    break;
                case CefPostDataElementType.File:
                    break;
                case CefPostDataElementType.Bytes:
                    return Encoding.UTF8.GetString(dataElement.GetBytes());
            }

            return string.Empty;
        }


        protected void ProcessRequest(string path, CefRequest request, CefCallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {
                    try
                    {
                        var response = new ChromelyResponse();
                        if (string.IsNullOrEmpty(path))
                        {
                            response.ReadyState = (int)ReadyState.ResponseIsReady;
                            response.Status = (int)HttpStatusCode.NotFound;
                            response.StatusText = "Url not found";

                            _chromelyResponse = response;
                        }
                        else
                        {
                            var parameters = request.Url.GetParameters();
                            var postData = GetPostData(request);

                            var jsonRequest = _serializerUtil.ObjectToJson(request);

                            var chromelyRequest = new ChromelyRequest(request.Identifier.ToString(), request.Method,
                                path, parameters, postData, jsonRequest);
                            _chromelyResponse = _requestTaskRunner.Run(chromelyRequest);
                            var jsonData = _serializerUtil.EnsureResponseDataIsJson(_chromelyResponse.Data);
                            _responseBytes = Encoding.UTF8.GetBytes(jsonData);
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Instance.Log.LogError(exception, exception.Message);

                        _chromelyResponse =
                            new ChromelyResponse
                            {
                                Status = (int)HttpStatusCode.BadRequest,
                                Data = "An error occured."
                            };
                    }
                    finally
                    {
                        callback.Continue();
                    }
                }
            });
        }

        protected void ProcessRequestAsync(string path, CefRequest request, CefCallback callback)
        {
            Task.Run(async () =>
            {
                using (callback)
                {
                    try
                    {
                        var response = new ChromelyResponse();
                        if (string.IsNullOrEmpty(path))
                        {
                            response.ReadyState = (int)ReadyState.ResponseIsReady;
                            response.Status = (int)HttpStatusCode.NotFound;
                            response.StatusText = "Url not found";

                            _chromelyResponse = response;
                        }
                        else
                        {
                            var parameters = request.Url.GetParameters();
                            var postData = GetPostData(request);

                            var jsonRequest = _serializerUtil.ObjectToJson(request);

                            var chromelyRequest = new ChromelyRequest(request.Identifier.ToString(), request.Method,
                                path, parameters, postData, jsonRequest);
                            _chromelyResponse = await _requestTaskRunner.RunAsync(chromelyRequest);
                            var jsonData = _serializerUtil.EnsureResponseDataIsJson(_chromelyResponse.Data);

                            _responseBytes = Encoding.UTF8.GetBytes(jsonData);
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Instance.Log.LogError(exception, exception.Message);

                        _chromelyResponse =
                            new ChromelyResponse
                            {
                                Status = (int)HttpStatusCode.BadRequest,
                                Data = "An error occured."
                            };
                    }
                    finally
                    {
                        callback.Continue();
                    }
                }
            });
        }

        #endregion
    }
}