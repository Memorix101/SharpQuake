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
using OpenTK;

// gl_refrag.c

namespace SharpQuake
{
    partial class Render
    {
        static entity_t _AddEnt; // r_addent
        static mnode_t _EfragTopNode; // r_pefragtopnode
        static Vector3 _EMins; // r_emins
        static Vector3 _EMaxs; // r_emaxs
        /// <summary>
        /// efrag_t **lastlink changed to object _LastObj
        /// and may be a reference to entity_t, in wich case assign *lastlink to ((entity_t)_LastObj).efrag
        /// or to efrag_t in wich case assign *lastlink value to ((efrag_t)_LastObj).entnext
        /// </summary>
        static object _LastObj; // see comments

        /// <summary>
        /// R_AddEfrags
        /// </summary>
        public static void AddEfrags(entity_t ent)
        {
            if (ent.model == null)
                return;

            _AddEnt = ent;
            _LastObj = ent; //  lastlink = &ent->efrag;
            _EfragTopNode = null;

            model_t entmodel = ent.model;
            _EMins = ent.origin + entmodel.mins;
            _EMaxs = ent.origin + entmodel.maxs;

            SplitEntityOnNode(Client.cl.worldmodel.nodes[0]);
            ent.topnode = _EfragTopNode;
        }

        /// <summary>
        /// R_SplitEntityOnNode
        /// </summary>
        static void SplitEntityOnNode(mnodebase_t node)
        {
            if (node.contents == Contents.CONTENTS_SOLID)
                return;

            // add an efrag if the node is a leaf
            if (node.contents < 0)
            {
                if (_EfragTopNode == null)
                    _EfragTopNode = node as mnode_t;

                mleaf_t leaf = (mleaf_t)(object)node;

                // grab an efrag off the free list
                efrag_t ef = Client.cl.free_efrags;
                if (ef == null)
                {
                    Con.Print("Too many efrags!\n");
                    return;	// no free fragments...
                }
                Client.cl.free_efrags = Client.cl.free_efrags.entnext;

                ef.entity = _AddEnt;

                // add the entity link
                // *lastlink = ef;
                if (_LastObj is entity_t)
                {
                    ((entity_t)_LastObj).efrag = ef;
                }
                else
                {
                    ((efrag_t)_LastObj).entnext = ef;
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
            mnode_t n = node as mnode_t;
            if (n == null)
                return;
            
            mplane_t splitplane = n.plane;
            int sides = Mathlib.BoxOnPlaneSide(ref _EMins, ref _EMaxs, splitplane);

            if (sides == 3)
            {
                // split on this plane
                // if this is the first splitter of this bmodel, remember it
                if (_EfragTopNode == null)
                    _EfragTopNode = n;
            }

            // recurse down the contacted sides
            if ((sides & 1) != 0)
                SplitEntityOnNode(n.children[0]);

            if ((sides & 2) != 0)
                SplitEntityOnNode(n.children[1]);
        }

        /// <summary>
        /// R_StoreEfrags
        /// FIXME: a lot of this goes away with edge-based
        /// </summary>
        static void StoreEfrags(efrag_t ef)
        {
            while (ef != null)
            {
                entity_t pent = ef.entity;
                model_t clmodel = pent.model;

                switch (clmodel.type)
                {
                    case modtype_t.mod_alias:
                    case modtype_t.mod_brush:
                    case modtype_t.mod_sprite:
                        if ((pent.visframe != _FrameCount) && (Client.NumVisEdicts < Client.MAX_VISEDICTS))
                        {
                            Client.VisEdicts[Client.NumVisEdicts++] = pent;

                            // mark that we've recorded this entity for this frame
                            pent.visframe = _FrameCount;
                        }

                        ef = ef.leafnext;
                        break;

                    default:
                        Sys.Error("R_StoreEfrags: Bad entity type {0}\n", clmodel.type);
                        break;
                }
            }
        }
    }
}
