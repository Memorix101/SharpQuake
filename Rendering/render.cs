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
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.Rendering.Models;
using SharpQuake.Game.Rendering.Textures;
using SharpQuake.Game.World;
using SharpQuake.Renderer;
using SharpQuake.Renderer.Models;
using SharpQuake.Renderer.Textures;

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

        public System.Boolean CacheTrash
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

        private CVar _NoRefresh;// = { "r_norefresh", "0" };
        private CVar _DrawEntities;// = { "r_drawentities", "1" };
        private CVar _DrawViewModel;// = { "r_drawviewmodel", "1" };
        private CVar _Speeds;// = { "r_speeds", "0" };
        private CVar _FullBright;// = { "r_fullbright", "0" };
        private CVar _LightMap;// = { "r_lightmap", "0" };
        private CVar _Shadows;// = { "r_shadows", "0" };
        //private CVar _MirrorAlpha;// = { "r_mirroralpha", "1" };
        private CVar _WaterAlpha;// = { "r_wateralpha", "1" };
        private CVar _Dynamic;// = { "r_dynamic", "1" };
        private CVar _NoVis;// = { "r_novis", "0" };

        private CVar _glFinish;// = { "gl_finish", "0" };
        private CVar _glClear;// = { "gl_clear", "0" };
        private CVar _glCull;// = { "gl_cull", "1" };
        private CVar _glTexSort;// = { "gl_texsort", "1" };
        private CVar _glSmoothModels;// = { "gl_smoothmodels", "1" };
        private CVar _glAffineModels;// = { "gl_affinemodels", "0" };
        private CVar _glPolyBlend;// = { "gl_polyblend", "1" };
        private CVar _glFlashBlend;// = { "gl_flashblend", "1" };
        private CVar _glPlayerMip;// = { "gl_playermip", "0" };
        private CVar _glNoColors;// = { "gl_nocolors", "0" };
        private CVar _glKeepTJunctions;// = { "gl_keeptjunctions", "0" };
        private CVar _glReportTJunctions;// = { "gl_reporttjunctions", "0" };
        private CVar _glDoubleEyes;// = { "gl_doubleeys", "1" };

        private Int32 _PlayerTextures; // playertextures	// up to 16 color translated skins
        private BaseTexture[] PlayerTextures;
        private System.Boolean _CacheThrash; // r_cache_thrash	// compatability

        // r_origin

        private Int32[] _LightStyleValue = new Int32[256]; // d_lightstylevalue  // 8.8 fraction of base light value
        private Entity _WorldEntity = new Entity( ); // r_worldentity
        private Entity _CurrentEntity; // currententity

        private MemoryLeaf _ViewLeaf; // r_viewleaf
        private MemoryLeaf _OldViewLeaf; // r_oldviewleaf

        private Int32 _SkyTextureNum; // skytexturenum
        //private Int32 _MirrorTextureNum; // mirrortexturenum	// quake texturenum, not gltexturenum

        private Int32 _VisFrameCount; // r_visframecount	// bumped when going to a new PVS
        private Int32 _FrameCount; // r_framecount		// used for dlight push checking
        private Int32 _BrushPolys; // c_brush_polys
        private Int32 _AliasPolys; // c_alias_polys
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

        private Plane[] _Frustum = new Plane[4]; // frustum
        private System.Boolean _IsEnvMap = false; // envmap	// true during envmap command capture
        private OpenTK.Matrix4 _WorldMatrix; // r_world_matrix
        private OpenTK.Matrix4 _BaseWorldMatrix; // r_base_world_matrix
        private Vector3 _ModelOrg; // modelorg
        private Vector3 _EntOrigin; // r_entorigin
        private Single _SpeedScale; // speedscale		// for top sky and bottom sky
        private Single _ShadeLight; // shadelight
        private Single _AmbientLight; // ambientlight
        private Single[] _ShadeDots = anorm_dots.Values[0]; // shadedots
        private Vector3 _ShadeVector; // shadevector
        private Int32 _LastPoseNum; // lastposenum
        private Vector3 _LightSpot; // lightspot

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public render( Host host )
        {
            Host = host;
        }

        /// <summary>
        /// R_Init
        /// </summary>
        public void Initialise( )
        {
            for ( var i = 0; i < _Frustum.Length; i++ )
                _Frustum[i] = new Plane( );

            Host.Command.Add( "timerefresh", TimeRefresh_f );
            //Cmd.Add("envmap", Envmap_f);
            //Cmd.Add("pointfile", ReadPointFile_f);

            if ( _NoRefresh == null )
            {
                _NoRefresh = new CVar( "r_norefresh", "0" );
                _DrawEntities = new CVar( "r_drawentities", "1" );
                _DrawViewModel = new CVar( "r_drawviewmodel", "1" );
                _Speeds = new CVar( "r_speeds", "0" );
                _FullBright = new CVar( "r_fullbright", "0" );
                _LightMap = new CVar( "r_lightmap", "0" );
                _Shadows = new CVar( "r_shadows", "0" );
                //_MirrorAlpha = new CVar( "r_mirroralpha", "1" );
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

            if ( Host.Video.Device.Desc.SupportsMultiTexture )
                CVar.Set( "gl_texsort", 0.0f );

            InitParticles( );
            InitParticleTexture( );

            // reserve 16 textures
            PlayerTextures = new BaseTexture[16];

            for ( var i = 0; i < PlayerTextures.Length; i++ )
            {
                PlayerTextures[i] = BaseTexture.FromDynamicBuffer( Host.Video.Device, "_PlayerTexture{i}", new ByteArraySegment( new Byte[512 * 256 * 4] ), 512, 256, false, false );
            }

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
            if ( _NoRefresh.Value != 0 )
                return;

            if ( _WorldEntity.model == null || Host.Client.cl.worldmodel == null )
                Utilities.Error( "R_RenderView: NULL worldmodel" );

            Double time1 = 0;
            if ( _Speeds.Value != 0 )
            {
                Host.Video.Device.Finish( );
                time1 = Timer.GetFloatTime( );
                _BrushPolys = 0;
                _AliasPolys = 0;
            }

            //_IsMirror = false;

            if ( _glFinish.Value != 0 )
                Host.Video.Device.Finish( );

            Clear( );

            // render normal view

            RenderScene( );
            DrawViewModel( );
            DrawWaterSurfaces( );

            // render mirror view
            //Mirror();

            PolyBlend( );

            if ( _Speeds.Value != 0 )
            {
                var time2 = Timer.GetFloatTime( );
                ConsoleWrapper.Print( "{0,3} ms  {1,4} wpoly {2,4} epoly\n", ( Int32 ) ( ( time2 - time1 ) * 1000 ), _BrushPolys, _AliasPolys );
            }
        }

        /// <summary>
        /// R_RemoveEfrags
        /// Call when removing an object from the world or moving it to another position
        /// </summary>
        public void RemoveEfrags( Entity ent )
        {
            var ef = ent.efrag;

            while ( ef != null )
            {
                var leaf = ef.leaf;
                while ( true )
                {
                    var walk = leaf.efrags;
                    if ( walk == null )
                        break;
                    if ( walk == ef )
                    {
                        // remove this fragment
                        leaf.efrags = ef.leafnext;
                        break;
                    }
                    else
                        leaf = ( MemoryLeaf ) ( Object ) walk.leafnext;
                }

                var old = ef;
                ef = ef.entnext;

                // put it on the free list
                old.entnext = Host.Client.cl.free_efrags;
                Host.Client.cl.free_efrags = old;
            }

            ent.efrag = null;
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
            if ( model.type != ModelType.mod_alias )
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
            PlayerTextures[playernum].TranslateAndUpload( original, translate, inwidth, inheight, ( Int32 ) Host.DrawingContext.glMaxSize, ( Int32 ) Host.DrawingContext.glMaxSize, ( Int32 ) _glPlayerMip.Value );
        }

        /// <summary>
        /// R_NewMap
        /// </summary>
        public void NewMap( )
        {
            for ( var i = 0; i < 256; i++ )
                _LightStyleValue[i] = 264;		// normal light value

            _WorldEntity.Clear( );
            _WorldEntity.model = Host.Client.cl.worldmodel;

            // clear out efrags in case the level hasn't been reloaded
            // FIXME: is this one short?
            for ( var i = 0; i < Host.Client.cl.worldmodel.numleafs; i++ )
                Host.Client.cl.worldmodel.leafs[i].efrags = null;

            _ViewLeaf = null;
            ClearParticles( );

            BuildLightMaps( );

            // identify sky texture
            _SkyTextureNum = -1;
            //_MirrorTextureNum = -1;
            var world = Host.Client.cl.worldmodel;
            for ( var i = 0; i < world.numtextures; i++ )
            {
                if ( world.textures[i] == null )
                    continue;
                if ( world.textures[i].name != null )
                {
                    if ( world.textures[i].name.StartsWith( "sky" ) )
                        _SkyTextureNum = i;
                    //if( world.textures[i].name.StartsWith( "window02_1" ) )
                    //    _MirrorTextureNum = i;
                }
                world.textures[i].texturechain = null;
            }
        }

        /// <summary>
        /// R_PolyBlend
        /// </summary>
        private void PolyBlend( )
        {
            if ( _glPolyBlend.Value == 0 )
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
        /// R_DrawViewModel
        /// </summary>
        private void DrawViewModel( )
        {
            if ( _DrawViewModel.Value == 0 )
                return;

            if ( Host.ChaseView.IsActive )
                return;

            if ( _IsEnvMap )
                return;

            if ( _DrawEntities.Value == 0 )
                return;

            if ( Host.Client.cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
                return;

            if ( Host.Client.cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
                return;

            _CurrentEntity = Host.Client.ViewEnt;
            if ( _CurrentEntity.model == null )
                return;

            var j = LightPoint( ref _CurrentEntity.origin );

            if ( j < 24 )
                j = 24;		// allways give some light on gun
            _AmbientLight = j;
            _ShadeLight = j;

            // add dynamic lights
            for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
            {
                var dl = Host.Client.DLights[lnum];
                if ( dl.radius == 0 )
                    continue;
                if ( dl.die < Host.Client.cl.time )
                    continue;

                var dist = _CurrentEntity.origin - dl.origin;
                var add = dl.radius - dist.Length;
                if ( add > 0 )
                    _AmbientLight += add;
            }

            // hack the depth range to prevent view model from poking into walls
            Host.Video.Device.SetDepth( _glDepthMin, _glDepthMin + 0.3f * ( _glDepthMax - _glDepthMin ) );
            DrawAliasModel( _CurrentEntity );
            Host.Video.Device.SetDepth( _glDepthMin, _glDepthMax );
        }

        /// <summary>
        /// R_RenderScene
        /// r_refdef must be set before the first call
        /// </summary>
        private void RenderScene( )
        {
            SetupFrame( );

            SetFrustum( );

            SetupGL( );

            MarkLeaves( );	// done here so we know if we're in water

            DrawWorld( );		// adds entities to the list

            Host.Sound.ExtraUpdate( );	// don't let sound get messed up if going slow

            DrawEntitiesOnList( );

            Host.Video.Device.DisableMultitexture( );

            RenderDlights( );

            DrawParticles( );

#if GLTEST
	        Test_Draw ();
#endif
        }

        /// <summary>
        /// R_DrawEntitiesOnList
        /// </summary>
        private void DrawEntitiesOnList( )
        {
            if ( _DrawEntities.Value == 0 )
                return;

            // draw sprites seperately, because of alpha blending
            for ( var i = 0; i < Host.Client.NumVisEdicts; i++ )
            {
                _CurrentEntity = Host.Client.VisEdicts[i];

                switch ( _CurrentEntity.model.type )
                {
                    case ModelType.mod_alias:
                        DrawAliasModel( _CurrentEntity );
                        break;

                    case ModelType.mod_brush:
                        DrawBrushModel( _CurrentEntity );
                        break;

                    default:
                        break;
                }
            }

            for ( var i = 0; i < Host.Client.NumVisEdicts; i++ )
            {
                _CurrentEntity = Host.Client.VisEdicts[i];

                switch ( _CurrentEntity.model.type )
                {
                    case ModelType.mod_sprite:
                        DrawSpriteModel( _CurrentEntity );
                        break;
                }
            }
        }

        /// <summary>
        /// R_DrawSpriteModel
        /// </summary>
        private void DrawSpriteModel( Entity e )
        {
            // don't even bother culling, because it's just a single
            // polygon without a surface cache
            var frame = GetSpriteFrame( e );
            var psprite = ( msprite_t ) e.model.cache.data; // Uze: changed from _CurrentEntity to e

            Vector3 v_forward, right, up;
            if ( psprite.type == SPR.SPR_ORIENTED )
            {
                // bullet marks on walls
                MathLib.AngleVectors( ref e.angles, out v_forward, out right, out up ); // Uze: changed from _CurrentEntity to e
            }
            else
            {	// normal sprite
                up = ViewUp;// vup;
                right = ViewRight;// vright;
            }

            var texture = Host.Model.SpriteTextures[frame.gl_texturenum];

            Host.Video.Device.Graphics.DrawSpriteModel( texture, frame, up, right, e.origin );
        }

        /// <summary>
        /// R_GetSpriteFrame
        /// </summary>
        private mspriteframe_t GetSpriteFrame( Entity currententity )
        {
            var psprite = ( msprite_t ) currententity.model.cache.data;
            var frame = currententity.frame;

            if ( ( frame >= psprite.numframes ) || ( frame < 0 ) )
            {
                Host.Console.Print( "R_DrawSprite: no such frame {0}\n", frame );
                frame = 0;
            }

            mspriteframe_t pspriteframe;
            if ( psprite.frames[frame].type == spriteframetype_t.SPR_SINGLE )
            {
                pspriteframe = ( mspriteframe_t ) psprite.frames[frame].frameptr;
            }
            else
            {
                var pspritegroup = ( mspritegroup_t ) psprite.frames[frame].frameptr;
                var pintervals = pspritegroup.intervals;
                var numframes = pspritegroup.numframes;
                var fullinterval = pintervals[numframes - 1];
                var time = ( Single ) Host.Client.cl.time + currententity.syncbase;

                // when loading in Mod_LoadSpriteGroup, we guaranteed all interval values
                // are positive, so we don't have to worry about division by 0
                var targettime = time - ( ( Int32 ) ( time / fullinterval ) ) * fullinterval;
                Int32 i;
                for ( i = 0; i < ( numframes - 1 ); i++ )
                {
                    if ( pintervals[i] > targettime )
                        break;
                }
                pspriteframe = pspritegroup.frames[i];
            }

            return pspriteframe;
        }

        /// <summary>
        /// R_DrawAliasModel
        /// </summary>
        private void DrawAliasModel( Entity e )
        {
            var clmodel = _CurrentEntity.model;
            var mins = _CurrentEntity.origin + clmodel.mins;
            var maxs = _CurrentEntity.origin + clmodel.maxs;

            if ( CullBox( ref mins, ref maxs ) )
                return;

            _EntOrigin = _CurrentEntity.origin;
            _ModelOrg = Origin - _EntOrigin;

            //
            // get lighting information
            //

            _AmbientLight = _ShadeLight = LightPoint( ref _CurrentEntity.origin );

            // allways give the gun some light
            if ( e == Host.Client.cl.viewent && _AmbientLight < 24 )
                _AmbientLight = _ShadeLight = 24;

            for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
            {
                if ( Host.Client.DLights[lnum].die >= Host.Client.cl.time )
                {
                    var dist = _CurrentEntity.origin - Host.Client.DLights[lnum].origin;
                    var add = Host.Client.DLights[lnum].radius - dist.Length;
                    if ( add > 0 )
                    {
                        _AmbientLight += add;
                        //ZOID models should be affected by dlights as well
                        _ShadeLight += add;
                    }
                }
            }

            // clamp lighting so it doesn't overbright as much
            if ( _AmbientLight > 128 )
                _AmbientLight = 128;
            if ( _AmbientLight + _ShadeLight > 192 )
                _ShadeLight = 192 - _AmbientLight;

            // ZOID: never allow players to go totally black
            var playernum = Array.IndexOf( Host.Client.Entities, _CurrentEntity, 0, Host.Client.cl.maxclients );
            if ( playernum >= 1 )// && i <= cl.maxclients)
                if ( _AmbientLight < 8 )
                    _AmbientLight = _ShadeLight = 8;

            // HACK HACK HACK -- no fullbright colors, so make torches full light
            if ( clmodel.name == "progs/flame2.mdl" || clmodel.name == "progs/flame.mdl" )
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
            var paliashdr = Host.Model.GetExtraData( _CurrentEntity.model );

            _AliasPolys += paliashdr.numtris;

            BaseModel model = null;

            if ( !BaseModel.ModelPool.ContainsKey( clmodel.name ) )
            {
                var anim = ( Int32 ) ( Host.Client.cl.time * 10 ) & 3;

                model = BaseModel.Create( Host.Video.Device, clmodel.name, Host.Model.SkinTextures[paliashdr.gl_texturenum[_CurrentEntity.skinnum, anim]], true );
            }
            else
                model = BaseModel.ModelPool[clmodel.name];

            model.Desc.ScaleOrigin = paliashdr.scale_origin;
            model.Desc.Scale = paliashdr.scale;
            model.Desc.MinimumBounds = clmodel.mins;
            model.Desc.MaximumBounds = clmodel.maxs;
            model.Desc.Origin = e.origin;
            model.Desc.EulerAngles = e.angles;
            model.Desc.AliasFrame = _CurrentEntity.frame;

            model.DrawAliasModel( _ShadeLight, _ShadeVector, _ShadeDots, _LightSpot.Z, paliashdr,
                Host.Client.cl.time, ( _Shadows.Value != 0 ), ( _glSmoothModels.Value != 0 ), ( _glAffineModels.Value != 0 ),
                _glNoColors.Value == 0, ( clmodel.name == "progs/eyes.mdl" && _glDoubleEyes.Value != 0 ) );
        }

        /// <summary>
        /// R_SetupGL
        /// </summary>
        private void SetupGL( )
        {
            Host.Video.Device.Setup3DScene( _glCull.Value != 0, _RefDef, _IsEnvMap );

            ////
            //// set up viewpoint
            ////
            //GL.MatrixMode( MatrixMode.Projection );
            //GL.LoadIdentity();
            //var x = _RefDef.vrect.x * Host.Screen.glWidth / Host.Screen.vid.width;
            //var x2 = ( _RefDef.vrect.x + _RefDef.vrect.width ) * Host.Screen.glWidth / Host.Screen.vid.width;
            //var y = ( Host.Screen.vid.height - _RefDef.vrect.y ) * Host.Screen.glHeight / Host.Screen.vid.height;
            //var y2 = ( Host.Screen.vid.height - ( _RefDef.vrect.y + _RefDef.vrect.height ) ) * Host.Screen.glHeight / Host.Screen.vid.height;

            //// fudge around because of frac screen scale
            //if( x > 0 )
            //    x--;
            //if( x2 < Host.Screen.glWidth )
            //    x2++;
            //if( y2 < 0 )
            //    y2--;
            //if( y < Host.Screen.glHeight )
            //    y++;

            //var w = x2 - x;
            //var h = y - y2;

            //if( _IsEnvMap )
            //{
            //    x = y2 = 0;
            //    w = h = 256;
            //}

            //GL.Viewport( Host.Screen.glX + x, Host.Screen.glY + y2, w, h );
            //var screenaspect = ( Single ) _RefDef.vrect.width / _RefDef.vrect.height;
            //MYgluPerspective( _RefDef.fov_y, screenaspect, 4, 4096 );

            //if( _IsMirror )
            //{
            //    if( _MirrorPlane.normal.Z != 0 )
            //        GL.Scale( 1f, -1f, 1f );
            //    else
            //        GL.Scale( -1f, 1f, 1f );
            //    GL.CullFace( CullFaceMode.Back );
            //}
            //else
            //    GL.CullFace( CullFaceMode.Front );

            //GL.MatrixMode( MatrixMode.Modelview );
            //GL.LoadIdentity();

            //GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            //GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up
            //GL.Rotate( -_RefDef.viewangles.Z, 1, 0, 0 );
            //GL.Rotate( -_RefDef.viewangles.X, 0, 1, 0 );
            //GL.Rotate( -_RefDef.viewangles.Y, 0, 0, 1 );
            //GL.Translate( -_RefDef.vieworg.X, -_RefDef.vieworg.Y, -_RefDef.vieworg.Z );

            //GL.GetFloat( GetPName.ModelviewMatrix, out _WorldMatrix );

            ////
            //// set drawing parms
            ////
            //if( _glCull.Value != 0 )
            //    GL.Enable( EnableCap.CullFace );
            //else
            //    GL.Disable( EnableCap.CullFace );

            //GL.Disable( EnableCap.Blend );
            //GL.Disable( EnableCap.AlphaTest );
            //GL.Enable( EnableCap.DepthTest );
        }

        /// <summary>
        /// R_SetFrustum
        /// </summary>
        private void SetFrustum( )
        {
            if ( _RefDef.fov_x == 90 )
            {
                // front side is visible
                _Frustum[0].normal = ViewPn + ViewRight;
                _Frustum[1].normal = ViewPn - ViewRight;

                _Frustum[2].normal = ViewPn + ViewUp;
                _Frustum[3].normal = ViewPn - ViewUp;
            }
            else
            {
                // rotate VPN right by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[0].normal, ref ViewUp, ref ViewPn, -( 90 - _RefDef.fov_x / 2 ) );
                // rotate VPN left by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[1].normal, ref ViewUp, ref ViewPn, 90 - _RefDef.fov_x / 2 );
                // rotate VPN up by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[2].normal, ref ViewRight, ref ViewPn, 90 - _RefDef.fov_y / 2 );
                // rotate VPN down by FOV_X/2 degrees
                MathLib.RotatePointAroundVector( out _Frustum[3].normal, ref ViewRight, ref ViewPn, -( 90 - _RefDef.fov_y / 2 ) );
            }

            for ( var i = 0; i < 4; i++ )
            {
                _Frustum[i].type = PlaneDef.PLANE_ANYZ;
                _Frustum[i].dist = Vector3.Dot( Origin, _Frustum[i].normal );
                _Frustum[i].signbits = ( Byte ) SignbitsForPlane( _Frustum[i] );
            }
        }

        private Int32 SignbitsForPlane( Plane p )
        {
            // for fast box on planeside test
            var bits = 0;
            if ( p.normal.X < 0 )
                bits |= 1 << 0;
            if ( p.normal.Y < 0 )
                bits |= 1 << 1;
            if ( p.normal.Z < 0 )
                bits |= 1 << 2;
            return bits;
        }

        /// <summary>
        /// R_SetupFrame
        /// </summary>
        private void SetupFrame( )
        {
            // don't allow cheats in multiplayer
            if ( Host.Client.cl.maxclients > 1 )
                CVar.Set( "r_fullbright", "0" );

            AnimateLight( );

            _FrameCount++;

            // build the transformation matrix for the given view angles
            Origin = _RefDef.vieworg;

            MathLib.AngleVectors( ref _RefDef.viewangles, out ViewPn, out ViewRight, out ViewUp );

            // current viewleaf
            _OldViewLeaf = _ViewLeaf;
            _ViewLeaf = Host.Model.PointInLeaf( ref Origin, Host.Client.cl.worldmodel );

            Host.View.SetContentsColor( _ViewLeaf.contents );
            Host.View.CalcBlend( );

            _CacheThrash = false;
            _BrushPolys = 0;
            _AliasPolys = 0;
        }

        /// <summary>
        /// R_Clear
        /// </summary>
        private void Clear( )
        {
            Host.Video.Device.Clear( Host.Video.glZTrick, _glClear.Value );
        }

        /// <summary>
        /// R_TimeRefresh_f
        /// For program optimization
        /// </summary>
        private void TimeRefresh_f( )
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
        /// R_CullBox
        /// Returns true if the box is completely outside the frustom
        /// </summary>
        private System.Boolean CullBox( ref Vector3 mins, ref Vector3 maxs )
        {
            for ( var i = 0; i < 4; i++ )
            {
                if ( MathLib.BoxOnPlaneSide( ref mins, ref maxs, _Frustum[i] ) == 2 )
                    return true;
            }
            return false;
        }
    }
}
