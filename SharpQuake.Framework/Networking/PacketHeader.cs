using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct PacketHeader
    {
        public Int32 length;
        public Int32 sequence;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( PacketHeader ) );
    }
}
