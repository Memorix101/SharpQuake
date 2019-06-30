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
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpQuake.Framework;

// refresh.h -- public interface to refresh functions
// gl_rmisc.c
// gl_rmain.c

namespace SharpQuake
{
    /// <summary>
    /// R_functions
    /// </summary>
    static partial class render
    {
        public static refdef_t RefDef
        {
            get
            {
                return _RefDef;
            }
        }

        public static System.Boolean CacheTrash
        {
            get
            {
                return _CacheThrash;
            }
        }

        public static texture_t NoTextureMip
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
        public static Vector3 ViewUp;

        // vup
        public static Vector3 ViewPn;

        // vpn
        public static Vector3 ViewRight;

        // vright
        public static Vector3 Origin;

        private const Single ONE_OVER_16 = 1.0f / 16.0f;

        private const Int32 MAX_LIGHTMAPS = 64;

        private const Int32 BLOCK_WIDTH = 128;
        private const Int32 BLOCK_HEIGHT = 128;

        private static refdef_t _RefDef = new refdef_t(); // refdef_t	r_refdef;
        private static texture_t _NoTextureMip; // r_notexture_mip

        private static CVar _NoRefresh;// = { "r_norefresh", "0" };
        private static CVar _DrawEntities;// = { "r_drawentities", "1" };
        private static CVar _DrawViewModel;// = { "r_drawviewmodel", "1" };
        private static CVar _Speeds;// = { "r_speeds", "0" };
        private static CVar _FullBright;// = { "r_fullbright", "0" };
        private static CVar _LightMap;// = { "r_lightmap", "0" };
        private static CVar _Shadows;// = { "r_shadows", "0" };
        private static CVar _MirrorAlpha;// = { "r_mirroralpha", "1" };
        private static CVar _WaterAlpha;// = { "r_wateralpha", "1" };
        private static CVar _Dynamic;// = { "r_dynamic", "1" };
        private static CVar _NoVis;// = { "r_novis", "0" };

        private static CVar _glFinish;// = { "gl_finish", "0" };
        private static CVar _glClear;// = { "gl_clear", "0" };
        private static CVar _glCull;// = { "gl_cull", "1" };
        private static CVar _glTexSort;// = { "gl_texsort", "1" };
        private static CVar _glSmoothModels;// = { "gl_smoothmodels", "1" };
        private static CVar _glAffineModels;// = { "gl_affinemodels", "0" };
        private static CVar _glPolyBlend;// = { "gl_polyblend", "1" };
        private static CVar _glFlashBlend;// = { "gl_flashblend", "1" };
        private static CVar _glPlayerMip;// = { "gl_playermip", "0" };
        private static CVar _glNoColors;// = { "gl_nocolors", "0" };
        private static CVar _glKeepTJunctions;// = { "gl_keeptjunctions", "0" };
        private static CVar _glReportTJunctions;// = { "gl_reporttjunctions", "0" };
        private static CVar _glDoubleEyes;// = { "gl_doubleeys", "1" };

        private static Int32 _PlayerTextures; // playertextures	// up to 16 color translated skins
        private static System.Boolean _CacheThrash; // r_cache_thrash	// compatability

        // r_origin

        private static Int32[] _LightStyleValue = new Int32[256]; // d_lightstylevalue  // 8.8 fraction of base light value
        private static entity_t _WorldEntity = new entity_t(); // r_worldentity
        private static entity_t _CurrentEntity; // currententity

        private static mleaf_t _ViewLeaf; // r_viewleaf
        private static mleaf_t _OldViewLeaf; // r_oldviewleaf

        private static Int32 _SkyTextureNum; // skytexturenum
        private static Int32 _MirrorTextureNum; // mirrortexturenum	// quake texturenum, not gltexturenum

        private static Int32[,] _Allocated = new Int32[MAX_LIGHTMAPS, BLOCK_WIDTH]; // allocated

        private static Int32 _VisFrameCount; // r_visframecount	// bumped when going to a new PVS
        private static Int32 _FrameCount; // r_framecount		// used for dlight push checking
        private static System.Boolean _MTexEnabled; // mtexenabled
        private static Int32 _BrushPolys; // c_brush_polys
        private static Int32 _AliasPolys; // c_alias_polys
        private static System.Boolean _IsMirror; // mirror
        private static Plane _MirrorPlane; // mirror_plane
        private static Single _glDepthMin; // gldepthmin
        private static Single _glDepthMax; // gldepthmax
        private static Int32 _TrickFrame; // static int trickframe from R_Clear()
        private static Plane[] _Frustum = new Plane[4]; // frustum
        private static System.Boolean _IsEnvMap = false; // envmap	// true during envmap command capture
        private static Matrix4 _WorldMatrix; // r_world_matrix
        private static Matrix4 _BaseWorldMatrix; // r_base_world_matrix
        private static Vector3 _ModelOrg; // modelorg
        private static Vector3 _EntOrigin; // r_entorigin
        private static Single _SpeedScale; // speedscale		// for top sky and bottom sky
        private static Single _ShadeLight; // shadelight
        private static Single _AmbientLight; // ambientlight
        private static Single[] _ShadeDots = anorm_dots.Values[0]; // shadedots
        private static Vector3 _ShadeVector; // shadevector
        private static Int32 _LastPoseNum; // lastposenum
        private static Vector3 _LightSpot; // lightspot

        /// <summary>
        /// R_Init
        /// </summary>
        public static void Init()
        {
            for( var i = 0; i < _Frustum.Length; i++ )
                _Frustum[i] = new Plane();

            Command.Add( "timerefresh", TimeRefresh_f );
            //Cmd.Add("envmap", Envmap_f);
            //Cmd.Add("pointfile", ReadPointFile_f);

            if( _NoRefresh == null )
            {
                _NoRefresh = new CVar( "r_norefresh", "0" );
                _DrawEntities = new CVar( "r_drawentities", "1" );
                _DrawViewModel = new CVar( "r_drawviewmodel", "1" );
                _Speeds = new CVar( "r_speeds", "0" );
                _FullBright = new CVar( "r_fullbright", "0" );
                _LightMap = new CVar( "r_lightmap", "0" );
                _Shadows = new CVar( "r_shadows", "0" );
                _MirrorAlpha = new CVar( "r_mirroralpha", "1" );
                _WaterAlpha = new CVar( "r_wateralpha", "1" );
                _Dynamic = new CVar( "r_dynamic", "1" );
                _NoVis = new CVar( "r_novis", "0" );

                _glFinish = new CVar( "gl_finish", "0" );
                _glClear = new CVar( "gl_clear", "0" );
                _glCull = new CVar( "gl_cull", "1" );
                _glTexSort = new CVar( "gl_texsort", "1" );
                _glSmoothModels = new CVar( "gl_smoothmodels", "1" );
                _glAffineModels = new CVar( "gl_affinemodels", "0" );
                _glPolyBlend = new CVar( "gl_polyblend", "1" );
                _glFlashBlend = new CVar( "gl_flashblend", "1" );
                _glPlayerMip = new CVar( "gl_playermip", "0" );
                _glNoColors = new CVar( "gl_nocolors", "0" );
                _glKeepTJunctions = new CVar( "gl_keeptjunctions", "0" );
                _glReportTJunctions = new CVar( "gl_reporttjunctions", "0" );
                _glDoubleEyes = new CVar( "gl_doubleeys", "1" );
            }

            if( vid.glMTexable )
                CVar.Set( "gl_texsort", 0.0f );

            InitParticles();
            InitParticleTexture();

            // reserve 16 textures
            _PlayerTextures = Drawer.GenerateTextureNumberRange( 16 );
        }

        // R_InitTextures
        public static void InitTextures()
        {
            // create a simple checkerboard texture for the default
            _NoTextureMip = new texture_t();
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

            Byte[] dest = _NoTextureMip.pixels;
            for( var m = 0; m < 4; m++ )
            {
                offset = _NoTextureMip.offsets[m];
                for( var y = 0; y < ( 16 >> m ); y++ )
                    for( var x = 0; x < ( 16 >> m ); x++ )
                    {
                        if( ( y < ( 8 >> m ) ) ^ ( x < ( 8 >> m ) ) )
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
        public static void RenderView()
        {
            if( _NoRefresh.Value != 0 )
                return;

            if( _WorldEntity.model == null || client.cl.worldmodel == null )
                Utilities.Error( "R_RenderView: NULL worldmodel" );

            Double time1 = 0;
            if( _Speeds.Value != 0 )
            {
                GL.Finish();
                time1 = sys.GetFloatTime();
                _BrushPolys = 0;
                _AliasPolys = 0;
            }

            _IsMirror = false;

            if( _glFinish.Value != 0 )
                GL.Finish();

            Clear();

            // render normal view

            RenderScene();
            DrawViewModel();
            DrawWaterSurfaces();

            // render mirror view
            Mirror();

            PolyBlend();

            if( _Speeds.Value != 0 )
            {
                var time2 = sys.GetFloatTime();
                Con.Print( "{0,3} ms  {1,4} wpoly {2,4} epoly\n", ( Int32 ) ( ( time2 - time1 ) * 1000 ), _BrushPolys, _AliasPolys );
            }
        }

        /// <summary>
        /// R_RemoveEfrags
        /// Call when removing an object from the world or moving it to another position
        /// </summary>
        public static void RemoveEfrags( entity_t ent )
        {
            efrag_t ef = ent.efrag;

            while( ef != null )
            {
                mleaf_t leaf = ef.leaf;
                while( true )
                {
                    efrag_t walk = leaf.efrags;
                    if( walk == null )
                        break;
                    if( walk == ef )
                    {
                        // remove this fragment
                        leaf.efrags = ef.leafnext;
                        break;
                    }
                    else
                        leaf = (mleaf_t)( Object ) walk.leafnext;
                }

                efrag_t old = ef;
                ef = ef.entnext;

                // put it on the free list
                old.entnext = client.cl.free_efrags;
                client.cl.free_efrags = old;
            }

            ent.efrag = null;
        }

        /// <summary>
        /// R_TranslatePlayerSkin
        /// Translates a skin texture by the per-player color lookup
        /// </summary>
        public static void TranslatePlayerSkin( Int32 playernum )
        {
            DisableMultitexture();

            var top = client.cl.scores[playernum].colors & 0xf0;
            var bottom = ( client.cl.scores[playernum].colors & 15 ) << 4;

            Byte[] translate = new Byte[256];
            for( var i = 0; i < 256; i++ )
                translate[i] = ( Byte ) i;

            for( var i = 0; i < 16; i++ )
            {
                if( top < 128 )	// the artists made some backwards ranges.  sigh.
                    translate[TOP_RANGE + i] = ( Byte ) ( top + i );
                else
                    translate[TOP_RANGE + i] = ( Byte ) ( top + 15 - i );

                if( bottom < 128 )
                    translate[BOTTOM_RANGE + i] = ( Byte ) ( bottom + i );
                else
                    translate[BOTTOM_RANGE + i] = ( Byte ) ( bottom + 15 - i );
            }

            //
            // locate the original skin pixels
            //
            _CurrentEntity = client.Entities[1 + playernum];
            model_t model = _CurrentEntity.model;
            if( model == null )
                return;		// player doesn't have a model yet
            if( model.type != modtype_t.mod_alias )
                return; // only translate skins on alias models

            aliashdr_t paliashdr = Mod.GetExtraData( model );
            var s = paliashdr.skinwidth * paliashdr.skinheight;
            if( ( s & 3 ) != 0 )
                Utilities.Error( "R_TranslateSkin: s&3" );

            Byte[] original;
            if( _CurrentEntity.skinnum < 0 || _CurrentEntity.skinnum >= paliashdr.numskins )
            {
                Con.Print( "({0}): Invalid player skin #{1}\n", playernum, _CurrentEntity.skinnum );
                original = ( Byte[])paliashdr.texels[0];// (byte *)paliashdr + paliashdr.texels[0];
            }
            else
                original = ( Byte[])paliashdr.texels[_CurrentEntity.skinnum];

            var inwidth = paliashdr.skinwidth;
            var inheight = paliashdr.skinheight;

            // because this happens during gameplay, do it fast
            // instead of sending it through gl_upload 8
            Drawer.Bind( _PlayerTextures + playernum );

            var scaled_width = ( Int32 ) ( Drawer.glMaxSize < 512 ? Drawer.glMaxSize : 512 );
            var scaled_height = ( Int32 ) ( Drawer.glMaxSize < 256 ? Drawer.glMaxSize : 256 );

            // allow users to crunch sizes down even more if they want
            scaled_width >>= ( Int32 ) _glPlayerMip.Value;
            scaled_height >>= ( Int32 ) _glPlayerMip.Value;

            UInt32 fracstep, frac;
            Int32 destOffset;

            UInt32[] translate32 = new UInt32[256];
            for( var i = 0; i < 256; i++ )
                translate32[i] = vid.Table8to24[translate[i]];

            UInt32[] dest = new UInt32[512 * 256];
            destOffset = 0;
            fracstep = ( UInt32 ) ( inwidth * 0x10000 / scaled_width );
            for( var i = 0; i < scaled_height; i++, destOffset += scaled_width )
            {
                var srcOffset = inwidth * ( i * inheight / scaled_height );
                frac = fracstep >> 1;
                for( var j = 0; j < scaled_width; j += 4 )
                {
                    dest[destOffset + j] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 1] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 2] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 3] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                }
            }
            GCHandle handle = GCHandle.Alloc( dest, GCHandleType.Pinned );
            try
            {
                GL.TexImage2D( TextureTarget.Texture2D, 0, Drawer.SolidFormat, scaled_width, scaled_height, 0,
                     PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject() );
            }
            finally
            {
                handle.Free();
            }
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
            Drawer.SetTextureFilters( TextureMinFilter.Linear, TextureMagFilter.Linear );
        }

        /// <summary>
        /// GL_DisableMultitexture
        /// </summary>
        public static void DisableMultitexture()
        {
            if( _MTexEnabled )
            {
                GL.Disable( EnableCap.Texture2D );
                Drawer.SelectTexture( MTexTarget.TEXTURE0_SGIS );
                _MTexEnabled = false;
            }
        }

        /// <summary>
        /// GL_EnableMultitexture
        /// </summary>
        public static void EnableMultitexture()
        {
            if( vid.glMTexable )
            {
                Drawer.SelectTexture( MTexTarget.TEXTURE1_SGIS );
                GL.Enable( EnableCap.Texture2D );
                _MTexEnabled = true;
            }
        }

        /// <summary>
        /// R_NewMap
        /// </summary>
        public static void NewMap()
        {
            for( var i = 0; i < 256; i++ )
                _LightStyleValue[i] = 264;		// normal light value

            _WorldEntity.Clear();
            _WorldEntity.model = client.cl.worldmodel;

            // clear out efrags in case the level hasn't been reloaded
            // FIXME: is this one short?
            for( var i = 0; i < client.cl.worldmodel.numleafs; i++ )
                client.cl.worldmodel.leafs[i].efrags = null;

            _ViewLeaf = null;
            ClearParticles();

            BuildLightMaps();

            // identify sky texture
            _SkyTextureNum = -1;
            _MirrorTextureNum = -1;
            model_t world = client.cl.worldmodel;
            for( var i = 0; i < world.numtextures; i++ )
            {
                if( world.textures[i] == null )
                    continue;
                if( world.textures[i].name != null )
                {
                    if( world.textures[i].name.StartsWith( "sky" ) )
                        _SkyTextureNum = i;
                    if( world.textures[i].name.StartsWith( "window02_1" ) )
                        _MirrorTextureNum = i;
                }
                world.textures[i].texturechain = null;
            }
        }

        /// <summary>
        /// R_PolyBlend
        /// </summary>
        private static void PolyBlend()
        {
            if( _glPolyBlend.Value == 0 )
                return;

            if( view.Blend.A == 0 )
                return;

            DisableMultitexture();

            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.Blend );
            GL.Disable( EnableCap.DepthTest );
            GL.Disable( EnableCap.Texture2D );

            GL.LoadIdentity();

            GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up

            GL.Color4( view.Blend );
            GL.Begin( PrimitiveType.Quads );
            GL.Vertex3( 10f, 100, 100 );
            GL.Vertex3( 10f, -100, 100 );
            GL.Vertex3( 10f, -100, -100 );
            GL.Vertex3( 10f, 100, -100 );
            GL.End();

            GL.Disable( EnableCap.Blend );
            GL.Enable( EnableCap.Texture2D );
            GL.Enable( EnableCap.AlphaTest );
        }

        /// <summary>
        /// R_Mirror
        /// </summary>
        private static void Mirror()
        {
            if( !_IsMirror )
                return;

            _BaseWorldMatrix = _WorldMatrix;

            var d = Vector3.Dot( _RefDef.vieworg, _MirrorPlane.normal ) - _MirrorPlane.dist;
            _RefDef.vieworg += _MirrorPlane.normal * -2 * d;

            d = Vector3.Dot( render.ViewPn, _MirrorPlane.normal );
            render.ViewPn += _MirrorPlane.normal * -2 * d;

            _RefDef.viewangles = new Vector3( ( Single ) ( Math.Asin( render.ViewPn.Z ) / Math.PI * 180.0 ),
                ( Single ) ( Math.Atan2( render.ViewPn.Y, render.ViewPn.X ) / Math.PI * 180.0 ),
                -_RefDef.viewangles.Z );

            entity_t ent = client.ViewEntity;
            if( client.NumVisEdicts < client.MAX_VISEDICTS )
            {
                client.VisEdicts[client.NumVisEdicts] = ent;
                client.NumVisEdicts++;
            }

            _glDepthMin = 0.5f;
            _glDepthMax = 1;
            GL.DepthRange( _glDepthMin, _glDepthMax );
            GL.DepthFunc( DepthFunction.Lequal );

            RenderScene();
            DrawWaterSurfaces();

            _glDepthMin = 0;
            _glDepthMax = 0.5f;
            GL.DepthRange( _glDepthMin, _glDepthMax );
            GL.DepthFunc( DepthFunction.Lequal );

            // blend on top
            GL.Enable( EnableCap.Blend );
            GL.MatrixMode( MatrixMode.Projection );
            if( _MirrorPlane.normal.Z != 0 )
                GL.Scale( 1f, -1, 1 );
            else
                GL.Scale( -1f, 1, 1 );
            GL.CullFace( CullFaceMode.Front );
            GL.MatrixMode( MatrixMode.Modelview );

            GL.LoadMatrix( ref _BaseWorldMatrix );

            GL.Color4( 1, 1, 1, _MirrorAlpha.Value );
            msurface_t s = client.cl.worldmodel.textures[_MirrorTextureNum].texturechain;
            for( ; s != null; s = s.texturechain )
                RenderBrushPoly( s );
            client.cl.worldmodel.textures[_MirrorTextureNum].texturechain = null;
            GL.Disable( EnableCap.Blend );
            GL.Color4( 1f, 1, 1, 1 );
        }

        /// <summary>
        /// R_DrawViewModel
        /// </summary>
        private static void DrawViewModel()
        {
            if( _DrawViewModel.Value == 0 )
                return;

            if( chase.IsActive )
                return;

            if( _IsEnvMap )
                return;

            if( _DrawEntities.Value == 0 )
                return;

            if( client.cl.HasItems( QItems.IT_INVISIBILITY ) )
                return;

            if( client.cl.stats[QStats.STAT_HEALTH] <= 0 )
                return;

            _CurrentEntity = client.ViewEnt;
            if( _CurrentEntity.model == null )
                return;

            var j = LightPoint( ref _CurrentEntity.origin );

            if( j < 24 )
                j = 24;		// allways give some light on gun
            _AmbientLight = j;
            _ShadeLight = j;

            // add dynamic lights
            for( var lnum = 0; lnum < client.MAX_DLIGHTS; lnum++ )
            {
                dlight_t dl = client.DLights[lnum];
                if( dl.radius == 0 )
                    continue;
                if( dl.die < client.cl.time )
                    continue;

                Vector3 dist = _CurrentEntity.origin - dl.origin;
                var add = dl.radius - dist.Length;
                if( add > 0 )
                    _AmbientLight += add;
            }

            // hack the depth range to prevent view model from poking into walls
            GL.DepthRange( _glDepthMin, _glDepthMin + 0.3f * ( _glDepthMax - _glDepthMin ) );
            DrawAliasModel( _CurrentEntity );
            GL.DepthRange( _glDepthMin, _glDepthMax );
        }

        /// <summary>
        /// R_RenderScene
        /// r_refdef must be set before the first call
        /// </summary>
        private static void RenderScene()
        {
            SetupFrame();

            SetFrustum();

            SetupGL();

            MarkLeaves();	// done here so we know if we're in water

            DrawWorld();		// adds static entities to the list

            snd.ExtraUpdate();	// don't let sound get messed up if going slow

            DrawEntitiesOnList();

            DisableMultitexture();

            RenderDlights();

            DrawParticles();

#if GLTEST
	        Test_Draw ();
#endif
        }

        /// <summary>
        /// R_DrawEntitiesOnList
        /// </summary>
        private static void DrawEntitiesOnList()
        {
            if( _DrawEntities.Value == 0 )
                return;

            // draw sprites seperately, because of alpha blending
            for( var i = 0; i < client.NumVisEdicts; i++ )
            {
                _CurrentEntity = client.VisEdicts[i];

                switch( _CurrentEntity.model.type )
                {
                    case modtype_t.mod_alias:
                        DrawAliasModel( _CurrentEntity );
                        break;

                    case modtype_t.mod_brush:
                        DrawBrushModel( _CurrentEntity );
                        break;

                    default:
                        break;
                }
            }

            for( var i = 0; i < client.NumVisEdicts; i++ )
            {
                _CurrentEntity = client.VisEdicts[i];

                switch( _CurrentEntity.model.type )
                {
                    case modtype_t.mod_sprite:
                        DrawSpriteModel( _CurrentEntity );
                        break;
                }
            }
        }

        /// <summary>
        /// R_DrawSpriteModel
        /// </summary>
        private static void DrawSpriteModel( entity_t e )
        {
            // don't even bother culling, because it's just a single
            // polygon without a surface cache
            mspriteframe_t frame = GetSpriteFrame( e );
            msprite_t psprite = (msprite_t)e.model.cache.data; // Uze: changed from _CurrentEntity to e

            Vector3 v_forward, right, up;
            if( psprite.type == SPR.SPR_ORIENTED )
            {
                // bullet marks on walls
                MathLib.AngleVectors( ref e.angles, out v_forward, out right, out up ); // Uze: changed from _CurrentEntity to e
            }
            else
            {	// normal sprite
                up = render.ViewUp;// vup;
                right = render.ViewRight;// vright;
            }

            GL.Color3( 1f, 1, 1 );

            DisableMultitexture();

            Drawer.Bind( frame.gl_texturenum );

            GL.Enable( EnableCap.AlphaTest );
            GL.Begin( PrimitiveType.Quads );

            GL.TexCoord2( 0f, 1 );
            Vector3 point = e.origin + up * frame.down + right * frame.left;
            GL.Vertex3( point );

            GL.TexCoord2( 0f, 0 );
            point = e.origin + up * frame.up + right * frame.left;
            GL.Vertex3( point );

            GL.TexCoord2( 1f, 0 );
            point = e.origin + up * frame.up + right * frame.right;
            GL.Vertex3( point );

            GL.TexCoord2( 1f, 1 );
            point = e.origin + up * frame.down + right * frame.right;
            GL.Vertex3( point );

            GL.End();
            GL.Disable( EnableCap.AlphaTest );
        }

        /// <summary>
        /// R_GetSpriteFrame
        /// </summary>
        private static mspriteframe_t GetSpriteFrame( entity_t currententity )
        {
            msprite_t psprite = (msprite_t)currententity.model.cache.data;
            var frame = currententity.frame;

            if( ( frame >= psprite.numframes ) || ( frame < 0 ) )
            {
                Con.Print( "R_DrawSprite: no such frame {0}\n", frame );
                frame = 0;
            }

            mspriteframe_t pspriteframe;
            if( psprite.frames[frame].type == spriteframetype_t.SPR_SINGLE )
            {
                pspriteframe = (mspriteframe_t)psprite.frames[frame].frameptr;
            }
            else
            {
                mspritegroup_t pspritegroup = (mspritegroup_t)psprite.frames[frame].frameptr;
                Single[] pintervals = pspritegroup.intervals;
                var numframes = pspritegroup.numframes;
                var fullinterval = pintervals[numframes - 1];
                var time = ( Single ) client.cl.time + currententity.syncbase;

                // when loading in Mod_LoadSpriteGroup, we guaranteed all interval values
                // are positive, so we don't have to worry about division by 0
                var targettime = time - ( ( Int32 ) ( time / fullinterval ) ) * fullinterval;
                Int32 i;
                for( i = 0; i < ( numframes - 1 ); i++ )
                {
                    if( pintervals[i] > targettime )
                        break;
                }
                pspriteframe = pspritegroup.frames[i];
            }

            return pspriteframe;
        }

        /// <summary>
        /// R_DrawAliasModel
        /// </summary>
        private static void DrawAliasModel( entity_t e )
        {
            model_t clmodel = _CurrentEntity.model;
            Vector3 mins = _CurrentEntity.origin + clmodel.mins;
            Vector3 maxs = _CurrentEntity.origin + clmodel.maxs;

            if( CullBox( ref mins, ref maxs ) )
                return;

            _EntOrigin = _CurrentEntity.origin;
            _ModelOrg = render.Origin - _EntOrigin;

            //
            // get lighting information
            //

            _AmbientLight = _ShadeLight = LightPoint( ref _CurrentEntity.origin );

            // allways give the gun some light
            if( e == client.cl.viewent && _AmbientLight < 24 )
                _AmbientLight = _ShadeLight = 24;

            for( var lnum = 0; lnum < client.MAX_DLIGHTS; lnum++ )
            {
                if( client.DLights[lnum].die >= client.cl.time )
                {
                    Vector3 dist = _CurrentEntity.origin - client.DLights[lnum].origin;
                    var add = client.DLights[lnum].radius - dist.Length;
                    if( add > 0 )
                    {
                        _AmbientLight += add;
                        //ZOID models should be affected by dlights as well
                        _ShadeLight += add;
                    }
                }
            }

            // clamp lighting so it doesn't overbright as much
            if( _AmbientLight > 128 )
                _AmbientLight = 128;
            if( _AmbientLight + _ShadeLight > 192 )
                _ShadeLight = 192 - _AmbientLight;

            // ZOID: never allow players to go totally black
            var playernum = Array.IndexOf( client.Entities, _CurrentEntity, 0, client.cl.maxclients );
            if( playernum >= 1 )// && i <= cl.maxclients)
                if( _AmbientLight < 8 )
                    _AmbientLight = _ShadeLight = 8;

            // HACK HACK HACK -- no fullbright colors, so make torches full light
            if( clmodel.name == "progs/flame2.mdl" || clmodel.name == "progs/flame.mdl" )
                _AmbientLight = _ShadeLight = 256;

            _ShadeDots = anorm_dots.Values[( ( Int32 ) ( e.angles.Y * ( anorm_dots.SHADEDOT_QUANT / 360.0 ) ) ) & ( anorm_dots.SHADEDOT_QUANT - 1 )];
            _ShadeLight = _ShadeLight / 200.0f;

            var an = e.angles.Y / 180.0 * Math.PI;
            _ShadeVector.X = ( Single ) Math.Cos( -an );
            _ShadeVector.Y = ( Single ) Math.Sin( -an );
            _ShadeVector.Z = 1;
            MathLib.Normalize( ref _ShadeVector );

            //
            // locate the proper data
            //
            aliashdr_t paliashdr = Mod.GetExtraData( _CurrentEntity.model );

            _AliasPolys += paliashdr.numtris;

            //
            // draw all the triangles
            //

            DisableMultitexture();

            GL.PushMatrix();
            RotateForEntity( e );
            if( clmodel.name == "progs/eyes.mdl" && _glDoubleEyes.Value != 0 )
            {
                Vector3 v = paliashdr.scale_origin;
                v.Z -= ( 22 + 8 );
                GL.Translate( v );
                // double size of eyes, since they are really hard to see in gl
                GL.Scale( paliashdr.scale * 2.0f );
            }
            else
            {
                GL.Translate( paliashdr.scale_origin );
                GL.Scale( paliashdr.scale );
            }

            var anim = ( Int32 ) ( client.cl.time * 10 ) & 3;
            Drawer.Bind( paliashdr.gl_texturenum[_CurrentEntity.skinnum, anim] );

            // we can't dynamically colormap textures, so they are cached
            // seperately for the players.  Heads are just uncolored.
            if( _CurrentEntity.colormap != Scr.vid.colormap && _glNoColors.Value == 0 && playernum >= 1 )
            {
                Drawer.Bind( _PlayerTextures - 1 + playernum );
            }

            if( _glSmoothModels.Value != 0 )
                GL.ShadeModel( ShadingModel.Smooth );

            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );

            if( _glAffineModels.Value != 0 )
                GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Fastest );

            SetupAliasFrame( _CurrentEntity.frame, paliashdr );

            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );

            GL.ShadeModel( ShadingModel.Flat );
            if( _glAffineModels.Value != 0 )
                GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Nicest );

            GL.PopMatrix();

            if( _Shadows.Value != 0 )
            {
                GL.PushMatrix();
                RotateForEntity( e );
                GL.Disable( EnableCap.Texture2D );
                GL.Enable( EnableCap.Blend );
                GL.Color4( 0, 0, 0, 0.5f );
                DrawAliasShadow( paliashdr, _LastPoseNum );
                GL.Enable( EnableCap.Texture2D );
                GL.Disable( EnableCap.Blend );
                GL.Color4( 1f, 1, 1, 1 );
                GL.PopMatrix();
            }
        }

        /// <summary>
        /// GL_DrawAliasShadow
        /// </summary>
        private static void DrawAliasShadow( aliashdr_t paliashdr, Int32 posenum )
        {
            var lheight = _CurrentEntity.origin.Z - _LightSpot.Z;
            Single height = 0;
            trivertx_t[] verts = paliashdr.posedata;
            var voffset = posenum * paliashdr.poseverts;
            Int32[] order = paliashdr.commands;

            height = -lheight + 1.0f;
            var orderOffset = 0;

            while( true )
            {
                // get the vertex count and primitive type
                var count = order[orderOffset++];
                if( count == 0 )
                    break;		// done

                if( count < 0 )
                {
                    count = -count;
                    GL.Begin( PrimitiveType.TriangleFan );
                }
                else
                    GL.Begin( PrimitiveType.TriangleStrip );

                do
                {
                    // texture coordinates come from the draw list
                    // (skipped for shadows) glTexCoord2fv ((float *)order);
                    orderOffset += 2;

                    // normals and vertexes come from the frame list
                    Vector3 point = new Vector3(
                        verts[voffset].v[0] * paliashdr.scale.X + paliashdr.scale_origin.X,
                        verts[voffset].v[1] * paliashdr.scale.Y + paliashdr.scale_origin.Y,
                        verts[voffset].v[2] * paliashdr.scale.Z + paliashdr.scale_origin.Z
                    );

                    point.X -= _ShadeVector.X * ( point.Z + lheight );
                    point.Y -= _ShadeVector.Y * ( point.Z + lheight );
                    point.Z = height;

                    GL.Vertex3( point );

                    voffset++;
                } while( --count > 0 );

                GL.End();
            }
        }

        /// <summary>
        /// R_SetupAliasFrame
        /// </summary>
        private static void SetupAliasFrame( Int32 frame, aliashdr_t paliashdr )
        {
            if( ( frame >= paliashdr.numframes ) || ( frame < 0 ) )
            {
                Con.DPrint( "R_AliasSetupFrame: no such frame {0}\n", frame );
                frame = 0;
            }

            var pose = paliashdr.frames[frame].firstpose;
            var numposes = paliashdr.frames[frame].numposes;

            if( numposes > 1 )
            {
                var interval = paliashdr.frames[frame].interval;
                pose += ( Int32 ) ( client.cl.time / interval ) % numposes;
            }

            DrawAliasFrame( paliashdr, pose );
        }

        /// <summary>
        /// GL_DrawAliasFrame
        /// </summary>
        private static void DrawAliasFrame( aliashdr_t paliashdr, Int32 posenum )
        {
            _LastPoseNum = posenum;

            trivertx_t[] verts = paliashdr.posedata;
            var vertsOffset = posenum * paliashdr.poseverts;
            Int32[] order = paliashdr.commands;
            var orderOffset = 0;

            while( true )
            {
                // get the vertex count and primitive type
                var count = order[orderOffset++];
                if( count == 0 )
                    break;		// done

                if( count < 0 )
                {
                    count = -count;
                    GL.Begin( PrimitiveType.TriangleFan );
                }
                else
                    GL.Begin( PrimitiveType.TriangleStrip );

                Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
                do
                {
                    // texture coordinates come from the draw list
                    u1.i0 = order[orderOffset + 0];
                    u2.i0 = order[orderOffset + 1];
                    orderOffset += 2;
                    GL.TexCoord2( u1.f0, u2.f0 );

                    // normals and vertexes come from the frame list
                    var l = _ShadeDots[verts[vertsOffset].lightnormalindex] * _ShadeLight;
                    GL.Color3( l, l, l );
                    GL.Vertex3( ( Single ) verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] );
                    vertsOffset++;
                } while( --count > 0 );
                GL.End();
            }
        }

        /// <summary>
        /// R_RotateForEntity
        /// </summary>
        private static void RotateForEntity( entity_t e )
        {
            GL.Translate( e.origin );

            GL.Rotate( e.angles.Y, 0, 0, 1 );
            GL.Rotate( -e.angles.X, 0, 1, 0 );
            GL.Rotate( e.angles.Z, 1, 0, 0 );
        }

        /// <summary>
        /// R_SetupGL
        /// </summary>
        private static void SetupGL()
        {
            //
            // set up viewpoint
            //
            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity();
            var x = _RefDef.vrect.x * Scr.glWidth / Scr.vid.width;
            var x2 = ( _RefDef.vrect.x + _RefDef.vrect.width ) * Scr.glWidth / Scr.vid.width;
            var y = ( Scr.vid.height - _RefDef.vrect.y ) * Scr.glHeight / Scr.vid.height;
            var y2 = ( Scr.vid.height - ( _RefDef.vrect.y + _RefDef.vrect.height ) ) * Scr.glHeight / Scr.vid.height;

            // fudge around because of frac screen scale
            if( x > 0 )
                x--;
            if( x2 < Scr.glWidth )
                x2++;
            if( y2 < 0 )
                y2--;
            if( y < Scr.glHeight )
                y++;

            var w = x2 - x;
            var h = y - y2;

            if( _IsEnvMap )
            {
                x = y2 = 0;
                w = h = 256;
            }

            GL.Viewport( Scr.glX + x, Scr.glY + y2, w, h );
            var screenaspect = ( Single ) _RefDef.vrect.width / _RefDef.vrect.height;
            MYgluPerspective( _RefDef.fov_y, screenaspect, 4, 4096 );

            if( _IsMirror )
            {
                if( _MirrorPlane.normal.Z != 0 )
                    GL.Scale( 1f, -1f, 1f );
                else
                    GL.Scale( -1f, 1f, 1f );
                GL.CullFace( CullFaceMode.Back );
            }
            else
                GL.CullFace( CullFaceMode.Front );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity();

            GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up
            GL.Rotate( -_RefDef.viewangles.Z, 1, 0, 0 );
            GL.Rotate( -_RefDef.viewangles.X, 0, 1, 0 );
            GL.Rotate( -_RefDef.viewangles.Y, 0, 0, 1 );
            GL.Translate( -_RefDef.vieworg.X, -_RefDef.vieworg.Y, -_RefDef.vieworg.Z );

            GL.GetFloat( GetPName.ModelviewMatrix, out _WorldMatrix );

            //
            // set drawing parms
            //
            if( _glCull.Value != 0 )
                GL.Enable( EnableCap.CullFace );
            else
                GL.Disable( EnableCap.CullFace );

            GL.Disable( EnableCap.Blend );
            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.DepthTest );
        }

        private static void MYgluPerspective( Double fovy, Double aspect, Double zNear, Double zFar )
        {
            var ymax = zNear * Math.Tan( fovy * Math.PI / 360.0 );
            var ymin = -ymax;

            var xmin = ymin * aspect;
            var xmax = ymax * aspect;

            GL.Frustum( xmin, xmax, ymin, ymax, zNear, zFar );
        }

        /// <summary>
        /// R_SetFrustum
        /// </summary>
        private static void SetFrustum()
        {
            if( _RefDef.fov_x == 90 )
            {
                // front side is visible
                _Frustum[0].normal = render.ViewPn + render.ViewRight;
                _Frustum[1].normal = render.ViewPn - render.ViewRight;

                _Frustum[2].normal = render.ViewPn + render.ViewUp;
                _Frustum[3].normal = render.ViewPn - render.ViewUp;
            }
            else
            {
                // rotate VPN right by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[0].normal, ref render.ViewUp, ref render.ViewPn, -( 90 - _RefDef.fov_x / 2 ) );
                // rotate VPN left by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[1].normal, ref render.ViewUp, ref render.ViewPn, 90 - _RefDef.fov_x / 2 );
                // rotate VPN up by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[2].normal, ref render.ViewRight, ref render.ViewPn, 90 - _RefDef.fov_y / 2 );
                // rotate VPN down by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[3].normal, ref render.ViewRight, ref render.ViewPn, -( 90 - _RefDef.fov_y / 2 ) );
            }

            for( var i = 0; i < 4; i++ )
            {
                _Frustum[i].type = Planes.PLANE_ANYZ;
                _Frustum[i].dist = Vector3.Dot( render.Origin, _Frustum[i].normal );
                _Frustum[i].signbits = ( Byte ) SignbitsForPlane( _Frustum[i] );
            }
        }

        private static Int32 SignbitsForPlane( Plane p )
        {
            // for fast box on planeside test
            var bits = 0;
            if( p.normal.X < 0 )
                bits |= 1 << 0;
            if( p.normal.Y < 0 )
                bits |= 1 << 1;
            if( p.normal.Z < 0 )
                bits |= 1 << 2;
            return bits;
        }

        /// <summary>
        /// R_SetupFrame
        /// </summary>
        private static void SetupFrame()
        {
            // don't allow cheats in multiplayer
            if( client.cl.maxclients > 1 )
                CVar.Set( "r_fullbright", "0" );

            AnimateLight();

            _FrameCount++;

            // build the transformation matrix for the given view angles
            render.Origin = _RefDef.vieworg;

            MathLib.AngleVectors( ref _RefDef.viewangles, out ViewPn, out ViewRight, out ViewUp );

            // current viewleaf
            _OldViewLeaf = _ViewLeaf;
            _ViewLeaf = Mod.PointInLeaf( ref render.Origin, client.cl.worldmodel );

            view.SetContentsColor( _ViewLeaf.contents );
            view.CalcBlend();

            _CacheThrash = false;
            _BrushPolys = 0;
            _AliasPolys = 0;
        }

        /// <summary>
        /// R_Clear
        /// </summary>
        private static void Clear()
        {
            if( _MirrorAlpha.Value != 1.0 )
            {
                if( _glClear.Value != 0 )
                    GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
                else
                    GL.Clear( ClearBufferMask.DepthBufferBit );
                _glDepthMin = 0;
                _glDepthMax = 0.5f;
                GL.DepthFunc( DepthFunction.Lequal );
            }
            else if( vid.glZTrick )
            {
                if( _glClear.Value != 0 )
                    GL.Clear( ClearBufferMask.ColorBufferBit );

                _TrickFrame++;
                if( ( _TrickFrame & 1 ) != 0 )
                {
                    _glDepthMin = 0;
                    _glDepthMax = 0.49999f;
                    GL.DepthFunc( DepthFunction.Lequal );
                }
                else
                {
                    _glDepthMin = 1;
                    _glDepthMax = 0.5f;
                    GL.DepthFunc( DepthFunction.Gequal );
                }
            }
            else
            {
                if( _glClear.Value != 0 )
                {
                    GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
                    // Uze
                    sbar.Changed();
                }
                else
                    GL.Clear( ClearBufferMask.DepthBufferBit );

                _glDepthMin = 0;
                _glDepthMax = 1;
                GL.DepthFunc( DepthFunction.Lequal );
            }

            GL.DepthRange( _glDepthMin, _glDepthMax );
        }

        /// <summary>
        /// R_TimeRefresh_f
        /// For program optimization
        /// </summary>
        private static void TimeRefresh_f()
        {
            //GL.DrawBuffer(DrawBufferMode.Front);
            GL.Finish();

            var start = sys.GetFloatTime();
            for( var i = 0; i < 128; i++ )
            {
                _RefDef.viewangles.Y = ( Single ) ( i / 128.0 * 360.0 );
                RenderView();
                MainWindow.Instance.SwapBuffers();
            }

            GL.Finish();
            var stop = sys.GetFloatTime();
            var time = stop - start;
            Con.Print( "{0:F} seconds ({1:F1} fps)\n", time, 128 / time );

            //GL.DrawBuffer(DrawBufferMode.Back);
            Scr.EndRendering();
        }

        /// <summary>
        /// R_CullBox
        /// Returns true if the box is completely outside the frustom
        /// </summary>
        private static System.Boolean CullBox( ref Vector3 mins, ref Vector3 maxs )
        {
            for( var i = 0; i < 4; i++ )
            {
                if( MathLib.BoxOnPlaneSide( ref mins, ref maxs, _Frustum[i] ) == 2 )
                    return true;
            }
            return false;
        }
    }

    internal class efrag_t
    {
        public mleaf_t leaf;
        public efrag_t leafnext;
        public entity_t entity;
        public efrag_t entnext;

        public void Clear()
        {
            this.leaf = null;
            this.leafnext = null;
            this.entity = null;
            this.entnext = null;
        }
    } // efrag_t;

    internal class entity_t
    {
        public System.Boolean forcelink;		// model changed
        public Int32 update_type;
        public EntityState baseline;		// to fill in defaults in updates
        public Double msgtime;		// time of last update
        public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)
        public Vector3 origin;
        public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
        public Vector3 angles;
        public model_t model;			// NULL = no model
        public efrag_t efrag;			// linked list of efrags
        public Int32 frame;
        public Single syncbase;		// for client-side animations
        public Byte[] colormap;
        public Int32 effects;		// light, particals, etc
        public Int32 skinnum;		// for Alias models
        public Int32 visframe;		// last frame this entity was
        //  found in an active leaf

        public Int32 dlightframe;	// dynamic lighting
        public Int32 dlightbits;

        // FIXME: could turn these into a union
        public Int32 trivial_accept;

        public mnode_t topnode;		// for bmodels, first world node
        //  that splits bmodel, or NULL if
        //  not split

        public void Clear()
        {
            this.forcelink = false;
            this.update_type = 0;

            this.baseline = EntityState.Empty;

            this.msgtime = 0;
            this.msg_origins[0] = Vector3.Zero;
            this.msg_origins[1] = Vector3.Zero;

            this.origin = Vector3.Zero;
            this.msg_angles[0] = Vector3.Zero;
            this.msg_angles[1] = Vector3.Zero;
            this.angles = Vector3.Zero;
            this.model = null;
            this.efrag = null;
            this.frame = 0;
            this.syncbase = 0;
            this.colormap = null;
            this.effects = 0;
            this.skinnum = 0;
            this.visframe = 0;

            this.dlightframe = 0;
            this.dlightbits = 0;

            this.trivial_accept = 0;
            this.topnode = null;
        }

        public entity_t()
        {
            msg_origins = new Vector3[2];
            msg_angles = new Vector3[2];
        }
    } // entity_t;

    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    internal class refdef_t
    {
        public vrect_t vrect;				// subwindow in video for refresh
        public Vector3 vieworg;
        public Vector3 viewangles;
        public Single fov_x, fov_y;
    } // refdef_t;
}
