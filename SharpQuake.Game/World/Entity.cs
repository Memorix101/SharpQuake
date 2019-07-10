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

namespace SharpQuake.Game.World
{
    public class Entity
    {
        public System.Boolean forcelink;		// model changed
        public Int32 update_type;
        public EntityState baseline;		// to fill in defaults in updates
        public Double msgtime;		// time of last update
        public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)
        public Vector3 origin;
        public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
        public Vector3 angles;
        public Model model;			// NULL = no model
        public EFrag efrag;			// linked list of efrags
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

        public MemoryNode topnode;		// for bmodels, first world node
        //  that splits bmodel, or NULL if
        //  not split

        public void Clear( )
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

        public Entity( )
        {
            msg_origins = new Vector3[2];
            msg_angles = new Vector3[2];
        }
    } // entity_t;
}
