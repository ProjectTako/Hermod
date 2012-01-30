﻿/*
 * Copyright (c) 2010-2011, Achim 'ahzf' Friedland <code@ahzf.de>
 * This file is part of Hermod
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

namespace de.ahzf.Hermod.HTTP
{

    /// <summary>
    /// Mapps a HTTP event request onto a .NET method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class HTTPEventMappingAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// The internal identification of the HTTP event.
        /// </summary>
        public String  EventIdentification      { get; private set; }

        /// <summary>
        /// The URI template of this HTTP event mapping.
        /// </summary>
        public String  UriTemplate              { get; private set; }

        /// <summary>
        /// Maximum number of cached events.
        /// Zero means infinite.
        /// </summary>
        public UInt32  MaxNumberOfCachedEvents  { get; private set; }

        /// <summary>
        /// The event source may be accessed via multiple URI templates.
        /// </summary>
        public Boolean IsSharedEventSource      { get; private set; }

        #endregion

        #region Constructor(s)

        #region HTTPEventMappingAttribute(EventIdentification, UriTemplate, MaxNumberOfCachedEvents = 0, IsSharedEventSource = false)

        /// <summary>
        /// Creates a new HTTP event mapping.
        /// </summary>
        /// <param name="EventIdentification">The internal identification of the HTTP event.</param>
        /// <param name="UriTemplate">The URI template of this HTTP event mapping.</param>
        /// <param name="MaxNumberOfCachedEvents">Maximum number of cached events (0 means infinite).</param>
        /// <param name="IsSharedEventSource">The event source may be accessed via multiple URI templates.</param>
        public HTTPEventMappingAttribute(String EventIdentification, String UriTemplate, UInt32 MaxNumberOfCachedEvents = 0, Boolean IsSharedEventSource = false)
        {
            this.EventIdentification     = EventIdentification;
            this.UriTemplate             = UriTemplate;
            this.MaxNumberOfCachedEvents = MaxNumberOfCachedEvents;
            this.IsSharedEventSource     = IsSharedEventSource;
        }

        #endregion

        #endregion

    }

}