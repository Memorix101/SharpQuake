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
using OpenTK;
using SharpQuake.Framework;

// client.h

namespace SharpQuake
{
    public struct lightstyle_t
    {
        //public int length;
        public String map; // [MAX_STYLESTRING];
    }

    public enum cactive_t
    {
        ca_dedicated, 		// a dedicated server with no ability to start a client
        ca_disconnected, 	// full screen console with no connection
        ca_connected		// valid netcon, talking to a server
    }

    

    //
    // cl_input
    //
    internal struct kbutton_t
    {
        public Boolean IsDown
        {
            get
            {
                return ( this.state & 1 ) != 0;
            }
        }

        public Int32 down0, down1;        // key nums holding it down
        public Int32 state;			// low bit is down state
    }

    public partial class client
    {
        public client_static_t cls
        {
            get
            {
                return _Static;
            }
        }

        public client_state_t cl
        {
            get
            {
                return _State;
            }
        }

        public Entity[] Entities
        {
            get
            {
                return _Entities;
            }
        }

        /// <summary>
        /// cl_entities[cl.viewentity]
        /// Player model (visible when out of body)
        /// </summary>
        public Entity ViewEntity
        {
            get
            {
                return _Entities[_State.viewentity];
            }
        }

        /// <summary>
        /// cl.viewent
        /// Weapon model (only visible from inside body)
        /// </summary>
        public Entity ViewEnt
        {
            get
            {
                return _State.viewent;
            }
        }

        public Single ForwardSpeed
        {
            get
            {
                return _ForwardSpeed.Value;
            }
        }

        public Boolean LookSpring
        {
            get
            {
                return ( _LookSpring.Value != 0 );
            }
        }

        public Boolean LookStrafe
        {
            get
            {
                return ( _LookStrafe.Value != 0 );
            }
        }

        public dlight_t[] DLights
        {
            get
            {
                return _DLights;
            }
        }

        public lightstyle_t[] LightStyle
        {
            get
            {
                return _LightStyle;
            }
        }

        public Entity[] VisEdicts
        {
            get
            {
                return _VisEdicts;
            }
        }

        public Single Sensitivity
        {
            get
            {
                return _Sensitivity.Value;
            }
        }

        public Single MSide
        {
            get
            {
                return _MSide.Value;
            }
        }

        public Single MYaw
        {
            get
            {
                return _MYaw.Value;
            }
        }

        public Single MPitch
        {
            get
            {
                return _MPitch.Value;
            }
        }

        public Single MForward
        {
            get
            {
                return _MForward.Value;
            }
        }

        public String Name
        {
            get
            {
                return _Name.String;
            }
        }

        public Single Color
        {
            get
            {
                return _Color.Value;
            }
        }
                
        public Int32 NumVisEdicts;

        private client_static_t _Static;
        private client_state_t _State;

        public client()
        {            
            _Static = new client_static_t();
            _State = new client_state_t();
        }

        private EFrag[] _EFrags = new EFrag[ClientDef.MAX_EFRAGS]; // cl_efrags
        private Entity[] _Entities = new Entity[QDef.MAX_EDICTS]; // cl_entities
        private Entity[] _StaticEntities = new Entity[ClientDef.MAX_STATIC_ENTITIES]; // cl_static_entities
        private lightstyle_t[] _LightStyle = new lightstyle_t[QDef.MAX_LIGHTSTYLES]; // cl_lightstyle
        private dlight_t[] _DLights = new dlight_t[ClientDef.MAX_DLIGHTS]; // cl_dlights

        private CVar _Name;// = { "_cl_name", "player", true };
        private CVar _Color;// = { "_cl_color", "0", true };
        private CVar _ShowNet;// = { "cl_shownet", "0" };	// can be 0, 1, or 2
        private CVar _NoLerp;// = { "cl_nolerp", "0" };
        private CVar _LookSpring;// = { "lookspring", "0", true };
        private CVar _LookStrafe;// = { "lookstrafe", "0", true };
        private CVar _Sensitivity;// = { "sensitivity", "3", true };
        private CVar _MPitch;// = { "m_pitch", "0.022", true };
        private CVar _MYaw;// = { "m_yaw", "0.022", true };
        private CVar _MForward;// = { "m_forward", "1", true };
        private CVar _MSide;// = { "m_side", "0.8", true };
        private CVar _UpSpeed;// = { "cl_upspeed", "200" };
        private CVar _ForwardSpeed;// = { "cl_forwardspeed", "200", true };
        private CVar _BackSpeed;// = { "cl_backspeed", "200", true };
        private CVar _SideSpeed;// = { "cl_sidespeed", "350" };
        private CVar _MoveSpeedKey;// = { "cl_movespeedkey", "2.0" };
        private CVar _YawSpeed;// = { "cl_yawspeed", "140" };
        private CVar _PitchSpeed;// = { "cl_pitchspeed", "150" };
        private CVar _AngleSpeedKey;// = { "cl_anglespeedkey", "1.5" };

        // cl_numvisedicts
        private Entity[] _VisEdicts = new Entity[ClientDef.MAX_VISEDICTS]; // cl_visedicts[MAX_VISEDICTS]
    }

    // lightstyle_t;

    internal static class ColorShift
    {
        public const Int32 CSHIFT_CONTENTS = 0;
        public const Int32 CSHIFT_DAMAGE = 1;
        public const Int32 CSHIFT_BONUS = 2;
        public const Int32 CSHIFT_POWERUP = 3;
        public const Int32 NUM_CSHIFTS = 4;
    }

    public class scoreboard_t
    {
        public String name; //[MAX_SCOREBOARDNAME];

        //public float entertime;
        public Int32 frags;

        public Int32 colors;			// two 4 bit fields
        public Byte[] translations; // [VID_GRADES*256];

        public scoreboard_t()
        {
            this.translations = new Byte[vid.VID_GRADES * 256];
        }
    } // scoreboard_t;

    public class cshift_t
    {
        public Int32[] destcolor; // [3];
        public Int32 percent;		// 0-256

        public void Clear()
        {
            this.destcolor[0] = 0;
            this.destcolor[1] = 0;
            this.destcolor[2] = 0;
            this.percent = 0;
        }

        public cshift_t()
        {
            destcolor = new Int32[3];
        }

        public cshift_t( Int32[] destColor, Int32 percent )
        {
            if( destColor.Length != 3 )
            {
                throw new ArgumentException( "destColor must have length of 3 elements!" );
            }
            this.destcolor = destColor;
            this.percent = percent;
        }
    } // cshift_t;

    public class dlight_t
    {
        public Vector3 origin;
        public Single radius;
        public Single die;				// stop lighting after this time
        public Single decay;				// drop this each second
        public Single minlight;			// don't add when contributing less
        public Int32 key;

        public void Clear()
        {
            this.origin = Vector3.Zero;
            this.radius = 0;
            this.die = 0;
            this.decay = 0;
            this.minlight = 0;
            this.key = 0;
        }
    } //dlight_t;

    internal class beam_t
    {
        public Int32 entity;
        public Model model;
        public Single endtime;
        public Vector3 start, end;

        public void Clear()
        {
            this.entity = 0;
            this.model = null;
            this.endtime = 0;
            this.start = Vector3.Zero;
            this.end = Vector3.Zero;
        }
    } // beam_t;

    // cactive_t;

    //
    // the client_static_t structure is persistant through an arbitrary number
    // of server connections
    //
    public class client_static_t
    {
        public cactive_t state;

        // personalization data sent to server
        public String mapstring; // [MAX_QPATH];

        public String spawnparms;//[MAX_MAPSTRING];	// to restart a level

        // demo loop control
        public Int32 demonum;		// -1 = don't play demos

        public String[] demos; // [MAX_DEMOS][MAX_DEMONAME];		// when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing client_state_t)
        public Boolean demorecording;

        public Boolean demoplayback;
        public Boolean timedemo;
        public Int32 forcetrack;			// -1 = use normal cd track
        public IDisposable demofile; // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
        public Int32 td_lastframe;		// to meter out one message a frame
        public Int32 td_startframe;		// host_framecount at start
        public Single td_starttime;		// realtime at second frame of timedemo

        // connection information
        public Int32 signon;			// 0 to SIGNONS

        public qsocket_t netcon; // qsocket_t	*netcon;
        public MessageWriter message; // sizebuf_t	message;		// writing buffer to send to server

        public client_static_t()
        {
            this.demos = new String[ClientDef.MAX_DEMOS];
            this.message = new MessageWriter( 1024 ); // like in Client_Init()
        }
    } // client_static_t;

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
        public Model[] model_precache; // [MAX_MODELS];

        public sfx_t[] sound_precache; // [MAX_SOUNDS];

        public String levelname; // char[40];	// for display on solo scoreboard
        public Int32 viewentity;		// cl_entitites[cl.viewentity] = player
        public Int32 maxclients;
        public Int32 gametype;

        // refresh related state
        public Model worldmodel;	// cl_entitites[0].model

        public EFrag free_efrags; // first free efrag in list
        public Int32 num_entities;	// held in cl_entities array
        public Int32 num_statics;	// held in cl_staticentities array
        public Entity viewent;			// the gun model

        public Int32 cdtrack, looptrack;	// cd audio

        // frag scoreboard
        public scoreboard_t[] scores;		// [cl.maxclients]

        public Boolean HasItems( Int32 item )
        {
            return ( this.items & item ) == item;
        }

        public void Clear()
        {
            this.movemessages = 0;
            this.cmd.Clear();
            Array.Clear( this.stats, 0, this.stats.Length );
            this.items = 0;
            Array.Clear( this.item_gettime, 0, this.item_gettime.Length );
            this.faceanimtime = 0;

            foreach( var cs in this.cshifts )
                cs.Clear();
            foreach( var cs in this.prev_cshifts )
                cs.Clear();

            this.mviewangles[0] = Vector3.Zero;
            this.mviewangles[1] = Vector3.Zero;
            this.viewangles = Vector3.Zero;
            this.mvelocity[0] = Vector3.Zero;
            this.mvelocity[1] = Vector3.Zero;
            this.velocity = Vector3.Zero;
            this.punchangle = Vector3.Zero;

            this.idealpitch = 0;
            this.pitchvel = 0;
            this.nodrift = false;
            this.driftmove = 0;
            this.laststop = 0;

            this.viewheight = 0;
            this.crouch = 0;

            this.paused = false;
            this.onground = false;
            this.inwater = false;

            this.intermission = 0;
            this.completed_time = 0;

            this.mtime[0] = 0;
            this.mtime[1] = 0;
            this.time = 0;
            this.oldtime = 0;
            this.last_received_message = 0;

            Array.Clear( this.model_precache, 0, this.model_precache.Length );
            Array.Clear( this.sound_precache, 0, this.sound_precache.Length );

            this.levelname = null;
            this.viewentity = 0;
            this.maxclients = 0;
            this.gametype = 0;

            this.worldmodel = null;
            this.free_efrags = null;
            this.num_entities = 0;
            this.num_statics = 0;
            this.viewent.Clear();

            this.cdtrack = 0;
            this.looptrack = 0;

            this.scores = null;
        }

        public client_state_t()
        {
            this.stats = new Int32[QStatsDef.MAX_CL_STATS];
            this.item_gettime = new Single[32]; // ???????????

            this.cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
                this.cshifts[i] = new cshift_t();

            this.prev_cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
                this.prev_cshifts[i] = new cshift_t();

            this.mviewangles = new Vector3[2]; //??????
            this.mvelocity = new Vector3[2];
            this.mtime = new Double[2];
            this.model_precache = new Model[QDef.MAX_MODELS];
            this.sound_precache = new sfx_t[QDef.MAX_SOUNDS];
            this.viewent = new Entity();
        }
    } //client_state_t;

    // usercmd_t;

    // kbutton_t;
}
