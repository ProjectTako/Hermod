﻿/*
 * Copyright (c) 2010-2015, Achim 'ahzf' Friedland <achim@graph-database.org>
 * This file is part of Vanaheimr Hermod <http://www.github.com/Vanaheimr/Hermod>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.HTTP
{

    /// <summary>
    /// A HTTP client extension methods.
    /// </summary>
    public static class HTTPClientExtensions
    {

        #region RFC 2616 - HTTP/1.1

        #region DELETE(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP DELETE request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder DELETE(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.DELETE, URI);
        }

        #endregion

        #region GET(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP GET request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder GET(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.GET, URI);
        }

        #endregion

        #region CREATE(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP CREATE request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder CREATE(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.CREATE, URI);
        }

        #endregion

        #region HEAD(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP HEAD request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder HEAD(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.HEAD, URI);
        }

        #endregion

        #region OPTIONS(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP OPTIONS request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder OPTIONS(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.OPTIONS, URI);
        }

        #endregion

        #region POST(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP POST request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder POST(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.POST, URI);
        }

        #endregion

        #region PUT(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP PUT request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder PUT(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.PUT, URI);
        }

        #endregion

        #region TRACE(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP TRACE request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder TRACE(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.TRACE, URI);
        }

        #endregion

        #endregion

        #region Additional methods

        #region PATCH(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP PATCH request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder PATCH(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.PATCH, URI);
        }

        #endregion

        #region TRAVERSE(this HTTPClient, URI = "/")

        /// <summary>
        /// Create a new HTTP TRAVERSE request.
        /// </summary>
        /// <param name="HTTPClient">A HTTP client.</param>
        /// <param name="URI">An URL path.</param>
        /// <returns>A HTTP request object.</returns>
        public static HTTPRequestBuilder TRAVERSE(this HTTPClient HTTPClient, String URI = "/")
        {
            return HTTPClient.CreateRequest(HTTPMethod.TRAVERSE, URI);
        }

        #endregion

        #endregion

    }

}
