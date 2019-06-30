using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    // MSG_WriteXxx() functions
    public class MessageWriter
    {
        public Byte[] Data
        {
            get
            {
                return _Buffer;
            }
        }

        public Boolean IsEmpty
        {
            get
            {
                return ( _Count == 0 );
            }
        }

        public Int32 Length
        {
            get
            {
                return _Count;
            }
        }

        public Boolean AllowOverflow
        {
            get; set;
        }

        public Boolean IsOveflowed
        {
            get; set;
        }

        public Int32 Capacity
        {
            get
            {
                return _Buffer.Length;
            }
            set
            {
                SetBufferSize( value );
            }
        }

        private Byte[] _Buffer;

        private Int32 _Count;

        private Union4B _Val = Union4B.Empty;

        public Object GetState( )
        {
            Object st = null;
            SaveState( ref st );
            return st;
        }

        public void SaveState( ref Object state )
        {
            if ( state == null )
            {
                state = new State( );
            }
            State st = GetState( state );
            if ( st.Buffer == null || st.Buffer.Length != _Buffer.Length )
            {
                st.Buffer = new Byte[_Buffer.Length];
            }
            Buffer.BlockCopy( _Buffer, 0, st.Buffer, 0, _Buffer.Length );
            st.Count = _Count;
        }

        public void RestoreState( Object state )
        {
            State st = GetState( state );
            SetBufferSize( st.Buffer.Length );
            Buffer.BlockCopy( st.Buffer, 0, _Buffer, 0, _Buffer.Length );
            _Count = st.Count;
        }

        // void MSG_WriteChar(sizebuf_t* sb, int c);
        public void WriteChar( Int32 c )
        {
#if PARANOID
            if (c < -128 || c > 127)
                Sys.Error("MSG_WriteChar: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = ( Byte ) c;
        }

        // MSG_WriteByte(sizebuf_t* sb, int c);
        public void WriteByte( Int32 c )
        {
#if PARANOID
            if (c < 0 || c > 255)
                Sys.Error("MSG_WriteByte: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = ( Byte ) c;
        }

        // MSG_WriteShort(sizebuf_t* sb, int c)
        public void WriteShort( Int32 c )
        {
#if PARANOID
            if (c < short.MinValue || c > short.MaxValue)
                Sys.Error("MSG_WriteShort: range error");
#endif
            NeedRoom( 2 );
            _Buffer[_Count++] = ( Byte ) ( c & 0xff );
            _Buffer[_Count++] = ( Byte ) ( c >> 8 );
        }

        // MSG_WriteLong(sizebuf_t* sb, int c);
        public void WriteLong( Int32 c )
        {
            NeedRoom( 4 );
            _Buffer[_Count++] = ( Byte ) ( c & 0xff );
            _Buffer[_Count++] = ( Byte ) ( ( c >> 8 ) & 0xff );
            _Buffer[_Count++] = ( Byte ) ( ( c >> 16 ) & 0xff );
            _Buffer[_Count++] = ( Byte ) ( c >> 24 );
        }

        // MSG_WriteFloat(sizebuf_t* sb, float f)
        public void WriteFloat( Single f )
        {
            NeedRoom( 4 );
            _Val.f0 = f;
            _Val.i0 = Common.LittleLong( _Val.i0 );

            _Buffer[_Count++] = _Val.b0;
            _Buffer[_Count++] = _Val.b1;
            _Buffer[_Count++] = _Val.b2;
            _Buffer[_Count++] = _Val.b3;
        }

        // MSG_WriteString(sizebuf_t* sb, char* s)
        public void WriteString( String s )
        {
            var count = 1;
            if ( !String.IsNullOrEmpty( s ) )
                count += s.Length;

            NeedRoom( count );
            for ( var i = 0; i < count - 1; i++ )
                _Buffer[_Count++] = ( Byte ) s[i];
            _Buffer[_Count++] = 0;
        }

        // SZ_Print()
        public void Print( String s )
        {
            if ( _Count > 0 && _Buffer[_Count - 1] == 0 )
                _Count--; // remove previous trailing 0
            WriteString( s );
        }

        // MSG_WriteCoord(sizebuf_t* sb, float f)
        public void WriteCoord( Single f )
        {
            WriteShort( ( Int32 ) ( f * 8 ) );
        }

        // MSG_WriteAngle(sizebuf_t* sb, float f)
        public void WriteAngle( Single f )
        {
            WriteByte( ( ( Int32 ) f * 256 / 360 ) & 255 );
        }

        public void Write( Byte[] src, Int32 offset, Int32 count )
        {
            if ( count > 0 )
            {
                NeedRoom( count );
                Buffer.BlockCopy( src, offset, _Buffer, _Count, count );
                _Count += count;
            }
        }

        public void Clear( )
        {
            _Count = 0;
        }

        public void FillFrom( Stream src, Int32 count )
        {
            Clear( );
            NeedRoom( count );
            while ( _Count < count )
            {
                var r = src.Read( _Buffer, _Count, count - _Count );
                if ( r == 0 )
                    break;
                _Count += r;
            }
        }

        public void FillFrom( Byte[] src, Int32 startIndex, Int32 count )
        {
            Clear( );
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, 0, count );
            _Count = count;
        }

        public Int32 FillFrom( Socket socket, ref EndPoint ep )
        {
            Clear( );
            var result = net.LanDriver.Read( socket, _Buffer, _Buffer.Length, ref ep );
            if ( result >= 0 )
                _Count = result;
            return result;
        }

        public void AppendFrom( Byte[] src, Int32 startIndex, Int32 count )
        {
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, _Count, count );
            _Count += count;
        }

        protected void NeedRoom( Int32 bytes )
        {
            if ( _Count + bytes > _Buffer.Length )
            {
                if ( !this.AllowOverflow )
                    sys.Error( "MsgWriter: overflow without allowoverflow set!" );

                this.IsOveflowed = true;
                _Count = 0;
                if ( bytes > _Buffer.Length )
                    sys.Error( "MsgWriter: Requested more than whole buffer has!" );
            }
        }

        private class State
        {
            public Byte[] Buffer;
            public Int32 Count;
        }

        private void SetBufferSize( Int32 value )
        {
            if ( _Buffer != null )
            {
                if ( _Buffer.Length == value )
                    return;

                Array.Resize( ref _Buffer, value );

                if ( _Count > _Buffer.Length )
                    _Count = _Buffer.Length;
            }
            else
                _Buffer = new Byte[value];
        }

        private State GetState( Object state )
        {
            if ( state == null )
            {
                throw new ArgumentNullException( );
            }
            State st = state as State;
            if ( st == null )
            {
                throw new ArgumentException( "Passed object is not a state!" );
            }
            return st;
        }

        public MessageWriter( )
                    : this( 0 )
        {
        }

        public MessageWriter( Int32 capacity )
        {
            SetBufferSize( capacity );
            this.AllowOverflow = false;
        }
    }
}
