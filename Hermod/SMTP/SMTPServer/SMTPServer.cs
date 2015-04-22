﻿/*
 * Copyright (c) 2010-2015, Achim 'ahzf' Friedland <achim@graphdefined.org>
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
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.Sockets;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;
using org.GraphDefined.Vanaheimr.Hermod.Services.Mail;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Services.SMTP
{

    public delegate void NewSMTPConnectionHandler(SMTPServer SMTPServer, DateTime Timestamp, IPSocket RemoteSocket, TCPConnection TCPConnection);


    /// <summary>
    /// A SMTP server.
    /// </summary>
    public class SMTPServer : ATCPServers,
                              IArrowSender<SMTPServer, EMailEnvelop>
    {

        #region Data

        internal const    String             __DefaultServerName  = "Vanaheimr Hermod SMTP Service v0.1";

        private readonly  SMTPConnection     _SMTPProcessor;

        #endregion

        #region Properties

        #region DefaultServerName

        private String _DefaultServerName;

        /// <summary>
        /// The default SMTP servername.
        /// </summary>
        public String DefaultServerName
        {

            get
            {
                return _DefaultServerName;
            }

            set
            {
                if (value.IsNotNullOrEmpty())
                    _DefaultServerName = value;
            }

        }

        #endregion

        #region UseTLS

        private readonly Boolean _UseTLS;

        public Boolean UseTLS
        {
            get
            {
                return _UseTLS;
            }
        }

        #endregion

        #endregion

        #region Events

        public event NewSMTPConnectionHandler                            OnNewConnection;

        public event NotificationEventHandler<SMTPServer, EMailEnvelop>  OnNotification;

        /// <summary>
        /// An event called whenever a request resulted in an error.
        /// </summary>
        public event ErrorLogHandler                                     ErrorLog;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Initialize the SMTP server using the given parameters.
        /// </summary>
        /// <param name="IPPort"></param>
        /// <param name="DefaultServerName">The default SMTP servername.</param>
        /// <param name="X509Certificate">Use this X509 certificate for TLS.</param>
        /// <param name="UseTLS">Use TLS (implicit true, if a X509 certificate was given!).</param>
        /// <param name="CallingAssemblies">Calling assemblies.</param>
        /// <param name="ServerThreadName">The optional name of the TCP server thread.</param>
        /// <param name="ServerThreadPriority">The optional priority of the TCP server thread.</param>
        /// <param name="ServerThreadIsBackground">Whether the TCP server thread is a background thread or not.</param>
        /// <param name="ConnectionIdBuilder">An optional delegate to build a connection identification based on IP socket information.</param>
        /// <param name="ConnectionThreadsNameBuilder">An optional delegate to set the name of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsPriorityBuilder">An optional delegate to set the priority of the TCP connection threads.</param>
        /// <param name="ConnectionThreadsAreBackground">Whether the TCP connection threads are background threads or not (default: yes).</param>
        /// <param name="ConnectionTimeout">The TCP client timeout for all incoming client connections in seconds (default: 30 sec).</param>
        /// <param name="MaxClientConnections">The maximum number of concurrent TCP client connections (default: 4096).</param>
        /// <param name="Autostart">Start the SMTP server thread immediately (default: no).</param>
        public SMTPServer(IPPort                            IPPort                            = null,
                          String                            DefaultServerName                 = __DefaultServerName,
                          X509Certificate2                  X509Certificate                   = null,
                          Boolean?                          UseTLS                            = true,
                          IEnumerable<Assembly>             CallingAssemblies                 = null,
                          String                            ServerThreadName                  = null,
                          ThreadPriority                    ServerThreadPriority              = ThreadPriority.AboveNormal,
                          Boolean                           ServerThreadIsBackground          = true,
                          ConnectionIdBuilder               ConnectionIdBuilder               = null,
                          ConnectionThreadsNameBuilder      ConnectionThreadsNameBuilder      = null,
                          ConnectionThreadsPriorityBuilder  ConnectionThreadsPriorityBuilder  = null,
                          Boolean                           ConnectionThreadsAreBackground    = true,
                          TimeSpan?                         ConnectionTimeout                 = null,
                          UInt32                            MaxClientConnections              = TCPServer.__DefaultMaxClientConnections,
                          Boolean                           Autostart                         = false)

            : base(DefaultServerName,
                   X509Certificate,
                   ServerThreadName,
                   ServerThreadPriority,
                   ServerThreadIsBackground,
                   ConnectionIdBuilder,
                   ConnectionThreadsNameBuilder,
                   ConnectionThreadsPriorityBuilder,
                   ConnectionThreadsAreBackground,
                   ConnectionTimeout,
                   MaxClientConnections,
                   false)

        {

            this._DefaultServerName         = DefaultServerName;
            this._UseTLS                    = UseTLS.HasValue
                                                 ? UseTLS.Value
                                                 : (X509Certificate != null
                                                       ? true
                                                       : false);

            _SMTPProcessor                  = new SMTPConnection(DefaultServerName, this._UseTLS);
            _SMTPProcessor.OnNotification  += ProcessNotification;
            _SMTPProcessor.ErrorLog        += (HTTPProcessor, ServerTimestamp, SMTPCommand, Request, Response, Error, LastException) =>
                                                  LogError (ServerTimestamp, SMTPCommand, Request, Response, Error, LastException);

            if (IPPort != null)
                this.AttachTCPPort(IPPort);

            if (Autostart)
                Start();

        }

        #endregion


        // Manage the underlying TCP sockets...

        #region AttachTCPPort(Port)

        public SMTPServer AttachTCPPort(IPPort Port)
        {

            this.AttachTCPPorts(Port);

            return this;

        }

        #endregion

        #region AttachTCPPorts(params Ports)

        public SMTPServer AttachTCPPorts(params IPPort[] Ports)
        {

            base.AttachTCPPorts(_TCPServer => {
                _TCPServer.OnNewConnection += ProcessTCPServerOnNewConnection;
                _TCPServer.SendTo(_SMTPProcessor);
            }, Ports);

            return this;

        }

        #endregion

        #region AttachTCPSocket(Socket)

        public SMTPServer AttachTCPSocket(IPSocket Socket)
        {

            this.AttachTCPSockets(Socket);

            return this;

        }

        #endregion

        #region AttachTCPSockets(params Sockets)

        public SMTPServer AttachTCPSockets(params IPSocket[] Sockets)
        {

            base.AttachTCPSockets(_TCPServer => {
                _TCPServer.OnNewConnection += ProcessTCPServerOnNewConnection;
                _TCPServer.SendTo(_SMTPProcessor);
            }, Sockets);

            return this;

        }

        #endregion


        #region DetachTCPPort(Port)

        public SMTPServer DetachTCPPort(IPPort Port)
        {

            DetachTCPPorts(Port);

            return this;

        }

        #endregion

        #region DetachTCPPorts(params Sockets)

        public SMTPServer DetachTCPPorts(params IPPort[] Ports)
        {

            base.DetachTCPPorts(_TCPServer => {
                                    _TCPServer.OnNotification      -= _SMTPProcessor.ProcessArrow;
                                    _TCPServer.OnExceptionOccured  -= _SMTPProcessor.ProcessExceptionOccured;
                                    _TCPServer.OnCompleted         -= _SMTPProcessor.ProcessCompleted;
                                },
                                Ports);

            return this;

        }

        #endregion


        // Events

        private void ProcessTCPServerOnNewConnection(TCPServer      TCPServer,
                                                     DateTime       Timestamp,
                                                     IPSocket       RemoteSocket,
                                                     String         ConnectionId,
                                                     TCPConnection  TCPConnection)
        {

            var OnNewConnectionLocal = OnNewConnection;
            if (OnNewConnectionLocal != null)
                OnNewConnectionLocal(this, Timestamp, RemoteSocket, TCPConnection);

        }

        private void ProcessNotification(EMailEnvelop MailEnvelop)
        {

            var OnNotificationLocal = OnNotification;
            if (OnNotificationLocal != null)
                OnNotificationLocal(this, MailEnvelop);

        }




        // SMTP Logging...

        #region LogError(ServerTimestamp, EMail, Response, Error = null, LastException = null)

        /// <summary>
        /// Log an error during request processing.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the incoming request.</param>
        /// <param name="EMail">The incoming request.</param>
        /// <param name="HTTPResponse">The outgoing response.</param>
        /// <param name="Error">The occured error.</param>
        /// <param name="LastException">The last occured exception.</param>
        public void LogError(DateTime              ServerTimestamp,
                             String                SMTPCommand,
                             EMail                 EMail,
                             SMTPExtendedResponse  Response,
                             String                Error          = null,
                             Exception             LastException  = null)
        {

            var ErrorLogLocal = ErrorLog;

            if (ErrorLogLocal != null)
                ErrorLogLocal(this, ServerTimestamp, SMTPCommand, EMail, Response, Error, LastException);

        }

        #endregion


    }

}
