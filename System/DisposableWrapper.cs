using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public class DisposableWrapper<T> : IDisposable where T : class, IDisposable
    {
        public T Object
        {
            get
            {
                return _Object;
            }
        }

        private T _Object;
        private Boolean _Owned;

        private void Dispose( Boolean disposing )
        {
            if ( _Object != null && _Owned )
            {
                _Object.Dispose( );
                _Object = null;
            }
        }

        public DisposableWrapper( T obj, Boolean dispose )
        {
            _Object = obj;
            _Owned = dispose;
        }

        ~DisposableWrapper( )
        {
            Dispose( false );
        }

        #region IDisposable Members

        public void Dispose( )
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable Members
    }
}
