/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace SharpQuake
{
    class NetVcr : INetDriver
    {
        VcrRecord _Next;
        bool _IsInitialized;

        #region INetDriver Members

        public string Name
        {
            get { return "VCR"; }
        }

        public bool IsInitialized
        {
            get { return _IsInitialized; }
        }

        public void Init()
        {
            _Next = Sys.ReadStructure<VcrRecord>(Host.VcrReader.BaseStream);
            _IsInitialized = true;
        }

        public void Listen(bool state)
        {
            // nothing to do
        }

        public void SearchForHosts(bool xmit)
        {
            // nothing to do
        }

        public qsocket_t Connect(string host)
        {
            return null;
        }

        public qsocket_t CheckNewConnections()
        {
            if (Host.Time != _Next.time || _Next.op != VcrOp.VCR_OP_CONNECT)
                Sys.Error("VCR missmatch");

            if (_Next.session == 0)
            {
                ReadNext();
                return null;
            }

            qsocket_t sock = Net.NewSocket();
            sock.driverdata = _Next.session;

            byte[] buf = new byte[Net.NET_NAMELEN];
            Host.VcrReader.Read(buf, 0, buf.Length);
            sock.address = Encoding.ASCII.GetString(buf);

            ReadNext();

            return sock;
        }

        public int GetMessage(qsocket_t sock)
        {
            if (Host.Time != _Next.time || _Next.op != VcrOp.VCR_OP_GETMESSAGE || _Next.session != SocketToSession(sock))
                Sys.Error("VCR missmatch");

            int ret = Host.VcrReader.ReadInt32();
            if (ret != 1)
            {
                ReadNext();
                return ret;
            }

            int length = Host.VcrReader.ReadInt32();
            Net.Message.FillFrom(Host.VcrReader.BaseStream, length);

            ReadNext();

            return 1;
        }

        /// <summary>
        /// VCR_ReadNext
        /// </summary>
        private void ReadNext()
        {
            try
            {
                _Next = Sys.ReadStructure<VcrRecord>(Host.VcrReader.BaseStream);
            }
            catch (IOException)
            {
                _Next = new VcrRecord();
                _Next.op = 255;
                Sys.Error("=== END OF PLAYBACK===\n");
            }
            if (_Next.op < 1 || _Next.op > VcrOp.VCR_MAX_MESSAGE)
                Sys.Error("VCR_ReadNext: bad op");
        }

        public int SendMessage(qsocket_t sock, MsgWriter data)
        {
            if (Host.Time != _Next.time || _Next.op != VcrOp.VCR_OP_SENDMESSAGE || _Next.session != SocketToSession(sock))
                Sys.Error("VCR missmatch");

            int ret = Host.VcrReader.ReadInt32();

            ReadNext();

            return ret;
        }

        public int SendUnreliableMessage(qsocket_t sock, MsgWriter data)
        {
            throw new NotImplementedException();
        }

        public bool CanSendMessage(qsocket_t sock)
        {
            if (Host.Time != _Next.time || _Next.op != VcrOp. VCR_OP_CANSENDMESSAGE || _Next.session != SocketToSession(sock))
                Sys.Error("VCR missmatch");

            int ret = Host.VcrReader.ReadInt32();

            ReadNext();

            return ret != 0;

        }

        public bool CanSendUnreliableMessage(qsocket_t sock)
        {
            return true;
        }

        public void Close(qsocket_t sock)
        {
            // nothing to do
        }

        public void Shutdown()
        {
            // nothing to do
        }

        #endregion

        public long SocketToSession(qsocket_t sock)
        {
            return (long)sock.driverdata;
        }
    }

    static class VcrOp
    {
        public const int VCR_OP_CONNECT = 1;
        public const int VCR_OP_GETMESSAGE = 2;
        public const int VCR_OP_SENDMESSAGE = 3;
        public const int VCR_OP_CANSENDMESSAGE = 4;
        
        public const int VCR_MAX_MESSAGE = 4;
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    class VcrRecord
    {
        public double time;
        public int op;
        public long session;

        public static int SizeInBytes = Marshal.SizeOf(typeof(VcrRecord));
    }

}
