﻿/*
 * Copyright (c) 2010-2013, Achim 'ahzf' Friedland <achim@graph-database.org>
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using eu.Vanaheimr.Hermod.Sockets.TCP;
using eu.Vanaheimr.Hermod.Datastructures;
using eu.Vanaheimr.Styx;

#endregion

namespace eu.Vanaheimr.Hermod.Services.CSV
{

    public static class SplitterArrowExtention
    {

        /// <summary>
        /// Turn a single arrow having multiple notifications into
        /// multiple arrows having a single notification each.
        /// </summary>
        /// <typeparam name="T">The type of the notifications.</typeparam>
        /// <param name="In">The arrow sender.</param>
        public static UnfoldArrow<T> Unfold<T>(this INotification<IEnumerable<T>> In)
        {
            return new UnfoldArrow<T>(In);
        }

    }

    /// <summary>
    /// Turn a single arrow having multiple notifications into
    /// multiple arrows having a single notification each.
    /// </summary>
    /// <typeparam name="T">The type of the notifications.</typeparam>
    public class UnfoldArrow<T> : IArrowReceiver<IEnumerable<T>>, INotification<T>
    {

        #region Events

        public event NotificationEventHandler<T> OnNotification;

        public event ExceptionEventHandler OnError;

        public event CompletedEventHandler OnCompleted;

        #endregion

        #region Constructor(s)

        #region SplitterArrow()

        public UnfoldArrow(INotification<IEnumerable<T>> In = null)
        {
            if (In != null)
                In.SendTo(this);
        }

        #endregion

        #endregion


        public void ProcessArrow(IEnumerable<T> Messages)
        {

            foreach (var Message in Messages)
            {
                if (OnNotification != null)
                    OnNotification(Message);
            }

        }

        public void ProcessError(dynamic Sender, Exception ExceptionMessage)
        {
            if (OnError != null)
                OnError(this, ExceptionMessage);
        }

        public void ProcessCompleted(dynamic Sender, String Message)
        {
            if (OnCompleted != null)
                OnCompleted(this, Message);
        }

    }

}