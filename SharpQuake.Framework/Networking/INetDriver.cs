/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // struct net_driver_t
    public interface INetDriver
    {
        String Name
        {
            get;
        }

        Boolean IsInitialised
        {
            get;
        }

        void Initialise( Object host );

        void Listen( Boolean state );

        void SearchForHosts( Boolean xmit );

        qsocket_t Connect( String host );

        qsocket_t CheckNewConnections( );

        Int32 GetMessage( qsocket_t sock );

        Int32 SendMessage( qsocket_t sock, MessageWriter data );

        Int32 SendUnreliableMessage( qsocket_t sock, MessageWriter data );

        Boolean CanSendMessage( qsocket_t sock );

        Boolean CanSendUnreliableMessage( qsocket_t sock );

        void Close( qsocket_t sock );

        void Shutdown( );
    } //net_driver_t;
}
