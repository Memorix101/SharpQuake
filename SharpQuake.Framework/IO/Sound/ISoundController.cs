using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sound
{
	public interface ISoundController
	{
		Boolean IsInitialised
		{
			get;
		}

		void Initialise( Object host );

		void Shutdown( );

		void ClearBuffer( );

		Byte[] LockBuffer( );

		void UnlockBuffer( Int32 count );

		Int32 GetPosition( );

		//void Submit();
	}
}
