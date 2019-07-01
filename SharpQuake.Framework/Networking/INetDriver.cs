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
