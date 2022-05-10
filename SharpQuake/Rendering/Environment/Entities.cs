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

using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework.World;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.Rendering.Memory;
using SharpQuake.Game.World;
using SharpQuake.Renderer;
using SharpQuake.Renderer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpQuake.Rendering.Environment
{
    public class Entities
    {
        // c_alias_polys
        public Int32 AliasPolys
        {
            get;
            private set;
        }

        public Surfaces Surfaces
        {
            get;
            private set;
        }

        private readonly Host _host;

        public Entities( Host host )
        {
            _host = host;

            Surfaces = new Surfaces( host );
        }

        public void Reset()
        {
            AliasPolys = 0;
        }

        /// <summary>
        /// R_DrawEntitiesOnList
        /// </summary>
        public void DrawEntitiesOnList( )
        {
            if ( !_host.Cvars.DrawEntities.Get<Boolean>( ) )
                return;

            for ( var i = 0; i < _host.Client.NumVisEdicts; i++ )
            {
                var currentEntity = _host.Client.VisEdicts[i];

                switch ( currentEntity.model.Type )
                {
                    case ModelType.Alias:
                        currentEntity.useInterpolation = _host.Cvars.AnimationBlend.Get<Boolean>( );
                        DrawAliasModel( currentEntity, 0f );
                        break;

                    case ModelType.Brush:
                        Surfaces.DrawBrushModel( currentEntity );
                        break;

                    default:
                        break;
                }
            }

            // draw sprites seperately, because of alpha blending

            for ( var i = 0; i < _host.Client.NumVisEdicts; i++ )
            {
                var _CurrentEntity = _host.Client.VisEdicts[i];

                switch ( _CurrentEntity.model.Type )
                {
                    case ModelType.Sprite:
                        DrawSpriteModel( _CurrentEntity );
                        break;
                }
            }
        }



        /// <summary>
        /// R_DrawAliasModel
        /// </summary>
        public void DrawAliasModel( Entity entity, float shadeLight )
        {
            var clmodel = entity.model;
            var mins = entity.origin + clmodel.BoundsMin;
            var maxs = entity.origin + clmodel.BoundsMax;

            if ( Utilities.CullBox( ref mins, ref maxs, _host.RenderContext.Frustum ) )
                return;

            var entOrigin = entity.origin;
            var modelOrg = _host.RenderContext.Origin - entOrigin;

            //
            // get lighting information
            //

            var ambientLight = shadeLight = _host.RenderContext.World.Lighting.LightPoint( ref entity.origin );

            // allways give the gun some light
            if ( entity == _host.Client.cl.viewent && ambientLight < 24 )
                ambientLight = shadeLight = 24;

            for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
            {
                if ( _host.Client.DLights[lnum].die >= _host.Client.cl.time )
                {
                    var dist = entity.origin - _host.Client.DLights[lnum].origin;
                    var add = _host.Client.DLights[lnum].radius - dist.Length;
                    if ( add > 0 )
                    {
                        ambientLight += add;
                        //ZOID models should be affected by dlights as well
                        shadeLight += add;
                    }
                }
            }

            // clamp lighting so it doesn't overbright as much
            if ( ambientLight > 128 )
                ambientLight = 128;
            if ( ambientLight + shadeLight > 192 )
                shadeLight = 192 - ambientLight;

            // ZOID: never allow players to go totally black
            var playernum = Array.IndexOf( _host.Client.Entities, entity, 0, _host.Client.cl.maxclients );
            if ( playernum >= 1 )// && i <= cl.maxclients)
                if ( ambientLight < 8 )
                    ambientLight = shadeLight = 8;

            // HACK HACK HACK -- no fullbright colors, so make torches full light
            if ( clmodel.Name == "progs/flame2.mdl" || clmodel.Name == "progs/flame.mdl" )
                ambientLight = shadeLight = 256;

            var shadeDots = anorm_dots.Values[( ( Int32 ) ( entity.angles.Y * ( anorm_dots.SHADEDOT_QUANT / 360.0 ) ) ) & ( anorm_dots.SHADEDOT_QUANT - 1 )];
            shadeLight = shadeLight / 200.0f;

            var an = entity.angles.Y / 180.0 * Math.PI;
            var shadeVector = new Vector3( );
            shadeVector.X = ( Single ) Math.Cos( -an );
            shadeVector.Y = ( Single ) Math.Sin( -an );
            shadeVector.Z = 1;
            MathLib.Normalize( ref shadeVector );

            //
            // locate the proper data
            //
            var paliashdr = _host.Model.GetExtraData( entity.model );

            AliasPolys += paliashdr.numtris;

            BaseAliasModel model = null;

            if ( !BaseModel.ModelPool.ContainsKey( clmodel.Name ) )
            {
                var anim = ( Int32 ) ( _host.Client.cl.time * 10 ) & 3;

                var tex = _host.Model.SkinTextures.Where( t => ( ( Renderer.OpenGL.Textures.GLTextureDesc ) t.Desc ).TextureNumber == paliashdr.gl_texturenum[entity.skinnum, anim] ).FirstOrDefault( );

                model = BaseAliasModel.Create( _host.Video.Device, clmodel.Name, tex, paliashdr );
            }
            else
                model = ( BaseAliasModel ) BaseModel.ModelPool[clmodel.Name];

            model.AliasDesc.ScaleOrigin = paliashdr.scale_origin;
            model.AliasDesc.Scale = paliashdr.scale;
            model.AliasDesc.MinimumBounds = clmodel.BoundsMin;
            model.AliasDesc.MaximumBounds = clmodel.BoundsMax;
            model.AliasDesc.Origin = entity.origin;
            model.AliasDesc.EulerAngles = entity.angles;
            model.AliasDesc.AliasFrame = entity.frame;

            model.DrawAliasModel( shadeLight, shadeVector, shadeDots, _host.RenderContext.World.Lighting.LightSpot.Z,
                _host.RealTime, _host.Client.cl.time,
                ref entity.pose1, ref entity.pose2, ref entity.frame_start_time, ref entity.frame_interval,
                ref entity.origin1, ref entity.origin2, ref entity.translate_start_time, ref entity.angles1,
                ref entity.angles2, ref entity.rotate_start_time,
                ( _host.Cvars.Shadows.Get<Boolean>( ) ), ( _host.Cvars.glSmoothModels.Get<Boolean>( ) ), ( _host.Cvars.glAffineModels.Get<Boolean>( ) ),
                !_host.Cvars.glNoColors.Get<Boolean>( ), ( clmodel.Name == "progs/eyes.mdl" && _host.Cvars.glDoubleEyes.Get<Boolean>( ) ), entity.useInterpolation );
        }


        /// <summary>
        /// R_DrawViewModel
        /// </summary>
        public void DrawViewModel( Boolean isEnvMap )
        {
            if ( !_host.Cvars.DrawViewModel.Get<Boolean>( ) )
                return;

            if ( _host.ChaseView.IsActive )
                return;

            if ( isEnvMap )
                return;

            if ( !_host.Cvars.DrawEntities.Get<Boolean>( ) )
                return;

            if ( _host.Client.cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
                return;

            if ( _host.Client.cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
                return;

            var currentEntity = _host.Client.ViewEnt;
            if ( currentEntity.model == null )
                return;

            var j = _host.RenderContext.World.Lighting.LightPoint( ref currentEntity.origin );

            if ( j < 24 )
                j = 24;		// allways give some light on gun
            float ambientLight = j;
            float shadeLight = j;

            // add dynamic lights
            for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
            {
                var dl = _host.Client.DLights[lnum];
                if ( dl.radius == 0 )
                    continue;
                if ( dl.die < _host.Client.cl.time )
                    continue;

                var dist = currentEntity.origin - dl.origin;
                var add = dl.radius - dist.Length;
                if ( add > 0 )
                    ambientLight += add;
            }
            var glDepthMax = _host.Video.Device.Desc.DepthMaximum;
            var glDepthMin = _host.Video.Device.Desc.DepthMinimum;
            // hack the depth range to prevent view model from poking into walls
            _host.Video.Device.SetDepth( glDepthMin, glDepthMin + 0.3f * ( glDepthMax - glDepthMin ) );
            DrawAliasModel( currentEntity, shadeLight );
            _host.Video.Device.SetDepth( glDepthMin, glDepthMax );
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
            if ( psprite.type == SpriteType.Oriented )
            {
                // bullet marks on walls
                MathLib.AngleVectors( ref e.angles, out v_forward, out right, out up ); // Uze: changed from _CurrentEntity to e
            }
            else
            {	// normal sprite
                up = _host.RenderContext.ViewUp;// vup;
                right = _host.RenderContext.ViewRight;// vright;
            }

            var texture = _host.Model.SpriteTextures.Where( t => ( ( Renderer.OpenGL.Textures.GLTextureDesc ) t.Desc ).TextureNumber == frame.gl_texturenum ).FirstOrDefault( );

            _host.Video.Device.Graphics.DrawSpriteModel( texture, frame, up, right, e.origin );
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
                _host.Console.Print( "R_DrawSprite: no such frame {0}\n", frame );
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
                var time = ( Single ) _host.Client.cl.time + currententity.syncbase;

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


        private Entity _AddEnt; // r_addent
        private MemoryNode _EfragTopNode; // r_pefragtopnode
        private Vector3 _EMins; // r_emins
        private Vector3 _EMaxs; // r_emaxs

        /// <summary>
        /// efrag_t **lastlink changed to object _LastObj
        /// and may be a reference to entity_t, in wich case assign *lastlink to ((entity_t)_LastObj).efrag
        /// or to efrag_t in wich case assign *lastlink value to ((efrag_t)_LastObj).entnext
        /// </summary>
        private Object _LastObj; // see comments

        /// <summary>
        /// R_AddEfrags
        /// </summary>
        public void AddEfrags( Entity ent )
        {
            if ( ent.model == null )
                return;

            _AddEnt = ent;
            _LastObj = ent; //  lastlink = &ent->efrag;
            _EfragTopNode = null;

            var entmodel = ent.model;
            _EMins = ent.origin + entmodel.BoundsMin;
            _EMaxs = ent.origin + entmodel.BoundsMax;

            SplitEntityOnNode( _host.Client.cl.worldmodel.Nodes[0] );
            ent.topnode = _EfragTopNode;
        }

        /// <summary>
        /// R_SplitEntityOnNode
        /// </summary>
        private void SplitEntityOnNode( MemoryNodeBase node )
        {
            if ( node.contents == ( Int32 ) Q1Contents.Solid )
                return;

            // add an efrag if the node is a leaf
            if ( node.contents < 0 )
            {
                if ( _EfragTopNode == null )
                    _EfragTopNode = node as MemoryNode;

                var leaf = ( MemoryLeaf ) ( System.Object ) node;

                // grab an efrag off the free list
                var ef = _host.Client.cl.free_efrags;
                if ( ef == null )
                {
                    _host.Console.Print( "Too many efrags!\n" );
                    return;	// no free fragments...
                }
                _host.Client.cl.free_efrags = _host.Client.cl.free_efrags.entnext;

                ef.entity = _AddEnt;

                // add the entity link
                // *lastlink = ef;
                if ( _LastObj is Entity )
                {
                    ( ( Entity ) _LastObj ).efrag = ef;
                }
                else
                {
                    ( ( EFrag ) _LastObj ).entnext = ef;
                }
                _LastObj = ef; // lastlink = &ef->entnext;
                ef.entnext = null;

                // set the leaf links
                ef.leaf = leaf;
                ef.leafnext = leaf.efrags;
                leaf.efrags = ef;

                return;
            }

            // NODE_MIXED
            var n = node as MemoryNode;
            if ( n == null )
                return;

            var splitplane = n.plane;
            var sides = MathLib.BoxOnPlaneSide( ref _EMins, ref _EMaxs, splitplane );

            if ( sides == 3 )
            {
                // split on this plane
                // if this is the first splitter of this bmodel, remember it
                if ( _EfragTopNode == null )
                    _EfragTopNode = n;
            }

            // recurse down the contacted sides
            if ( ( sides & 1 ) != 0 )
                SplitEntityOnNode( n.children[0] );

            if ( ( sides & 2 ) != 0 )
                SplitEntityOnNode( n.children[1] );
        }

        /// <summary>
        /// R_StoreEfrags
        /// FIXME: a lot of this goes away with edge-based
        /// </summary>
        public void StoreEfrags( EFrag ef )
        {
            while ( ef != null )
            {
                var pent = ef.entity;
                var clmodel = pent.model;

                switch ( clmodel.Type )
                {
                    case ModelType.Alias:
                    case ModelType.Brush:
                    case ModelType.Sprite:
                        if ( ( pent.visframe != _host.RenderContext.World.Lighting.FrameCount ) && ( _host.Client.NumVisEdicts < ClientDef.MAX_VISEDICTS ) )
                        {
                            _host.Client.VisEdicts[_host.Client.NumVisEdicts++] = pent;

                            // mark that we've recorded this entity for this frame
                            pent.visframe = _host.RenderContext.World.Lighting.FrameCount;
                        }

                        ef = ef.leafnext;
                        break;

                    default:
                        Utilities.Error( "R_StoreEfrags: Bad entity type {0}\n", clmodel.Type );
                        break;
                }
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
                old.entnext = _host.Client.cl.free_efrags;
                _host.Client.cl.free_efrags = old;
            }

            ent.efrag = null;
        }

    }
}
