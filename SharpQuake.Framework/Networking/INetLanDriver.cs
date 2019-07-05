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
