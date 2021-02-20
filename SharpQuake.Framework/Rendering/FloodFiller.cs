using System;

namespace SharpQuake.Framework.Rendering
{
	public class FloodFiller
    {
        private struct floodfill_t
        {
            public Int16 x, y;
        } // floodfill_t;

        // must be a power of 2
        private const Int32 FLOODFILL_FIFO_SIZE = 0x1000;

        private const Int32 FLOODFILL_FIFO_MASK = FLOODFILL_FIFO_SIZE - 1;

        private ByteArraySegment _Skin;
        private floodfill_t[] _Fifo;
        private Int32 _Width;
        private Int32 _Height;

        //int _Offset;
        private Int32 _X;

        private Int32 _Y;
        private Int32 _Fdc;
        private Byte _FillColor;
        private Int32 _Inpt;

        public void Perform( UInt32[] table8to24 )
        {
            var filledcolor = 0;
            // attempt to find opaque black
            var t8to24 = table8to24;
            for ( var i = 0; i < 256; ++i )
                if ( t8to24[i] == ( 255 << 0 ) ) // alpha 1.0
                {
                    filledcolor = i;
                    break;
                }

            // can't fill to filled color or to transparent color (used as visited marker)
            if ( ( _FillColor == filledcolor ) || ( _FillColor == 255 ) )
            {
                return;
            }

            var outpt = 0;
            _Inpt = 0;
            _Fifo[_Inpt].x = 0;
            _Fifo[_Inpt].y = 0;
            _Inpt = ( _Inpt + 1 ) & FLOODFILL_FIFO_MASK;

            while ( outpt != _Inpt )
            {
                _X = _Fifo[outpt].x;
                _Y = _Fifo[outpt].y;
                _Fdc = filledcolor;
                var offset = _X + _Width * _Y;

                outpt = ( outpt + 1 ) & FLOODFILL_FIFO_MASK;

                if ( _X > 0 )
                    Step( offset - 1, -1, 0 );
                if ( _X < _Width - 1 )
                    Step( offset + 1, 1, 0 );
                if ( _Y > 0 )
                    Step( offset - _Width, 0, -1 );
                if ( _Y < _Height - 1 )
                    Step( offset + _Width, 0, 1 );

                _Skin.Data[_Skin.StartIndex + offset] = ( Byte ) _Fdc;
            }
        }

        private void Step( Int32 offset, Int32 dx, Int32 dy )
        {
            var pos = _Skin.Data;
            var off = _Skin.StartIndex + offset;

            if ( pos[off] == _FillColor )
            {
                pos[off] = 255;
                _Fifo[_Inpt].x = ( Int16 ) ( _X + dx );
                _Fifo[_Inpt].y = ( Int16 ) ( _Y + dy );
                _Inpt = ( _Inpt + 1 ) & FLOODFILL_FIFO_MASK;
            }
            else if ( pos[off] != 255 )
                _Fdc = pos[off];
        }

        public FloodFiller( ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            _Skin = skin;
            _Width = skinwidth;
            _Height = skinheight;
            _Fifo = new floodfill_t[FLOODFILL_FIFO_SIZE];
            _FillColor = _Skin.Data[_Skin.StartIndex]; // *skin; // assume this is the pixel to fill
        }
    }
}
