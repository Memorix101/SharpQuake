/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
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
using System.Linq;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework.World;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Game.World;
using SharpQuake.Renderer;
using SharpQuake.Renderer.Models;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering;
using SharpQuake.Rendering.Environment;

// refresh.h -- public interface to refresh functions
// gl_rmisc.c
// gl_rmain.c

namespace SharpQuake
{
	/// <summary>
	/// R_functions
	/// </summary>
	public partial class render
    {
        public refdef_t RefDef
        {
            get
            {
                return _RefDef;
            }
        }

        public Boolean CacheTrash
        {
            get
            {
                return _CacheThrash;
            }
        }

        public ModelTexture NoTextureMip
        {
            get
            {
                return _NoTextureMip;
            }
        }


        public const Int32 MAXCLIPPLANES = 11;
        public const Int32 TOP_RANGE = 16;			// soldier uniform colors
        public const Int32 BOTTOM_RANGE = 96;

        //
        // view origin
        //
        public Vector3 ViewUp;

        // vup
        public Vector3 ViewPn;

        // vpn
        public Vector3 ViewRight;

        // vright
        public Vector3 Origin;

        private refdef_t _RefDef = new refdef_t( ); // refdef_t	r_refdef;
        private ModelTexture _NoTextureMip; // r_notexture_mip
        
        private BaseTexture[] PlayerTextures;
        private System.Boolean _CacheThrash; // r_cache_thrash	// compatability

        // r_origin

        private Entity _CurrentEntity; // currententity

                                      //private Int32 _MirrorTextureNum; // mirrortexturenum	// quake texturenum, not gltexturenum

       

        public World World
        {
            get;
            private set;
        }

        //private System.Boolean _IsMirror; // mirror
        //private Plane _MirrorPlane; // mirror_plane

        // Temporarily turn into property until GL stripped out of this project
        private Single _glDepthMin
        {
            get
            {
                return Host.Video.Device.Desc.DepthMinimum;
            }
            set
            {
                Host.Video.Device.Desc.DepthMinimum = value;
            }
        }

        private Single _glDepthMax
        {
            get
            {
                return Host.Video.Device.Desc.DepthMaximum;
            }
            set
            {
                Host.Video.Device.Desc.DepthMaximum = value;
            }
        }

        public Plane[] Frustum
        {
            get;
            private set;
        } = new Plane[4]; // frustum

        private System.Boolean _IsEnvMap = false; // envmap	// true during envmap command capture
        private Vector3 _ModelOrg; // modelorg
        private Vector3 _EntOrigin; // r_entorigin
        private Single _ShadeLight; // shadelight
        private Single _AmbientLight; // ambientlight
        private Single[] _ShadeDots = anorm_dots.Values[0]; // shadedots
        private Vector3 _ShadeVector; // shadevector

        public TextureChains TextureChains
        {
            get;
            protected set;
        }

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

		public WarpableTextures WarpableTextures
		{
			get;
			private set;
		}

		public render( Host host )
        {
            Host = host;
            World = new World( Host );
            WarpableTextures = new WarpableTextures( Host.Video.Device );
		}

        private void InitialiseClientVariables()
		{
            if ( Host.Cvars.NoRefresh == null )
            {
                Host.Cvars.NoRefresh = Host.CVars.Add( "r_norefresh", false );
                Host.Cvars.DrawEntities = Host.CVars.Add( "r_drawentities", true );
                Host.Cvars.DrawViewModel = Host.CVars.Add( "r_drawviewmodel", true );
                Host.Cvars.Speeds = Host.CVars.Add( "r_speeds", false );
                Host.Cvars.FullBright = Host.CVars.Add( "r_fullbright", false );
                Host.Cvars.LightMap = Host.CVars.Add( "r_lightmap", false );
                Host.Cvars.Shadows = Host.CVars.Add( "r_shadows", false );
                //_MirrorAlpha = Host.CVars.Add( "r_mirroralpha", "1" );
                Host.Cvars.WaterAlpha = Host.CVars.Add( "r_wateralpha", 1f );
                Host.Cvars.Dynamic = Host.CVars.Add( "r_dynamic", true );
                Host.Cvars.NoVis = Host.CVars.Add( "r_novis", false );

                Host.Cvars.glFinish = Host.CVars.Add( "gl_finish", false );
                Host.Cvars.glClear = Host.CVars.Add( "gl_clear", 0f );
                Host.Cvars.glCull = Host.CVars.Add( "gl_cull", true );
                Host.Cvars.glTexSort = Host.CVars.Add( "gl_texsort", true );
                Host.Cvars.glSmoothModels = Host.CVars.Add( "gl_smoothmodels", true );
                Host.Cvars.glAffineModels = Host.CVars.Add( "gl_affinemodels", false );
                Host.Cvars.glPolyBlend = Host.CVars.Add( "gl_polyblend", true );
                Host.Cvars.glFlashBlend = Host.CVars.Add( "gl_flashblend", true );
                Host.Cvars.glPlayerMip = Host.CVars.Add( "gl_playermip", 0 );
                Host.Cvars.glNoColors = Host.CVars.Add( "gl_nocolors", false );
                Host.Cvars.glKeepTJunctions = Host.CVars.Add( "gl_keeptjunctions", false );
                Host.Cvars.glReportTJunctions = Host.CVars.Add( "gl_reporttjunctions", false );
                Host.Cvars.glDoubleEyes = Host.CVars.Add( "gl_doubleeys", true );
            }

            if ( Host.Video.Device.Desc.SupportsMultiTexture )
                Host.CVars.Set( "gl_texsort", 0.0f );
        }

        /// <summary>
        /// R_Init
        /// </summary>
        public void Initialise( )
        {            
            for ( var i = 0; i < Frustum.Length; i++ )
                Frustum[i] = new Plane( );

            Host.Commands.Add( "timerefresh", TimeRefresh_f );
            //Cmd.Add("envmap", Envmap_f);
            //Cmd.Add("pointfile", ReadPointFile_f);

            InitialiseClientVariables();

            World.Particles.InitParticles( );

            // reserve 16 textures
            PlayerTextures = new BaseTexture[16];

            for ( var i = 0; i < PlayerTextures.Length; i++ )
            {
                PlayerTextures[i] = BaseTexture.FromDynamicBuffer( Host.Video.Device, "_PlayerTexture{i}", new ByteArraySegment( new Byte[512 * 256 * 4] ), 512, 256, false, false );
            }

            TextureChains = new TextureChains();
            World.Initialise( TextureChains );
        }

        // R_InitTextures
        public void InitTextures( )
        {
            // create a simple checkerboard texture for the default
            _NoTextureMip = new ModelTexture( );
            _NoTextureMip.pixels = new Byte[16 * 16 + 8 * 8 + 4 * 4 + 2 * 2];
            _NoTextureMip.width = _NoTextureMip.height = 16;
            var offset = 0;
            _NoTextureMip.offsets[0] = offset;
            offset += 16 * 16;
            _NoTextureMip.offsets[1] = offset;
            offset += 8 * 8;
            _NoTextureMip.offsets[2] = offset;
            offset += 4 * 4;
            _NoTextureMip.offsets[3] = offset;

            var dest = _NoTextureMip.pixels;
            for ( var m = 0; m < 4; m++ )
            {
                offset = _NoTextureMip.offsets[m];
                for ( var y = 0; y < ( 16 >> m ); y++ )
                    for ( var x = 0; x < ( 16 >> m ); x++ )
                    {
                        if ( ( y < ( 8 >> m ) ) ^ ( x < ( 8 >> m ) ) )
                            dest[offset] = 0;
                        else
                            dest[offset] = 0xff;

                        offset++;
                    }
            }
        }

        /// <summary>
        /// R_RenderView
        /// r_refdef must be set before the first call
        /// </summary>
        public void RenderView( )
        {
            if ( Host.Cvars.NoRefresh.Get<Boolean>() )
                return;

            if ( World.WorldEntity.model == null || Host.Client.cl.worldmodel == null )
                Utilities.Error( "R_RenderView: NULL worldmodel" );

            Double time1 = 0;
            if ( Host.Cvars.Speeds.Get<Boolean>( ) )
            {
                Host.Video.Device.Finish( );
                time1 = Timer.GetFloatTime( );
                World.Entities.Surfaces.Reset( );
                World.Entities.Reset( );
            }

            //_IsMirror = false;

            if ( Host.Cvars.glFinish.Get<Boolean>() )
                Host.Video.Device.Finish( );

            Clear( );

            // render normal view

            RenderScene( );
            World.Entities.DrawViewModel( _IsEnvMap );
            World.Entities.Surfaces.DrawWaterSurfaces( );

            // render mirror view
            //Mirror();

            PolyBlend( );

            if ( Host.Cvars.Speeds.Get<Boolean>() )
            {
                var time2 = Timer.GetFloatTime( );
                ConsoleWrapper.Print( "{0,3} ms  {1,4} wpoly {2,4} epoly\n", ( Int32 ) ( ( time2 - time1 ) * 1000 ), World.Entities.Surfaces.BrushPolys, World.Entities.AliasPolys );
            }
        }

        /// <summary>
        /// R_TranslatePlayerSkin
        /// Translates a skin texture by the per-player color lookup
        /// </summary>
        public void TranslatePlayerSkin( Int32 playernum )
        {
            Host.Video.Device.DisableMultitexture( );

            var top = Host.Client.cl.scores[playernum].colors & 0xf0;
            var bottom = ( Host.Client.cl.scores[playernum].colors & 15 ) << 4;

            var translate = new Byte[256];
            for ( var i = 0; i < 256; i++ )
                translate[i] = ( Byte ) i;

            for ( var i = 0; i < 16; i++ )
            {
                if ( top < 128 )	// the artists made some backwards ranges.  sigh.
                    translate[TOP_RANGE + i] = ( Byte ) ( top + i );
                else
                    translate[TOP_RANGE + i] = ( Byte ) ( top + 15 - i );

                if ( bottom < 128 )
                    translate[BOTTOM_RANGE + i] = ( Byte ) ( bottom + i );
                else
                    translate[BOTTOM_RANGE + i] = ( Byte ) ( bottom + 15 - i );
            }

            //
            // locate the original skin pixels
            //
            _CurrentEntity = Host.Client.Entities[1 + playernum];
            var model = _CurrentEntity.model;
            if ( model == null )
                return;		// player doesn't have a model yet
            if ( model.Type != ModelType.Alias )
                return; // only translate skins on alias models

            var paliashdr = Host.Model.GetExtraData( model );
            var s = paliashdr.skinwidth * paliashdr.skinheight;
            if ( ( s & 3 ) != 0 )
                Utilities.Error( "R_TranslateSkin: s&3" );

            Byte[] original;
            if ( _CurrentEntity.skinnum < 0 || _CurrentEntity.skinnum >= paliashdr.numskins )
            {
                ConsoleWrapper.Print( "({0}): Invalid player skin #{1}\n", playernum, _CurrentEntity.skinnum );
                original = ( Byte[] ) paliashdr.texels[0];// (byte *)paliashdr + paliashdr.texels[0];
            }
            else
                original = ( Byte[] ) paliashdr.texels[_CurrentEntity.skinnum];

            var inwidth = paliashdr.skinwidth;
            var inheight = paliashdr.skinheight;

            // because this happens during gameplay, do it fast
            // instead of sending it through gl_upload 8
            var maxSize = Host.Cvars.glMaxSize.Get<Int32>();
            PlayerTextures[playernum].TranslateAndUpload( original, translate, inwidth, inheight, maxSize, maxSize, ( Int32 ) Host.Cvars.glPlayerMip.Get<Int32>() );
        }

        /// <summary>
        /// R_PolyBlend
        /// </summary>
        private void PolyBlend( )
        {
            if ( !Host.Cvars.glPolyBlend.Get<Boolean>() )
                return;

            if ( Host.View.Blend.A == 0 )
                return;

            Host.Video.Device.Graphics.PolyBlend( Host.View.Blend );
        }

        /// <summary>
        /// R_Mirror
        /// </summary>
        //private void Mirror()
        //{
        //    if( !_IsMirror )
        //        return;

        //    _BaseWorldMatrix = _WorldMatrix;

        //    var d = Vector3.Dot( _RefDef.vieworg, _MirrorPlane.normal ) - _MirrorPlane.dist;
        //    _RefDef.vieworg += _MirrorPlane.normal * -2 * d;

        //    d = Vector3.Dot( ViewPn, _MirrorPlane.normal );
        //    ViewPn += _MirrorPlane.normal * -2 * d;

        //    _RefDef.viewangles = new Vector3( ( Single ) ( Math.Asin( ViewPn.Z ) / Math.PI * 180.0 ),
        //        ( Single ) ( Math.Atan2( ViewPn.Y, ViewPn.X ) / Math.PI * 180.0 ),
        //        -_RefDef.viewangles.Z );

        //    var ent = Host.Client.ViewEntity;
        //    if( Host.Client.NumVisEdicts < ClientDef.MAX_VISEDICTS )
        //    {
        //        Host.Client.VisEdicts[Host.Client.NumVisEdicts] = ent;
        //        Host.Client.NumVisEdicts++;
        //    }

        //    _glDepthMin = 0.5f;
        //    _glDepthMax = 1;
        //    GL.DepthRange( _glDepthMin, _glDepthMax );
        //    GL.DepthFunc( DepthFunction.Lequal );

        //    RenderScene();
        //    DrawWaterSurfaces();

        //    _glDepthMin = 0;
        //    _glDepthMax = 0.5f;
        //    GL.DepthRange( _glDepthMin, _glDepthMax );
        //    GL.DepthFunc( DepthFunction.Lequal );

        //    // blend on top
        //    GL.Enable( EnableCap.Blend );
        //    GL.MatrixMode( MatrixMode.Projection );
        //    if( _MirrorPlane.normal.Z != 0 )
        //        GL.Scale( 1f, -1, 1 );
        //    else
        //        GL.Scale( -1f, 1, 1 );
        //    GL.CullFace( CullFaceMode.Front );
        //    GL.MatrixMode( MatrixMode.Modelview );

        //    GL.LoadMatrix( ref _BaseWorldMatrix );

        //    GL.Color4( 1, 1, 1, _MirrorAlpha.Value );
        //    var s = Host.Client.cl.worldmodel.textures[_MirrorTextureNum].texturechain;
        //    for( ; s != null; s = s.texturechain )
        //        RenderBrushPoly( s );
        //    Host.Client.cl.worldmodel.textures[_MirrorTextureNum].texturechain = null;
        //    GL.Disable( EnableCap.Blend );
        //    GL.Color4( 1f, 1, 1, 1 );
        //}

        /// <summary>
        /// R_RenderScene
        /// r_refdef must be set before the first call
        /// </summary>
        private void RenderScene( )
        {
            SetupFrame( );

            SetFrustum( );

            SetupGL( );

            World.Occlusion.MarkLeaves( );	// done here so we know if we're in water

            World.Entities.Surfaces.DrawWorld( );		// adds entities to the list

            Host.Sound.ExtraUpdate( );	// don't let sound get messed up if going slow

            World.Entities.DrawEntitiesOnList( );

            Host.Video.Device.DisableMultitexture( );

            World.Lighting.RenderDlights( );

            World.Particles.DrawParticles( Host.Client.cl.time, Host.Client.cl.oldtime, Host.Server.Gravity, Origin, ViewUp, ViewRight, ViewPn );

#if GLTEST
	        Test_Draw ();
#endif
        }

		/// <summary>
		/// R_SetupGL
		/// </summary>
		private void SetupGL( )
        {
            Host.Video.Device.Setup3DScene( Host.Cvars.glCull.Get<Boolean>(), _RefDef, _IsEnvMap );
        }

        /// <summary>
        /// R_SetFrustum
        /// </summary>
        private void SetFrustum( )
        {
            if ( _RefDef.fov_x == 90 )
            {
                // front side is visible
                Frustum[0].normal = ViewPn + ViewRight;
                Frustum[1].normal = ViewPn - ViewRight;

                Frustum[2].normal = ViewPn + ViewUp;
                Frustum[3].normal = ViewPn - ViewUp;
            }
            else
            {
                // rotate VPN right by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out Frustum[0].normal, ref ViewUp, ref ViewPn, -( 90 - _RefDef.fov_x / 2 ) );
                // rotate VPN left by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out Frustum[1].normal, ref ViewUp, ref ViewPn, 90 - _RefDef.fov_x / 2 );
                // rotate VPN up by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out Frustum[2].normal, ref ViewRight, ref ViewPn, 90 - _RefDef.fov_y / 2 );
                // rotate VPN down by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out Frustum[3].normal, ref ViewRight, ref ViewPn, -( 90 - _RefDef.fov_y / 2 ) );
            }

            for ( var i = 0; i < 4; i++ )
            {
                Frustum[i].type = PlaneDef.PLANE_ANYZ;
                Frustum[i].dist = Vector3.Dot( Origin, Frustum[i].normal );
                Frustum[i].signbits = ( Byte )  Frustum[i].SignbitsForPlane();
            }
        }

        /// <summary>
        /// R_SetupFrame
        /// </summary>
        private void SetupFrame( )
        {
            // don't allow cheats in multiplayer
            if ( Host.Client.cl.maxclients > 1 )
                Host.CVars.Set( "r_fullbright", false );

            World.Lighting.UpdateAnimations();

            World.Lighting.FrameCount++;

            // build the transformation matrix for the given view angles
            Origin = _RefDef.vieworg;

            MathLib.AngleVectors( ref _RefDef.viewangles, out ViewPn, out ViewRight, out ViewUp );

            // current viewleaf
            World.Occlusion.SetupFrame( ref Origin );
            Host.View.SetContentsColor( World.Occlusion.ViewLeaf.contents );
            Host.View.CalcBlend( );

            _CacheThrash = false;
            World.Entities.Surfaces.Reset( );
            World.Entities.Reset( );
        }

        /// <summary>
        /// R_Clear
        /// </summary>
        private void Clear( )
        {
            Host.Video.Device.Clear( Host.Video.glZTrick, Host.Cvars.glClear.Get<Single>( ) );
        }

        /// <summary>
        /// R_TimeRefresh_f
        /// For program optimization
        /// </summary>
        private void TimeRefresh_f( CommandMessage msg )
        {
            //GL.DrawBuffer(DrawBufferMode.Front);
            Host.Video.Device.Finish( );

            var start = Timer.GetFloatTime( );
            for ( var i = 0; i < 128; i++ )
            {
                _RefDef.viewangles.Y = ( Single ) ( i / 128.0 * 360.0 );
                RenderView( );
                MainWindow.Instance.Present( );
            }

            Host.Video.Device.Finish( );
            var stop = Timer.GetFloatTime( );
            var time = stop - start;
            Host.Console.Print( "{0:F} seconds ({1:F1} fps)\n", time, 128 / time );

            //GL.DrawBuffer(DrawBufferMode.Back);
            Host.Screen.EndRendering( );
        }

        /// <summary>
        /// R_TextureAnimation
        /// Returns the proper texture for a given time and base texture
        /// </summary>
        public ModelTexture TextureAnimation( ModelTexture t )
        {
            if ( _CurrentEntity.frame != 0 )
            {
                if ( t.alternate_anims != null )
                    t = t.alternate_anims;
            }

            if ( t.anim_total == 0 )
                return t;

            var reletive = ( Int32 ) ( Host.Client.cl.time * 10 ) % t.anim_total;
            var count = 0;
            while ( t.anim_min > reletive || t.anim_max <= reletive )
            {
                t = t.anim_next;
                if ( t == null )
                    Utilities.Error( "R_TextureAnimation: broken cycle" );
                if ( ++count > 100 )
                    Utilities.Error( "R_TextureAnimation: infinite cycle" );
            }

            return t;
        }
    }
}
