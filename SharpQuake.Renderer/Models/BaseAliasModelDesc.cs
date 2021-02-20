using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Renderer.Models
{
	public class BaseAliasModelDesc : BaseModelDesc
	{
		public virtual Int32 AliasFrame
		{
			get;
			set;
		}

		// model animation interpolation
		public virtual Int32 LastPoseNumber0
		{
			get;
			set;
		}

		public virtual Int32 LastPoseNumber
		{
			get;
			set;
		}
	}
}
