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
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.BSP;
using SharpQuake.Framework.IO;

// view.h
// view.c -- player eye positioning

// The view is allowed to move slightly from it's true position for bobbing,
// but if it exceeds 8 pixels linear distance (spherical, not box), the list of
// entities sent from the server may not include everything in the pvs, especially
// when crossing a water boudnary.

namespace SharpQuake
{
	/// <summary>
	/// V_functions
	/// </summary>
	public class View
	{
		public Single Crosshair
		{
			get
			{
				return Host.Cvars.Crosshair.Get<Single>();
			}
		}

		public Single Gamma
		{
			get
			{
				return Host.Cvars.Gamma.Get<Single>();
			}
		}

		public Color4 Blend;
		private static readonly Vector3 SmallOffset = Vector3.One / 32f;

		private Byte[] _GammaTable; // [256];	// palette is sent through this
		private cshift_t _CShift_empty;// = { { 130, 80, 50 }, 0 };
		private cshift_t _CShift_water;// = { { 130, 80, 50 }, 128 };
		private cshift_t _CShift_slime;// = { { 0, 25, 5 }, 150 };
		private cshift_t _CShift_lava;// = { { 255, 80, 0 }, 150 };

		// v_blend[4]		// rgba 0.0 - 1.0
		private Byte[,] _Ramps = new Byte[3, 256]; // ramps[3][256]

		private Vector3 _Forward; // vec3_t forward
		private Vector3 _Right; // vec3_t right
		private Vector3 _Up; // vec3_t up

		private Single _DmgTime; // v_dmg_time
		private Single _DmgRoll; // v_dmg_roll
		private Single _DmgPitch; // v_dmg_pitch

		private Single _OldZ = 0; // static oldz  from CalcRefdef()
		private Single _OldYaw = 0; // static oldyaw from CalcGunAngle
		private Single _OldPitch = 0; // static oldpitch from CalcGunAngle
		private Single _OldGammaValue; // static float oldgammavalue from CheckGamma

		// Instances
		private Host Host
		{
			get;
			set;
		}

		// V_Init
		public void Initialise( )
		{
			InitialiseCommands();
			InitialiseClientVariables();
		}

		private void InitialiseCommands()
		{
			Host.Commands.Add( "v_cshift", CShift_f );
			Host.Commands.Add( "bf", BonusFlash_f );
			Host.Commands.Add( "centerview", StartPitchDrift );
		}

		private void InitialiseClientVariables()
		{
			if ( Host.Cvars.LcdX == null )
			{
				Host.Cvars.LcdX = Host.CVars.Add( "lcd_x", 0f );
				Host.Cvars.LcdYaw = Host.CVars.Add( "lcd_yaw", 0f );

				Host.Cvars.ScrOfsX = Host.CVars.Add( "scr_ofsx", 0f );
				Host.Cvars.ScrOfsY = Host.CVars.Add( "scr_ofsy", 0f );
				Host.Cvars.ScrOfsZ = Host.CVars.Add( "scr_ofsz", 0f );

				Host.Cvars.ClRollSpeed = Host.CVars.Add( "cl_rollspeed", 200f );
				Host.Cvars.ClRollAngle = Host.CVars.Add( "cl_rollangle", 2.0f );

				Host.Cvars.ClBob = Host.CVars.Add( "cl_bob", 0.02f );
				Host.Cvars.ClBobCycle = Host.CVars.Add( "cl_bobcycle", 0.6f );
				Host.Cvars.ClBobUp = Host.CVars.Add( "cl_bobup", 0.5f );

				Host.Cvars.KickTime = Host.CVars.Add( "v_kicktime", 0.5f );
				Host.Cvars.KickRoll = Host.CVars.Add( "v_kickroll", 0.6f );
				Host.Cvars.KickPitch = Host.CVars.Add( "v_kickpitch", 0.6f );

				Host.Cvars.IYawCycle = Host.CVars.Add( "v_iyaw_cycle", 2f );
				Host.Cvars.IRollCycle = Host.CVars.Add( "v_iroll_cycle", 0.5f );
				Host.Cvars.IPitchCycle = Host.CVars.Add( "v_ipitch_cycle", 1f );
				Host.Cvars.IYawLevel = Host.CVars.Add( "v_iyaw_level", 0.3f );
				Host.Cvars.IRollLevel = Host.CVars.Add( "v_iroll_level", 0.1f );
				Host.Cvars.IPitchLevel = Host.CVars.Add( "v_ipitch_level", 0.3f );

				Host.Cvars.IdleScale = Host.CVars.Add( "v_idlescale", 0f );

				Host.Cvars.Crosshair = Host.CVars.Add( "crosshair", 0f, ClientVariableFlags.Archive );
				Host.Cvars.ClCrossX = Host.CVars.Add( "cl_crossx", 0f );
				Host.Cvars.ClCrossY = Host.CVars.Add( "cl_crossy", 0f );

				Host.Cvars.glCShiftPercent = Host.CVars.Add( "gl_cshiftpercent", 100f );

				Host.Cvars.CenterMove = Host.CVars.Add( "v_centermove", 0.15f );
				Host.Cvars.CenterSpeed = Host.CVars.Add( "v_centerspeed", 500f );

				BuildGammaTable( 1.0f );    // no gamma yet
				Host.Cvars.Gamma = Host.CVars.Add( "gamma", 1f, ClientVariableFlags.Archive );
			}
		}

		/// <summary>
		/// V_RenderView
		/// The player's clipping box goes from (-16 -16 -24) to (16 16 32) from
		/// the entity origin, so any view position inside that will be valid
		/// </summary>
		public void RenderView( )
		{
			if ( Host.Console.ForcedUp )
				return;

			// don't allow cheats in multiplayer
			if ( Host.Client.cl.maxclients > 1 )
			{
				Host.CVars.Set( "scr_ofsx", 0f );
				Host.CVars.Set( "scr_ofsy", 0f );
				Host.CVars.Set( "scr_ofsz", 0f );
			}

			if ( Host.Client.cl.intermission > 0 )
			{
				// intermission / finale rendering
				CalcIntermissionRefDef();
			}
			else if ( !Host.Client.cl.paused )
				CalcRefDef();

			Host.RenderContext.PushDlights();

			if ( Host.Cvars.LcdX.Get<Single>() != 0 )
			{
				//
				// render two interleaved views
				//
				var vid = Host.Screen.vid;
				var rdef = Host.RenderContext.RefDef;

				vid.rowbytes <<= 1;
				vid.aspect *= 0.5f;

				rdef.viewangles.Y -= Host.Cvars.LcdYaw.Get<Single>();
				rdef.vieworg -= _Right * Host.Cvars.LcdX.Get<Single>();

				Host.RenderContext.RenderView();

				// ???????? vid.buffer += vid.rowbytes>>1;

				Host.RenderContext.PushDlights();

				rdef.viewangles.Y += Host.Cvars.LcdYaw.Get<Single>() * 2;
				rdef.vieworg += _Right * Host.Cvars.LcdX.Get<Single>() * 2;

				Host.RenderContext.RenderView();

				// ????????? vid.buffer -= vid.rowbytes>>1;

				rdef.vrect.height <<= 1;

				vid.rowbytes >>= 1;
				vid.aspect *= 2;
			}
			else
			{
				Host.RenderContext.RenderView();
			}
		}

		/// <summary>
		/// V_CalcRoll
		/// Used by view and sv_user
		/// </summary>
		public Single CalcRoll( ref Vector3 angles, ref Vector3 velocity )
		{
			MathLib.AngleVectors( ref angles, out _Forward, out _Right, out _Up );
			var side = Vector3.Dot( velocity, _Right );
			Single sign = side < 0 ? -1 : 1;
			side = Math.Abs( side );

			var value = Host.Cvars.ClRollAngle.Get<Single>();
			if ( side < Host.Cvars.ClRollSpeed.Get<Single>() )
				side = side * value / Host.Cvars.ClRollSpeed.Get<Single>();
			else
				side = value;

			return side * sign;
		}

		// V_UpdatePalette
		public void UpdatePalette( )
		{
			CalcPowerupCshift();

			var isnew = false;

			var cl = Host.Client.cl;
			for ( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
			{
				if ( cl.cshifts[i].percent != cl.prev_cshifts[i].percent )
				{
					isnew = true;
					cl.prev_cshifts[i].percent = cl.cshifts[i].percent;
				}
				for ( var j = 0; j < 3; j++ )
					if ( cl.cshifts[i].destcolor[j] != cl.prev_cshifts[i].destcolor[j] )
					{
						isnew = true;
						cl.prev_cshifts[i].destcolor[j] = cl.cshifts[i].destcolor[j];
					}
			}

			// drop the damage value
			cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent -= ( Int32 ) ( Host.FrameTime * 150 );
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;

			// drop the bonus value
			cl.cshifts[ColorShift.CSHIFT_BONUS].percent -= ( Int32 ) ( Host.FrameTime * 100 );
			if ( cl.cshifts[ColorShift.CSHIFT_BONUS].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 0;

			var force = CheckGamma();
			if ( !isnew && !force )
				return;

			CalcBlend();

			var a = Blend.A;
			var r = 255 * Blend.R * a;
			var g = 255 * Blend.G * a;
			var b = 255 * Blend.B * a;

			a = 1 - a;
			for ( var i = 0; i < 256; i++ )
			{
				var ir = ( Int32 ) ( i * a + r );
				var ig = ( Int32 ) ( i * a + g );
				var ib = ( Int32 ) ( i * a + b );
				if ( ir > 255 )
					ir = 255;
				if ( ig > 255 )
					ig = 255;
				if ( ib > 255 )
					ib = 255;

				_Ramps[0, i] = _GammaTable[ir];
				_Ramps[1, i] = _GammaTable[ig];
				_Ramps[2, i] = _GammaTable[ib];
			}

			var basepal = Host.BasePal;
			var offset = 0;
			var newpal = new Byte[768];

			for ( var i = 0; i < 256; i++ )
			{
				Int32 ir = basepal[offset + 0];
				Int32 ig = basepal[offset + 1];
				Int32 ib = basepal[offset + 2];

				newpal[offset + 0] = _Ramps[0, ir];
				newpal[offset + 1] = _Ramps[1, ig];
				newpal[offset + 2] = _Ramps[2, ib];

				offset += 3;
			}

			ShiftPalette( newpal );
		}

		// V_StartPitchDrift
		public void StartPitchDrift( CommandMessage msg )
		{
			var cl = Host.Client.cl;
			if ( cl.laststop == cl.time )
			{
				return; // something else is keeping it from drifting
			}
			if ( cl.nodrift || cl.pitchvel == 0 )
			{
				cl.pitchvel = Host.Cvars.CenterSpeed.Get<Single>();
				cl.nodrift = false;
				cl.driftmove = 0;
			}
		}

		// V_StopPitchDrift
		public void StopPitchDrift( )
		{
			var cl = Host.Client.cl;
			cl.laststop = cl.time;
			cl.nodrift = true;
			cl.pitchvel = 0;
		}

		/// <summary>
		/// V_CalcBlend
		/// </summary>
		public void CalcBlend( )
		{
			Single r = 0;
			Single g = 0;
			Single b = 0;
			Single a = 0;

			var cshifts = Host.Client.cl.cshifts;

			if ( Host.Cvars.glCShiftPercent.Get<Single>() != 0 )
			{
				for ( var j = 0; j < ColorShift.NUM_CSHIFTS; j++ )
				{
					var a2 = ( ( cshifts[j].percent * Host.Cvars.glCShiftPercent.Get<Single>() ) / 100.0f ) / 255.0f;

					if ( a2 == 0 )
						continue;

					a = a + a2 * ( 1 - a );

					a2 = a2 / a;
					r = r * ( 1 - a2 ) + cshifts[j].destcolor[0] * a2;
					g = g * ( 1 - a2 ) + cshifts[j].destcolor[1] * a2;
					b = b * ( 1 - a2 ) + cshifts[j].destcolor[2] * a2;
				}
			}

			Blend.R = r / 255.0f;
			Blend.G = g / 255.0f;
			Blend.B = b / 255.0f;
			Blend.A = a;
			if ( Blend.A > 1 )
				Blend.A = 1;
			if ( Blend.A < 0 )
				Blend.A = 0;
		}

		// V_ParseDamage
		public void ParseDamage( )
		{
			var armor = Host.Network.Reader.ReadByte();
			var blood = Host.Network.Reader.ReadByte();
			var from = Host.Network.Reader.ReadCoords();

			var count = blood * 0.5f + armor * 0.5f;
			if ( count < 10 )
				count = 10;

			var cl = Host.Client.cl;
			cl.faceanimtime = ( Single ) cl.time + 0.2f; // put sbar face into pain frame

			cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent += ( Int32 ) ( 3 * count );
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent > 150 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 150;

			if ( armor > blood )
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 200;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 100;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 100;
			}
			else if ( armor != 0 )
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 220;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 50;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 50;
			}
			else
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 255;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 0;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 0;
			}

			//
			// calculate view angle kicks
			//
			var ent = Host.Client.Entities[cl.viewentity];

			from -= ent.origin; //  VectorSubtract (from, ent->origin, from);
			MathLib.Normalize( ref from );

			Vector3 forward, right, up;
			MathLib.AngleVectors( ref ent.angles, out forward, out right, out up );

			var side = Vector3.Dot( from, right );

			_DmgRoll = count * side * Host.Cvars.KickRoll.Get<Single>();

			side = Vector3.Dot( from, forward );
			_DmgPitch = count * side * Host.Cvars.KickPitch.Get<Single>();

			_DmgTime = Host.Cvars.KickTime.Get<Single>();
		}

		/// <summary>
		/// V_SetContentsColor
		/// Underwater, lava, etc each has a color shift
		/// </summary>
		public void SetContentsColor( Int32 contents )
		{
			switch ( ( Q1Contents ) contents )
			{
				case Q1Contents.Empty:
				case Q1Contents.Solid:
					Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_empty;
					break;

				case Q1Contents.Lava:
					Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_lava;
					break;

				case Q1Contents.Slime:
					Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_slime;
					break;

				default:
					Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_water;
					break;
			}
		}

		// BuildGammaTable
		private void BuildGammaTable( Single g )
		{
			if ( g == 1.0f )
			{
				for ( var i = 0; i < 256; i++ )
				{
					_GammaTable[i] = ( Byte ) i;
				}
			}
			else
			{
				for ( var i = 0; i < 256; i++ )
				{
					var inf = ( Int32 ) ( 255 * Math.Pow( ( i + 0.5 ) / 255.5, g ) + 0.5 );
					if ( inf < 0 )
						inf = 0;
					if ( inf > 255 )
						inf = 255;
					_GammaTable[i] = ( Byte ) inf;
				}
			}
		}

		// V_cshift_f
		private void CShift_f( CommandMessage msg )
		{
			Int32.TryParse( msg.Parameters[0], out _CShift_empty.destcolor[0] );
			Int32.TryParse( msg.Parameters[1], out _CShift_empty.destcolor[1] );
			Int32.TryParse( msg.Parameters[2], out _CShift_empty.destcolor[2] );
			Int32.TryParse( msg.Parameters[3], out _CShift_empty.percent );
		}

		// V_BonusFlash_f
		//
		// When you run over an item, the server sends this command
		private void BonusFlash_f( CommandMessage msg )
		{
			var cl = Host.Client.cl;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[0] = 215;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[1] = 186;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[2] = 69;
			cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 50;
		}

		// V_CalcIntermissionRefdef
		private void CalcIntermissionRefDef( )
		{
			// ent is the player model (visible when out of body)
			var ent = Host.Client.ViewEntity;

			// view is the weapon model (only visible from inside body)
			var view = Host.Client.ViewEnt;

			var rdef = Host.RenderContext.RefDef;
			rdef.vieworg = ent.origin;
			rdef.viewangles = ent.angles;
			view.model = null;

			// allways idle in intermission
			AddIdle( 1 );
		}

		// V_CalcRefdef
		private void CalcRefDef( )
		{
			DriftPitch();

			// ent is the player model (visible when out of body)
			var ent = Host.Client.ViewEntity;
			// view is the weapon model (only visible from inside body)
			var view = Host.Client.ViewEnt;

			// transform the view offset by the model's matrix to get the offset from
			// model origin for the view
			ent.angles.Y = Host.Client.cl.viewangles.Y; // the model should face the view dir
			ent.angles.X = -Host.Client.cl.viewangles.X;    // the model should face the view dir

			var bob = CalcBob();

			var rdef = Host.RenderContext.RefDef;
			var cl = Host.Client.cl;

			// refresh position
			rdef.vieworg = ent.origin;
			rdef.vieworg.Z += cl.viewheight + bob;

			// never let it sit exactly on a node line, because a water plane can
			// dissapear when viewed with the eye exactly on it.
			// the server protocol only specifies to 1/16 pixel, so add 1/32 in each axis
			rdef.vieworg += SmallOffset;
			rdef.viewangles = cl.viewangles;

			CalcViewRoll();
			AddIdle( Host.Cvars.IdleScale.Get<Single>() );

			// offsets
			var angles = ent.angles;
			angles.X = -angles.X; // because entity pitches are actually backward

			Vector3 forward, right, up;
			MathLib.AngleVectors( ref angles, out forward, out right, out up );

			rdef.vieworg += forward * Host.Cvars.ScrOfsX.Get<Single>() + right * Host.Cvars.ScrOfsY.Get<Single>() + up * Host.Cvars.ScrOfsZ.Get<Single>();

			BoundOffsets();

			// set up gun position
			view.angles = cl.viewangles;

			CalcGunAngle();

			view.origin = ent.origin;
			view.origin.Z += cl.viewheight;
			view.origin += forward * bob * 0.4f;
			view.origin.Z += bob;

			// fudge position around to keep amount of weapon visible
			// roughly equal with different FOV
			var viewSize = Host.Screen.ViewSize.Get<Single>(); // scr_viewsize

			if ( viewSize == 110 )
				view.origin.Z += 1;
			else if ( viewSize == 100 )
				view.origin.Z += 2;
			else if ( viewSize == 90 )
				view.origin.Z += 1;
			else if ( viewSize == 80 )
				view.origin.Z += 0.5f;

			view.model = cl.model_precache[cl.stats[QStatsDef.STAT_WEAPON]];
			view.frame = cl.stats[QStatsDef.STAT_WEAPONFRAME];
			view.colormap = Host.Screen.vid.colormap;

			// set up the refresh position
			rdef.viewangles += cl.punchangle;

			// smooth out stair step ups
			if ( cl.onground && ent.origin.Z - _OldZ > 0 )
			{
				var steptime = ( Single ) ( cl.time - cl.oldtime );
				if ( steptime < 0 )
					steptime = 0;

				_OldZ += steptime * 80;
				if ( _OldZ > ent.origin.Z )
					_OldZ = ent.origin.Z;
				if ( ent.origin.Z - _OldZ > 12 )
					_OldZ = ent.origin.Z - 12;
				rdef.vieworg.Z += _OldZ - ent.origin.Z;
				view.origin.Z += _OldZ - ent.origin.Z;
			}
			else
				_OldZ = ent.origin.Z;

			if ( Host.ChaseView.IsActive )
				Host.ChaseView.Update();
		}

		// V_AddIdle
		//
		// Idle swaying
		private void AddIdle( Single idleScale )
		{
			var time = Host.Client.cl.time;
			var v = new Vector3(
				( Single ) ( Math.Sin( time * Host.Cvars.IPitchCycle.Get<Single>() ) * Host.Cvars.IPitchLevel.Get<Single>() ),
				( Single ) ( Math.Sin( time * Host.Cvars.IYawCycle.Get<Single>() ) * Host.Cvars.IYawLevel.Get<Single>() ),
				( Single ) ( Math.Sin( time * Host.Cvars.IRollCycle.Get<Single>() ) * Host.Cvars.IRollLevel.Get<Single>() ) );
			Host.RenderContext.RefDef.viewangles += v * idleScale;
		}

		// V_DriftPitch
		//
		// Moves the client pitch angle towards cl.idealpitch sent by the server.
		//
		// If the user is adjusting pitch manually, either with lookup/lookdown,
		// mlook and mouse, or klook and keyboard, pitch drifting is constantly stopped.
		//
		// Drifting is enabled when the center view key is hit, mlook is released and
		// lookspring is non 0, or when
		private void DriftPitch( )
		{
			var cl = Host.Client.cl;
			if ( Host.NoClipAngleHack || !cl.onground || Host.Client.cls.demoplayback )
			{
				cl.driftmove = 0;
				cl.pitchvel = 0;
				return;
			}

			// don't count small mouse motion
			if ( cl.nodrift )
			{
				if ( Math.Abs( cl.cmd.forwardmove ) < Host.Client.ForwardSpeed )
					cl.driftmove = 0;
				else
					cl.driftmove += ( Single ) Host.FrameTime;

				if ( cl.driftmove > Host.Cvars.CenterMove.Get<Single>() )
				{
					StartPitchDrift( null );
				}
				return;
			}

			var delta = cl.idealpitch - cl.viewangles.X;
			if ( delta == 0 )
			{
				cl.pitchvel = 0;
				return;
			}

			var move = ( Single ) Host.FrameTime * cl.pitchvel;
			cl.pitchvel += ( Single ) Host.FrameTime * Host.Cvars.CenterSpeed.Get<Single>();

			if ( delta > 0 )
			{
				if ( move > delta )
				{
					cl.pitchvel = 0;
					move = delta;
				}
				cl.viewangles.X += move;
			}
			else if ( delta < 0 )
			{
				if ( move > -delta )
				{
					cl.pitchvel = 0;
					move = -delta;
				}
				cl.viewangles.X -= move;
			}
		}

		// V_CalcBob
		private Single CalcBob( )
		{
			var cl = Host.Client.cl;
			var bobCycle = Host.Cvars.ClBobCycle.Get<Single>();
			var bobUp = Host.Cvars.ClBobUp.Get<Single>();
			var cycle = ( Single ) ( cl.time - ( Int32 ) ( cl.time / bobCycle ) * bobCycle );
			cycle /= bobCycle;
			if ( cycle < bobUp )
				cycle = ( Single ) Math.PI * cycle / bobUp;
			else
				cycle = ( Single ) ( Math.PI + Math.PI * ( cycle - bobUp ) / ( 1.0 - bobUp ) );

			// bob is proportional to velocity in the xy plane
			// (don't count Z, or jumping messes it up)
			var tmp = cl.velocity.Xy;
			Double bob = tmp.Length * Host.Cvars.ClBob.Get<Single>();
			bob = bob * 0.3 + bob * 0.7 * Math.Sin( cycle );
			if ( bob > 4 )
				bob = 4;
			else if ( bob < -7 )
				bob = -7;
			return ( Single ) bob;
		}

		// V_CalcViewRoll
		//
		// Roll is induced by movement and damage
		private void CalcViewRoll( )
		{
			var cl = Host.Client.cl;
			var rdef = Host.RenderContext.RefDef;
			var side = CalcRoll( ref Host.Client.ViewEntity.angles, ref cl.velocity );
			rdef.viewangles.Z += side;

			if ( _DmgTime > 0 )
			{
				rdef.viewangles.Z += _DmgTime / Host.Cvars.KickTime.Get<Single>() * _DmgRoll;
				rdef.viewangles.X += _DmgTime / Host.Cvars.KickTime.Get<Single>() * _DmgPitch;
				_DmgTime -= ( Single ) Host.FrameTime;
			}

			if ( cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
			{
				rdef.viewangles.Z = 80; // dead view angle
				return;
			}
		}

		// V_BoundOffsets
		private void BoundOffsets( )
		{
			var ent = Host.Client.ViewEntity;

			// absolutely bound refresh reletive to entity clipping hull
			// so the view can never be inside a solid wall
			var rdef = Host.RenderContext.RefDef;
			if ( rdef.vieworg.X < ent.origin.X - 14 )
				rdef.vieworg.X = ent.origin.X - 14;
			else if ( rdef.vieworg.X > ent.origin.X + 14 )
				rdef.vieworg.X = ent.origin.X + 14;

			if ( rdef.vieworg.Y < ent.origin.Y - 14 )
				rdef.vieworg.Y = ent.origin.Y - 14;
			else if ( rdef.vieworg.Y > ent.origin.Y + 14 )
				rdef.vieworg.Y = ent.origin.Y + 14;

			if ( rdef.vieworg.Z < ent.origin.Z - 22 )
				rdef.vieworg.Z = ent.origin.Z - 22;
			else if ( rdef.vieworg.Z > ent.origin.Z + 30 )
				rdef.vieworg.Z = ent.origin.Z + 30;
		}

		/// <summary>
		/// CalcGunAngle
		/// </summary>
		private void CalcGunAngle( )
		{
			var rdef = Host.RenderContext.RefDef;
			var yaw = rdef.viewangles.Y;
			var pitch = -rdef.viewangles.X;

			yaw = AngleDelta( yaw - rdef.viewangles.Y ) * 0.4f;
			if ( yaw > 10 )
				yaw = 10;
			if ( yaw < -10 )
				yaw = -10;
			pitch = AngleDelta( -pitch - rdef.viewangles.X ) * 0.4f;
			if ( pitch > 10 )
				pitch = 10;
			if ( pitch < -10 )
				pitch = -10;
			var move = ( Single ) Host.FrameTime * 20;
			if ( yaw > _OldYaw )
			{
				if ( _OldYaw + move < yaw )
					yaw = _OldYaw + move;
			}
			else
			{
				if ( _OldYaw - move > yaw )
					yaw = _OldYaw - move;
			}

			if ( pitch > _OldPitch )
			{
				if ( _OldPitch + move < pitch )
					pitch = _OldPitch + move;
			}
			else
			{
				if ( _OldPitch - move > pitch )
					pitch = _OldPitch - move;
			}

			_OldYaw = yaw;
			_OldPitch = pitch;

			var cl = Host.Client.cl;
			cl.viewent.angles.Y = rdef.viewangles.Y + yaw;
			cl.viewent.angles.X = -( rdef.viewangles.X + pitch );

			var idleScale = Host.Cvars.IdleScale.Get<Single>();
			cl.viewent.angles.Z -= ( Single ) ( idleScale * Math.Sin( cl.time * Host.Cvars.IRollCycle.Get<Single>() ) * Host.Cvars.IRollLevel.Get<Single>() );
			cl.viewent.angles.X -= ( Single ) ( idleScale * Math.Sin( cl.time * Host.Cvars.IPitchCycle.Get<Single>() ) * Host.Cvars.IPitchLevel.Get<Single>() );
			cl.viewent.angles.Y -= ( Single ) ( idleScale * Math.Sin( cl.time * Host.Cvars.IYawCycle.Get<Single>() ) * Host.Cvars.IYawLevel.Get<Single>() );
		}

		// angledelta()
		private Single AngleDelta( Single a )
		{
			a = MathLib.AngleMod( a );
			if ( a > 180 )
				a -= 360;
			return a;
		}

		// V_CalcPowerupCshift
		private void CalcPowerupCshift( )
		{
			var cl = Host.Client.cl;
			if ( cl.HasItems( QItemsDef.IT_QUAD ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
			}
			else if ( cl.HasItems( QItemsDef.IT_SUIT ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 20;
			}
			else if ( cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 100;
			}
			else if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
			}
			else
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 0;
		}

		// V_CheckGamma
		private Boolean CheckGamma( )
		{
			if ( Host.Cvars.Gamma.Get<Single>() == _OldGammaValue )
				return false;

			_OldGammaValue = Host.Cvars.Gamma.Get<Single>();

			BuildGammaTable( Host.Cvars.Gamma.Get<Single>() );
			Host.Screen.vid.recalc_refdef = true;   // force a surface cache flush

			return true;
		}

		// VID_ShiftPalette from gl_vidnt.c
		private void ShiftPalette( Byte[] palette )
		{
			//	VID_SetPalette (palette);
			//	gammaworks = SetDeviceGammaRamp (maindc, ramps);
		}

		public View( Host host )
		{
			Host = host;

			_GammaTable = new Byte[256];

			_CShift_empty = new cshift_t( new[] { 130, 80, 50 }, 0 );
			_CShift_water = new cshift_t( new[] { 130, 80, 50 }, 128 );
			_CShift_slime = new cshift_t( new[] { 0, 25, 5 }, 150 );
			_CShift_lava = new cshift_t( new[] { 255, 80, 0 }, 150 );
		}
	}
}
