﻿/*
 * Copyright (c) 2010-2014, Achim 'ahzf' Friedland <achim@graphdefined.org>
 * This file is part of Hermod <http://www.github.com/Vanaheimr/Hermod>
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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using eu.Vanaheimr.Illias.Commons;
using eu.Vanaheimr.Hermod.Sockets.TCP;
using eu.Vanaheimr.Styx.Arrows;
using System.Threading;
using eu.Vanaheimr.Hermod.Services.TCP;

#endregion

namespace eu.Vanaheimr.Hermod.HTTP
{

    /// <summary>
    /// A HTTP server.
    /// </summary>
    public class HTTPServer : ACustomTCPServer,
                              IHTTPServer
    {

        #region Data

        internal const    String            __DefaultServerName  = "Hermod HTTP Server v0.9";
        private readonly  URIMapping        _URIMapping;

        private readonly  HTTPProcessor     _HTTPProcessor;

        #endregion

        #region Properties

        #region DefaultServerName

        private readonly String _DefaultServerName;

        /// <summary>
        /// The default HTTP servername, used whenever
        /// no HTTP Host-header had been given.
        /// </summary>
        public String DefaultServerName
        {
            get
            {
                return _DefaultServerName;
            }
        }

        #endregion

        #region HTTPSecurity

        private readonly HTTPSecurity _HTTPSecurity;

        /// <summary>
        /// An associated HTTP security object.
        /// </summary>
        public HTTPSecurity HTTPSecurity
        {
            get
            {
                return _HTTPSecurity;
            }
        }

        #endregion


        #region CallingAssemblies

        private List<Assembly> _CallingAssemblies;

        /// <summary>
        /// The list of calling assemblies.
        /// </summary>
        public List<Assembly> CallingAssemblies
        {

            get
            {
                return _CallingAssemblies;
            }

            set
            {

                if (value == null)
                    return;

                this._CallingAssemblies = value;

                // Add Hermod to the list of assemblies
                this._CallingAssemblies.Add(Assembly.GetExecutingAssembly());

                _CallingAssemblies.
                    SelectMany(_Assembly => _Assembly.GetManifestResourceNames().
                                                      Select(_Resource => new {
                                                          Assembly  = _Assembly,
                                                          Ressource = _Resource
                                                      })
                              ).ForEach(v => this._AllResources.Add(v.Ressource, v.Assembly));

            }

        }

        #endregion

        #region AllResources

        private readonly Dictionary<String, Assembly> _AllResources;

        /// <summary>
        /// All discovered embedded resources.
        /// </summary>
        public Dictionary<String, Assembly> AllResources
        {
            get
            {
                return _AllResources;
            }
        }

        #endregion

        #endregion

        #region Events

        public event BoomerangSenderHandler<String, DateTime, HTTPRequest, HTTPResponse> OnNotification;


        /// <summary>
        /// An event called whenever a request came in.
        /// </summary>
        public event RequestLogHandler  RequestLog;

        /// <summary>
        /// An event called whenever a request could successfully be processed.
        /// </summary>
        public event AccessLogHandler   AccessLog;

        /// <summary>
        /// An event called whenever a request resulted in an error.
        /// </summary>
        public event ErrorLogHandler    ErrorLog;

        #endregion

        #region Constructor(s)

        #region HTTPServer(Port, ...)

        /// <summary>
        /// Initialize the HTTP server using IPAddress.Any and the given parameters.
        /// </summary>
        /// <param name="Port">The listening port</param>
        /// <param name="DefaultServername">The default HTTP servername, used whenever no HTTP Host-header had been given.</param>
        /// <param name="ServerThreadName">The optional name of the TCP server thread.</param>
        /// <param name="ServerThreadPriority">The optional priority of the TCP server thread.</param>
        /// <param name="ServerThreadIsBackground">Whether the TCP server thread is a background thread or not.</param>
        /// <param name="ConnectionIdBuilder"></param>
        /// <param name="ConnectionThreadsNameCreator">An optional delegate to set the name of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsPriority">The optional priority of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsAreBackground">Whether the TCP conncection threads are background threads or not.</param>
        /// <param name="ConnectionTimeoutSeconds">The TCP client timeout for all incoming client connections in seconds.</param>
        /// <param name="Autostart">Start the TCP server thread immediately.</param>
        public HTTPServer(IPPort                       Port,
                          String                       DefaultServername               = __DefaultServerName,
                          Char[]                       SplitCharacters                 = null,
                          String                       ServerThreadName                = null,
                          ThreadPriority               ServerThreadPriority            = ThreadPriority.AboveNormal,
                          Boolean                      ServerThreadIsBackground        = true,
                          Func<IPSocket, String>       ConnectionIdBuilder             = null,
                          Func<TCPConnection, String>  ConnectionThreadsNameCreator    = null,
                          ThreadPriority               ConnectionThreadsPriority       = ThreadPriority.AboveNormal,
                          Boolean                      ConnectionThreadsAreBackground  = true,
                          UInt64                       ConnectionTimeoutSeconds        = 30,
                          Boolean                      Autostart                       = false)

            : this(IPv4Address.Any,
                   Port,
                   DefaultServername,
                   SplitCharacters,
                   ServerThreadName,
                   ServerThreadPriority,
                   ServerThreadIsBackground,
                   ConnectionIdBuilder,
                   ConnectionThreadsNameCreator,
                   ConnectionThreadsPriority,
                   ConnectionThreadsAreBackground,
                   ConnectionTimeoutSeconds,
                   Autostart)

        { }

        #endregion

        #region HTTPServer(IIPAddress, Port, ...)

        /// <summary>
        /// Initialize the HTTP server using the given parameters.
        /// </summary>
        /// <param name="IIPAddress">The listening IP address(es)</param>
        /// <param name="Port">The listening port</param>
        /// <param name="DefaultServername">The default HTTP servername, used whenever no HTTP Host-header had been given.</param>
        /// <param name="Mapper">A delegate to transform the incoming TCP connection data into custom data structures.</param>
        /// <param name="ServerThreadName">The optional name of the TCP server thread.</param>
        /// <param name="ServerThreadPriority">The optional priority of the TCP server thread.</param>
        /// <param name="ServerThreadIsBackground">Whether the TCP server thread is a background thread or not.</param>
        /// <param name="ConnectionIdBuilder"></param>
        /// <param name="ConnectionThreadsNameCreator">An optional delegate to set the name of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsPriority">The optional priority of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsAreBackground">Whether the TCP conncection threads are background threads or not.</param>
        /// <param name="ConnectionTimeoutSeconds">The TCP client timeout for all incoming client connections in seconds.</param>
        /// <param name="Autostart">Start the TCP server thread immediately.</param>
        public HTTPServer(IIPAddress                   IIPAddress,
                          IPPort                       Port,
                          String                       DefaultServername               = __DefaultServerName,
                          Char[]                       SplitCharacters                 = null,
                          String                       ServerThreadName                = null,
                          ThreadPriority               ServerThreadPriority            = ThreadPriority.AboveNormal,
                          Boolean                      ServerThreadIsBackground        = true,
                          Func<IPSocket, String>       ConnectionIdBuilder             = null,
                          Func<TCPConnection, String>  ConnectionThreadsNameCreator    = null,
                          ThreadPriority               ConnectionThreadsPriority       = ThreadPriority.AboveNormal,
                          Boolean                      ConnectionThreadsAreBackground  = true,
                          UInt64                       ConnectionTimeoutSeconds        = 30,
                          Boolean                      Autostart                       = false)

            : base(IIPAddress,
                   Port,
                   ServerThreadName,
                   ServerThreadPriority,
                   ServerThreadIsBackground,
                   ConnectionIdBuilder,
                   ConnectionThreadsNameCreator,
                   ConnectionThreadsPriority,
                   ConnectionThreadsAreBackground,
                   ConnectionTimeoutSeconds)

        {


            this._DefaultServerName  = DefaultServerName;
            this._URIMapping         = new URIMapping();
            this._AllResources       = new Dictionary<String, Assembly>();

            _HTTPProcessor           = new HTTPProcessor();
            _TCPServer.SendTo(_HTTPProcessor);
            _TCPServer.OnNewConnection    += SendNewConnection;
            _TCPServer.OnConnectionClosed += SendConnectionClosed;

            _HTTPProcessor.OnNotification += ProcessBoomerang;
            _HTTPProcessor.RequestLog     += (HTTPProcessor, ServerTimestamp, Request)                                 => LogRequest(ServerTimestamp, Request);
            _HTTPProcessor.ErrorLog       += (HTTPProcessor, ServerTimestamp, Request, Response, Error, LastException) => LogError  (ServerTimestamp, Request, Response, Error, LastException);

            //_TCPCSVCommandProcessor = new TCPCSVCommandProcessor();
            //_TCPCSVProcessor.ConnectTo(_TCPCSVCommandProcessor);

            if (Autostart)
                Start();

        }

        #endregion

        #region HTTPServer(IPSocket, ...)

        /// <summary>
        /// Initialize the HTTP server using IPAddress.Any and the given parameters.
        /// </summary>
        /// <param name="IPSocket">The IP socket to listen.</param>
        /// <param name="DefaultServername">The default HTTP servername, used whenever no HTTP Host-header had been given.</param>
        /// <param name="ServerThreadName">The optional name of the TCP server thread.</param>
        /// <param name="ServerThreadPriority">The optional priority of the TCP server thread.</param>
        /// <param name="ServerThreadIsBackground">Whether the TCP server thread is a background thread or not.</param>
        /// <param name="ConnectionIdBuilder"></param>
        /// <param name="ConnectionThreadsNameCreator">An optional delegate to set the name of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsPriority">The optional priority of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsAreBackground">Whether the TCP conncection threads are background threads or not.</param>
        /// <param name="ConnectionTimeoutSeconds">The TCP client timeout for all incoming client connections in seconds.</param>
        /// <param name="Autostart">Start the TCP server thread immediately.</param>
        public HTTPServer(IPSocket                     IPSocket,
                          String                       DefaultServername               = __DefaultServerName,
                          Char[]                       SplitCharacters                 = null,
                          String                       ServerThreadName                = null,
                          ThreadPriority               ServerThreadPriority            = ThreadPriority.AboveNormal,
                          Boolean                      ServerThreadIsBackground        = true,
                          Func<IPSocket, String>       ConnectionIdBuilder             = null,
                          Func<TCPConnection, String>  ConnectionThreadsNameCreator    = null,
                          ThreadPriority               ConnectionThreadsPriority       = ThreadPriority.AboveNormal,
                          Boolean                      ConnectionThreadsAreBackground  = true,
                          UInt64                       ConnectionTimeoutSeconds        = 30,
                          Boolean                      Autostart                       = false)

            : this(IPSocket.IPAddress,
                   IPSocket.Port,
                   DefaultServername,
                   SplitCharacters,
                   ServerThreadName,
                   ServerThreadPriority,
                   ServerThreadIsBackground,
                   ConnectionIdBuilder,
                   ConnectionThreadsNameCreator,
                   ConnectionThreadsPriority,
                   ConnectionThreadsAreBackground,
                   ConnectionTimeoutSeconds,
                   Autostart)

        { }

        #endregion

        #endregion


        #region Add Method Callbacks

        #region AddMethodCallback(HTTPDelegate, Hostname, URITemplate, HTTPMethod, HTTPContentType = null, HostAuthentication = false, URIAuthentication = false, HTTPMethodAuthentication = false, ContentTypeAuthentication = false)

        /// <summary>
        /// Add a method callback for the given URI template.
        /// </summary>
        /// <param name="Hostname">The HTTP hostname.</param>
        /// <param name="HTTPMethod">The HTTP method.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// <param name="HTTPContentType">The HTTP content type.</param>
        /// <param name="HostAuthentication">Whether this method needs explicit host authentication or not.</param>
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        /// <param name="HTTPMethodAuthentication">Whether this method needs explicit HTTP method authentication or not.</param>
        /// <param name="ContentTypeAuthentication">Whether this method needs explicit HTTP content type authentication or not.</param>
        /// <param name="HTTPDelegate">The method to call.</param>
        public void AddMethodCallback(String              Hostname                    = "*",
                                      HTTPMethod          HTTPMethod                  = null,
                                      String              URITemplate                 = "/",
                                      HTTPContentType     HTTPContentType             = null,
                                      HTTPAuthentication  HostAuthentication          = null,
                                      Boolean             URIAuthentication           = false,
                                      Boolean             HTTPMethodAuthentication    = false,
                                      Boolean             ContentTypeAuthentication   = false,
                                      HTTPDelegate        HTTPDelegate                = null)

        {

            _URIMapping.AddHandler(HTTPDelegate,
                                   Hostname,
                                   URITemplate,
                                   (HTTPMethod != null) ? HTTPMethod : HTTPMethod.GET,
                                   HTTPContentType,
                                   HostAuthentication,
                                   URIAuthentication,
                                   HTTPMethodAuthentication,
                                   ContentTypeAuthentication);

        }

        #endregion

        #region AddMethodCallback(MethodHandler, Hostname, URITemplate, HTTPMethod, HTTPContentType = null, HostAuthentication = false, URIAuthentication = false, HTTPMethodAuthentication = false, ContentTypeAuthentication = false)

        /// <summary>
        /// Add a method callback for the given URI template.
        /// </summary>
        /// <param name="MethodHandler">The method to call.</param>
        /// <param name="Hostname">The HTTP hostname.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// <param name="HTTPMethod">The HTTP method.</param>
        /// <param name="HTTPContentType">The HTTP content type.</param>
        /// <param name="HostAuthentication">Whether this method needs explicit host authentication or not.</param>
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        /// <param name="HTTPMethodAuthentication">Whether this method needs explicit HTTP method authentication or not.</param>
        /// <param name="ContentTypeAuthentication">Whether this method needs explicit HTTP content type authentication or not.</param>
        public void AddMethodCallback(MethodInfo          MethodHandler,
                                      String              Hostname,
                                      String              URITemplate,
                                      HTTPMethod          HTTPMethod,
                                      HTTPContentType     HTTPContentType             = null,
                                      HTTPAuthentication  HostAuthentication          = null,
                                      Boolean             URIAuthentication           = false,
                                      Boolean             HTTPMethodAuthentication    = false,
                                      Boolean             ContentTypeAuthentication   = false)

        {

            _URIMapping.AddHandler(MethodHandler,
                                   Hostname,
                                   URITemplate,
                                   HTTPMethod,
                                   HTTPContentType,
                                   HostAuthentication,
                                   URIAuthentication,
                                   HTTPMethodAuthentication,
                                   ContentTypeAuthentication);

        }

        #endregion

        #region AddMethodCallback(MethodHandler, Hostnames, URITemplate, HTTPMethod, HTTPContentType = null, HostAuthentication = false, URIAuthentication = false, HTTPMethodAuthentication = false, ContentTypeAuthentication = false)

        /// <summary>
        /// Add a method callback for the given URI template.
        /// </summary>
        /// <param name="MethodHandler">The method to call.</param>
        /// <param name="Host">The HTTP host.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// <param name="HTTPMethod">The HTTP method.</param>
        /// <param name="HTTPContentType">The HTTP content type.</param>
        /// <param name="HostAuthentication">Whether this method needs explicit host authentication or not.</param>
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        /// <param name="HTTPMethodAuthentication">Whether this method needs explicit HTTP method authentication or not.</param>
        /// <param name="ContentTypeAuthentication">Whether this method needs explicit HTTP content type authentication or not.</param>
        public void AddMethodCallback(MethodInfo           MethodHandler,
                                      IEnumerable<String>  Hostnames,
                                      String               URITemplate,
                                      HTTPMethod           HTTPMethod,
                                      HTTPContentType      HTTPContentType             = null,
                                      HTTPAuthentication   HostAuthentication          = null,
                                      Boolean              URIAuthentication           = false,
                                      Boolean              HTTPMethodAuthentication    = false,
                                      Boolean              ContentTypeAuthentication   = false)

        {

            _URIMapping.AddHandler(MethodHandler,
                                   Hostnames,
                                   URITemplate,
                                   HTTPMethod,
                                   HTTPContentType,
                                   HostAuthentication,
                                   URIAuthentication,
                                   HTTPMethodAuthentication,
                                   ContentTypeAuthentication);

        }

        #endregion

        #endregion

        #region Get Method Callbacks

        #region GetHandler(Host, URL, HTTPMethod = null, HTTPContentType = null)

        /// <summary>
        /// Return the best matching method handler for the given parameters.
        /// </summary>
        public Tuple<HTTPDelegate, IEnumerable<Object>> GetHandler(String           Host              = "*",
                                                                   String           URL               = "/",
                                                                   HTTPMethod       HTTPMethod        = null,
                                                                   HTTPContentType  HTTPContentType   = null)
        {

            return _URIMapping.GetHandler(Host,
                                          URL,
                                          HTTPMethod,
                                          HTTPContentType);

        }

        #endregion

        #endregion


        #region Add HTTP Server Sent Events

        #region AddEventSource(EventIdentification, MaxNumberOfCachedEvents = 100, RetryIntervall = null)

        /// <summary>
        /// Add a HTTP Sever Sent Events source.
        /// </summary>
        /// <param name="EventIdentification">The unique identification of the event source.</param>
        /// <param name="MaxNumberOfCachedEvents">Maximum number of cached events.</param>
        /// <param name="RetryIntervall">The retry intervall.</param>
        public HTTPEventSource AddEventSource(String     EventIdentification,
                                              UInt32     MaxNumberOfCachedEvents  = 100,
                                              TimeSpan?  RetryIntervall           = null)
        {

            return _URIMapping.AddEventSource(EventIdentification,
                                              MaxNumberOfCachedEvents,
                                              RetryIntervall);

        }

        #endregion

        #region AddEventSource(MethodInfo, Host, URITemplate, HTTPMethod, EventIdentification, MaxNumberOfCachedEvents = 100, RetryIntervall = null, IsSharedEventSource = false, HostAuthentication = false, URIAuthentication = false)

        /// <summary>
        /// Add a method call back for the given URI template and
        /// add a HTTP Sever Sent Events source.
        /// </summary>
        /// <param name="MethodInfo">The method to call.</param>
        /// <param name="Host">The HTTP host.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// <param name="HTTPMethod">The HTTP method.</param>
        /// <param name="EventIdentification">The unique identification of the event source.</param>
        /// <param name="MaxNumberOfCachedEvents">Maximum number of cached events.</param>
        /// <param name="RetryIntervall">The retry intervall.</param>
        /// <param name="IsSharedEventSource">Whether this event source will be shared.</param>
        /// <param name="HostAuthentication">Whether this method needs explicit host authentication or not.</param>
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        public HTTPEventSource AddEventSource(MethodInfo          MethodInfo,
                                              String              Host,
                                              String              URITemplate,
                                              HTTPMethod          HTTPMethod,
                                              String              EventIdentification,
                                              UInt32              MaxNumberOfCachedEvents  = 100,
                                              TimeSpan?           RetryIntervall           = null,
                                              Boolean             IsSharedEventSource      = false,
                                              HTTPAuthentication  HostAuthentication       = null,
                                              Boolean             URIAuthentication        = false)

        {

            return _URIMapping.AddEventSource(MethodInfo,
                                              Host,
                                              URITemplate,
                                              HTTPMethod,
                                              EventIdentification,
                                              MaxNumberOfCachedEvents,
                                              RetryIntervall,
                                              IsSharedEventSource,
                                              HostAuthentication,
                                              URIAuthentication);

        }

        #endregion

        #region AddEventSourceHandler(MethodInfo, Host, URITemplate, HostAuthentication = false, URLAuthentication = false)

        /// <summary>
        /// Add an HTTP event source method handler for the given URI template.
        /// </summary>
        /// <param name="MethodInfo">The method to call.</param>
        /// <param name="Host">The HTTP host.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// <param name="HTTPMethod">The HTTP method.</param>
        /// <param name="HostAuthentication">Whether this method needs explicit host authentication or not.</param>
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        public void AddEventSourceHandler(MethodInfo          MethodInfo,
                                          String              Host,
                                          String              URITemplate,
                                          HTTPMethod          HTTPMethod,
                                          HTTPAuthentication  HostAuthentication  = null,
                                          Boolean             URIAuthentication   = false)
        {

            _URIMapping.AddEventSourceHandler(MethodInfo,
                                              Host,
                                              URITemplate,
                                              HTTPMethod,
                                              HostAuthentication,
                                              URIAuthentication);

        }

        #endregion

        #endregion

        #region Get HTTP Server Sent Events

        #region GetEventSource(EventSourceIdentification)

        /// <summary>
        /// Return the event source identified by the given event source identification.
        /// </summary>
        /// <param name="EventSourceIdentification">A string to identify an event source.</param>
        public HTTPEventSource GetEventSource(String EventSourceIdentification)
        {
            return _URIMapping.GetEventSource(EventSourceIdentification);
        }

        #endregion

        #region TryGetEventSource(EventSourceIdentification, EventSource)

        /// <summary>
        /// Return the event source identified by the given event source identification.
        /// </summary>
        /// <param name="EventSourceIdentification">A string to identify an event source.</param>
        /// <param name="EventSource">The event source.</param>
        public Boolean TryGetEventSource(String EventSourceIdentification, out HTTPEventSource EventSource)
        {
            return _URIMapping.TryGetEventSource(EventSourceIdentification, out EventSource);
        }

        #endregion

        #region GetEventSources(EventSourceSelector = null)

        /// <summary>
        /// An enumeration of all event sources.
        /// </summary>
        public IEnumerable<HTTPEventSource> GetEventSources(Func<HTTPEventSource, Boolean> EventSourceSelector = null)
        {
            return GetEventSources(EventSourceSelector);
        }

        #endregion

        #endregion


        #region GetErrorHandler(Host, URL, HTTPMethod = null, HTTPContentType = null, HTTPStatusCode = null)

        /// <summary>
        /// Return the best matching error handler for the given parameters.
        /// </summary>
        public Tuple<MethodInfo, IEnumerable<Object>> GetErrorHandler(String           Host,
                                                                      String           URL, 
                                                                      HTTPMethod       HTTPMethod       = null,
                                                                      HTTPContentType  HTTPContentType  = null,
                                                                      HTTPStatusCode   HTTPStatusCode   = null)

        {

            return _URIMapping.GetErrorHandler(Host,
                                               URL,
                                               HTTPMethod,
                                               HTTPContentType,
                                               HTTPStatusCode);

        }

        #endregion



        #region Logging

        #region LogRequest(ServerTimestamp, Request)

        /// <summary>
        /// Log an incoming request.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the incoming request.</param>
        /// <param name="Request">The incoming request.</param>
        public void LogRequest(DateTime     ServerTimestamp,
                               HTTPRequest  Request)
        {

            var RequestLogLocal = RequestLog;

            if (RequestLogLocal != null)
                RequestLogLocal(this, ServerTimestamp, Request);

        }

        #endregion

        #region LogAccess(ServerTimestamp, Request, Response)

        /// <summary>
        /// Log an successful request processing.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the incoming request.</param>
        /// <param name="Request">The incoming request.</param>
        /// <param name="Response">The outgoing response.</param>
        public void LogAccess(DateTime      ServerTimestamp,
                              HTTPRequest   Request,
                              HTTPResponse  Response)
        {

            var AccessLogLocal = AccessLog;

            if (AccessLogLocal != null)
                AccessLogLocal(this, ServerTimestamp, Request, Response);

        }

        #endregion

        #region LogError(ServerTimestamp, Request, Response, Error = null, LastException = null)

        /// <summary>
        /// Log an error during request processing.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the incoming request.</param>
        /// <param name="Request">The incoming request.</param>
        /// <param name="HTTPResponse">The outgoing response.</param>
        /// <param name="Error">The occured error.</param>
        /// <param name="LastException">The last occured exception.</param>
        public void LogError(DateTime      ServerTimestamp,
                             HTTPRequest   Request,
                             HTTPResponse  Response,
                             String        Error          = null,
                             Exception     LastException  = null)
        {

            var ErrorLogLocal = ErrorLog;

            if (ErrorLogLocal != null)
                ErrorLogLocal(this, ServerTimestamp, Request, Response, Error, LastException);

        }

        #endregion

        #endregion



        #region ProcessBoomerang(ConnectionId, Timestamp, HTTPRequest)

        private HTTPResponse ProcessBoomerang(String       ConnectionId,
                                              DateTime     Timestamp,
                                              HTTPRequest  HTTPRequest)
        {

            #region Check if any HTTP delegate matches...

            var _ParsedCallbackWithParameters = GetHandler(HTTPRequest.Host,
                                                           HTTPRequest.UrlPath,
                                                           HTTPRequest.HTTPMethod,
                                                           HTTPRequest.BestMatchingAcceptType);

            if (_ParsedCallbackWithParameters != null)
                return _ParsedCallbackWithParameters.Item1(HTTPRequest, new String[0]);

            #endregion

            #region ...or call default delegate!

            var OnNotificationLocal = OnNotification;
            if (OnNotificationLocal != null)
                return OnNotificationLocal(ConnectionId,
                                           Timestamp,
                                           HTTPRequest);

            #endregion

            #region ...or fail!

            return new HTTPResponseBuilder() {
                HTTPStatusCode  = HTTPStatusCode.InternalServerError,
                ContentType     = HTTPContentType.TEXT_UTF8,
                Content         = "Error 500 - Internal Server Error!".ToUTF8Bytes(),
                Server          = "Hermod",
                Connection      = "close"
            };

            #endregion

        }

        #endregion

    }

}
