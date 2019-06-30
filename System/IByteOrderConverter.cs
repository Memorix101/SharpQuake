using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public interface IByteOrderConverter
    {
        Int16 BigShort( Int16 l );

        Int16 LittleShort( Int16 l );

        Int32 BigLong( Int32 l );

        Int32 LittleLong( Int32 l );

        Single BigFloat( Single l );

        Single LittleFloat( Single l );
    }
}
