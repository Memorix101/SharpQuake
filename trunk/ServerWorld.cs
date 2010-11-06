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

// world.c -- world query functions

// entities never clip against themselves, or their owner
//
// line of sight checks trace->crosscontent, but bullets don't

namespace SharpQuake
{
    partial class Server
    {
        // 1/32 epsilon to keep floating point happy
        const float DIST_EPSILON = 0.03125f;
        
        const int MOVE_NORMAL = 0;
        const int MOVE_NOMONSTERS = 1;
        const int MOVE_MISSILE = 2;

        class moveclip_t
        {
            public Vector3 boxmins, boxmaxs;// enclose the test object along entire move
            public Vector3 mins, maxs;	// size of the moving object
            public Vector3 mins2, maxs2;	// size when clipping against mosnters
            public Vector3 start, end;
            public trace_t trace;
            public int type;
            public edict_t passedict;
        } //moveclip_t;

        const int AREA_DEPTH = 4;
        const int AREA_NODES = 32;

        static areanode_t[] _AreaNodes = new areanode_t[AREA_NODES]; // sv_areanodes
        static int _NumAreaNodes; // sv_numareanodes
        static hull_t _BoxHull = new hull_t(); // box_hull
        static dclipnode_t[] _BoxClipNodes = new dclipnode_t[6];
        static mplane_t[] _BoxPlanes = new mplane_t[6];

        
        /// <summary>
        /// SV_ClearWorld
        /// called after the world model has been loaded, before linking any entities
        /// </summary>
        public static void ClearWorld()
        {
            InitBoxHull();

            foreach (areanode_t node in _AreaNodes)
                node.Clear();
            _NumAreaNodes = 0;

            CreateAreaNode(0, ref sv.worldmodel.mins, ref sv.worldmodel.maxs);
        }

        /// <summary>
        /// SV_CreateAreaNode
        /// </summary>
        static areanode_t CreateAreaNode(int depth, ref Vector3 mins, ref Vector3 maxs)
        {
            areanode_t anode = _AreaNodes[_NumAreaNodes];
            _NumAreaNodes++;

            anode.trigger_edicts.Clear();
            anode.solid_edicts.Clear();

            if (depth == AREA_DEPTH)
            {
                anode.axis = -1;
                anode.children[0] = anode.children[1] = null;
                return anode;
            }

            Vector3 size = maxs - mins;
            Vector3 mins1 = mins;
            Vector3 mins2 = mins;
            Vector3 maxs1 = maxs;
            Vector3 maxs2 = maxs;

            if (size.X > size.Y)
            {
                anode.axis = 0;
                anode.dist = 0.5f * (maxs.X + mins.X);
                maxs1.X = mins2.X = anode.dist;
            }
            else
            {
                anode.axis = 1;
                anode.dist = 0.5f * (maxs.Y + mins.Y);
                maxs1.Y = mins2.Y = anode.dist;
            }

            anode.children[0] = CreateAreaNode(depth + 1, ref mins2, ref maxs2);
            anode.children[1] = CreateAreaNode(depth + 1, ref mins1, ref maxs1);

            return anode;
        }

        /// <summary>
        /// SV_UnlinkEdict
        /// call before removing an entity, and before trying to move one,
        /// so it doesn't clip against itself
        /// flags ent->v.modified
        /// </summary>
        public static void UnlinkEdict(edict_t ent)
        {
            if (ent.area.Prev == null)
                return;		// not linked in anywhere

            ent.area.Remove();  //RemoveLink(&ent->area);
            //ent->area.prev = ent->area.next = NULL;

        }

        /// <summary>
        /// SV_LinkEdict
        /// 
        /// Needs to be called any time an entity changes origin, mins, maxs, or solid
        /// flags ent->v.modified
        /// sets ent->v.absmin and ent->v.absmax
        /// if touchtriggers, calls prog functions for the intersected triggers
        /// </summary>
        public static void LinkEdict(edict_t ent, bool touch_triggers)
        {
            if (ent.area.Prev != null)
                UnlinkEdict(ent);	// unlink from old position

            if (ent == sv.edicts[0])
                return;		// don't add the world

            if (ent.free)
                return;

            // set the abs box
            Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.mins, out ent.v.absmin);
            Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.maxs, out ent.v.absmax);

            //
            // to make items easier to pick up and allow them to be grabbed off
            // of shelves, the abs sizes are expanded
            //
            if (((int)ent.v.flags & EdictFlags.FL_ITEM) != 0)
            {
                ent.v.absmin.x -= 15;
                ent.v.absmin.y -= 15;
                ent.v.absmax.x += 15;
                ent.v.absmax.y += 15;
            }
            else
            {	// because movement is clipped an epsilon away from an actual edge,
                // we must fully check even when bounding boxes don't quite touch
                ent.v.absmin.x -= 1;
                ent.v.absmin.y -= 1;
                ent.v.absmin.z -= 1;
                ent.v.absmax.x += 1;
                ent.v.absmax.y += 1;
                ent.v.absmax.z += 1;
            }

            // link to PVS leafs
            ent.num_leafs = 0;
            if (ent.v.modelindex != 0)
                FindTouchedLeafs(ent, sv.worldmodel.nodes[0]);

            if (ent.v.solid == Solids.SOLID_NOT)
                return;

            // find the first node that the ent's box crosses
            areanode_t node = _AreaNodes[0];
            while (true)
            {
                if (node.axis == -1)
                    break;
                if (Mathlib.Comp(ref ent.v.absmin, node.axis) > node.dist)
                    node = node.children[0];
                else if (Mathlib.Comp(ref ent.v.absmax, node.axis) < node.dist)
                    node = node.children[1];
                else
                    break;		// crosses the node
            }

            // link it in	

            if (ent.v.solid == Solids.SOLID_TRIGGER)
                ent.area.InsertBefore(node.trigger_edicts);
            else
                ent.area.InsertBefore(node.solid_edicts);

            // if touch_triggers, touch all entities at this node and decend for more
            if (touch_triggers)
                TouchLinks(ent, _AreaNodes[0]);
        }
        
        /// <summary>
        /// SV_PointContents
        /// </summary>
        public static int PointContents(ref Vector3 p)
        {
            int cont = HullPointContents(sv.worldmodel.hulls[0], 0, ref p);
            if (cont <= Contents.CONTENTS_CURRENT_0 && cont >= Contents.CONTENTS_CURRENT_DOWN)
                cont = Contents.CONTENTS_WATER;
            return cont;
        }

        /// <summary>
        /// SV_TestEntityPosition
        /// This could be a lot more efficient...
        /// </summary>
        static edict_t TestEntityPosition(edict_t ent)
        {
            trace_t trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref ent.v.origin, 0, ent);

            if (trace.startsolid)
                return sv.edicts[0];

            return null;
        }

        /// <summary>
        /// SV_Move
        /// mins and maxs are relative
        /// if the entire move stays in a solid volume, trace.allsolid will be set
        /// if the starting point is in a solid, it will be allowed to move out to an open area
        /// nomonsters is used for line of sight or edge testing, where mosnters
        /// shouldn't be considered solid objects
        /// passedict is explicitly excluded from clipping checks (normally NULL)
        /// </summary>
        public static trace_t Move(ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, int type, edict_t passedict)
        {
            moveclip_t clip = new moveclip_t();

            // clip to world
            clip.trace = ClipMoveToEntity(sv.edicts[0], ref start, ref mins, ref maxs, ref end);

            clip.start = start;
            clip.end = end;
            clip.mins = mins;
            clip.maxs = maxs;
            clip.type = type;
            clip.passedict = passedict;

            if (type ==  MOVE_MISSILE)
            {
                clip.mins2 = Vector3.One * -15;
                clip.maxs2 = Vector3.One * 15;
            }
            else
            {
                clip.mins2 = mins;
                clip.maxs2 = maxs;
            }

            // create the bounding box of the entire move
            MoveBounds(ref start, ref clip.mins2, ref clip.maxs2, ref end, out clip.boxmins, out clip.boxmaxs);

            // clip to entities
            ClipToLinks(_AreaNodes[0], clip);

            return clip.trace;
        }

        /// <summary>
        /// SV_InitBoxHull
        /// Set up the planes and clipnodes so that the six floats of a bounding box
        /// can just be stored out and get a proper hull_t structure.
        /// </summary>
        static void InitBoxHull()
        {
	        _BoxHull.clipnodes = _BoxClipNodes;
            _BoxHull.planes = _BoxPlanes;
            _BoxHull.firstclipnode = 0;
            _BoxHull.lastclipnode = 5;

	        for (int i = 0; i < 6; i++)
	        {
                _BoxClipNodes[i].planenum = i;
		
		        int side = i & 1;
		
		        _BoxClipNodes[i].children[side] = Contents.CONTENTS_EMPTY;
		        if (i != 5)
			        _BoxClipNodes[i].children[side ^ 1] = (short)(i + 1);
		        else
			        _BoxClipNodes[i].children[side ^ 1] = Contents.CONTENTS_SOLID;
		
		        _BoxPlanes[i].type = (byte)(i >> 1);
                switch (i >> 1)
                {
                    case 0: _BoxPlanes[i].normal.X = 1;
                        break;

                    case 1: _BoxPlanes[i].normal.Y = 1;
                        break;

                    case 2: _BoxPlanes[i].normal.Z = 1;
                        break;
                }
		        //_BoxPlanes[i].normal[i>>1] = 1;
	        }
        }


        // SV_HullForBox
        //
        // To keep everything totally uniform, bounding boxes are turned into small
        // BSP trees instead of being compared directly.
        static hull_t HullForBox(ref Vector3 mins, ref Vector3 maxs)
        {
	        _BoxPlanes[0].dist = maxs.X;
            _BoxPlanes[1].dist = mins.X;
            _BoxPlanes[2].dist = maxs.Y;
            _BoxPlanes[3].dist = mins.Y;
            _BoxPlanes[4].dist = maxs.Z;
            _BoxPlanes[5].dist = mins.Z;

	        return _BoxHull;
        }

        /// <summary>
        /// SV_HullForEntity
        /// Returns a hull that can be used for testing or clipping an object of mins/maxs size.
        /// Offset is filled in to contain the adjustment that must be added to the
        /// testing object's origin to get a point to use with the returned hull.
        /// </summary>
        static hull_t HullForEntity(edict_t ent, ref Vector3 mins, ref Vector3 maxs, out Vector3 offset)
        {
            hull_t hull = null;

            // decide which clipping hull to use, based on the size
            if (ent.v.solid == Solids.SOLID_BSP)
            {	// explicit hulls in the BSP model
                if (ent.v.movetype != Movetypes.MOVETYPE_PUSH)
                    Sys.Error("SOLID_BSP without MOVETYPE_PUSH");

                model_t model = sv.models[(int)ent.v.modelindex];

                if (model == null || model.type != modtype_t.mod_brush)
                    Sys.Error("MOVETYPE_PUSH with a non bsp model");

                Vector3 size = maxs - mins;
                if (size.X < 3)
                    hull = model.hulls[0];
                else if (size.X <= 32)
                    hull = model.hulls[1];
                else
                    hull = model.hulls[2];

                // calculate an offset value to center the origin
                offset = hull.clip_mins - mins;
                offset += Common.ToVector(ref ent.v.origin);
            }
            else
            {
                // create a temp hull from bounding box sizes
                Vector3 hullmins = Common.ToVector(ref ent.v.mins) - maxs;
                Vector3 hullmaxs = Common.ToVector(ref ent.v.maxs) - mins;
                hull = HullForBox(ref hullmins, ref hullmaxs);

                offset = Common.ToVector(ref ent.v.origin);
            }

            return hull;
        }


        /// <summary>
        /// SV_RecursiveHullCheck
        /// </summary>
        public static bool RecursiveHullCheck(hull_t hull, int num, float p1f, float p2f, ref Vector3 p1, ref Vector3 p2, trace_t trace)
        {
            // check for empty
            if (num < 0)
            {
                if (num != Contents.CONTENTS_SOLID)
                {
                    trace.allsolid = false;
                    if (num == Contents.CONTENTS_EMPTY)
                        trace.inopen = true;
                    else
                        trace.inwater = true;
                }
                else
                    trace.startsolid = true;
                return true;		// empty
            }

            if (num < hull.firstclipnode || num > hull.lastclipnode)
                Sys.Error("SV_RecursiveHullCheck: bad node number");

            //
            // find the point distances
            //
            short[] node_children = hull.clipnodes[num].children;
            mplane_t plane = hull.planes[hull.clipnodes[num].planenum];
            float t1, t2;

            if (plane.type < 3)
            {
                t1 = Mathlib.Comp(ref p1, plane.type) - plane.dist;
                t2 = Mathlib.Comp(ref p2, plane.type) - plane.dist;
            }
            else
            {
                t1 = Vector3.Dot(plane.normal, p1) - plane.dist;
                t2 = Vector3.Dot(plane.normal, p2) - plane.dist;
            }

            if (t1 >= 0 && t2 >= 0)
                return RecursiveHullCheck(hull, node_children[0], p1f, p2f, ref p1, ref p2, trace);
            if (t1 < 0 && t2 < 0)
                return RecursiveHullCheck(hull, node_children[1], p1f, p2f, ref p1, ref p2, trace);

            // put the crosspoint DIST_EPSILON pixels on the near side
            float frac;
            if (t1 < 0)
                frac = (t1 + DIST_EPSILON) / (t1 - t2);
            else
                frac = (t1 - DIST_EPSILON) / (t1 - t2);
            if (frac < 0)
                frac = 0;
            if (frac > 1)
                frac = 1;

            float midf = p1f + (p2f - p1f) * frac;
            Vector3 mid = p1 + (p2 - p1) * frac;

            int side = (t1 < 0) ? 1 : 0;

            // move up to the node
            if (!RecursiveHullCheck(hull, node_children[side], p1f, midf, ref p1, ref mid, trace))
                return false;

            if (HullPointContents(hull, node_children[side ^ 1], ref mid) != Contents.CONTENTS_SOLID)
                // go past the node
                return RecursiveHullCheck(hull, node_children[side ^ 1], midf, p2f, ref mid, ref p2, trace);

            if (trace.allsolid)
                return false;		// never got out of the solid area

            //==================
            // the other side of the node is solid, this is the impact point
            //==================
            if (side == 0)
            {
                trace.plane.normal = plane.normal;
                trace.plane.dist = plane.dist;
            }
            else
            {
                trace.plane.normal = -plane.normal;
                trace.plane.dist = -plane.dist;
            }

            while (HullPointContents(hull, hull.firstclipnode, ref mid) == Contents.CONTENTS_SOLID)
            {
                // shouldn't really happen, but does occasionally
                frac -= 0.1f;
                if (frac < 0)
                {
                    trace.fraction = midf;
                    trace.endpos = mid;
                    Con.DPrint("backup past 0\n");
                    return false;
                }
                midf = p1f + (p2f - p1f) * frac;
                mid = p1 + (p2 - p1) * frac;
            }

            trace.fraction = midf;
            trace.endpos = mid;

            return false;
        }

        /// <summary>
        /// SV_FindTouchedLeafs
        /// </summary>
        static void FindTouchedLeafs(edict_t ent, mnodebase_t node)
        {
            if (node.contents == Contents.CONTENTS_SOLID)
                return;

            // add an efrag if the node is a leaf

            if (node.contents < 0)
            {
                if (ent.num_leafs == Progs.MAX_ENT_LEAFS)
                    return;

                mleaf_t leaf = (mleaf_t)node;
                int leafnum = Array.IndexOf(sv.worldmodel.leafs, leaf) - 1;

                ent.leafnums[ent.num_leafs] = (short)leafnum;
                ent.num_leafs++;
                return;
            }

            // NODE_MIXED
            mnode_t n = (mnode_t)node;
            mplane_t splitplane = n.plane;
            int sides = Mathlib.BoxOnPlaneSide(ref ent.v.absmin, ref ent.v.absmax, splitplane);

            // recurse down the contacted sides
            if ((sides & 1) != 0)
                FindTouchedLeafs(ent, n.children[0]);

            if ((sides & 2) != 0)
                FindTouchedLeafs(ent, n.children[1]);
        }

        /// <summary>
        /// SV_TouchLinks
        /// </summary>
        static void TouchLinks(edict_t ent, areanode_t node)
        {
            // touch linked edicts
            link_t next;
            for (link_t l = node.trigger_edicts.Next; l != node.trigger_edicts; l = next)
            {
                next = l.Next;
                edict_t touch = (edict_t)l.Owner;// EDICT_FROM_AREA(l);
                if (touch == ent)
                    continue;
                if (touch.v.touch == 0 || touch.v.solid != Solids.SOLID_TRIGGER)
                    continue;
                if (ent.v.absmin.x > touch.v.absmax.x || ent.v.absmin.y > touch.v.absmax.y ||
                    ent.v.absmin.z > touch.v.absmax.z || ent.v.absmax.x < touch.v.absmin.x ||
                    ent.v.absmax.y < touch.v.absmin.y || ent.v.absmax.z < touch.v.absmin.z)
                    continue;

                int old_self = Progs.GlobalStruct.self;
                int old_other = Progs.GlobalStruct.other;

                Progs.GlobalStruct.self = EdictToProg(touch);
                Progs.GlobalStruct.other = EdictToProg(ent);
                Progs.GlobalStruct.time = (float)sv.time;
                Progs.Execute(touch.v.touch);

                Progs.GlobalStruct.self = old_self;
                Progs.GlobalStruct.other = old_other;
            }

            // recurse down both sides
            if (node.axis == -1)
                return;

            if (Mathlib.Comp(ref ent.v.absmax, node.axis) > node.dist)
                TouchLinks(ent, node.children[0]);
            if (Mathlib.Comp(ref ent.v.absmin, node.axis) < node.dist)
                TouchLinks(ent, node.children[1]);
        }

        /// <summary>
        /// SV_ClipMoveToEntity
        /// Handles selection or creation of a clipping hull, and offseting (and
        /// eventually rotation) of the end points
        /// </summary>
        static trace_t ClipMoveToEntity (edict_t ent, ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end)
        {
            trace_t trace = new trace_t();
            // fill in a default trace
            trace.fraction = 1;
            trace.allsolid = true;
            trace.endpos = end;

            // get the clipping hull
            Vector3 offset;
            hull_t hull = HullForEntity(ent, ref mins, ref maxs, out offset);

            Vector3 start_l = start - offset;
            Vector3 end_l = end - offset;

            // trace a line through the apropriate clipping hull
            RecursiveHullCheck(hull, hull.firstclipnode, 0, 1, ref start_l, ref end_l, trace);

            // fix trace up by the offset
            if (trace.fraction != 1)
                trace.endpos += offset;

            // did we clip the move?
            if (trace.fraction < 1 || trace.startsolid)
                trace.ent = ent;

            return trace;
        }

        /// <summary>
        /// SV_MoveBounds
        /// </summary>
        static void MoveBounds(ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, out Vector3 boxmins, out Vector3 boxmaxs)
        {
            boxmins = Vector3.ComponentMin(start, end) + mins - Vector3.One;
            boxmaxs = Vector3.ComponentMax(start, end) + maxs + Vector3.One;
        }

        /// <summary>
        /// SV_ClipToLinks
        /// Mins and maxs enclose the entire area swept by the move
        /// </summary>
        static void ClipToLinks(areanode_t node, moveclip_t clip)
        {
            link_t next;
            trace_t trace;

            // touch linked edicts
            for (link_t l = node.solid_edicts.Next; l != node.solid_edicts; l = next)
            {
                next = l.Next;
                edict_t touch = (edict_t)l.Owner;// EDICT_FROM_AREA(l);
                if (touch.v.solid == Solids.SOLID_NOT)
                    continue;
                if (touch == clip.passedict)
                    continue;
                if (touch.v.solid == Solids.SOLID_TRIGGER)
                    Sys.Error("Trigger in clipping list");

                if (clip.type == MOVE_NOMONSTERS && touch.v.solid != Solids.SOLID_BSP)
                    continue;

                if (clip.boxmins.X > touch.v.absmax.x || clip.boxmins.Y > touch.v.absmax.y ||
                    clip.boxmins.Z > touch.v.absmax.z || clip.boxmaxs.X < touch.v.absmin.x ||
                    clip.boxmaxs.Y < touch.v.absmin.y || clip.boxmaxs.Z < touch.v.absmin.z)
                    continue;

                if (clip.passedict != null && clip.passedict.v.size.x != 0 && touch.v.size.x == 0)
                    continue;	// points never interact

                // might intersect, so do an exact clip
                if (clip.trace.allsolid)
                    return;
                if (clip.passedict != null)
                {
                    if (ProgToEdict(touch.v.owner) == clip.passedict)
                        continue;	// don't clip against own missiles
                    if (ProgToEdict(clip.passedict.v.owner) == touch)
                        continue;	// don't clip against owner
                }

                if (((int)touch.v.flags & EdictFlags.FL_MONSTER) != 0)
                    trace = ClipMoveToEntity(touch, ref clip.start, ref clip.mins2, ref clip.maxs2, ref clip.end);
                else
                    trace = ClipMoveToEntity(touch, ref clip.start, ref clip.mins, ref clip.maxs, ref clip.end);

                if (trace.allsolid || trace.startsolid || trace.fraction < clip.trace.fraction)
                {
                    trace.ent = touch;
                    if (clip.trace.startsolid)
                    {
                        clip.trace = trace;
                        clip.trace.startsolid = true;
                    }
                    else
                        clip.trace = trace;
                }
                else if (trace.startsolid)
                    clip.trace.startsolid = true;
            }

            // recurse down both sides
            if (node.axis == -1)
                return;

            if (Mathlib.Comp(ref clip.boxmaxs, node.axis) > node.dist)
                ClipToLinks(node.children[0], clip);
            if (Mathlib.Comp(ref clip.boxmins, node.axis) < node.dist)
                ClipToLinks(node.children[1], clip);
        }

        /// <summary>
        /// SV_HullPointContents
        /// </summary>
        static int HullPointContents(hull_t hull, int num, ref Vector3 p)
        {
            while (num >= 0)
            {
                if (num < hull.firstclipnode || num > hull.lastclipnode)
                    Sys.Error("SV_HullPointContents: bad node number");

                short[] node_children = hull.clipnodes[num].children;
                mplane_t plane = hull.planes[hull.clipnodes[num].planenum];
                float d;
                if (plane.type < 3)
                    d = Mathlib.Comp(ref p, plane.type) - plane.dist;
                else
                    d = Vector3.Dot(plane.normal, p) - plane.dist;
                if (d < 0)
                    num = node_children[1];
                else
                    num = node_children[0];
            }

            return num;
        }

    }
}
