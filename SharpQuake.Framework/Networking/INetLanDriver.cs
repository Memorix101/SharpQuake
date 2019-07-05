/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // struct net_landriver_t
    public interface INetLanDriver
    {
        String Name
        {
            get;
        }

        Boolean IsInitialised
        {
            get;
        }

        Socket ControlSocket
        {
            get;
        }

        Boolean Initialise( );

        void Dispose( );

        void Listen( Boolean state );

        Socket OpenSocket( Int32 port );

        Int32 CloseSocket( Socket socket );

        Int32 Connect( Socket socket, EndPoint addr );

        Socket CheckNewConnections( );

        Int32 Read( Socket socket, Byte[] buf, Int32 len, ref EndPoint ep );

        Int32 Write( Socket socket, Byte[] buf, Int32 len, EndPoint ep );

        Int32 Broadcast( Socket socket, Byte[] buf, Int32 len );

        String GetNameFromAddr( EndPoint addr );

        EndPoint GetAddrFromName( String name );

        Int32 AddrCompare( EndPoint addr1, EndPoint addr2 );

        Int32 GetSocketPort( EndPoint addr );

        Int32 SetSocketPort( EndPoint addr, Int32 port );
    } //net_landriver_t;
}
