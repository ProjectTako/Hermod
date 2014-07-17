﻿///*
// * Copyright (c) 2011-2013 Achim 'ahzf' Friedland <achim@ahzf.de>
// * This file is part of Hermod <http://www.github.com/Vanaheimr/Hermod>
// * 
// * This program is free software; you can redistribute it and/or modify
// * it under the terms of the GNU General Public License as published by
// * the Free Software Foundation; either version 3 of the License, or
// * (at your option) any later version.
// * 
// * You may obtain a copy of the License at
// *   http://www.gnu.org/licenses/gpl.html
// * 
// * This program is distributed in the hope that it will be useful, but
// * WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// * General Public License for more details.
// */

//#region Usings

//using System;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Collections.Generic;

//using eu.Vanaheimr.Illias.Commons;
//using System.Text.RegularExpressions;
//using System.Threading;
//using Newtonsoft.Json.Linq;
//using System.Xml.Linq;
//using eu.Vanaheimr.Hermod.Services.DNS;

//#endregion

//namespace eu.Vanaheimr.Hermod.HTTP
//{

//    /// <summary>
//    /// An abstract HTTP service implementation.
//    /// </summary>
//    public abstract class AHTTPService
//    {

//        #region Data

//        /// <summary>
//        /// The 'Callback' parameter within the query string
//        /// </summary>
//        protected ThreadLocal<String> Callback;

//        /// <summary>
//        /// The 'Skip' parameter within the query string
//        /// </summary>
//        protected ThreadLocal<UInt64> Skip;

//        /// <summary>
//        /// The 'Take' parameter within the query string
//        /// </summary>
//        protected ThreadLocal<UInt64> Take;

//        protected readonly DNSClient _DNSClient;

//        #endregion

//        #region Properties

//        /// <summary>
//        /// The HTTP connection.
//        /// </summary>
//        public IHTTPConnection                  IHTTPConnection        { get; set; }

//        /// <summary>
//        /// All embedded ressources.
//        /// </summary>
//        public IDictionary<String, Assembly>    AllResources            { get; set; }

//        /// <summary>
//        /// An enumeration of all associated content types.
//        /// </summary>
//        public IEnumerable<HTTPContentType>     HTTPContentTypes       { get; private set; }

//        /// <summary>
//        /// The file system root mapped to HTTP root.
//        /// </summary>
//        public String                           HTTPRoot               { get; set; }

//        #endregion

//        #region Constructor(s)

//        #region AHTTPService()

//        /// <summary>
//        /// Creates a new AHTTPService.
//        /// </summary>
//        public AHTTPService()
//        {
//            _DNSClient  = new DNSClient();
//        }

//        #endregion

//        #region AHTTPService(HTTPContentType)

//        /// <summary>
//        /// Creates a new abstract HTTPService.
//        /// </summary>
//        /// <param name="HTTPContentType">A content type.</param>
//        public AHTTPService(HTTPContentType HTTPContentType)
//            : this()
//        {

//            #region Initial checks

//            if (HTTPContentType == null)
//                throw new ArgumentNullException("The given HTTPContentType must not be null!");

//            #endregion

//            this.HTTPContentTypes = new HTTPContentType[1] { HTTPContentType };

//        }

//        #endregion

//        #region AHTTPService(HTTPContentTypes)

//        /// <summary>
//        /// Creates a new abstract HTTPService.
//        /// </summary>
//        /// <param name="HTTPContentTypes">A content type.</param>
//        public AHTTPService(IEnumerable<HTTPContentType> HTTPContentTypes)
//            : this()
//        {

//            #region Initial checks

//            if (HTTPContentTypes == null)
//                throw new ArgumentNullException("The given HTTPContentTypes must not be null!");

//            #endregion

//            this.HTTPContentTypes = HTTPContentTypes;

//        }

//        #endregion

//        #region AHTTPService(IHTTPConnection, HTTPContentType)

//        /// <summary>
//        /// Creates a new abstract HTTPService.
//        /// </summary>
//        /// <param name="IHTTPConnection">The http connection for this request.</param>
//        /// <param name="HTTPContentType">A http content type.</param>
//        public AHTTPService(IHTTPConnection IHTTPConnection, HTTPContentType HTTPContentType)
//            : this()
//        {

//            #region Initial checks

//            if (IHTTPConnection == null)
//                throw new ArgumentNullException("The given IHTTPConnection must not be null!");

//            if (HTTPContentType == null)
//                throw new ArgumentNullException("The given HTTPContentType must not be null!");

//            #endregion

//            this.IHTTPConnection  = IHTTPConnection;
//            this.HTTPContentTypes = new HTTPContentType[1] { HTTPContentType };

//        }

//        #endregion

//        #region AHTTPService(IHTTPConnection, HTTPContentTypes)

//        /// <summary>
//        /// Creates a new abstract HTTPService.
//        /// </summary>
//        /// <param name="IHTTPConnection">The http connection for this request.</param>
//        /// <param name="HTTPContentTypes">An enumeration of http content types.</param>
//        public AHTTPService(IHTTPConnection IHTTPConnection, IEnumerable<HTTPContentType> HTTPContentTypes)
//            : this()
//        {

//            #region Initial checks

//            if (IHTTPConnection == null)
//                throw new ArgumentNullException("The given IHTTPConnection must not be null!");

//            if (HTTPContentTypes.IsNullOrEmpty())
//                throw new ArgumentNullException("The given HTTPContentTypes must not be null or empty!");

//            #endregion

//            this.IHTTPConnection  = IHTTPConnection;
//            this.HTTPContentTypes = HTTPContentTypes;

//        }

//        #endregion

//        #endregion


//        #region (protected) _TryGetLastQueryParameter_UInt64(ParameterName, OnSuccess = null, OnNotFound = null)

//        /// <summary>
//        /// Try to return a single optional HTTP query parameter as UInt64.
//        /// </summary>
//        /// <param name="ParameterName">The name of the parameter.</param>
//        /// <param name="OnSuccess">A delegate called whenever a valid value was found.</param>
//        /// <param name="OnNotFound">A delegate called whenever no value was found.</param>
//        protected HTTPResult<UInt64> _TryGetLastQueryParameter_UInt64(String ParameterName, Action<UInt64> OnSuccess = null, Func<UInt64> OnNotFound = null)
//        {

//            List<String> StringValues = null;

//            if (IHTTPConnection.RequestHeader.QueryString != null)
//                if (IHTTPConnection.RequestHeader.QueryString.TryGetValue(ParameterName, out StringValues))
//                {

//                    var Value = 0UL;

//                    foreach (var StringValue in StringValues)
//                        if (!UInt64.TryParse(StringValue, out Value))
//                            return new HTTPResult<UInt64>(IHTTPConnection.RequestHeader,
//                                                          HTTPStatusCode.BadRequest,
//                                                          "The given optional query parameter '" + ParameterName + "=" + StringValue + "' is invalid!");

//                    // Return the last parsed value!
//                    if (OnSuccess != null)
//                        OnSuccess(Value);

//                    return new HTTPResult<UInt64>(Value);

//                }

//            if (OnNotFound != null)
//                return new HTTPResult<UInt64>(OnNotFound());

//            return new HTTPResult<UInt64>(default(UInt64));

//        }

//        #endregion


//        #region (protected) _TryGetParameter_String(Name, out HTTPResult)

//        /// <summary>
//        /// Try to return a single optional HTTP parameter as UInt64.
//        /// </summary>
//        /// <param name="Name">The name of the parameter.</param>
//        /// <param name="HTTPResult">The HTTPResult.</param>
//        /// <returns>True if the parameter exist; False otherwise.</returns>
//        protected Boolean _TryGetParameter_String(String Name, out String Result)
//        {

//            List<String> _StringValues = null;

//            if (IHTTPConnection.RequestHeader.QueryString != null)
//                if (IHTTPConnection.RequestHeader.QueryString.TryGetValue(Name.ToLower(), out _StringValues))
//                    if (_StringValues.Any())
//                        if (_StringValues[0] != null && _StringValues[0] != "")
//                        {
//                            Result = _StringValues[0];
//                            return true;
//                        }

//            Result = default(String);
//            return false;

//        }

//        #endregion

//        #region (protected) _GetRequestBodyAsUTF8String(HTTPContentType)

//        protected String _GetRequestBodyAsUTF8String()
//        {

//            if (IHTTPConnection.RequestBody == null || IHTTPConnection.RequestBody.Length == 0)
//                return String.Empty;

//            var RequestBodyString = IHTTPConnection.RequestBody.ToUTF8String();

//            if (RequestBodyString.IsNullOrEmpty())
//                return String.Empty;

//            return RequestBodyString;

//        }

//        #endregion

//        #region (protected) _GetRequestBodyAsUTF8String(HTTPContentType)

//        protected HTTPResult<String> _GetRequestBodyAsUTF8String(HTTPContentType HTTPContentType)
//        {

//            if (IHTTPConnection.RequestHeader.ContentType != HTTPContentType)
//                return new HTTPResult<String>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);

//            if (IHTTPConnection.RequestBody == null || IHTTPConnection.RequestBody.Length == 0)
//                return new HTTPResult<String>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);

//            var RequestBodyString = IHTTPConnection.RequestBody.ToUTF8String();

//            if (RequestBodyString.IsNullOrEmpty())
//                return new HTTPResult<String>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);

//            return new HTTPResult<String>(Result: RequestBodyString);

//        }

//        #endregion

//        #region (protected) _ParseRequestBody<T>(ExpectedContentType, OnSuccess, OnError, FailWhenNoContent = true)

//        protected HTTPResult<T> _ParseRequestBody<T>(HTTPContentType  ExpectedContentType,
//                                                     Func<String, T>  OnSuccess,
//                                                     Func<T>          OnError,
//                                                     Boolean          FailWhenNoContent = true)

//        {

//            try
//            {

//                if (IHTTPConnection.RequestHeader.ContentType != ExpectedContentType)
//                    return new HTTPResult<T>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest, "HTTP request body content type must be '" + ExpectedContentType.MediaType + "'!");

//                #region Check if the request body is null or empty

//                var RequestBodyUTF8 = String.Empty;

//                if (IHTTPConnection.RequestBody != null)
//                    RequestBodyUTF8 = IHTTPConnection.RequestBody.ToUTF8String().Trim();

//                if (RequestBodyUTF8.IsNullOrEmpty())
//                {

//                    if (FailWhenNoContent)
//                        return new HTTPResult<T>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest, "HTTP request body missing!");

//                    return new HTTPResult<T>(OnError());

//                }

//                #endregion

//                return new HTTPResult<T>(OnSuccess(RequestBodyUTF8));

//            }
//            catch (Exception)
//            {
//                return new HTTPResult<T>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);
//            }

//        }

//        #endregion

//        #region (protected) _ParseJSONRequestBody()

//        protected HTTPResult<JObject> _ParseJSONRequestBody()
//        {

//            var RequestBodyString = _GetRequestBodyAsUTF8String(HTTPContentType.JSON_UTF8);
//            if (RequestBodyString.HasErrors)
//                return new HTTPResult<JObject>(RequestBodyString.Error);

//            JObject RequestBodyJSON;

//            try
//            {
//                RequestBodyJSON = JObject.Parse(RequestBodyString.Data);
//            }
//            catch (Exception)
//            {
//                return new HTTPResult<JObject>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);
//            }

//            return new HTTPResult<JObject>(RequestBodyJSON);

//        }

//        #endregion

//        #region (protected) _ParseXMLRequestBody()

//        protected HTTPResult<XDocument> _ParseXMLRequestBody()
//        {

//            var RequestBodyString = _GetRequestBodyAsUTF8String(HTTPContentType.XMLTEXT_UTF8);
//            if (RequestBodyString.HasErrors)
//                return new HTTPResult<XDocument>(RequestBodyString.Error);

//            XDocument RequestBodyXML;

//            try
//            {
//                RequestBodyXML = XDocument.Parse(RequestBodyString.Data);
//            }
//            catch (Exception)
//            {
//                return new HTTPResult<XDocument>(IHTTPConnection.RequestHeader, HTTPStatusCode.BadRequest);
//            }

//            return new HTTPResult<XDocument>(RequestBodyXML);

//        }

//        #endregion


//        #region (protected) _ParseCallbackParameter()

//        /// <summary>
//        /// Parse and check the parameter CALLBACK.
//        /// </summary>
//        protected void _ParseCallbackParameter()
//        {

//            String _Callback;

//            if (_TryGetParameter_String(Tokens.CALLBACK, out _Callback))
//                Callback.Value = new Regex("[^a-zA-Z0-9_]").Replace(_Callback, "");

//        }

//        #endregion

//        #region (protected) _ParseSkipParameter(Default = 0)

//        /// <summary>
//        /// Parse and check the parameter SKIP.
//        /// </summary>
//        protected HTTPResult<UInt64> _ParseSkipParameter(UInt64 Default = 0)
//        {

//            return _TryGetLastQueryParameter_UInt64(Tokens.SKIP,
//                                                   result => Skip.Value = result,
//                                                   ()     => Skip.Value = Default);

//        }

//        #endregion

//        #region (protected) _ParseTakeParameter(Default)

//        /// <summary>
//        /// Parse and check the parameter TAKE.
//        /// </summary>
//        protected HTTPResult<UInt64> _ParseTakeParameter(UInt64 Default)
//        {

//            return _TryGetLastQueryParameter_UInt64(Tokens.TAKE,
//                                                   result => Take.Value = result,
//                                                   ()     => Take.Value = Default);

//        }

//        #endregion



//        #region (protected) _GetResourceStream(Resource)

//        protected Stream _GetResourceStream(String Resource)
//        {

//            Assembly _Assembly;

//            if (this.AllResources.TryGetValue(Resource, out _Assembly))
//                return _Assembly.GetManifestResourceStream(Resource);

//            return new MemoryStream();

//        }

//        #endregion

//        #region (protected) _TryGetResourceStream(Resource, out ResourceStream)

//        protected Boolean _TryGetResourceStream(String Resource, out Stream ResourceStream)
//        {

//            Assembly _Assembly;

//            if (this.AllResources.TryGetValue(Resource, out _Assembly))
//            {
//                ResourceStream = _Assembly.GetManifestResourceStream(Resource);
//                return true;
//            }

//            ResourceStream = null;
//            return false;

//        }

//        #endregion

//        #region (protected) _GetResources(ResourceName)

//        /// <summary>
//        /// Returns internal resources embedded within the assembly.
//        /// </summary>
//        /// <param name="ResourceName">The path and name of the resource.</param>
//        protected HTTPResponse _GetResources(String ResourceName)
//        {
//            return _GetResources(HTTPRoot, ResourceName);
//        }

//        #endregion

//        #region (protected) _GetResources(ResourcePath, ResourceName)

//        /// <summary>
//        /// Returns internal resources embedded within the assembly.
//        /// </summary>
//        /// <param name="ResourceName">The path and name of the resource.</param>
//        protected HTTPResponse _GetResources(String ResourcePath, String ResourceName)
//        {

//            if (ResourcePath == null)
//                return new HTTPResponseBuilder() {
//                            HTTPStatusCode  = HTTPStatusCode.NotFound,
//                            Connection      = "close"
//                        };

//            #region Return internal assembly resources...

//            ResourcePath = ResourcePath.Replace('/', '.');
//            ResourcePath = (ResourcePath.EndsWith(".")) ? ResourcePath : ResourcePath + ".";
//            ResourceName = ResourceName.Replace('/', '.');
//            ResourceName = (ResourceName.StartsWith(".")) ? ResourceName.Substring(1) : ResourceName;

//            Stream _ResourceContent;

//            if (_TryGetResourceStream(ResourcePath + ResourceName, out _ResourceContent))
//            {

//                HTTPContentType _ResponseContentType = null;

//                // Get the apropriate content type based on the suffix of the requested resource
//                switch (ResourceName.Remove(0, ResourceName.LastIndexOf(".") + 1))
//                {
//                    case "htm":  _ResponseContentType = HTTPContentType.HTML_UTF8;       break;
//                    case "html": _ResponseContentType = HTTPContentType.HTML_UTF8;       break;
//                    case "css":  _ResponseContentType = HTTPContentType.CSS_UTF8;        break;
//                    case "gif":  _ResponseContentType = HTTPContentType.GIF;             break;
//                    case "jpg":  _ResponseContentType = HTTPContentType.JPEG;            break;
//                    case "jpeg": _ResponseContentType = HTTPContentType.JPEG;            break;
//                    case "png":  _ResponseContentType = HTTPContentType.PNG;             break;
//                    case "ico":  _ResponseContentType = HTTPContentType.ICO;             break;
//                    case "swf":  _ResponseContentType = HTTPContentType.SWF;             break;
//                    case "js":   _ResponseContentType = HTTPContentType.JAVASCRIPT_UTF8; break;
//                    case "txt":  _ResponseContentType = HTTPContentType.TEXT_UTF8;       break;
//                    default:     _ResponseContentType = HTTPContentType.OCTETSTREAM;     break;
//                }

//                return
//                    new HTTPResponseBuilder() {
//                            HTTPStatusCode = HTTPStatusCode.OK,
//                            ContentType    = _ResponseContentType,
//                            ContentLength  = (UInt64) _ResourceContent.Length,
//                            CacheControl   = "max-age=600",
//                            Connection     = "close",
//                            ContentStream  = _ResourceContent
//                        };

//            }

//            #endregion

//            #region ...or send an (custom) error 404!

//            else
//            {

//                if (_TryGetResourceStream(HTTPRoot + ".errorpages.Error404.html", out _ResourceContent))
//                    return new HTTPResponseBuilder() {
//                        HTTPStatusCode  = HTTPStatusCode.NotFound,
//                        ContentType     = HTTPContentType.HTML_UTF8,
//                        CacheControl    = "no-cache",
//                        Connection      = "close",
//                        ContentStream   = _ResourceContent
//                    };

//                else
//                    return new HTTPResult<Object>(IHTTPConnection.RequestHeader, HTTPStatusCode.NotFound).Error;

//            }

//            #endregion

//        }

//        #endregion

//        #region (protected) _GetResourceStream(Path, ResourceName)

//        /// <summary>
//        /// Returns internal resources embedded within the assembly.
//        /// </summary>
//        /// <param name="ResourceName">The path and name of the resource.</param>
//        protected Stream _GetResourceStream(String ResourcePath, String ResourceName)
//        {

//            ResourcePath = ResourcePath.Replace('/', '.');
//            ResourcePath = (ResourcePath.EndsWith(".")) ? ResourcePath : ResourcePath + ".";
//            ResourceName = ResourceName.Replace('/', '.');

//            Stream _ResourceContent;

//            if (_TryGetResourceStream(ResourcePath + ResourceName, out _ResourceContent))
//                return _ResourceContent;

//            return null;

//        }

//        #endregion



//        #region (protected) _GetEventStream(HTTPEventSource, AccessControlAllowOrigin = "*", AccessControlAllowMethods = "GET,POST,MONITOR")

//        protected HTTPResponse _GetEventStream(HTTPEventSource  HTTPEventSource,
//                                               String           AccessControlAllowOrigin   = "*",
//                                               String           AccessControlAllowMethods  = "GET,POST,MONITOR")
//        {

//            if (HTTPEventSource == null)
//                return new HTTPResponseBuilder() {
//                    HTTPStatusCode = HTTPStatusCode.NotFound
//                };

//            var                 _LastEventId      = 0UL;
//            IEnumerable<String> _HTTPEvents       = null;
//            String              _ResourceContent  = Environment.NewLine;

//            if (IHTTPConnection.RequestHeader.TryGet<UInt64>("Last-Event-Id", out _LastEventId))
//                _HTTPEvents      = HTTPEventSource.
//                                      GetAllEventsGreater(_LastEventId).
//                                      Select(e => e.ToString());

//            else
//                _HTTPEvents      = HTTPEventSource.
//                                      GetAllEventsGreater(0).
//                                      Select(e => e.ToString());


//            // Transform HTTP events into an UTF8 string
//            _ResourceContent += String.Concat(_HTTPEvents.AggregateOrDefault((a, b) => a + Environment.NewLine + b, String.Empty),
//                                              Environment.NewLine);

//            _ResourceContent += String.Concat("retry: ",
//                                              HTTPEventSource.RetryIntervall.TotalMilliseconds.ToString("F0"),
//                                              Environment.NewLine,
//                                              Environment.NewLine);


//            return new HTTPResponseBuilder() {
//                HTTPStatusCode             = HTTPStatusCode.OK,
//                ContentType                = HTTPContentType.EVENTSTREAM,
//                AccessControlAllowOrigin   = AccessControlAllowOrigin,
//                AccessControlAllowMethods  = AccessControlAllowMethods,
//                AccessControlAllowHeaders  = "ContentType",
//                CacheControl               = "no-cache",
//                Connection                 = "keep-alive",
//                Content                    = _ResourceContent.ToUTF8Bytes()
//            };

//        }

//        #endregion

//        #region (protected) _GetEventStream(EventSourceIdentification, AccessControlAllowOrigin = "*", AccessControlAllowMethods = "GET,POST,MONITOR")

//        protected HTTPResponse _GetEventStream(String EventSourceIdentification,
//                                               String AccessControlAllowOrigin   = "*",
//                                               String AccessControlAllowMethods  = "GET,POST,MONITOR")
//        {

//            HTTPEventSource HTTPEventSource;

//            if (IHTTPConnection.HTTPServer.TryGetEventSource(EventSourceIdentification, out HTTPEventSource))
//                return _GetEventStream(HTTPEventSource, AccessControlAllowOrigin, AccessControlAllowMethods);

//            else
//                return new HTTPResponseBuilder() {
//                    HTTPStatusCode = HTTPStatusCode.NotFound
//                };

//        }

//        #endregion


//    }

//}