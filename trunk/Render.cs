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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

// refresh.h -- public interface to refresh functions
// gl_rmisc.c
// gl_rmain.c

namespace SharpQuake
{
    /// <summary>
    /// R_functions
    /// </summary>
    static partial class Render
    {
        public const int MAXCLIPPLANES = 11;
        public const int TOP_RANGE = 16;			// soldier uniform colors
        public const int BOTTOM_RANGE = 96;
        const float ONE_OVER_16 = 1.0f / 16.0f;
        
        const int MAX_LIGHTMAPS = 64;

        const int BLOCK_WIDTH = 128;
        const int BLOCK_HEIGHT = 128;

        static refdef_t _RefDef = new refdef_t(); // refdef_t	r_refdef;
        static texture_t _NoTextureMip; // r_notexture_mip

        static Cvar _NoRefresh;// = { "r_norefresh", "0" };
        static Cvar _DrawEntities;// = { "r_drawentities", "1" };
        static Cvar _DrawViewModel;// = { "r_drawviewmodel", "1" };
        static Cvar _Speeds;// = { "r_speeds", "0" };
        static Cvar _FullBright;// = { "r_fullbright", "0" };
        static Cvar _LightMap;// = { "r_lightmap", "0" };
        static Cvar _Shadows;// = { "r_shadows", "0" };
        static Cvar _MirrorAlpha;// = { "r_mirroralpha", "1" };
        static Cvar _WaterAlpha;// = { "r_wateralpha", "1" };
        static Cvar _Dynamic;// = { "r_dynamic", "1" };
        static Cvar _NoVis;// = { "r_novis", "0" };

        static Cvar _glFinish;// = { "gl_finish", "0" };
        static Cvar _glClear;// = { "gl_clear", "0" };
        static Cvar _glCull;// = { "gl_cull", "1" };
        static Cvar _glTexSort;// = { "gl_texsort", "1" };
        static Cvar _glSmoothModels;// = { "gl_smoothmodels", "1" };
        static Cvar _glAffineModels;// = { "gl_affinemodels", "0" };
        static Cvar _glPolyBlend;// = { "gl_polyblend", "1" };
        static Cvar _glFlashBlend;// = { "gl_flashblend", "1" };
        static Cvar _glPlayerMip;// = { "gl_playermip", "0" };
        static Cvar _glNoColors;// = { "gl_nocolors", "0" };
        static Cvar _glKeepTJunctions;// = { "gl_keeptjunctions", "0" };
        static Cvar _glReportTJunctions;// = { "gl_reporttjunctions", "0" };
        static Cvar _glDoubleEyes;// = { "gl_doubleeys", "1" };

        static int _PlayerTextures; // playertextures	// up to 16 color translated skins
        static bool _CacheThrash; // r_cache_thrash	// compatability
        
        //
        // view origin
        //
        public static Vector3 ViewUp; // vup
        public static Vector3 ViewPn; // vpn
        public static Vector3 ViewRight; // vright
        public static Vector3 Origin; // r_origin

        static int[] _LightStyleValue = new int[256]; // d_lightstylevalue  // 8.8 fraction of base light value
        static entity_t _WorldEntity = new entity_t(); // r_worldentity
        static entity_t _CurrentEntity; // currententity

        static mleaf_t _ViewLeaf; // r_viewleaf
        static mleaf_t _OldViewLeaf; // r_oldviewleaf

        static int _SkyTextureNum; // skytexturenum
        static int _MirrorTextureNum; // mirrortexturenum	// quake texturenum, not gltexturenum

        static int[,] _Allocated = new int[MAX_LIGHTMAPS,BLOCK_WIDTH]; // allocated

        static int _VisFrameCount; // r_visframecount	// bumped when going to a new PVS
        static int _FrameCount; // r_framecount		// used for dlight push checking
        static bool _MTexEnabled; // mtexenabled
        static int _BrushPolys; // c_brush_polys
        static int _AliasPolys; // c_alias_polys
        static bool _IsMirror; // mirror
        static mplane_t _MirrorPlane; // mirror_plane
        static float _glDepthMin; // gldepthmin
        static float _glDepthMax; // gldepthmax
        static int _TrickFrame; // static int trickframe from R_Clear()
        static mplane_t[] _Frustum = new mplane_t[4]; // frustum
        static bool _IsEnvMap = false; // envmap	// true during envmap command capture 
        static Matrix4 _WorldMatrix; // r_world_matrix
        static Matrix4 _BaseWorldMatrix; // r_base_world_matrix
        static Vector3 _ModelOrg; // modelorg
        static Vector3 _EntOrigin; // r_entorigin
        static float _SpeedScale; // speedscale		// for top sky and bottom sky
        static float _ShadeLight; // shadelight
        static float _AmbientLight; // ambientlight
        static float[] _ShadeDots = AnormDots.Values[0]; // shadedots
        static Vector3 _ShadeVector; // shadevector
        static int _LastPoseNum; // lastposenum
        static Vector3 _LightSpot; // lightspot
        
        public static refdef_t RefDef
        {
            get { return _RefDef; }
        }
        public static bool CacheTrash
        {
            get { return _CacheThrash; }
        }
        public static texture_t NoTextureMip
        {
            get { return _NoTextureMip; }
        }

        /// <summary>
        /// R_Init
        /// </summary>
        public static void Init()
        {
            for (int i = 0; i < _Frustum.Length; i++)
                _Frustum[i] = new mplane_t();

            Cmd.Add("timerefresh", TimeRefresh_f);
	        //Cmd.Add("envmap", Envmap_f);
	        //Cmd.Add("pointfile", ReadPointFile_f);

            if (_NoRefresh == null)
            {
                _NoRefresh = new Cvar("r_norefresh", "0");
                _DrawEntities = new Cvar("r_drawentities", "1");
                _DrawViewModel = new Cvar("r_drawviewmodel", "1");
                _Speeds = new Cvar("r_speeds", "0");
                _FullBright = new Cvar("r_fullbright", "0");
                _LightMap = new Cvar("r_lightmap", "0");
                _Shadows = new Cvar("r_shadows", "0");
                _MirrorAlpha = new Cvar("r_mirroralpha", "1");
                _WaterAlpha = new Cvar("r_wateralpha", "1");
                _Dynamic = new Cvar("r_dynamic", "1");
                _NoVis = new Cvar("r_novis", "0");

                _glFinish = new Cvar("gl_finish", "0");
                _glClear = new Cvar("gl_clear", "0");
                _glCull = new Cvar("gl_cull", "1");
                _glTexSort = new Cvar("gl_texsort", "1");
                _glSmoothModels = new Cvar("gl_smoothmodels", "1");
                _glAffineModels = new Cvar("gl_affinemodels", "0");
                _glPolyBlend = new Cvar("gl_polyblend", "1");
                _glFlashBlend = new Cvar("gl_flashblend", "1");
                _glPlayerMip = new Cvar("gl_playermip", "0");
                _glNoColors = new Cvar("gl_nocolors", "0");
                _glKeepTJunctions = new Cvar("gl_keeptjunctions", "0");
                _glReportTJunctions = new Cvar("gl_reporttjunctions", "0");
                _glDoubleEyes = new Cvar("gl_doubleeys", "1");
            }

 	        if (Vid.glMTexable)
		        Cvar.Set("gl_texsort", 0.0f);

            InitParticles();
            InitParticleTexture();

            // reserve 16 textures
            _PlayerTextures = Drawer.GenerateTextureNumberRange(16);
        }
        
        // R_InitTextures
        public static void InitTextures()
        {
            // create a simple checkerboard texture for the default
            _NoTextureMip = new texture_t();
            _NoTextureMip.pixels = new byte[16 * 16 + 8 * 8 + 4 * 4 + 2 * 2];
            _NoTextureMip.width = _NoTextureMip.height = 16;
            int offset = 0;
            _NoTextureMip.offsets[0] = offset;
            offset += 16 * 16;
            _NoTextureMip.offsets[1] = offset;
            offset += 8 * 8;
            _NoTextureMip.offsets[2] = offset;
            offset += 4 * 4;
            _NoTextureMip.offsets[3] = offset;

            byte[] dest = _NoTextureMip.pixels;
            for (int m = 0; m < 4; m++)
            {
                offset = _NoTextureMip.offsets[m];
                for (int y = 0; y < (16 >> m); y++)
                    for (int x = 0; x < (16 >> m); x++)
                    {
                        if ((y < (8 >> m)) ^ (x < (8 >> m)))
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
            if (_NoRefresh.Value != 0)
                return;

            if (_WorldEntity.model == null || Client.cl.worldmodel == null)
                Sys.Error("R_RenderView: NULL worldmodel");

            double time1 = 0;
            if (_Speeds.Value != 0)
            {
                GL.Finish();
                time1 = Sys.GetFloatTime();
                _BrushPolys = 0;
                _AliasPolys = 0;
            }

            _IsMirror = false;

            if (_glFinish.Value != 0)
                GL.Finish();

            Clear();

            // render normal view

            RenderScene();
            DrawViewModel();
            DrawWaterSurfaces();

            // render mirror view
            Mirror();

            PolyBlend();

            if (_Speeds.Value != 0)
            {
                double time2 = Sys.GetFloatTime();
                Con.Print("{0,3} ms  {1,4} wpoly {2,4} epoly\n", (int)((time2 - time1) * 1000), _BrushPolys, _AliasPolys);
            }
        }

        /// <summary>
        /// R_PolyBlend
        /// </summary>
        static void PolyBlend()
        {
            if (_glPolyBlend.Value == 0)
                return;
            
            if (View.Blend.A == 0)
                return;

            DisableMultitexture();

            GL.Disable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);

            GL.LoadIdentity();

            GL.Rotate(-90f, 1, 0, 0);	    // put Z going up
            GL.Rotate(90f, 0, 0, 1);	    // put Z going up

            GL.Color4(View.Blend);
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(10f, 100, 100);
            GL.Vertex3(10f, -100, 100);
            GL.Vertex3(10f, -100, -100);
            GL.Vertex3(10f, 100, -100);
            GL.End();

            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.AlphaTest);
        }

        /// <summary>
        /// R_Mirror
        /// </summary>
        static void Mirror()
        {
            if (!_IsMirror)
                return;

            _BaseWorldMatrix = _WorldMatrix;

            float d = Vector3.Dot(_RefDef.vieworg, _MirrorPlane.normal) - _MirrorPlane.dist;
            _RefDef.vieworg += _MirrorPlane.normal * -2 * d;

            d = Vector3.Dot(Render.ViewPn, _MirrorPlane.normal);
            Render.ViewPn += _MirrorPlane.normal * -2 * d;

            _RefDef.viewangles = new Vector3((float)(Math.Asin(Render.ViewPn.Z) / Math.PI * 180.0),
                (float)(Math.Atan2(Render.ViewPn.Y, Render.ViewPn.X) / Math.PI * 180.0),
                -_RefDef.viewangles.Z);

            entity_t ent = Client.ViewEntity;
            if (Client.NumVisEdicts < Client.MAX_VISEDICTS)
            {
                Client.VisEdicts[Client.NumVisEdicts] = ent;
                Client.NumVisEdicts++;
            }

            _glDepthMin = 0.5f;
            _glDepthMax = 1;
            GL.DepthRange(_glDepthMin, _glDepthMax);
            GL.DepthFunc(DepthFunction.Lequal);

            RenderScene();
            DrawWaterSurfaces();

            _glDepthMin = 0;
            _glDepthMax = 0.5f;
            GL.DepthRange(_glDepthMin, _glDepthMax);
            GL.DepthFunc(DepthFunction.Lequal);

            // blend on top
            GL.Enable(EnableCap.Blend);
            GL.MatrixMode(MatrixMode.Projection);
            if (_MirrorPlane.normal.Z != 0)
                GL.Scale(1f, -1, 1);
            else
                GL.Scale(-1f, 1, 1);
            GL.CullFace(CullFaceMode.Front);
            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadMatrix(ref _BaseWorldMatrix);

            GL.Color4(1, 1, 1, _MirrorAlpha.Value);
            msurface_t s = Client.cl.worldmodel.textures[_MirrorTextureNum].texturechain;
            for (; s != null; s = s.texturechain)
                RenderBrushPoly(s);
            Client.cl.worldmodel.textures[_MirrorTextureNum].texturechain = null;
            GL.Disable(EnableCap.Blend);
            GL.Color4(1f, 1, 1, 1);
        }

        /// <summary>
        /// R_DrawViewModel
        /// </summary>
        static void DrawViewModel()
        {
            if (_DrawViewModel.Value == 0)
                return;

            if (Chase.IsActive)
                return;

            if (_IsEnvMap)
                return;

            if (_DrawEntities.Value == 0)
                return;

            if (Client.cl.HasItems(QItems.IT_INVISIBILITY))
                return;

            if (Client.cl.stats[QStats.STAT_HEALTH] <= 0)
                return;

            _CurrentEntity = Client.ViewEnt;
            if (_CurrentEntity.model == null)
                return;

            int j = LightPoint(ref _CurrentEntity.origin);

            if (j < 24)
                j = 24;		// allways give some light on gun
            _AmbientLight = j;
            _ShadeLight = j;

            // add dynamic lights		
            for (int lnum = 0; lnum < Client.MAX_DLIGHTS; lnum++)
            {
                dlight_t dl = Client.DLights[lnum];
                if (dl.radius == 0)
                    continue;
                if (dl.die < Client.cl.time)
                    continue;

                Vector3 dist = _CurrentEntity.origin - dl.origin;
                float add = dl.radius - dist.Length;
                if (add > 0)
                    _AmbientLight += add;
            }

            // hack the depth range to prevent view model from poking into walls
            GL.DepthRange(_glDepthMin, _glDepthMin + 0.3f * (_glDepthMax - _glDepthMin));
            DrawAliasModel(_CurrentEntity);
            GL.DepthRange(_glDepthMin, _glDepthMax);
        }

        /// <summary>
        /// R_RenderScene
        /// r_refdef must be set before the first call
        /// </summary>
        static void RenderScene()
        {
            SetupFrame();

            SetFrustum();

            SetupGL();

            MarkLeaves();	// done here so we know if we're in water

            DrawWorld();		// adds static entities to the list

            Sound.ExtraUpdate();	// don't let sound get messed up if going slow

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
            if (_DrawEntities.Value == 0)
                return;

            // draw sprites seperately, because of alpha blending
            for (int i = 0; i < Client.NumVisEdicts; i++)
            {
                _CurrentEntity = Client.VisEdicts[i];

                switch (_CurrentEntity.model.type)
                {
                    case modtype_t.mod_alias:
                        DrawAliasModel(_CurrentEntity);
                        break;

                    case modtype_t.mod_brush:
                        DrawBrushModel(_CurrentEntity);
                        break;

                    default:
                        break;
                }
            }

            for (int i = 0; i < Client.NumVisEdicts; i++)
            {
                _CurrentEntity = Client.VisEdicts[i];

                switch (_CurrentEntity.model.type)
                {
                    case modtype_t.mod_sprite:
                        DrawSpriteModel(_CurrentEntity);
                        break;
                }
            }
        }

        /// <summary>
        /// R_DrawSpriteModel
        /// </summary>
        private static void DrawSpriteModel(entity_t e)
        {
            // don't even bother culling, because it's just a single
            // polygon without a surface cache
            mspriteframe_t frame = GetSpriteFrame(e);
            msprite_t psprite = (msprite_t)e.model.cache.data; // Uze: changed from _CurrentEntity to e

            Vector3 v_forward, right, up;
            if (psprite.type == SPR.SPR_ORIENTED)
            {
                // bullet marks on walls
                Mathlib.AngleVectors(ref e.angles, out v_forward, out right, out up); // Uze: changed from _CurrentEntity to e
            }
            else
            {	// normal sprite
                up = Render.ViewUp;// vup;
                right = Render.ViewRight;// vright;
            }

            GL.Color3(1f, 1, 1);

            DisableMultitexture();

            Drawer.Bind(frame.gl_texturenum);

            GL.Enable(EnableCap.AlphaTest);
            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0f, 1);
            Vector3 point = e.origin + up * frame.down + right * frame.left;
            GL.Vertex3(point);

            GL.TexCoord2(0f, 0);
            point = e.origin + up * frame.up + right * frame.left;
            GL.Vertex3(point);

            GL.TexCoord2(1f, 0);
            point = e.origin + up * frame.up + right * frame.right;
            GL.Vertex3(point);

            GL.TexCoord2(1f, 1);
            point = e.origin + up * frame.down + right * frame.right;
            GL.Vertex3(point);

            GL.End();
            GL.Disable(EnableCap.AlphaTest);
        }

        /// <summary>
        /// R_GetSpriteFrame
        /// </summary>
        static mspriteframe_t GetSpriteFrame(entity_t currententity)
        {
            msprite_t psprite = (msprite_t)currententity.model.cache.data;
            int frame = currententity.frame;

            if ((frame >= psprite.numframes) || (frame < 0))
            {
                Con.Print("R_DrawSprite: no such frame {0}\n", frame);
                frame = 0;
            }

            mspriteframe_t pspriteframe;
            if (psprite.frames[frame].type == spriteframetype_t.SPR_SINGLE)
            {
                pspriteframe = (mspriteframe_t)psprite.frames[frame].frameptr;
            }
            else
            {
                mspritegroup_t pspritegroup = (mspritegroup_t)psprite.frames[frame].frameptr;
                float[] pintervals = pspritegroup.intervals;
                int numframes = pspritegroup.numframes;
                float fullinterval = pintervals[numframes - 1];
                float time = (float)Client.cl.time + currententity.syncbase;

                // when loading in Mod_LoadSpriteGroup, we guaranteed all interval values
                // are positive, so we don't have to worry about division by 0
                float targettime = time - ((int)(time / fullinterval)) * fullinterval;
                int i;
                for (i = 0; i < (numframes - 1); i++)
                {
                    if (pintervals[i] > targettime)
                        break;
                }
                pspriteframe = pspritegroup.frames[i];
            }

            return pspriteframe;
        }

        /// <summary>
        /// R_DrawAliasModel
        /// </summary>
        private static void DrawAliasModel(entity_t e)
        {
            model_t clmodel = _CurrentEntity.model;
            Vector3 mins = _CurrentEntity.origin + clmodel.mins;
            Vector3 maxs = _CurrentEntity.origin + clmodel.maxs;

            if (CullBox(ref mins, ref maxs))
                return;

            _EntOrigin = _CurrentEntity.origin;
            _ModelOrg = Render.Origin - _EntOrigin;

            //
            // get lighting information
            //

            _AmbientLight = _ShadeLight = LightPoint(ref _CurrentEntity.origin);

            // allways give the gun some light
            if (e == Client.cl.viewent && _AmbientLight < 24)
                _AmbientLight = _ShadeLight = 24;

            for (int lnum = 0; lnum < Client.MAX_DLIGHTS; lnum++)
            {
                if (Client.DLights[lnum].die >= Client.cl.time)
                {
                    Vector3 dist = _CurrentEntity.origin - Client.DLights[lnum].origin;
                    float add = Client.DLights[lnum].radius - dist.Length;
                    if (add > 0)
                    {
                        _AmbientLight += add;
                        //ZOID models should be affected by dlights as well
                        _ShadeLight += add;
                    }
                }
            }

            // clamp lighting so it doesn't overbright as much
            if (_AmbientLight > 128)
                _AmbientLight = 128;
            if (_AmbientLight + _ShadeLight > 192)
                _ShadeLight = 192 - _AmbientLight;

            // ZOID: never allow players to go totally black
            int playernum = Array.IndexOf(Client.Entities, _CurrentEntity, 0, Client.cl.maxclients);
            if (playernum >= 1)// && i <= cl.maxclients)
                if (_AmbientLight < 8)
                    _AmbientLight = _ShadeLight = 8;

            // HACK HACK HACK -- no fullbright colors, so make torches full light
            if (clmodel.name == "progs/flame2.mdl" || clmodel.name == "progs/flame.mdl")
                _AmbientLight = _ShadeLight = 256;

            _ShadeDots = AnormDots.Values[((int)(e.angles.Y * (AnormDots.SHADEDOT_QUANT / 360.0))) & (AnormDots.SHADEDOT_QUANT - 1)];
            _ShadeLight = _ShadeLight / 200.0f;

            double an = e.angles.Y / 180.0 * Math.PI;
            _ShadeVector.X = (float)Math.Cos(-an);
            _ShadeVector.Y = (float)Math.Sin(-an);
            _ShadeVector.Z = 1;
            Mathlib.Normalize(ref _ShadeVector);

            //
            // locate the proper data
            //
            aliashdr_t paliashdr = Mod.GetExtraData(_CurrentEntity.model);

            _AliasPolys += paliashdr.numtris;

            //
            // draw all the triangles
            //

            DisableMultitexture();

            GL.PushMatrix();
            RotateForEntity(e);
            if (clmodel.name == "progs/eyes.mdl" && _glDoubleEyes.Value != 0)
            {
                Vector3 v = paliashdr.scale_origin;
                v.Z -= (22 + 8);
                GL.Translate(v);
                // double size of eyes, since they are really hard to see in gl
                GL.Scale(paliashdr.scale * 2.0f);
            }
            else
            {
                GL.Translate(paliashdr.scale_origin);
                GL.Scale(paliashdr.scale);
            }

            int anim = (int)(Client.cl.time * 10) & 3;
            Drawer.Bind(paliashdr.gl_texturenum[_CurrentEntity.skinnum, anim]);

            // we can't dynamically colormap textures, so they are cached
            // seperately for the players.  Heads are just uncolored.
            if (_CurrentEntity.colormap != Scr.vid.colormap && _glNoColors.Value == 0 && playernum >= 1)
            {
                Drawer.Bind(_PlayerTextures - 1 + playernum);
            }

            if (_glSmoothModels.Value != 0)
                GL.ShadeModel(ShadingModel.Smooth);

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);

            if (_glAffineModels.Value != 0)
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Fastest);

            SetupAliasFrame(_CurrentEntity.frame, paliashdr);

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);

            GL.ShadeModel(ShadingModel.Flat);
            if (_glAffineModels.Value != 0)
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.PopMatrix();

            if (_Shadows.Value != 0)
            {
                GL.PushMatrix();
                RotateForEntity(e);
                GL.Disable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
                GL.Color4(0, 0, 0, 0.5f);
                DrawAliasShadow(paliashdr, _LastPoseNum);
                GL.Enable(EnableCap.Texture2D);
                GL.Disable(EnableCap.Blend);
                GL.Color4(1f, 1, 1, 1);
                GL.PopMatrix();
            }
        }

        /// <summary>
        /// GL_DrawAliasShadow
        /// </summary>
        static void DrawAliasShadow(aliashdr_t paliashdr, int posenum)
        {
            float lheight = _CurrentEntity.origin.Z - _LightSpot.Z;
            float height = 0;
            trivertx_t[] verts = paliashdr.posedata;
            int voffset = posenum * paliashdr.poseverts;
            int[] order = paliashdr.commands;

            height = -lheight + 1.0f;
            int orderOffset = 0;

            while (true)
            {
                // get the vertex count and primitive type
                int count = order[orderOffset++];
                if (count == 0)
                    break;		// done

                if (count < 0)
                {
                    count = -count;
                    GL.Begin(BeginMode.TriangleFan);
                }
                else
                    GL.Begin(BeginMode.TriangleStrip);

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

                    point.X -= _ShadeVector.X * (point.Z + lheight);
                    point.Y -= _ShadeVector.Y * (point.Z + lheight);
                    point.Z = height;

                    GL.Vertex3(point);

                    voffset++;
                } while (--count > 0);

                GL.End();
            }
        }

        /// <summary>
        /// R_SetupAliasFrame
        /// </summary>
        static void SetupAliasFrame(int frame, aliashdr_t paliashdr)
        {
            if ((frame >= paliashdr.numframes) || (frame < 0))
            {
                Con.DPrint("R_AliasSetupFrame: no such frame {0}\n", frame);
                frame = 0;
            }

            int pose = paliashdr.frames[frame].firstpose;
            int numposes = paliashdr.frames[frame].numposes;

            if (numposes > 1)
            {
                float interval = paliashdr.frames[frame].interval;
                pose += (int)(Client.cl.time / interval) % numposes;
            }

            DrawAliasFrame(paliashdr, pose);
        }

        /// <summary>
        /// GL_DrawAliasFrame
        /// </summary>
        static void DrawAliasFrame(aliashdr_t paliashdr, int posenum)
        {
            _LastPoseNum = posenum;

            trivertx_t[] verts = paliashdr.posedata;
            int vertsOffset = posenum * paliashdr.poseverts;
            int[] order = paliashdr.commands;
            int orderOffset = 0;

            while (true)
            {
                // get the vertex count and primitive type
                int count = order[orderOffset++];
                if (count == 0)
                    break;		// done

                if (count < 0)
                {
                    count = -count;
                    GL.Begin(BeginMode.TriangleFan);
                }
                else
                    GL.Begin(BeginMode.TriangleStrip);

                Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
                do
                {
                    // texture coordinates come from the draw list
                    u1.i0 = order[orderOffset + 0];
                    u2.i0 = order[orderOffset + 1];
                    orderOffset += 2;
                    GL.TexCoord2(u1.f0, u2.f0);

                    // normals and vertexes come from the frame list
                    float l = _ShadeDots[verts[vertsOffset].lightnormalindex] * _ShadeLight;
                    GL.Color3(l, l, l);
                    GL.Vertex3((float)verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2]);
                    vertsOffset++;
                } while (--count > 0);
                GL.End();
            }
        }

        /// <summary>
        /// R_RotateForEntity
        /// </summary>
        static void RotateForEntity(entity_t e)
        {
            GL.Translate(e.origin);

            GL.Rotate(e.angles.Y, 0, 0, 1);
            GL.Rotate(-e.angles.X, 0, 1, 0);
            GL.Rotate(e.angles.Z, 1, 0, 0);
        }

        /// <summary>
        /// R_SetupGL
        /// </summary>
        private static void SetupGL()
        {
            //
            // set up viewpoint
            //
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            int x = _RefDef.vrect.x * Scr.glWidth / Scr.vid.width;
            int x2 = (_RefDef.vrect.x + _RefDef.vrect.width) * Scr.glWidth / Scr.vid.width;
            int y = (Scr.vid.height - _RefDef.vrect.y) * Scr.glHeight / Scr.vid.height;
            int y2 = (Scr.vid.height - (_RefDef.vrect.y + _RefDef.vrect.height)) * Scr.glHeight / Scr.vid.height;

            // fudge around because of frac screen scale
            if (x > 0)
                x--;
            if (x2 < Scr.glWidth)
                x2++;
            if (y2 < 0)
                y2--;
            if (y < Scr.glHeight)
                y++;

            int w = x2 - x;
            int h = y - y2;

            if (_IsEnvMap)
            {
                x = y2 = 0;
                w = h = 256;
            }

            GL.Viewport(Scr.glX + x, Scr.glY + y2, w, h);
            float screenaspect = (float)_RefDef.vrect.width / _RefDef.vrect.height;
            MYgluPerspective(_RefDef.fov_y, screenaspect, 4, 4096);

            if (_IsMirror)
            {
                if (_MirrorPlane.normal.Z != 0)
                    GL.Scale(1f, -1f, 1f);
                else
                    GL.Scale(-1f, 1f, 1f);
                GL.CullFace(CullFaceMode.Back);
            }
            else
                GL.CullFace(CullFaceMode.Front);


            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Rotate(-90f, 1, 0, 0);	    // put Z going up
            GL.Rotate(90f, 0, 0, 1);	    // put Z going up
            GL.Rotate(-_RefDef.viewangles.Z, 1, 0, 0);
            GL.Rotate(-_RefDef.viewangles.X, 0, 1, 0);
            GL.Rotate(-_RefDef.viewangles.Y, 0, 0, 1);
            GL.Translate(-_RefDef.vieworg.X, -_RefDef.vieworg.Y, -_RefDef.vieworg.Z);

            GL.GetFloat(GetPName.ModelviewMatrix, out _WorldMatrix);

            //
            // set drawing parms
            //
            if (_glCull.Value != 0)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.DepthTest);
        }

        static void MYgluPerspective(double fovy, double aspect, double zNear, double zFar)
        {
            double ymax = zNear * Math.Tan(fovy * Math.PI / 360.0);
            double ymin = -ymax;

            double xmin = ymin * aspect;
            double xmax = ymax * aspect;

            GL.Frustum(xmin, xmax, ymin, ymax, zNear, zFar);
        }

        /// <summary>
        /// R_SetFrustum
        /// </summary>
        static void SetFrustum()
        {
            if (_RefDef.fov_x == 90)
            {
                // front side is visible
                _Frustum[0].normal = Render.ViewPn + Render.ViewRight;
                _Frustum[1].normal = Render.ViewPn - Render.ViewRight;

                _Frustum[2].normal = Render.ViewPn + Render.ViewUp;
                _Frustum[3].normal = Render.ViewPn - Render.ViewUp;
            }
            else
            {
                // rotate VPN right by FOV_X/2 degrees
                Mathlib.RotatePointAroundVector(out _Frustum[0].normal, ref Render.ViewUp, ref Render.ViewPn, -(90 - _RefDef.fov_x / 2));
                // rotate VPN left by FOV_X/2 degrees
                Mathlib.RotatePointAroundVector(out _Frustum[1].normal, ref Render.ViewUp, ref Render.ViewPn, 90 - _RefDef.fov_x / 2);
                // rotate VPN up by FOV_X/2 degrees
                Mathlib.RotatePointAroundVector(out _Frustum[2].normal, ref Render.ViewRight, ref Render.ViewPn, 90 - _RefDef.fov_y / 2);
                // rotate VPN down by FOV_X/2 degrees
                Mathlib.RotatePointAroundVector(out _Frustum[3].normal, ref Render.ViewRight, ref Render.ViewPn, -(90 - _RefDef.fov_y / 2));
            }

            for (int i = 0; i < 4; i++)
            {
                _Frustum[i].type = Planes.PLANE_ANYZ;
                _Frustum[i].dist = Vector3.Dot(Render.Origin, _Frustum[i].normal);
                _Frustum[i].signbits = (byte)SignbitsForPlane(_Frustum[i]);
            }
        }

        static int SignbitsForPlane (mplane_t p)
        {
	        // for fast box on planeside test
            int bits = 0;
            if (p.normal.X < 0) bits |= 1 << 0;
            if (p.normal.Y < 0) bits |= 1 << 1;
            if (p.normal.Z < 0) bits |= 1 << 2;
	        return bits;
        }

        /// <summary>
        /// R_SetupFrame
        /// </summary>
        static void SetupFrame()
        {
            // don't allow cheats in multiplayer
            if (Client.cl.maxclients > 1)
                Cvar.Set("r_fullbright", "0");

            AnimateLight();

            _FrameCount++;

            // build the transformation matrix for the given view angles
            Render.Origin = _RefDef.vieworg;

            Mathlib.AngleVectors(ref _RefDef.viewangles, out ViewPn, out ViewRight, out ViewUp);

            // current viewleaf
            _OldViewLeaf = _ViewLeaf;
            _ViewLeaf = Mod.PointInLeaf(ref Render.Origin, Client.cl.worldmodel);

            View.SetContentsColor(_ViewLeaf.contents);
            View.CalcBlend();

            _CacheThrash = false;
            _BrushPolys = 0;
            _AliasPolys = 0;
        }

        /// <summary>
        /// R_Clear
        /// </summary>
        static void Clear()
        {
            if (_MirrorAlpha.Value != 1.0)
            {
                if (_glClear.Value != 0)
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                else
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                _glDepthMin = 0;
                _glDepthMax = 0.5f;
                GL.DepthFunc(DepthFunction.Lequal);
            }
            else if (Vid.glZTrick)
            {
                if (_glClear.Value != 0)
                    GL.Clear(ClearBufferMask.ColorBufferBit);

                _TrickFrame++;
                if ((_TrickFrame & 1) != 0)
                {
                    _glDepthMin = 0;
                    _glDepthMax = 0.49999f;
                    GL.DepthFunc(DepthFunction.Lequal);
                }
                else
                {
                    _glDepthMin = 1;
                    _glDepthMax = 0.5f;
                    GL.DepthFunc(DepthFunction.Gequal);
                }
            }
            else
            {
                if (_glClear.Value != 0)
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    // Uze
                    Sbar.Changed();
                }
                else
                    GL.Clear(ClearBufferMask.DepthBufferBit);

                _glDepthMin = 0;
                _glDepthMax = 1;
                GL.DepthFunc(DepthFunction.Lequal);
            }

            GL.DepthRange(_glDepthMin, _glDepthMax);
        }

        /// <summary>
        /// R_RemoveEfrags
        /// Call when removing an object from the world or moving it to another position
        /// </summary>
        public static void RemoveEfrags(entity_t ent)
        {
            efrag_t ef = ent.efrag;

            while (ef != null)
            {
                mleaf_t leaf = ef.leaf;
                while (true)
                {
                    efrag_t walk = leaf.efrags;
                    if (walk == null)
                        break;
                    if (walk == ef)
                    {
                        // remove this fragment
                        leaf.efrags = ef.leafnext;
                        break;
                    }
                    else
                        leaf = (mleaf_t)(object)walk.leafnext;
                }

                efrag_t old = ef;
                ef = ef.entnext;

                // put it on the free list
                old.entnext = Client.cl.free_efrags;
                Client.cl.free_efrags = old;
            }

            ent.efrag = null;
        }

        /// <summary>
        /// R_TimeRefresh_f
        /// For program optimization
        /// </summary>
        static void TimeRefresh_f()
        {
            //GL.DrawBuffer(DrawBufferMode.Front);
            GL.Finish();

            double start = Sys.GetFloatTime();
            for (int i = 0; i < 128; i++)
            {
                _RefDef.viewangles.Y = (float)(i / 128.0 * 360.0);
                RenderView();
                MainForm.Instance.SwapBuffers();
            }

            GL.Finish();
            double stop = Sys.GetFloatTime();
            double time = stop - start;
            Con.Print("{0:F} seconds ({1:F1} fps)\n", time, 128 / time);

            //GL.DrawBuffer(DrawBufferMode.Back);
            Scr.EndRendering();
        }

        /// <summary>
        /// R_TranslatePlayerSkin 
        /// Translates a skin texture by the per-player color lookup
        /// </summary>
        public static void TranslatePlayerSkin(int playernum)
        {
            DisableMultitexture();

            int top = Client.cl.scores[playernum].colors & 0xf0;
            int bottom = (Client.cl.scores[playernum].colors & 15) << 4;

            byte[] translate = new byte[256];
            for (int i = 0; i < 256; i++)
                translate[i] = (byte)i;

            for (int i = 0; i < 16; i++)
            {
                if (top < 128)	// the artists made some backwards ranges.  sigh.
                    translate[TOP_RANGE + i] = (byte)(top + i);
                else
                    translate[TOP_RANGE + i] = (byte)(top + 15 - i);

                if (bottom < 128)
                    translate[BOTTOM_RANGE + i] = (byte)(bottom + i);
                else
                    translate[BOTTOM_RANGE + i] = (byte)(bottom + 15 - i);
            }

            //
            // locate the original skin pixels
            //
            _CurrentEntity = Client.Entities[1 + playernum];
            model_t model = _CurrentEntity.model;
            if (model == null)
                return;		// player doesn't have a model yet
            if (model.type != modtype_t.mod_alias)
                return; // only translate skins on alias models

            aliashdr_t paliashdr = Mod.GetExtraData(model);
            int s = paliashdr.skinwidth * paliashdr.skinheight;
            if ((s & 3) != 0)
                Sys.Error("R_TranslateSkin: s&3");

            byte[] original;
            if (_CurrentEntity.skinnum < 0 || _CurrentEntity.skinnum >= paliashdr.numskins)
            {
                Con.Print("({0}): Invalid player skin #{1}\n", playernum, _CurrentEntity.skinnum);
                original = (byte[])paliashdr.texels[0];// (byte *)paliashdr + paliashdr.texels[0];
            }
            else
                original = (byte[])paliashdr.texels[_CurrentEntity.skinnum];

            int inwidth = paliashdr.skinwidth;
            int inheight = paliashdr.skinheight;

            // because this happens during gameplay, do it fast
            // instead of sending it through gl_upload 8
            Drawer.Bind(_PlayerTextures + playernum);

            int scaled_width = (int)(Drawer.glMaxSize < 512 ? Drawer.glMaxSize : 512);
            int scaled_height = (int)(Drawer.glMaxSize < 256 ? Drawer.glMaxSize : 256);

            // allow users to crunch sizes down even more if they want
            scaled_width >>= (int)_glPlayerMip.Value;
            scaled_height >>= (int)_glPlayerMip.Value;

            uint fracstep, frac;
            int destOffset;

            uint[] translate32 = new uint[256];
            for (int i = 0; i < 256; i++)
                translate32[i] = Vid.Table8to24[translate[i]];

            uint[] dest = new uint[512 * 256];
            destOffset = 0;
            fracstep = (uint)(inwidth * 0x10000 / scaled_width);
            for (int i = 0; i < scaled_height; i++, destOffset += scaled_width)
            {
                int srcOffset = inwidth * (i * inheight / scaled_height);
                frac = fracstep >> 1;
                for (int j = 0; j < scaled_width; j += 4)
                {
                    dest[destOffset + j] = translate32[original[srcOffset + (frac >> 16)]];
                    frac += fracstep;
                    dest[destOffset + j + 1] = translate32[original[srcOffset + (frac >> 16)]];
                    frac += fracstep;
                    dest[destOffset + j + 2] = translate32[original[srcOffset + (frac >> 16)]];
                    frac += fracstep;
                    dest[destOffset + j + 3] = translate32[original[srcOffset + (frac >> 16)]];
                    frac += fracstep;
                }
            }
            GCHandle handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, Drawer.SolidFormat, scaled_width, scaled_height, 0,
                     PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
            Drawer.SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);
        }

        /// <summary>
        /// GL_DisableMultitexture
        /// </summary>
        public static void DisableMultitexture() 
        {
            if (_MTexEnabled)
            {
                GL.Disable(EnableCap.Texture2D);
                Drawer.SelectTexture(MTexTarget.TEXTURE0_SGIS);
                _MTexEnabled = false;
            }
        }

        /// <summary>
        /// GL_EnableMultitexture
        /// </summary>
        public static void EnableMultitexture() 
        {
            if (Vid.glMTexable)
            {
                Drawer.SelectTexture(MTexTarget.TEXTURE1_SGIS);
                GL.Enable(EnableCap.Texture2D);
                _MTexEnabled = true;
            }
        }

        /// <summary>
        /// R_NewMap
        /// </summary>
        public static void NewMap()
        {
            for (int i = 0; i < 256; i++)
                _LightStyleValue[i] = 264;		// normal light value

            _WorldEntity.Clear();
            _WorldEntity.model = Client.cl.worldmodel;

            // clear out efrags in case the level hasn't been reloaded
            // FIXME: is this one short?
            for (int i = 0; i < Client.cl.worldmodel.numleafs; i++)
                Client.cl.worldmodel.leafs[i].efrags = null;

            _ViewLeaf = null;
            ClearParticles();

            BuildLightMaps();

            // identify sky texture
            _SkyTextureNum = -1;
            _MirrorTextureNum = -1;
            model_t world = Client.cl.worldmodel;
            for (int i = 0; i < world.numtextures; i++)
            {
                if (world.textures[i] == null)
                    continue;
                if (world.textures[i].name.StartsWith("sky"))
                    _SkyTextureNum = i;
                if (world.textures[i].name.StartsWith("window02_1"))
                    _MirrorTextureNum = i;
                world.textures[i].texturechain = null;
            }
        }

        /// <summary>
        /// R_CullBox
        /// Returns true if the box is completely outside the frustom
        /// </summary>
        static bool CullBox(ref Vector3 mins, ref Vector3 maxs)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Mathlib.BoxOnPlaneSide(ref mins, ref maxs, _Frustum[i]) == 2)
                    return true;
            }
            return false;
        }
    }

    
    class efrag_t
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


    class entity_t
    {
        public bool forcelink;		// model changed
        public int update_type;
        public entity_state_t baseline;		// to fill in defaults in updates
        public double msgtime;		// time of last update
        public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)	
        public Vector3 origin;
        public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
        public Vector3 angles;
        public model_t model;			// NULL = no model
        public efrag_t efrag;			// linked list of efrags
        public int frame;
        public float syncbase;		// for client-side animations
        public byte[] colormap;
        public int effects;		// light, particals, etc
        public int skinnum;		// for Alias models
        public int visframe;		// last frame this entity was
        //  found in an active leaf

        public int dlightframe;	// dynamic lighting
        public int dlightbits;

        // FIXME: could turn these into a union
        public int trivial_accept;
        public mnode_t topnode;		// for bmodels, first world node
        //  that splits bmodel, or NULL if
        //  not split

        public entity_t()
        {
            msg_origins = new Vector3[2];
            msg_angles = new Vector3[2];
        }

        public void Clear()
        {
            this.forcelink = false;
            this.update_type = 0;

            this.baseline = entity_state_t.Empty;

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
    } // entity_t;

    // !!! if this is changed, it must be changed in asm_draw.h too !!!
    class refdef_t
    {
        public vrect_t vrect;				// subwindow in video for refresh
        public Vector3 vieworg;
        public Vector3 viewangles;
        public float fov_x, fov_y;
    } // refdef_t;

}
