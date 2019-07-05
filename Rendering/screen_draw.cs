/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using OpenTK.Graphics.OpenGL;

// gl_draw.c

namespace SharpQuake
{
    public partial class Scr
    {
        // GL_Set2D
        //
        // Setup as if the screen was 320*200
        private void Set2D()
        {
            GL.Viewport( glX, glY, glWidth, glHeight );

            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity();
            GL.Ortho( 0, _VidDef.width, _VidDef.height, 0, -99999, 99999 );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity();

            GL.Disable( EnableCap.DepthTest );
            GL.Disable( EnableCap.CullFace );
            GL.Disable( EnableCap.Blend );
            GL.Enable( EnableCap.AlphaTest );

            GL.Color4( 1.0f, 1.0f, 1.0f, 1.0f );
        }
    }
}
