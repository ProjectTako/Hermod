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
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Services.DNS
{

    /// <summary>
    /// A cache for DNS entries.
    /// </summary>
    public class DNSCache
    {

        #region Data

        private readonly Dictionary<String, DNSCacheEntry>  _DNSCache;
        private readonly Timer                              _CleanUpTimer;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DNS cache.
        /// </summary>
        /// <param name="CleanUpEvery">How often to remove outdated entries from DNS cache.</param>
        public DNSCache(TimeSpan? CleanUpEvery = null)
        {

            _DNSCache      = new Dictionary<String, DNSCacheEntry>();

            _CleanUpTimer  = new Timer(RemoveExpiredCacheEntries,
                                       null,
                                       CleanUpEvery.HasValue ? CleanUpEvery.Value : TimeSpan.FromMinutes(1), // delay one round!
                                       CleanUpEvery.HasValue ? CleanUpEvery.Value : TimeSpan.FromMinutes(1));

            _DNSCache.Add("localhost",
                          new DNSCacheEntry(RefreshTime:  DateTime.Now,
                                            EndOfLife:    DateTime.Now + TimeSpan.FromDays(3650),
                                            DNSInfo:      new DNSInfo(Origin:               new IPSocket(IPv4Address.Parse("127.0.0.1"), IPPort.Parse(53)),
                                                                      QueryId:              0,
                                                                      IsAuthorativeAnswer:  true,
                                                                      IsTruncated:          false,
                                                                      RecursionDesired:     false,
                                                                      RecursionAvailable:   false,
                                                                      ResponseCode:         DNSResponseCodes.NoError,
                                                                      Answers:              new ADNSResourceRecord[] {
                                                                                                new    A("localhost", DNSQueryClasses.IN, TimeSpan.FromDays(3650), IPv4Address.Parse("127.0.0.1")),
                                                                                                new AAAA("localhost", DNSQueryClasses.IN, TimeSpan.FromDays(3650), IPv6Address.Parse("::1"))
                                                                                            },
                                                                      Authorities:          new ADNSResourceRecord[0],
                                                                      AdditionalRecords:    new ADNSResourceRecord[0])
                                                         ));

            _DNSCache.Add("loopback",
                          new DNSCacheEntry(RefreshTime:  DateTime.Now,
                                            EndOfLife:    DateTime.Now + TimeSpan.FromDays(3650),
                                            DNSInfo:      new DNSInfo(Origin:               new IPSocket(IPv4Address.Parse("127.0.0.1"), IPPort.Parse(53)),
                                                                      QueryId:              0,
                                                                      IsAuthorativeAnswer:  true,
                                                                      IsTruncated:          false,
                                                                      RecursionDesired:     false,
                                                                      RecursionAvailable:   false,
                                                                      ResponseCode:         DNSResponseCodes.NoError,
                                                                      Answers:              new ADNSResourceRecord[] {
                                                                                                new    A("loopback", DNSQueryClasses.IN, TimeSpan.FromDays(3650), IPv4Address.Parse("127.0.0.1")),
                                                                                                new AAAA("loopback", DNSQueryClasses.IN, TimeSpan.FromDays(3650), IPv6Address.Parse("::1"))
                                                                                            },
                                                                      Authorities:          new ADNSResourceRecord[0],
                                                                      AdditionalRecords:    new ADNSResourceRecord[0])
                                                         ));

        }

        #endregion


        #region Add(Domainname, DNSInformation)

        /// <summary>
        /// Add the given DNS information to the DNS cache.
        /// </summary>
        /// <param name="Domainname">The domain name.</param>
        /// <param name="DNSInformation">The DNS information to add.</param>
        public DNSCache Add(String   Domainname,
                            DNSInfo  DNSInformation)
        {

            lock (_DNSCache)
            {

                DNSCacheEntry CacheEntry = null;

                if (!_DNSCache.TryGetValue(Domainname, out CacheEntry))
                    _DNSCache.Add(Domainname, new DNSCacheEntry(
                                                         DateTime.Now + TimeSpan.FromSeconds(DNSInformation.Answers.First().TimeToLive.TotalSeconds / 2),
                                                         DateTime.Now + DNSInformation.Answers.First().TimeToLive,
                                                         DNSInformation));

                else
                {
                    // ToDo: Merge of DNS responses!
                    _DNSCache[Domainname] = new DNSCacheEntry(
                                                       DateTime.Now + TimeSpan.FromSeconds(DNSInformation.Answers.First().TimeToLive.TotalSeconds / 2),
                                                       DateTime.Now + DNSInformation.Answers.First().TimeToLive,
                                                       DNSInformation);
                }

                return this;

            }

        }

        #endregion

        #region Add(Domainname, Origin, ResourceRecord)

        /// <summary>
        /// Add the given DNS resource record to the DNS cache.
        /// </summary>
        /// <param name="Domainname">The domain name.</param>
        /// <param name="Origin">The origin of the DNS resource record.</param>
        /// <param name="ResourceRecord">The DNS resource record to add.</param>
        public DNSCache Add(String              Domainname,
                            IPSocket            Origin,
                            ADNSResourceRecord  ResourceRecord)
        {

            lock (_DNSCache)
            {

                DNSCacheEntry CacheEntry = null;

                Debug.WriteLine("[" + DateTime.Now + "] Adding '" + Domainname + "' to the DNS cache!");

                if (!_DNSCache.TryGetValue(Domainname, out CacheEntry))
                    _DNSCache.Add(Domainname, new DNSCacheEntry(
                                                         DateTime.Now + TimeSpan.FromSeconds(ResourceRecord.TimeToLive.TotalSeconds / 2),
                                                         DateTime.Now + ResourceRecord.TimeToLive,
                                                         new DNSInfo(Origin:               Origin,
                                                                     QueryId:              new Random().Next(),
                                                                     IsAuthorativeAnswer:  false,
                                                                     IsTruncated:          false,
                                                                     RecursionDesired:     false,
                                                                     RecursionAvailable:   false,
                                                                     ResponseCode:         DNSResponseCodes.NoError,
                                                                     Answers:              new ADNSResourceRecord[1] { ResourceRecord },
                                                                     Authorities:          new ADNSResourceRecord[0],
                                                                     AdditionalRecords:    new ADNSResourceRecord[0])));

                else
                {

                    // ToDo: Merge of DNS responses!
                    Debug.WriteLine("[" + DateTime.Now + "] Resource record for '" + Domainname + "' already exists within the DNS cache!");

                }

                return this;

            }

        }

        #endregion

        #region GetDNSInfo(DomainName)

        /// <summary>
        /// Get the cached DNS information from the DNS cache.
        /// </summary>
        /// <param name="DomainName">The domain name.</param>
        public DNSInfo GetDNSInfo(String DomainName)
        {

            lock (_DNSCache)
            {

                DNSCacheEntry CacheEntry = null;

                if (_DNSCache.TryGetValue(DomainName, out CacheEntry))
                    return CacheEntry.DNSInfo;

                return null;

            }

        }

        #endregion


        #region (private, Timer) RemoveExpiredCacheEntries(State)

        private void RemoveExpiredCacheEntries(Object State)
        {

            if (Monitor.TryEnter(_DNSCache))
            {

                try
                {

                    var Now             = DateTime.Now;

                    var ExpiredEntries  = _DNSCache.
                                              Where(entry => entry.Value.EndOfLife < Now).
                                              ToArray();

                    ExpiredEntries.ForEach(entry => _DNSCache.Remove(entry.Key));

#if DEBUG
                    ExpiredEntries.ForEach(entry => Debug.WriteLine("[" + Now + "] Removed '" + entry.Key + "' from the DNS cache!"));
#endif

                }

                catch (Exception e)
                {
                    Debug.WriteLine("[" + DateTime.Now + "] An exception occured during DNS cache cleanup: " + e.Message);
                }

                finally
                {
                    Monitor.Exit(_DNSCache);
                }

            }

        }

        #endregion


        #region ToString()

        /// <summary>
        /// Get a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return _DNSCache.Count + " cached DNS entries";
        }

        #endregion

    }

}
