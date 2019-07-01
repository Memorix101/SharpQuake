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

using OpenTK;
using SharpQuake.Framework;

// gl_refrag.c

namespace SharpQuake
{
    partial class render
    {
        private static Entity _AddEnt; // r_addent
        private static MemoryNode _EfragTopNode; // r_pefragtopnode
        private static Vector3 _EMins; // r_emins
        private static Vector3 _EMaxs; // r_emaxs

        /// <summary>
        /// efrag_t **lastlink changed to object _LastObj
        /// and may be a reference to entity_t, in wich case assign *lastlink to ((entity_t)_LastObj).efrag
        /// or to efrag_t in wich case assign *lastlink value to ((efrag_t)_LastObj).entnext
        /// </summary>
        private static System.Object _LastObj; // see comments

        /// <summary>
        /// R_AddEfrags
        /// </summary>
        public static void AddEfrags( Entity ent )
        {
            if( ent.model == null )
                return;

            _AddEnt = ent;
            _LastObj = ent; //  lastlink = &ent->efrag;
            _EfragTopNode = null;

            Model entmodel = ent.model;
            _EMins = ent.origin + entmodel.mins;
            _EMaxs = ent.origin + entmodel.maxs;

            SplitEntityOnNode( client.cl.worldmodel.nodes[0] );
            ent.topnode = _EfragTopNode;
        }

        /// <summary>
        /// R_SplitEntityOnNode
        /// </summary>
        private static void SplitEntityOnNode( MemoryNodeBase node )
        {
            if( node.contents == ContentsDef.CONTENTS_SOLID )
                return;

            // add an efrag if the node is a leaf
            if( node.contents < 0 )
            {
                if( _EfragTopNode == null )
                    _EfragTopNode = node as MemoryNode;

                MemoryLeaf leaf = (MemoryLeaf)( System.Object ) node;

                // grab an efrag off the free list
                EFrag ef = client.cl.free_efrags;
                if( ef == null )
                {
                    Host.Console.Print( "Too many efrags!\n" );
                    return;	// no free fragments...
                }
                client.cl.free_efrags = client.cl.free_efrags.entnext;

                ef.entity = _AddEnt;

                // add the entity link
                // *lastlink = ef;
                if( _LastObj is Entity )
                {
                    ( (Entity)_LastObj ).efrag = ef;
                }
                else
                {
                    ( (EFrag)_LastObj ).entnext = ef;
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
            MemoryNode n = node as MemoryNode;
            if( n == null )
                return;

            Plane splitplane = n.plane;
            var sides = MathLib.BoxOnPlaneSide( ref _EMins, ref _EMaxs, splitplane );

            if( sides == 3 )
            {
                // split on this plane
                // if this is the first splitter of this bmodel, remember it
                if( _EfragTopNode == null )
                    _EfragTopNode = n;
            }

            // recurse down the contacted sides
            if( ( sides & 1 ) != 0 )
                SplitEntityOnNode( n.children[0] );

            if( ( sides & 2 ) != 0 )
                SplitEntityOnNode( n.children[1] );
        }

        /// <summary>
        /// R_StoreEfrags
        /// FIXME: a lot of this goes away with edge-based
        /// </summary>
        private static void StoreEfrags( EFrag ef )
        {
            while( ef != null )
            {
                Entity pent = ef.entity;
                Model clmodel = pent.model;

                switch( clmodel.type )
                {
                    case ModelType.mod_alias:
                    case ModelType.mod_brush:
                    case ModelType.mod_sprite:
                        if( ( pent.visframe != _FrameCount ) && ( client.NumVisEdicts < client.MAX_VISEDICTS ) )
                        {
                            client.VisEdicts[client.NumVisEdicts++] = pent;

                            // mark that we've recorded this entity for this frame
                            pent.visframe = _FrameCount;
                        }

                        ef = ef.leafnext;
                        break;

                    default:
                        Utilities.Error( "R_StoreEfrags: Bad entity type {0}\n", clmodel.type );
                        break;
                }
            }
        }
    }
}
