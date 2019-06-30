using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class ByteArraySegment
    {
        public Byte[] Data
        {
            get
            {
                return _Segment.Array;
            }
        }

        public Int32 StartIndex
        {
            get
            {
                return _Segment.Offset;
            }
        }

        public Int32 Length
        {
            get
            {
                return _Segment.Count;
            }
        }

        private ArraySegment<Byte> _Segment;

        public ByteArraySegment( Byte[] array )
            : this( array, 0, -1 )
        {
        }

        public ByteArraySegment( Byte[] array, Int32 startIndex )
            : this( array, startIndex, -1 )
        {
        }

        public ByteArraySegment( Byte[] array, Int32 startIndex, Int32 length )
        {
            if ( array == null )
            {
                throw new ArgumentNullException( "array" );
            }
            if ( length == -1 )
            {
                length = array.Length - startIndex;
            }
            if ( length <= 0 )
            {
                throw new ArgumentException( "Invalid length!" );
            }
            _Segment = new ArraySegment<Byte>( array, startIndex, length );
        }
    }
}
