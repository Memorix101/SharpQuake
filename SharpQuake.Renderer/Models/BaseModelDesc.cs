using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.Models
{
    public class BaseModelDesc
    {
        public virtual String Name
        {
            get;
            set;
        }

        public virtual BaseTexture Texture
        {
            get;
            set;
        }

        public virtual Vector3 Origin
        {
            get;
            set;
        }

        public virtual Vector3 EulerAngles
        {
            get;
            set;
        }

        public virtual Vector3 Scale
        {
            get;
            set;
        }

        public virtual Vector3 ScaleOrigin
        {
            get;
            set;
        }

        public virtual Vector3 MinimumBounds
        {
            get;
            set;
        }

        public virtual Vector3 MaximumBounds
        {
            get;
            set;
        }

        public virtual Boolean IsAliasModel
        {
            get;
            set;
        }

        public virtual Int32 AliasFrame
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
