using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public class BigEndianConverter : IByteOrderConverter
    {
        #region IByteOrderConverter Members

        Int16 IByteOrderConverter.BigShort( Int16 l )
        {
            return l;
        }

        Int16 IByteOrderConverter.LittleShort( Int16 l )
        {
            return SwapHelper.ShortSwap( l );
        }

        Int32 IByteOrderConverter.BigLong( Int32 l )
        {
            return l;
        }

        Int32 IByteOrderConverter.LittleLong( Int32 l )
        {
            return SwapHelper.LongSwap( l );
        }

        Single IByteOrderConverter.BigFloat( Single l )
        {
            return l;
        }

        Single IByteOrderConverter.LittleFloat( Single l )
        {
            return SwapHelper.FloatSwap( l );
        }

        #endregion IByteOrderConverter Members
    }
}
