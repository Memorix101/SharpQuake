using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace SharpQuake.Framework
{
    public class glmode_t
    {
        public String name;
        public TextureMinFilter minimize;
        public TextureMagFilter maximize;

        public glmode_t( String name, TextureMinFilter minFilter, TextureMagFilter magFilter )
        {
            this.name = name;
            this.minimize = minFilter;
            this.maximize = magFilter;
        }
    } //glmode_t;
}
