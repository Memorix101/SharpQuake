using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspModel
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Single[] mins; // [3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Single[] maxs; //[3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public System.Single[] origin; // [3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BspDef.MAX_MAP_HULLS )]
        public System.Int32[] headnode; //[MAX_MAP_HULLS];

        public System.Int32 visleafs;		// not including the solid leaf 0
        public System.Int32 firstface, numfaces;

        public static System.Int32 SizeInBytes = Marshal.SizeOf( typeof( BspModel ) );
    } // dmodel_t
}
