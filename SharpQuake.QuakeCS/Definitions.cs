using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.World;

namespace SharpQuake.QuakeCS
{
    public static class Definitions
	{
		//
		// system globals
		//

		public static Entity Self
		{
			get;
			set;
		}

		public static Entity Other
		{
			get;
			set;
		}

		public static Entity World
		{
			get;
			set;
		}

		public static Single Time
		{
			get;
			set;
		}

		public static Single FrameTime
		{
			get;
			set;
		}

		// force all entities to touch triggers
		// next frame.  this is needed because
		// non-moving things don't normally scan
		// for triggers, and when a trigger is
		// created (like a teleport trigger), it
		// needs to catch everything.
		// decremented each frame, so set to 2
		// to guarantee everything is touched
		public static Single ForceRetouch
		{
			get;
			set;
		}

		public static Single MapName
		{
			get;
			set;
		}

		public static Single Deathmatch
		{
			get;
			set;
		}

		public static Single Coop
		{
			get;
			set;
		}

		public static Single Teamplay
		{
			get;
			set;
		}

		// propagated from level to level, used to
		// keep track of completed episodes
		public static Single ServerFlags
		{
			get;
			set;
		}

		public static Single TotalSecrets
		{
			get;
			set;
		}

		public static Single TotalMonsters
		{
			get;
			set;
		}

		// number of secrets found
		public static Single FoundSecrets
		{
			get;
			set;
		}

		// number of monsters killed
		public static Single KilledMonsters
		{
			get;
			set;
		}

		// spawnparms are used to encode information about clients across server
		// level changes
		// 16
		public static Single[] SpawnParameters
		{
			get;
			set;
		}

		//
		// global variables set by built in functions
		//	
		public static Vector3 Forward
		{
			get;
			set;
		}

		public static Vector3 Up
		{
			get;
			set;
		}

		public static Vector3 Right
		{
			get;
			set;
		}

		// set by traceline / tracebox
		public static Single TraceAllSolid
		{
			get;
			set;
		}

		public static Single TraceStartSolid
		{
			get;
			set;
		}

		public static Single TraceFraction
		{
			get;
			set;
		}

		public static Vector3 TraceEndPosition
		{
			get;
			set;
		}

		public static Vector3 TracePlaneNormal
		{
			get;
			set;
		}

		public static Single TracePlaneDistance
		{
			get;
			set;
		}

		public static Entity TraceEntity
		{
			get;
			set;
		}

		public static Single TraceInOpen
		{
			get;
			set;
		}

		public static Single TraceInWater
		{
			get;
			set;
		}
		
		// destination of single entity writes
		public static Entity MessageEntity
		{
			get;
			set;
		}

		//
		// required prog functions
		//

		public static void main( )                     // only for testing
		{ 
		}

		public static void StartFrame( )
		{
		}

		public static void PlayerPreThink( )
		{
		}
		public static void PlayerPostThink( )
		{
		}

		public static void ClientKill( )
		{
		}

		public static void ClientConnect( )
		{
		}

		public static void PutClientInServer( )    // call after setting the parm1... parms
		{
		}

		public static void ClientDisconnect( )
		{
		}

		public static void SetNewParms( )				// called when a client first connects to
		{												// a server. sets parms so they can be
														// saved off for restarts
		}

		public static void SetChangeParms( )        // call to set parms for self so they can
		{											// be saved for a level transition
		}

		//================================================
		public static void end_sys_globals()       // flag for structure dumping		
		{										   //================================================
		}

		/*
		==============================================================================

					SOURCE FOR ENTVARS_T C STRUCTURE

		==============================================================================
		*/
	}
}
