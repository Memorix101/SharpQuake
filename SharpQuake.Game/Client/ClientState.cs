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
using SharpQuake.Framework.Definitions;
using SharpQuake.Framework.IO.Sound;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpQuake.Game.Client
{
    //
    // the client_state_t structure is wiped completely at every
    // server signon
    //
    public class client_state_t
    {
        public Int32 movemessages;	// since connecting to this server

        // throw out the first couple, so the player
        // doesn't accidentally do something the
        // first frame
        public usercmd_t cmd;			// last command sent to the server

        // information for local display
        public Int32[] stats; //[MAX_CL_STATS];	// health, etc

        public Int32 items;			// inventory bit flags
        public Single[] item_gettime; //[32];	// cl.time of aquiring item, for blinking
        public Single faceanimtime;	// use anim frame if cl.time < this

        public cshift_t[] cshifts; //[NUM_CSHIFTS];	// color shifts for damage, powerups
        public cshift_t[] prev_cshifts; //[NUM_CSHIFTS];	// and content types

        // the client maintains its own idea of view angles, which are
        // sent to the server each frame.  The server sets punchangle when
        // the view is temporarliy offset, and an angle reset commands at the start
        // of each level and after teleporting.
        public Vector3[] mviewangles; //[2];	// during demo playback viewangles is lerped

        // between these
        public Vector3 viewangles;

        public Vector3[] mvelocity; //[2];	// update by server, used for lean+bob

        // (0 is newest)
        public Vector3 velocity;		// lerped between mvelocity[0] and [1]

        public Vector3 punchangle;		// temporary offset

        // pitch drifting vars
        public Single idealpitch;

        public Single pitchvel;
        public Boolean nodrift;
        public Single driftmove;
        public Double laststop;

        public Single viewheight;
        public Single crouch;			// local amount for smoothing stepups

        public Boolean paused;			// send over by server
        public Boolean onground;
        public Boolean inwater;

        public Int32 intermission;	// don't change view angle, full screen, etc
        public Int32 completed_time;	// latched at intermission start

        public Double[] mtime; //[2];		// the timestamp of last two messages
        public Double time;			// clients view of time, should be between

        // servertime and oldservertime to generate
        // a lerp point for other data
        public Double oldtime;		// previous cl.time, time-oldtime is used

        // to decay light values and smooth step ups

        public Single last_received_message;	// (realtime) for net trouble icon

        //
        // information that is static for the entire time connected to a server
        //
        public ModelData[] model_precache; // [MAX_MODELS];

        public SoundEffect_t[] sound_precache; // [MAX_SOUNDS];

        public String levelname; // char[40];	// for display on solo scoreboard
        public Int32 viewentity;		// cl_entitites[cl.viewentity] = player
        public Int32 maxclients;
        public Int32 gametype;

        // refresh related state
        public BrushModelData worldmodel;	// cl_entitites[0].model

        public EFrag free_efrags; // first free efrag in list
        public Int32 num_entities;	// held in cl_entities array
        public Int32 num_statics;	// held in cl_staticentities array
        public Entity viewent;			// the gun model

        public Int32 cdtrack, looptrack;	// cd audio

        // frag scoreboard
        public scoreboard_t[] scores;		// [cl.maxclients]

        public Boolean HasItems( Int32 item )
        {
            return ( items & item ) == item;
        }

        public void Clear( )
        {
            movemessages = 0;
            cmd.Clear();
            Array.Clear( stats, 0, stats.Length );
            items = 0;
            Array.Clear( item_gettime, 0, item_gettime.Length );
            faceanimtime = 0;

            foreach ( var cs in cshifts )
                cs.Clear();
            foreach ( var cs in prev_cshifts )
                cs.Clear();

            mviewangles[0] = Vector3.Zero;
            mviewangles[1] = Vector3.Zero;
            viewangles = Vector3.Zero;
            mvelocity[0] = Vector3.Zero;
            mvelocity[1] = Vector3.Zero;
            velocity = Vector3.Zero;
            punchangle = Vector3.Zero;

            idealpitch = 0;
            pitchvel = 0;
            nodrift = false;
            driftmove = 0;
            laststop = 0;

            viewheight = 0;
            crouch = 0;

            paused = false;
            onground = false;
            inwater = false;

            intermission = 0;
            completed_time = 0;

            mtime[0] = 0;
            mtime[1] = 0;
            time = 0;
            oldtime = 0;
            last_received_message = 0;

            Array.Clear( model_precache, 0, model_precache.Length );
            Array.Clear( sound_precache, 0, sound_precache.Length );

            levelname = null;
            viewentity = 0;
            maxclients = 0;
            gametype = 0;

            worldmodel = null;
            free_efrags = null;
            num_entities = 0;
            num_statics = 0;
            viewent.Clear();

            cdtrack = 0;
            looptrack = 0;

            scores = null;
        }

        public client_state_t( )
        {
            stats = new Int32[QStatsDef.MAX_CL_STATS];
            item_gettime = new Single[32]; // ???????????

            cshifts = new cshift_t[ColourShiftDef.NUM_CSHIFTS];
            for ( var i = 0; i < ColourShiftDef.NUM_CSHIFTS; i++ )
                cshifts[i] = new cshift_t();

            prev_cshifts = new cshift_t[ColourShiftDef.NUM_CSHIFTS];
            for ( var i = 0; i < ColourShiftDef.NUM_CSHIFTS; i++ )
                prev_cshifts[i] = new cshift_t();

            mviewangles = new Vector3[2]; //??????
            mvelocity = new Vector3[2];
            mtime = new Double[2];
            model_precache = new ModelData[QDef.MAX_MODELS];
            sound_precache = new SoundEffect_t[QDef.MAX_SOUNDS];
            viewent = new Entity();
        }
    } //client_state_t;
}
