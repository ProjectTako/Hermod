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
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Security;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Services;
using org.GraphDefined.Vanaheimr.Hermod.Sockets;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;
using org.GraphDefined.Vanaheimr.Hermod.Services.Mail;

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.Asn1;
using System.Diagnostics;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Services.SMTP
{

    public static class Ext3
    {

        public static void WriteSMTP(this TCPConnection TCPConn, SMTPStatusCode StatusCode, String Text)
        {

            TCPConn.WriteToResponseStream(((Int32) StatusCode) + " " + Text);
            Debug.WriteLine(">> " +       ((Int32) StatusCode) + " " + Text);

            TCPConn.Flush();

        }

        public static void WriteLineSMTP(this TCPConnection TCPConn, params SMTPResponse[] Response)
        {

            var n = (UInt64) Response.Where(line => line.Response.IsNotNullOrEmpty()).Count();

            Response.
                Where(line => line.Response.IsNotNullOrEmpty()).
                ForEachCounted((i, response) => {
                    TCPConn.WriteLineToResponseStream(((Int32) response.StatusCode) + (i < n ? "-" : " ") + response.Response);
                    Debug.WriteLine(">> " +           ((Int32) response.StatusCode) + (i < n ? "-" : " ") + response.Response);
                });

            TCPConn.Flush();

        }

        public static void WriteLineSMTP(this TCPConnection TCPConn, SMTPStatusCode StatusCode, params String[] Response)
        {

            var n = (UInt64) Response.Where(line => line.IsNotNullOrEmpty()).Count();

            Response.
                Where(line => line.IsNotNullOrEmpty()).
                ForEachCounted((i, response) => {
                    TCPConn.WriteLineToResponseStream(((Int32) StatusCode) + (i < n ? "-" : " ") + response);
                    Debug.WriteLine(">> " +           ((Int32) StatusCode) + (i < n ? "-" : " ") + response);
                });

            TCPConn.Flush();

        }

    }

    public enum SMTPServerStatus
    {
        commandmode,
        mailmode
    }


    /// <summary>
    /// Accept incoming SMTP TCP connections and
    /// decode the transmitted data as E-Mails.
    /// </summary>
    public class SMTPConnection : IArrowReceiver<TCPConnection>,
                                  IArrowSender<EMail, String>
    {

        #region Data

        private const UInt32 ReadTimeout           = 180000U;

        #endregion

        #region Properties

        #region DefaultServerName

        private readonly String _DefaultServerName;

        /// <summary>
        /// The default SMTP servername.
        /// </summary>
        public String DefaultServerName
        {
            get
            {
                return _DefaultServerName;
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

        #region TLSEnabled

        private Boolean _TLSEnabled;

        /// <summary>
        /// TLS was enabled for this SMTP connection.
        /// </summary>
        public Boolean TLSEnabled
        {
            get
            {
                return _TLSEnabled;
            }
        }

        #endregion

        #endregion

        #region Events

        public   event StartedEventHandler                                                      OnStarted;

        public   event NotificationEventHandler<EMail, String>                                  OnNotification;

        public   event CompletedEventHandler                                                    OnCompleted;

        /// <summary>
        /// An event called whenever a request resulted in an error.
        /// </summary>
        internal event InternalErrorLogHandler                                                  ErrorLog;

        public   event ExceptionOccuredEventHandler                                             OnExceptionOccured;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// This processor will accept incoming SMTP TCP connections and
        /// decode the transmitted data as SMTP requests.
        /// </summary>
        /// <param name="DefaultServername">The default SMTP servername.</param>
        /// <param name="UseTLS">Allow TLS on SMTP connections.</param>
        public SMTPConnection(String  DefaultServername  = SMTPServer.__DefaultServerName,
                              Boolean UseTLS             = true)
        {

            this._DefaultServerName  = DefaultServername;
            this._UseTLS             = UseTLS;
            this._TLSEnabled         = false;

        }

        #endregion



        #region NotifyErrors(...)

        private void NotifyErrors(TCPConnection         TCPConnection,
                                  DateTime              Timestamp,
                                  String                SMTPCommand,
                                  SMTPStatusCode        SMTPStatusCode,
                                  EMail                 EMail            = null,
                                  SMTPExtendedResponse  Response         = null,
                                  String                Error            = null,
                                  Exception             LastException    = null,
                                  Boolean               CloseConnection  = true)
        {

            var ErrorLogLocal = ErrorLog;
            if (ErrorLogLocal != null)
            {
                ErrorLogLocal(this, Timestamp, SMTPCommand, EMail, Response, Error, LastException);
            }

        }

        #endregion

        #region ProcessArrow(TCPConnection)

        public void ProcessArrow(TCPConnection TCPConnection)
        {

            #region Start

            //TCPConnection.WriteLineToResponseStream(ServiceBanner);
            TCPConnection.NoDelay = true;

            Byte Byte;
            var MemoryStream      = new MemoryStream();
            var EndOfSMTPCommand  = EOLSearch.NotYet;
            var ClientClose       = false;
            var ServerClose       = false;
            var MailClientName    = "";

            #endregion

            try
            {

                var MailFroms  = new List<String>();
                var RcptTos    = new List<String>();
                var MailText   = "";

                TCPConnection.WriteLineSMTP(SMTPStatusCode.ServiceReady,
                                            _DefaultServerName + " ESMTP Vanaheimr Hermod Mail Transport Service");

                do
                {

                    switch (TCPConnection.TryRead(out Byte, MaxInitialWaitingTimeMS: ReadTimeout))
                    {

                        // 421 4.4.2 mail.ahzf.de Error: timeout exceeded

                        #region DataAvailable

                        case TCPClientResponse.DataAvailable:

                            #region Check for end of SMTP line...

                            if (EndOfSMTPCommand == EOLSearch.NotYet)
                            {
                                // \n
                                if (Byte == 0x0a)
                                    EndOfSMTPCommand = EOLSearch.EoL_Found;
                                // \r
                                else if (Byte == 0x0d)
                                    EndOfSMTPCommand = EOLSearch.R_Read;
                            }

                            // \n after a \r
                            else if (EndOfSMTPCommand == EOLSearch.R_Read)
                            {
                                if (Byte == 0x0a)
                                    EndOfSMTPCommand = EOLSearch.EoL_Found;
                                else
                                    EndOfSMTPCommand = EOLSearch.NotYet;
                            }

                            #endregion

                            MemoryStream.WriteByte(Byte);

                            #region If end-of-line -> process data...

                            if (EndOfSMTPCommand == EOLSearch.EoL_Found)
                            {

                                if (MemoryStream.Length > 0)
                                {

                                    var RequestTimestamp = DateTime.Now;

                                    #region Check UTF8 encoding

                                    var SMTPCommand = String.Empty;

                                    try
                                    {

                                        SMTPCommand = Encoding.UTF8.GetString(MemoryStream.ToArray()).Trim();

                                        Debug.WriteLine("<< " + SMTPCommand);

                                    }
                                    catch (Exception e)
                                    {

                                        NotifyErrors(TCPConnection,
                                                     RequestTimestamp,
                                                     "",
                                                     SMTPStatusCode.SyntaxError,
                                                     Error: "Protocol Error: Invalid UTF8 encoding!");

                                    }

                                    #endregion

                                    #region Try to parse SMTP commands

                                    #region ""

                                    if (SMTPCommand == "")
                                    { }

                                    #endregion

                                    #region HELO <MailClientName>

                                    else if (SMTPCommand.ToUpper().StartsWith("HELO"))
                                    {

                                        if (SMTPCommand.Trim().Length > 5 && SMTPCommand.Trim()[4] == ' ')
                                        {

                                            MailClientName = SMTPCommand.Trim().Substring(5);

                                            // 250 mail.ahzf.de
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, DefaultServerName);

                                        }
                                        else
                                        {
                                            // 501 Syntax: HELO hostname
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.SyntaxError, "Syntax: HELO hostname");
                                        }

                                    }

                                    #endregion

                                    #region EHLO <MailClientName>

                                    else if (SMTPCommand.ToUpper().StartsWith("EHLO"))
                                    {

                                        if (SMTPCommand.Trim().Length > 5 && SMTPCommand.Trim()[4] == ' ')
                                        {

                                            MailClientName = SMTPCommand.Trim().Substring(5);

                                            // 250-mail.graphdefined.org
                                            // 250-PIPELINING
                                            // 250-SIZE 204800000
                                            // 250-VRFY
                                            // 250-ETRN
                                            // 250-STARTTLS
                                            // 250-AUTH PLAIN LOGIN CRAM-MD5 DIGEST-MD5
                                            // 250-AUTH=PLAIN LOGIN CRAM-MD5 DIGEST-MD5
                                            // 250-ENHANCEDSTATUSCODES
                                            // 250-8BITMIME
                                            // 250 DSN
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok,
                                                                        DefaultServerName,
                                                                        "VRFY",
                                                                        _UseTLS     ? "STARTTLS"         : null,
                                                                        _TLSEnabled ? "AUTH PLAIN LOGIN" : null,
                                                                        "SIZE 204800000",
                                                                        "ENHANCEDSTATUSCODES",
                                                                        "8BITMIME");

                                        }
                                        else
                                        {
                                            // 501 Syntax: EHLO hostname
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.SyntaxError, "Syntax: EHLO hostname");
                                        }

                                    }

                                    #endregion

                                    #region STARTTLS

                                    else if (SMTPCommand.ToUpper() == "STARTTLS")
                                    {

                                        if (_TLSEnabled)
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.BadCommandSequence, "5.5.1 TLS already started");

                                        else if (MailClientName.IsNullOrEmpty())
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.BadCommandSequence, "5.5.1 EHLO/HELO first");

                                        else
                                        {

                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.ServiceReady, "2.0.0 Ready to start TLS");

                                            //                                            var _TLSStream = new SslStream(TCPConnection.NetworkStream);
                                            //                                            _TLSStream.AuthenticateAsServer(TLSCert, false, SslProtocols.Tls12, false);
                                            _TLSEnabled = true;

                                        }

                                    }

                                    #endregion

                                    #region AUTH LOGIN|PLAIN|...

                                    else if (SMTPCommand.ToUpper().StartsWith("AUTH "))
                                    {

                                        if (!_TLSEnabled)
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.BadCommandSequence, "5.5.1 STARTTLS first");

                                    }

                                    #endregion

                                    #region MAIL FROM: <SenderMailAddress>

                                    else if (SMTPCommand.ToUpper().StartsWith("MAIL FROM"))
                                    {

                                        var SMTPCommandParts = SMTPCommand.Split(new Char[2] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

                                        if (SMTPCommandParts.Length >= 3)
                                        {

                                            var MailFrom = SMTPCommandParts[2];

                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, "2.1.0 " + MailFrom + " Sender ok");

                                            if (MailFrom[0] == '<' && MailFrom[MailFrom.Length - 1] == '>')
                                                MailFrom = MailFrom.Substring(1, MailFrom.Length - 3);

                                            MailFroms.Add(MailFrom);

                                        }
                                        else
                                        {
                                            // 501 Syntax: EHLO hostname
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.SyntaxError, "Syntax: MAIL FROM: <mail@domain.tld>");
                                        }

                                    }

                                    #endregion

                                    #region RCPT TO: <ReceiverMailAddress>

                                    else if (SMTPCommand.ToUpper().StartsWith("RCPT TO"))
                                    {

                                        var SMTPCommandParts = SMTPCommand.Split(new Char[2] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

                                        if (SMTPCommandParts.Length >= 3)
                                        {

                                            var RcptTo = SMTPCommandParts[2];

                                            // telnet: > telnet mx1.example.com smtp
                                            // telnet: Trying 192.0.2.2...
                                            // telnet: Connected to mx1.example.com.
                                            // telnet: Escape character is '^]'.
                                            // server: 220 mx1.example.com ESMTP server ready Tue, 20 Jan 2004 22:33:36 +0200
                                            // client: HELO client.example.com
                                            // server: 250 mx1.example.com
                                            // client: MAIL from: <sender@example.com>
                                            // server: 250 Sender <sender@example.com> Ok
                                            // client: RCPT to: <recipient@example.com>
                                            // server: 250 Recipient <recipient@example.com> Ok
                                            // client: DATA
                                            // server: 354 Ok Send data ending with <CRLF>.<CRLF>
                                            // client: From: sender@example.com
                                            // client: To: recipient@example.com
                                            // client: Subject: Test message
                                            // client: 
                                            // client: This is a test message.
                                            // client: .
                                            // server: 250 Message received: 20040120203404.CCCC18555.mx1.example.com@client.example.com
                                            // client: QUIT
                                            // server: 221 mx1.example.com ESMTP server closing connection

                                            // MAIL FROM: mail@domain.ext
                                            // 250 2.1.0 mail@domain.ext... Sender ok
                                            // 
                                            // RCPT TO: mail@otherdomain.ext
                                            // 250 2.1.0 mail@otherdomain.ext... Recipient ok

                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, "2.1.0 " + RcptTo + " Recipient ok");

                                            if (RcptTo[0] == '<' && RcptTo[RcptTo.Length - 1] == '>')
                                                RcptTo = RcptTo.Substring(1, RcptTo.Length - 3);

                                            RcptTos.Add(RcptTo);

                                        }
                                        else
                                        {
                                            // 501 Syntax: EHLO hostname
                                            TCPConnection.WriteLineSMTP(SMTPStatusCode.SyntaxError, "Syntax: RCPT TO: <mail@domain.tld>");
                                        }

                                    }

                                    #endregion

                                    #region DATA

                                    else if (SMTPCommand.ToUpper().StartsWith("DATA"))
                                    {

                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.StartMailInput, "Ok Send data ending with <CRLF>.<CRLF>");

                                        var MailTextBuilder  = new StringBuilder();
                                        var MailLine         = "";

                                        do
                                        {

                                            MailLine = TCPConnection.ReadLine();

                                            // "." == End-of-EMail...
                                            if (MailLine != null && MailLine != ".")
                                                MailTextBuilder.AppendLine(MailLine);

                                        } while (MailLine != ".");

                                        MailText = MailTextBuilder.ToString();

                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, "Message received: 20040120203404.CCCC18555.mx1.example.com@opendata.social");

                                        Debug.WriteLine(MailText);
                                        Debug.WriteLine(".");

                                        var OnNotificationLocal = OnNotification;
                                        if (OnNotificationLocal != null)
                                            OnNotificationLocal(null, MailText);

                                    }

                                    #endregion

                                    #region RSET

                                    else if (SMTPCommand.ToUpper() == "RSET")
                                    {
                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, "2.0.0 Ok");
                                        MailClientName = "";
                                        MailFroms.Clear();
                                        RcptTos.  Clear();
                                        MailText = "";
                                    }

                                    #endregion

                                    #region NOOP

                                    else if (SMTPCommand.ToUpper() == "NOOP")
                                    {
                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.Ok, "2.0.0 Ok");
                                    }

                                    #endregion

                                    #region VRFY

                                    else if (SMTPCommand.ToUpper().StartsWith("VRFY"))
                                    {
                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.CannotVerifyUserWillAttemptDelivery, "2.0.0 Send some mail. I'll try my best!");
                                        MailClientName = "";
                                        MailFroms.Clear();
                                        RcptTos.Clear();
                                        MailText = "";
                                    }

                                    #endregion

                                    #region QUIT

                                    else if (SMTPCommand.ToUpper() == "QUIT")
                                    {
                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.ServiceClosingTransmissionChannel, "2.0.0 closing connection");
                                        ClientClose = true;
                                    }

                                    #endregion

                                    #region else error...

                                    else
                                    {

                                        TCPConnection.WriteLineSMTP(SMTPStatusCode.CommandUnrecognized, "2.0.0 I don't understand how to handle '" + SMTPCommand + "'!");

                                        NotifyErrors(TCPConnection,
                                                     RequestTimestamp,
                                                     SMTPCommand.Trim(),
                                                     SMTPStatusCode.BadCommandSequence,
                                                     Error: "Invalid SMTP command!");

                                    }

                                    #endregion

                                    #endregion

                                }

                                MemoryStream.SetLength(0);
                                MemoryStream.Seek(0, SeekOrigin.Begin);
                                EndOfSMTPCommand = EOLSearch.NotYet;

                            }

                            #endregion

                            break;

                        #endregion

                        #region CanNotRead

                        case TCPClientResponse.CanNotRead:
                            ServerClose = true;
                            break;

                        #endregion

                        #region ClientClose

                        case TCPClientResponse.ClientClose:
                            ClientClose = true;
                            break;

                        #endregion

                        #region Timeout

                        case TCPClientResponse.Timeout:
                            ServerClose = true;
                            break;

                        #endregion

                    }

                } while (!ClientClose && !ServerClose);

            }

            #region Process exceptions

            catch (IOException ioe)
            {

                if      (ioe.Message.StartsWith("Unable to read data from the transport connection")) { }
                else if (ioe.Message.StartsWith("Unable to write data to the transport connection")) { }

                else
                {

                    //if (OnError != null)
                    //    OnError(this, DateTime.Now, ConnectionIdBuilder(newTCPConnection.RemoteIPAddress, newTCPConnection.RemotePort), ioe, MemoryStream);

                }

            }

            catch (Exception e)
            {

                //if (OnError != null)
                //    OnError(this, DateTime.Now, ConnectionIdBuilder(newTCPConnection.RemoteIPAddress, newTCPConnection.RemotePort), e, MemoryStream);

            }

            #endregion

            #region Close the TCP connection

            try
            {
                TCPConnection.Close((ClientClose) ? ConnectionClosedBy.Client : ConnectionClosedBy.Server);
            }
            catch (Exception)
            { }

            #endregion

        }

        #endregion

        #region ProcessExceptionOccured(Sender, Timestamp, ExceptionMessage)

        public void ProcessExceptionOccured(Object     Sender,
                                            DateTime   Timestamp,
                                            Exception  ExceptionMessage)
        {

            var OnExceptionOccuredLocal = OnExceptionOccured;
            if (OnExceptionOccuredLocal != null)
                OnExceptionOccuredLocal(Sender,
                                        Timestamp,
                                        ExceptionMessage);

        }

        #endregion

        #region ProcessCompleted(Sender, Timestamp, Message = null)

        public void ProcessCompleted(Object    Sender,
                                     DateTime  Timestamp,
                                     String    Message = null)
        {

            var OnCompletedLocal = OnCompleted;
            if (OnCompletedLocal != null)
                OnCompletedLocal(Sender,
                                 Timestamp,
                                 Message);

        }

        #endregion


    }

}
