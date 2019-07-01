/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

// r_part.c

namespace SharpQuake
{
    partial class render
    {
        private enum ptype_t
        {
            pt_static, pt_grav, pt_slowgrav, pt_fire, pt_explode, pt_explode2, pt_blob, pt_blob2
        } //ptype_t;

        private const Int32 MAX_PARTICLES = 2048;

        // default max # of particles at one time
        private const Int32 ABSOLUTE_MIN_PARTICLES = 512;

        // no fewer than this no matter what's on the command line
        private const Int32 NUMVERTEXNORMALS = 162;

        private static Int32[] _Ramp1 = new Int32[] { 0x6f, 0x6d, 0x6b, 0x69, 0x67, 0x65, 0x63, 0x61 };

        private static Int32[] _Ramp2 = new Int32[] { 0x6f, 0x6e, 0x6d, 0x6c, 0x6b, 0x6a, 0x68, 0x66 };

        private static Int32[] _Ramp3 = new Int32[] { 0x6d, 0x6b, 6, 5, 4, 3 };

        private static Byte[,] _DotTexture = new Byte[8, 8]
        {
            {0,1,1,0,0,0,0,0},
            {1,1,1,1,0,0,0,0},
            {1,1,1,1,0,0,0,0},
            {0,1,1,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
        };

        private static Int32 _NumParticles;

        // r_numparticles
        private static particle_t[] _Particles;

        private static Int32 _ParticleTexture;

        private static particle_t _ActiveParticles;

        // active_particles
        private static particle_t _FreeParticles;

        // free_particles
        private static Int32 _TracerCount;

        // static tracercount from RocketTrail()
        private static Vector3[] _AVelocities = new Vector3[NUMVERTEXNORMALS];

        // avelocities
        private static Single _BeamLength = 16;

        /// <summary>
        /// R_RocketTrail
        /// </summary>
        public static void RocketTrail( ref Vector3 start, ref Vector3 end, Int32 type )
        {
            var vec = end - start;
            var len = MathLib.Normalize( ref vec );
            Int32 dec;
            if( type < 128 )
                dec = 3;
            else
            {
                dec = 1;
                type -= 128;
            }

            while( len > 0 )
            {
                len -= dec;

                var p = AllocParticle();
                if( p == null )
                    return;

                p.vel = Vector3.Zero;
                p.die = ( Single ) Host.Client.cl.time + 2;

                switch( type )
                {
                    case 0:	// rocket trail
                        p.ramp = ( MathLib.Random() & 3 );
                        p.color = _Ramp3[( Int32 ) p.ramp];
                        p.type = ptype_t.pt_fire;
                        p.org = new Vector3( start.X + ( ( MathLib.Random() % 6 ) - 3 ),
                            start.Y + ( ( MathLib.Random() % 6 ) - 3 ), start.Z + ( ( MathLib.Random() % 6 ) - 3 ) );
                        break;

                    case 1:	// smoke smoke
                        p.ramp = ( MathLib.Random() & 3 ) + 2;
                        p.color = _Ramp3[( Int32 ) p.ramp];
                        p.type = ptype_t.pt_fire;
                        p.org = new Vector3( start.X + ( ( MathLib.Random() % 6 ) - 3 ),
                            start.Y + ( ( MathLib.Random() % 6 ) - 3 ), start.Z + ( ( MathLib.Random() % 6 ) - 3 ) );
                        break;

                    case 2:	// blood
                        p.type = ptype_t.pt_grav;
                        p.color = 67 + ( MathLib.Random() & 3 );
                        p.org = new Vector3( start.X + ( ( MathLib.Random() % 6 ) - 3 ),
                            start.Y + ( ( MathLib.Random() % 6 ) - 3 ), start.Z + ( ( MathLib.Random() % 6 ) - 3 ) );
                        break;

                    case 3:
                    case 5:	// tracer
                        p.die = ( Single ) Host.Client.cl.time + 0.5f;
                        p.type = ptype_t.pt_static;
                        if( type == 3 )
                            p.color = 52 + ( ( _TracerCount & 4 ) << 1 );
                        else
                            p.color = 230 + ( ( _TracerCount & 4 ) << 1 );

                        _TracerCount++;

                        p.org = start;
                        if( ( _TracerCount & 1 ) != 0 )
                        {
                            p.vel.X = 30 * vec.Y; // Uze: why???
                            p.vel.Y = 30 * -vec.X;
                        }
                        else
                        {
                            p.vel.X = 30 * -vec.Y;
                            p.vel.Y = 30 * vec.X;
                        }
                        break;

                    case 4:	// slight blood
                        p.type = ptype_t.pt_grav;
                        p.color = 67 + ( MathLib.Random() & 3 );
                        p.org = new Vector3( start.X + ( ( MathLib.Random() % 6 ) - 3 ),
                            start.Y + ( ( MathLib.Random() % 6 ) - 3 ), start.Z + ( ( MathLib.Random() % 6 ) - 3 ) );
                        len -= 3;
                        break;

                    case 6:	// voor trail
                        p.color = 9 * 16 + 8 + ( MathLib.Random() & 3 );
                        p.type = ptype_t.pt_static;
                        p.die = ( Single ) Host.Client.cl.time + 0.3f;
                        p.org = new Vector3( start.X + ( ( MathLib.Random() % 15 ) - 8 ),
                            start.Y + ( ( MathLib.Random() % 15 ) - 8 ), start.Z + ( ( MathLib.Random() % 15 ) - 8 ) );
                        break;
                }

                start += vec;
            }
        }

        /// <summary>
        /// R_ParticleExplosion
        /// </summary>
        public static void ParticleExplosion( ref Vector3 org )
        {
            for( var i = 0; i < 1024; i++ ) // Uze: Why 1024 if MAX_PARTICLES = 2048?
            {
                var p = AllocParticle();
                if( p == null )
                    return;

                p.die = ( Single ) Host.Client.cl.time + 5;
                p.color = _Ramp1[0];
                p.ramp = MathLib.Random() & 3;
                if( ( i & 1 ) != 0 )
                    p.type = ptype_t.pt_explode;
                else
                    p.type = ptype_t.pt_explode2;
                p.org = org + new Vector3( ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16 );
                p.vel = new Vector3( ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256 );
            }
        }

        /// <summary>
        /// R_RunParticleEffect
        /// </summary>
        public static void RunParticleEffect( ref Vector3 org, ref Vector3 dir, Int32 color, Int32 count )
        {
            for( var i = 0; i < count; i++ )
            {
                var p = AllocParticle();
                if( p == null )
                    return;

                if( count == 1024 )
                {	// rocket explosion
                    p.die = ( Single ) Host.Client.cl.time + 5;
                    p.color = _Ramp1[0];
                    p.ramp = MathLib.Random() & 3;
                    if( ( i & 1 ) != 0 )
                        p.type = ptype_t.pt_explode;
                    else
                        p.type = ptype_t.pt_explode2;
                    p.org = org + new Vector3( ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16 );
                    p.vel = new Vector3( ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256 );
                }
                else
                {
                    p.die = ( Single ) Host.Client.cl.time + 0.1f * ( MathLib.Random() % 5 );
                    p.color = ( color & ~7 ) + ( MathLib.Random() & 7 );
                    p.type = ptype_t.pt_slowgrav;
                    p.org = org + new Vector3( ( MathLib.Random() & 15 ) - 8, ( MathLib.Random() & 15 ) - 8, ( MathLib.Random() & 15 ) - 8 );
                    p.vel = dir * 15.0f;
                }
            }
        }

        /// <summary>
        /// R_ParseParticleEffect
        /// Parse an effect out of the server message
        /// </summary>
        public static void ParseParticleEffect()
        {
            var org = Host.Network.Reader.ReadCoords();
            var dir = new Vector3( Host.Network.Reader.ReadChar() * ONE_OVER_16,
                Host.Network.Reader.ReadChar() * ONE_OVER_16,
                Host.Network.Reader.ReadChar() * ONE_OVER_16 );
            var count = Host.Network.Reader.ReadByte();
            var color = Host.Network.Reader.ReadByte();

            if( count == 255 )
                count = 1024;

            RunParticleEffect( ref org, ref dir, color, count );
        }

        /// <summary>
        /// R_TeleportSplash
        /// </summary>
        public static void TeleportSplash( ref Vector3 org )
        {
            for( var i = -16; i < 16; i += 4 )
                for( var j = -16; j < 16; j += 4 )
                    for( var k = -24; k < 32; k += 4 )
                    {
                        var p = AllocParticle();
                        if( p == null )
                            return;

                        p.die = ( Single ) ( Host.Client.cl.time + 0.2 + ( MathLib.Random() & 7 ) * 0.02 );
                        p.color = 7 + ( MathLib.Random() & 7 );
                        p.type = ptype_t.pt_slowgrav;

                        var dir = new Vector3( j * 8, i * 8, k * 8 );

                        p.org = org + new Vector3( i + ( MathLib.Random() & 3 ), j + ( MathLib.Random() & 3 ), k + ( MathLib.Random() & 3 ) );

                        MathLib.Normalize( ref dir );
                        Single vel = 50 + ( MathLib.Random() & 63 );
                        p.vel = dir * vel;
                    }
        }

        /// <summary>
        /// R_LavaSplash
        /// </summary>
        public static void LavaSplash( ref Vector3 org )
        {
            Vector3 dir;

            for( var i = -16; i < 16; i++ )
                for( var j = -16; j < 16; j++ )
                    for( var k = 0; k < 1; k++ )
                    {
                        var p = AllocParticle();
                        if( p == null )
                            return;

                        p.die = ( Single ) ( Host.Client.cl.time + 2 + ( MathLib.Random() & 31 ) * 0.02 );
                        p.color = 224 + ( MathLib.Random() & 7 );
                        p.type = ptype_t.pt_slowgrav;

                        dir.X = j * 8 + ( MathLib.Random() & 7 );
                        dir.Y = i * 8 + ( MathLib.Random() & 7 );
                        dir.Z = 256;

                        p.org = org + dir;
                        p.org.Z += MathLib.Random() & 63;

                        MathLib.Normalize( ref dir );
                        Single vel = 50 + ( MathLib.Random() & 63 );
                        p.vel = dir * vel;
                    }
        }

        /// <summary>
        /// R_ParticleExplosion2
        /// </summary>
        public static void ParticleExplosion( ref Vector3 org, Int32 colorStart, Int32 colorLength )
        {
            var colorMod = 0;

            for( var i = 0; i < 512; i++ )
            {
                var p = AllocParticle();
                if( p == null )
                    return;

                p.die = ( Single ) ( Host.Client.cl.time + 0.3 );
                p.color = colorStart + ( colorMod % colorLength );
                colorMod++;

                p.type = ptype_t.pt_blob;
                p.org = org + new Vector3( ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16 );
                p.vel = new Vector3( ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256 );
            }
        }

        /// <summary>
        /// R_BlobExplosion
        /// </summary>
        public static void BlobExplosion( ref Vector3 org )
        {
            for( var i = 0; i < 1024; i++ )
            {
                var p = AllocParticle();
                if( p == null )
                    return;

                p.die = ( Single ) ( Host.Client.cl.time + 1 + ( MathLib.Random() & 8 ) * 0.05 );

                if( ( i & 1 ) != 0 )
                {
                    p.type = ptype_t.pt_blob;
                    p.color = 66 + MathLib.Random() % 6;
                }
                else
                {
                    p.type = ptype_t.pt_blob2;
                    p.color = 150 + MathLib.Random() % 6;
                }
                p.org = org + new Vector3( ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16, ( MathLib.Random() % 32 ) - 16 );
                p.vel = new Vector3( ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256, ( MathLib.Random() % 512 ) - 256 );
            }
        }

        /// <summary>
        /// R_EntityParticles
        /// </summary>
        public static void EntityParticles( Entity ent )
        {
            Single dist = 64;

            if( _AVelocities[0].X == 0 )
            {
                for( var i = 0; i < NUMVERTEXNORMALS; i++ )
                {
                    _AVelocities[i].X = ( MathLib.Random() & 255 ) * 0.01f;
                    _AVelocities[i].Y = ( MathLib.Random() & 255 ) * 0.01f;
                    _AVelocities[i].Z = ( MathLib.Random() & 255 ) * 0.01f;
                }
            }

            for( var i = 0; i < NUMVERTEXNORMALS; i++ )
            {
                var angle = Host.Client.cl.time * _AVelocities[i].X;
                var sy = Math.Sin( angle );
                var cy = Math.Cos( angle );
                angle = Host.Client.cl.time * _AVelocities[i].Y;
                var sp = Math.Sin( angle );
                var cp = Math.Cos( angle );
                angle = Host.Client.cl.time * _AVelocities[i].Z;
                var sr = Math.Sin( angle );
                var cr = Math.Cos( angle );

                var forward = new Vector3( ( Single ) ( cp * cy ), ( Single ) ( cp * sy ),  -( ( System.Single ) sp ) );
                var p = AllocParticle();
                if( p == null )
                    return;

                p.die = ( Single ) ( Host.Client.cl.time + 0.01 );
                p.color = 0x6f;
                p.type = ptype_t.pt_explode;

                p.org = ent.origin + anorms.Values[i] * dist + forward * _BeamLength;
            }
        }

        // R_InitParticles
        private static void InitParticles()
        {
            var i = CommandLine.CheckParm( "-particles" );
            if( i > 0 && i < CommandLine.Argc - 1 )
            {
                _NumParticles = Int32.Parse( CommandLine.Argv( i + 1 ) );
                if( _NumParticles < ABSOLUTE_MIN_PARTICLES )
                    _NumParticles = ABSOLUTE_MIN_PARTICLES;
            }
            else
                _NumParticles = MAX_PARTICLES;

            _Particles = new particle_t[_NumParticles];
            for( i = 0; i < _NumParticles; i++ )
                _Particles[i] = new particle_t();
        }

        // beamlength
        // R_InitParticleTexture
        private static void InitParticleTexture()
        {
            _ParticleTexture = Drawer.GenerateTextureNumber();// texture_extension_number++;
            Drawer.Bind( _ParticleTexture );

            var data = new Byte[8, 8, 4];
            for( var x = 0; x < 8; x++ )
            {
                for( var y = 0; y < 8; y++ )
                {
                    data[y, x, 0] = 255;
                    data[y, x, 1] = 255;
                    data[y, x, 2] = 255;
                    data[y, x, 3] = ( Byte ) ( _DotTexture[x, y] * 255 );
                }
            }
            GL.TexImage2D( TextureTarget.Texture2D, 0, Drawer.AlphaFormat, 8, 8, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            Drawer.SetTextureFilters( TextureMinFilter.Linear, TextureMagFilter.Linear );
        }

        // particletexture	// little dot for particles
        /// <summary>
        /// R_ClearParticles
        /// </summary>
        private static void ClearParticles()
        {
            _FreeParticles = _Particles[0];
            _ActiveParticles = null;

            for( var i = 0; i < _NumParticles - 1; i++ )
                _Particles[i].next = _Particles[i + 1];
            _Particles[_NumParticles - 1].next = null;
        }

        /// <summary>
        /// R_DrawParticles
        /// </summary>
        private static void DrawParticles()
        {
            Drawer.Bind( _ParticleTexture );
            GL.Enable( EnableCap.Blend );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            GL.Begin( PrimitiveType.Triangles );

            var up = render.ViewUp * 1.5f;
            var right = render.ViewRight * 1.5f;
            var frametime = ( Single ) ( Host.Client.cl.time - Host.Client.cl.oldtime );
            var time3 = frametime * 15;
            var time2 = frametime * 10;
            var time1 = frametime * 5;
            var grav = frametime * Host.Server.Gravity * 0.05f;
            var dvel = 4 * frametime;

            while( true )
            {
                var kill = _ActiveParticles;
                if( kill != null && kill.die < Host.Client.cl.time )
                {
                    _ActiveParticles = kill.next;
                    kill.next = _FreeParticles;
                    _FreeParticles = kill;
                    continue;
                }
                break;
            }

            for( var p = _ActiveParticles; p != null; p = p.next )
            {
                while( true )
                {
                    var kill = p.next;
                    if( kill != null && kill.die < Host.Client.cl.time )
                    {
                        p.next = kill.next;
                        kill.next = _FreeParticles;
                        _FreeParticles = kill;
                        continue;
                    }
                    break;
                }

                // hack a scale up to keep particles from disapearing
                var scale = Vector3.Dot( ( p.org - render.Origin ), render.ViewPn );
                if( scale < 20 )
                    scale = 1;
                else
                    scale = 1 + scale * 0.004f;

                // Uze todo: check if this is correct
                var color = vid.Table8to24[( Byte ) p.color];
                GL.Color4( ( Byte ) ( color & 0xff ), ( Byte ) ( ( color >> 8 ) & 0xff ), ( Byte ) ( ( color >> 16 ) & 0xff ), ( Byte ) ( ( color >> 24 ) & 0xff ) );
                GL.TexCoord2( 0f, 0 );
                GL.Vertex3( p.org );
                GL.TexCoord2( 1f, 0 );
                var v = p.org + up * scale;
                GL.Vertex3( v );
                GL.TexCoord2( 0f, 1 );
                v = p.org + right * scale;
                GL.Vertex3( v );

                p.org += p.vel * frametime;

                switch( p.type )
                {
                    case ptype_t.pt_static:
                        break;

                    case ptype_t.pt_fire:
                        p.ramp += time1;
                        if( p.ramp >= 6 )
                            p.die = -1;
                        else
                            p.color = _Ramp3[( Int32 ) p.ramp];
                        p.vel.Z += grav;
                        break;

                    case ptype_t.pt_explode:
                        p.ramp += time2;
                        if( p.ramp >= 8 )
                            p.die = -1;
                        else
                            p.color = _Ramp1[( Int32 ) p.ramp];
                        p.vel += p.vel * dvel;
                        p.vel.Z -= grav;
                        break;

                    case ptype_t.pt_explode2:
                        p.ramp += time3;
                        if( p.ramp >= 8 )
                            p.die = -1;
                        else
                            p.color = _Ramp2[( Int32 ) p.ramp];
                        p.vel -= p.vel * frametime;
                        p.vel.Z -= grav;
                        break;

                    case ptype_t.pt_blob:
                        p.vel += p.vel * dvel;
                        p.vel.Z -= grav;
                        break;

                    case ptype_t.pt_blob2:
                        p.vel -= p.vel * dvel;
                        p.vel.Z -= grav;
                        break;

                    case ptype_t.pt_grav:
                    case ptype_t.pt_slowgrav:
                        p.vel.Z -= grav;
                        break;
                }
            }
            GL.End();
            GL.Disable( EnableCap.Blend );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
        }

        private static particle_t AllocParticle()
        {
            if( _FreeParticles == null )
                return null;

            var p = _FreeParticles;
            _FreeParticles = p.next;
            p.next = _ActiveParticles;
            _ActiveParticles = p;

            return p;
        }

        // !!! if this is changed, it must be changed in d_ifacea.h too !!!
        private class particle_t
        {
            // driver-usable fields
            public Vector3  org; // vec3_t

            public Single color;

            // drivers never touch the following fields
            public particle_t next;

            public Vector3 vel; // vec3_t
            public Single ramp;
            public Single die;
            public ptype_t type;
        } // particle_t;
    }
}
