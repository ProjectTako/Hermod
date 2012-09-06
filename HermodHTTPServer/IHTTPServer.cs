﻿/*
 * Copyright (c) 2010-2012, Achim 'ahzf' Friedland <achim@graph-database.org>
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

using de.ahzf.Hermod.Datastructures;

#endregion

namespace de.ahzf.Hermod.HTTP
{

    /// <summary>
    /// The HTTP server interface.
    /// </summary>
    public interface IHTTPServer : ITCPServer
    {

        /// <summary>
        /// The HTTP server name.
        /// </summary>
        String ServerName { get; set; }

        /// <summary>
        /// The default HTTP server name.
        /// </summary>
        String DefaultServerName { get; }


        URLMapping URLMapping { get; }

    }

}