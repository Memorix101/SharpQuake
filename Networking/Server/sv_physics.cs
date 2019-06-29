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

namespace SharpQuake
{
    partial class server
    {
        private const float STOP_EPSILON = 0.1f;
        private const int MAX_CLIP_PLANES = 5;
        private const float STEPSIZE = 18;

        /// <summary>
        /// SV_Physics
        /// </summary>
        public static void Physics()
        {
            // let the progs know that a new frame has started
            progs.GlobalStruct.self = EdictToProg( sv.edicts[0] );
            progs.GlobalStruct.other = progs.GlobalStruct.self;
            progs.GlobalStruct.time = (float)sv.time;
            progs.Execute( progs.GlobalStruct.StartFrame );

            //
            // treat each object in turn
            //
            for( int i = 0; i < sv.num_edicts; i++ )
            {
                edict_t ent = sv.edicts[i];
                if( ent.free )
                    continue;

                if( progs.GlobalStruct.force_retouch != 0 )
                {
                    LinkEdict( ent, true );	// force retouch even for stationary
                }

                if( i > 0 && i <= svs.maxclients )
                    Physics_Client( ent, i );
                else
                    switch( (int)ent.v.movetype )
                    {
                        case Movetypes.MOVETYPE_PUSH:
                            Physics_Pusher( ent );
                            break;

                        case Movetypes.MOVETYPE_NONE:
                            Physics_None( ent );
                            break;

                        case Movetypes.MOVETYPE_NOCLIP:
                            Physics_Noclip( ent );
                            break;

                        case Movetypes.MOVETYPE_STEP:
                            Physics_Step( ent );
                            break;

                        case Movetypes.MOVETYPE_TOSS:
                        case Movetypes.MOVETYPE_BOUNCE:
                        case Movetypes.MOVETYPE_FLY:
                        case Movetypes.MOVETYPE_FLYMISSILE:
                            Physics_Toss( ent );
                            break;

                        default:
                            sys.Error( "SV_Physics: bad movetype {0}", (int)ent.v.movetype );
                            break;
                    }
            }

            if( progs.GlobalStruct.force_retouch != 0 )
                progs.GlobalStruct.force_retouch -= 1;

            sv.time += host.FrameTime;
        }

        /// <summary>
        /// SV_Physics_Toss
        /// Toss, bounce, and fly movement.  When onground, do nothing.
        /// </summary>
        private static void Physics_Toss( edict_t ent )
        {
            // regular thinking
            if( !RunThink( ent ) )
                return;

            // if onground, return without moving
            if( ( (int)ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )
                return;

            CheckVelocity( ent );

            // add gravity
            if( ent.v.movetype != Movetypes.MOVETYPE_FLY && ent.v.movetype != Movetypes.MOVETYPE_FLYMISSILE )
                AddGravity( ent );

            // move angles
            mathlib.VectorMA( ref ent.v.angles, (float)host.FrameTime, ref ent.v.avelocity, out ent.v.angles );

            // move origin
            v3f move;
            mathlib.VectorScale( ref ent.v.velocity, (float)host.FrameTime, out move );
            trace_t trace = PushEntity( ent, ref move );

            if( trace.fraction == 1 )
                return;
            if( ent.free )
                return;

            float backoff;
            if( ent.v.movetype == Movetypes.MOVETYPE_BOUNCE )
                backoff = 1.5f;
            else
                backoff = 1;

            ClipVelocity( ref ent.v.velocity, ref trace.plane.normal, out ent.v.velocity, backoff );

            // stop if on ground
            if( trace.plane.normal.Z > 0.7f )
            {
                if( ent.v.velocity.z < 60 || ent.v.movetype != Movetypes.MOVETYPE_BOUNCE )
                {
                    ent.v.flags = (int)ent.v.flags | EdictFlags.FL_ONGROUND;
                    ent.v.groundentity = EdictToProg( trace.ent );
                    ent.v.velocity = default( v3f );
                    ent.v.avelocity = default( v3f );
                }
            }

            // check for in water
            CheckWaterTransition( ent );
        }

        /// <summary>
        /// ClipVelocity
        /// Slide off of the impacting object
        /// returns the blocked flags (1 = floor, 2 = step / wall)
        /// </summary>
        private static int ClipVelocity( ref v3f src, ref Vector3 normal, out v3f dest, float overbounce )
        {
            int blocked = 0;
            if( normal.Z > 0 )
                blocked |= 1;       // floor
            if( normal.Z == 0 )
                blocked |= 2;       // step

            float backoff = ( src.x * normal.X + src.y * normal.Y + src.z * normal.Z ) * overbounce;

            dest.x = src.x - normal.X * backoff;
            dest.y = src.y - normal.Y * backoff;
            dest.z = src.z - normal.Z * backoff;

            if( dest.x > -STOP_EPSILON && dest.x < STOP_EPSILON )
                dest.x = 0;
            if( dest.y > -STOP_EPSILON && dest.y < STOP_EPSILON )
                dest.y = 0;
            if( dest.z > -STOP_EPSILON && dest.z < STOP_EPSILON )
                dest.z = 0;

            return blocked;
        }

        /// <summary>
        /// PushEntity
        /// Does not change the entities velocity at all
        /// </summary>
        private static trace_t PushEntity( edict_t ent, ref v3f push )
        {
            v3f end;
            mathlib.VectorAdd( ref ent.v.origin, ref push, out end );

            trace_t trace;
            if( ent.v.movetype == Movetypes.MOVETYPE_FLYMISSILE )
                trace = Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, MOVE_MISSILE, ent );
            else if( ent.v.solid == Solids.SOLID_TRIGGER || ent.v.solid == Solids.SOLID_NOT )
                // only clip against bmodels
                trace = Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, MOVE_NOMONSTERS, ent );
            else
                trace = Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, MOVE_NORMAL, ent );

            mathlib.Copy( ref trace.endpos, out ent.v.origin );
            LinkEdict( ent, true );

            if( trace.ent != null )
                Impact( ent, trace.ent );

            return trace;
        }

        /// <summary>
        /// SV_CheckWaterTransition
        /// </summary>
        private static void CheckWaterTransition( edict_t ent )
        {
            Vector3 org = common.ToVector( ref ent.v.origin );
            int cont = PointContents( ref org );

            if( ent.v.watertype == 0 )
            {
                // just spawned here
                ent.v.watertype = cont;
                ent.v.waterlevel = 1;
                return;
            }

            if( cont <= Contents.CONTENTS_WATER )
            {
                if( ent.v.watertype == Contents.CONTENTS_EMPTY )
                {
                    // just crossed into water
                    StartSound( ent, 0, "misc/h2ohit1.wav", 255, 1 );
                }
                ent.v.watertype = cont;
                ent.v.waterlevel = 1;
            }
            else
            {
                if( ent.v.watertype != Contents.CONTENTS_EMPTY )
                {
                    // just crossed into water
                    StartSound( ent, 0, "misc/h2ohit1.wav", 255, 1 );
                }
                ent.v.watertype = Contents.CONTENTS_EMPTY;
                ent.v.waterlevel = cont;
            }
        }

        /// <summary>
        /// SV_AddGravity
        /// </summary>
        private static void AddGravity( edict_t ent )
        {
            float val = progs.GetEdictFieldFloat( ent, "gravity" );
            if( val == 0 )
                val = 1;
            ent.v.velocity.z -= (float)( val * _Gravity.Value * host.FrameTime );
        }

        /// <summary>
        /// SV_Physics_Step
        /// </summary>
        private static void Physics_Step( edict_t ent )
        {
            bool hitsound;

            // freefall if not onground
            if( ( (int)ent.v.flags & ( EdictFlags.FL_ONGROUND | EdictFlags.FL_FLY | EdictFlags.FL_SWIM ) ) == 0 )
            {
                if( ent.v.velocity.z < _Gravity.Value * -0.1 )
                    hitsound = true;
                else
                    hitsound = false;

                AddGravity( ent );
                CheckVelocity( ent );
                FlyMove( ent, (float)host.FrameTime, null );
                LinkEdict( ent, true );

                if( ( (int)ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )	// just hit ground
                {
                    if( hitsound )
                        StartSound( ent, 0, "demon/dland2.wav", 255, 1 );
                }
            }

            // regular thinking
            RunThink( ent );

            CheckWaterTransition( ent );
        }

        /// <summary>
        /// SV_Physics_Noclip
        /// A moving object that doesn't obey physics
        /// </summary>
        private static void Physics_Noclip( edict_t ent )
        {
            // regular thinking
            if( !RunThink( ent ) )
                return;

            mathlib.VectorMA( ref ent.v.angles, (float)host.FrameTime, ref ent.v.avelocity, out ent.v.angles );
            mathlib.VectorMA( ref ent.v.origin, (float)host.FrameTime, ref ent.v.velocity, out ent.v.origin );
            LinkEdict( ent, false );
        }

        /// <summary>
        /// SV_Physics_None
        /// Non moving objects can only think
        /// </summary>
        private static void Physics_None( edict_t ent )
        {
            // regular thinking
            RunThink( ent );
        }

        /// <summary>
        /// SV_Physics_Pusher
        /// </summary>
        private static void Physics_Pusher( edict_t ent )
        {
            float oldltime = ent.v.ltime;
            float thinktime = ent.v.nextthink;
            float movetime;
            if( thinktime < ent.v.ltime + host.FrameTime )
            {
                movetime = thinktime - ent.v.ltime;
                if( movetime < 0 )
                    movetime = 0;
            }
            else
                movetime = (float)host.FrameTime;

            if( movetime != 0 )
            {
                PushMove( ent, movetime );	// advances ent.v.ltime if not blocked
            }

            if( thinktime > oldltime && thinktime <= ent.v.ltime )
            {
                ent.v.nextthink = 0;
                progs.GlobalStruct.time = (float)sv.time;
                progs.GlobalStruct.self = EdictToProg( ent );
                progs.GlobalStruct.other = EdictToProg( sv.edicts[0] );
                progs.Execute( ent.v.think );
                if( ent.free )
                    return;
            }
        }

        /// <summary>
        /// SV_Physics_Client
        /// Player character actions
        /// </summary>
        private static void Physics_Client( edict_t ent, int num )
        {
            if( !svs.clients[num - 1].active )
                return;		// unconnected slot

            //
            // call standard client pre-think
            //
            progs.GlobalStruct.time = (float)sv.time;
            progs.GlobalStruct.self = EdictToProg( ent );
            progs.Execute( progs.GlobalStruct.PlayerPreThink );

            //
            // do a move
            //
            CheckVelocity( ent );

            //
            // decide which move function to call
            //
            switch( (int)ent.v.movetype )
            {
                case Movetypes.MOVETYPE_NONE:
                    if( !RunThink( ent ) )
                        return;
                    break;

                case Movetypes.MOVETYPE_WALK:
                    if( !RunThink( ent ) )
                        return;
                    if( !CheckWater( ent ) && ( (int)ent.v.flags & EdictFlags.FL_WATERJUMP ) == 0 )
                        AddGravity( ent );
                    CheckStuck( ent );

                    WalkMove( ent );
                    break;

                case Movetypes.MOVETYPE_TOSS:
                case Movetypes.MOVETYPE_BOUNCE:
                    Physics_Toss( ent );
                    break;

                case Movetypes.MOVETYPE_FLY:
                    if( !RunThink( ent ) )
                        return;
                    FlyMove( ent, (float)host.FrameTime, null );
                    break;

                case Movetypes.MOVETYPE_NOCLIP:
                    if( !RunThink( ent ) )
                        return;
                    mathlib.VectorMA( ref ent.v.origin, (float)host.FrameTime, ref ent.v.velocity, out ent.v.origin );
                    break;

                default:
                    sys.Error( "SV_Physics_client: bad movetype {0}", (int)ent.v.movetype );
                    break;
            }

            //
            // call standard player post-think
            //
            LinkEdict( ent, true );

            progs.GlobalStruct.time = (float)sv.time;
            progs.GlobalStruct.self = EdictToProg( ent );
            progs.Execute( progs.GlobalStruct.PlayerPostThink );
        }

        /// <summary>
        /// SV_WalkMove
        /// Only used by players
        /// </summary>
        private static void WalkMove( edict_t ent )
        {
            //
            // do a regular slide move unless it looks like you ran into a step
            //
            int oldonground = (int)ent.v.flags & EdictFlags.FL_ONGROUND;
            ent.v.flags = (int)ent.v.flags & ~EdictFlags.FL_ONGROUND;

            v3f oldorg = ent.v.origin;
            v3f oldvel = ent.v.velocity;
            trace_t steptrace = new trace_t();
            int clip = FlyMove( ent, (float)host.FrameTime, steptrace );

            if( ( clip & 2 ) == 0 )
                return;		// move didn't block on a step

            if( oldonground == 0 && ent.v.waterlevel == 0 )
                return;		// don't stair up while jumping

            if( ent.v.movetype != Movetypes.MOVETYPE_WALK )
                return;		// gibbed by a trigger

            if( _NoStep.Value != 0 )
                return;

            if( ( (int)_Player.v.flags & EdictFlags.FL_WATERJUMP ) != 0 )
                return;

            v3f nosteporg = ent.v.origin;
            v3f nostepvel = ent.v.velocity;

            //
            // try moving up and forward to go up a step
            //
            ent.v.origin = oldorg;	// back to start pos

            v3f upmove = common.ZeroVector3f;
            v3f downmove = upmove;
            upmove.z = STEPSIZE;
            downmove.z = (float)( -STEPSIZE + oldvel.z * host.FrameTime );

            // move up
            PushEntity( ent, ref upmove );	// FIXME: don't link?

            // move forward
            ent.v.velocity.x = oldvel.x;
            ent.v.velocity.y = oldvel.y;
            ent.v.velocity.z = 0;
            clip = FlyMove( ent, (float)host.FrameTime, steptrace );

            // check for stuckness, possibly due to the limited precision of floats
            // in the clipping hulls
            if( clip != 0 )
            {
                if( Math.Abs( oldorg.y - ent.v.origin.y ) < 0.03125 && Math.Abs( oldorg.x - ent.v.origin.x ) < 0.03125 )
                {
                    // stepping up didn't make any progress
                    clip = TryUnstick( ent, ref oldvel );
                }
            }

            // extra friction based on view angle
            if( ( clip & 2 ) != 0 )
                WallFriction( ent, steptrace );

            // move down
            trace_t downtrace = PushEntity( ent, ref downmove );	// FIXME: don't link?

            if( downtrace.plane.normal.Z > 0.7 )
            {
                if( ent.v.solid == Solids.SOLID_BSP )
                {
                    ent.v.flags = (int)ent.v.flags | EdictFlags.FL_ONGROUND;
                    ent.v.groundentity = EdictToProg( downtrace.ent );
                }
            }
            else
            {
                // if the push down didn't end up on good ground, use the move without
                // the step up.  This happens near wall / slope combinations, and can
                // cause the player to hop up higher on a slope too steep to climb
                ent.v.origin = nosteporg;
                ent.v.velocity = nostepvel;
            }
        }

        /// <summary>
        /// SV_TryUnstick
        /// Player has come to a dead stop, possibly due to the problem with limited
        /// float precision at some angle joins in the BSP hull.
        ///
        /// Try fixing by pushing one pixel in each direction.
        ///
        /// This is a hack, but in the interest of good gameplay...
        /// </summary>
        private static int TryUnstick( edict_t ent, ref v3f oldvel )
        {
            v3f oldorg = ent.v.origin;
            v3f dir = common.ZeroVector3f;

            trace_t steptrace = new trace_t();
            for( int i = 0; i < 8; i++ )
            {
                // try pushing a little in an axial direction
                switch( i )
                {
                    case 0:
                        dir.x = 2;
                        dir.y = 0;
                        break;

                    case 1:
                        dir.x = 0;
                        dir.y = 2;
                        break;

                    case 2:
                        dir.x = -2;
                        dir.y = 0;
                        break;

                    case 3:
                        dir.x = 0;
                        dir.y = -2;
                        break;

                    case 4:
                        dir.x = 2;
                        dir.y = 2;
                        break;

                    case 5:
                        dir.x = -2;
                        dir.y = 2;
                        break;

                    case 6:
                        dir.x = 2;
                        dir.y = -2;
                        break;

                    case 7:
                        dir.x = -2;
                        dir.y = -2;
                        break;
                }

                PushEntity( ent, ref dir );

                // retry the original move
                ent.v.velocity.x = oldvel.x;
                ent.v.velocity.y = oldvel.y;
                ent.v.velocity.z = 0;
                int clip = FlyMove( ent, 0.1f, steptrace );

                if( Math.Abs( oldorg.y - ent.v.origin.y ) > 4 || Math.Abs( oldorg.x - ent.v.origin.x ) > 4 )
                {
                    return clip;
                }

                // go back to the original pos and try again
                ent.v.origin = oldorg;
            }

            ent.v.velocity = common.ZeroVector3f;
            return 7;		// still not moving
        }

        /// <summary>
        /// SV_WallFriction
        /// </summary>
        private static void WallFriction( edict_t ent, trace_t trace )
        {
            Vector3 forward, right, up, vangle = common.ToVector( ref ent.v.v_angle );
            mathlib.AngleVectors( ref vangle, out forward, out right, out up );
            float d = Vector3.Dot( trace.plane.normal, forward );

            d += 0.5f;
            if( d >= 0 )
                return;

            // cut the tangential velocity
            Vector3 vel = common.ToVector( ref ent.v.velocity );
            float i = Vector3.Dot( trace.plane.normal, vel );
            Vector3 into = trace.plane.normal * i;
            Vector3 side = vel - into;

            ent.v.velocity.x = side.X * ( 1 + d );
            ent.v.velocity.y = side.Y * ( 1 + d );
        }

        /// <summary>
        /// SV_CheckStuck
        /// This is a big hack to try and fix the rare case of getting stuck in the world
        /// clipping hull.
        /// </summary>
        private static void CheckStuck( edict_t ent )
        {
            if( TestEntityPosition( ent ) == null )
            {
                ent.v.oldorigin = ent.v.origin;
                return;
            }

            v3f org = ent.v.origin;
            ent.v.origin = ent.v.oldorigin;
            if( TestEntityPosition( ent ) == null )
            {
                Con.DPrint( "Unstuck.\n" );
                LinkEdict( ent, true );
                return;
            }

            for( int z = 0; z < 18; z++ )
                for( int i = -1; i <= 1; i++ )
                    for( int j = -1; j <= 1; j++ )
                    {
                        ent.v.origin.x = org.x + i;
                        ent.v.origin.y = org.y + j;
                        ent.v.origin.z = org.z + z;
                        if( TestEntityPosition( ent ) == null )
                        {
                            Con.DPrint( "Unstuck.\n" );
                            LinkEdict( ent, true );
                            return;
                        }
                    }

            ent.v.origin = org;
            Con.DPrint( "player is stuck.\n" );
        }

        /// <summary>
        /// SV_CheckWater
        /// </summary>
        private static bool CheckWater( edict_t ent )
        {
            Vector3 point;
            point.X = ent.v.origin.x;
            point.Y = ent.v.origin.y;
            point.Z = ent.v.origin.z + ent.v.mins.z + 1;

            ent.v.waterlevel = 0;
            ent.v.watertype = Contents.CONTENTS_EMPTY;
            int cont = PointContents( ref point );
            if( cont <= Contents.CONTENTS_WATER )
            {
                ent.v.watertype = cont;
                ent.v.waterlevel = 1;
                point.Z = ent.v.origin.z + ( ent.v.mins.z + ent.v.maxs.z ) * 0.5f;
                cont = PointContents( ref point );
                if( cont <= Contents.CONTENTS_WATER )
                {
                    ent.v.waterlevel = 2;
                    point.Z = ent.v.origin.z + ent.v.view_ofs.z;
                    cont = PointContents( ref point );
                    if( cont <= Contents.CONTENTS_WATER )
                        ent.v.waterlevel = 3;
                }
            }

            return ent.v.waterlevel > 1;
        }

        /// <summary>
        /// SV_RunThink
        /// Runs thinking code if time.  There is some play in the exact time the think
        /// function will be called, because it is called before any movement is done
        /// in a frame.  Not used for pushmove objects, because they must be exact.
        /// Returns false if the entity removed itself.
        /// </summary>
        private static bool RunThink( edict_t ent )
        {
            float thinktime;

            thinktime = ent.v.nextthink;
            if( thinktime <= 0 || thinktime > sv.time + host.FrameTime )
                return true;

            if( thinktime < sv.time )
                thinktime = (float)sv.time;	// don't let things stay in the past.

            // it is possible to start that way
            // by a trigger with a local time.
            ent.v.nextthink = 0;
            progs.GlobalStruct.time = thinktime;
            progs.GlobalStruct.self = EdictToProg( ent );
            progs.GlobalStruct.other = EdictToProg( sv.edicts[0] );
            progs.Execute( ent.v.think );

            return !ent.free;
        }

        /// <summary>
        /// SV_CheckVelocity
        /// </summary>
        private static void CheckVelocity( edict_t ent )
        {
            //
            // bound velocity
            //
            if( mathlib.CheckNaN( ref ent.v.velocity, 0 ) )
            {
                Con.Print( "Got a NaN velocity on {0}\n", progs.GetString( ent.v.classname ) );
            }

            if( mathlib.CheckNaN( ref ent.v.origin, 0 ) )
            {
                Con.Print( "Got a NaN origin on {0}\n", progs.GetString( ent.v.classname ) );
            }

            Vector3 max = Vector3.One * _MaxVelocity.Value;
            Vector3 min = -Vector3.One * _MaxVelocity.Value;
            mathlib.Clamp( ref ent.v.velocity, ref min, ref max, out ent.v.velocity );
        }

        /// <summary>
        /// SV_FlyMove
        /// The basic solid body movement clip that slides along multiple planes
        /// Returns the clipflags if the velocity was modified (hit something solid)
        /// 1 = floor
        /// 2 = wall / step
        /// 4 = dead stop
        /// If steptrace is not NULL, the trace of any vertical wall hit will be stored
        /// </summary>
        private static int FlyMove( edict_t ent, float time, trace_t steptrace )
        {
            v3f original_velocity = ent.v.velocity;
            v3f primal_velocity = ent.v.velocity;

            int numbumps = 4;
            int blocked = 0;
            Vector3[] planes = new Vector3[MAX_CLIP_PLANES];
            int numplanes = 0;
            float time_left = time;

            for( int bumpcount = 0; bumpcount < numbumps; bumpcount++ )
            {
                if( ent.v.velocity.IsEmpty )
                    break;

                v3f end;
                mathlib.VectorMA( ref ent.v.origin, time_left, ref ent.v.velocity, out end );

                trace_t trace = Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent );

                if( trace.allsolid )
                {	// entity is trapped in another solid
                    ent.v.velocity = default( v3f );
                    return 3;
                }

                if( trace.fraction > 0 )
                {	// actually covered some distance
                    mathlib.Copy( ref trace.endpos, out ent.v.origin );
                    original_velocity = ent.v.velocity;
                    numplanes = 0;
                }

                if( trace.fraction == 1 )
                    break;		// moved the entire distance

                if( trace.ent == null )
                    sys.Error( "SV_FlyMove: !trace.ent" );

                if( trace.plane.normal.Z > 0.7 )
                {
                    blocked |= 1;		// floor
                    if( trace.ent.v.solid == Solids.SOLID_BSP )
                    {
                        ent.v.flags = (int)ent.v.flags | EdictFlags.FL_ONGROUND;
                        ent.v.groundentity = EdictToProg( trace.ent );
                    }
                }

                if( trace.plane.normal.Z == 0 )
                {
                    blocked |= 2;		// step
                    if( steptrace != null )
                        steptrace.CopyFrom( trace );	// save for player extrafriction
                }

                //
                // run the impact function
                //
                Impact( ent, trace.ent );
                if( ent.free )
                    break;		// removed by the impact function

                time_left -= time_left * trace.fraction;

                // cliped to another plane
                if( numplanes >= MAX_CLIP_PLANES )
                {
                    // this shouldn't really happen
                    ent.v.velocity = default( v3f );
                    return 3;
                }

                planes[numplanes] = trace.plane.normal;
                numplanes++;

                //
                // modify original_velocity so it parallels all of the clip planes
                //
                v3f new_velocity = default( v3f );
                int i, j;
                for( i = 0; i < numplanes; i++ )
                {
                    ClipVelocity( ref original_velocity, ref planes[i], out new_velocity, 1 );
                    for( j = 0; j < numplanes; j++ )
                        if( j != i )
                        {
                            float dot = new_velocity.x * planes[j].X + new_velocity.y * planes[j].Y + new_velocity.z * planes[j].Z;
                            if( dot < 0 )
                                break;	// not ok
                        }
                    if( j == numplanes )
                        break;
                }

                if( i != numplanes )
                {
                    // go along this plane
                    ent.v.velocity = new_velocity;
                }
                else
                {
                    // go along the crease
                    if( numplanes != 2 )
                    {
                        ent.v.velocity = default( v3f );
                        return 7;
                    }
                    Vector3 dir = Vector3.Cross( planes[0], planes[1] );
                    float d = dir.X * ent.v.velocity.x + dir.Y * ent.v.velocity.y + dir.Z * ent.v.velocity.z;
                    mathlib.Copy( ref dir, out ent.v.velocity );
                    mathlib.VectorScale( ref ent.v.velocity, d, out ent.v.velocity );
                }

                //
                // if original velocity is against the original velocity, stop dead
                // to avoid tiny occilations in sloping corners
                //
                if( mathlib.DotProduct( ref ent.v.velocity, ref primal_velocity ) <= 0 )
                {
                    ent.v.velocity = default( v3f );
                    return blocked;
                }
            }

            return blocked;
        }

        private static trace_t Move( ref v3f start, ref v3f mins, ref v3f maxs, ref v3f end, int type, edict_t passedict )
        {
            Vector3 vstart, vmins, vmaxs, vend;
            mathlib.Copy( ref start, out vstart );
            mathlib.Copy( ref mins, out vmins );
            mathlib.Copy( ref maxs, out vmaxs );
            mathlib.Copy( ref end, out vend );
            return Move( ref vstart, ref vmins, ref vmaxs, ref vend, type, passedict );
        }

        /// <summary>
        /// SV_Impact
        /// Two entities have touched, so run their touch functions
        /// </summary>
        private static void Impact( edict_t e1, edict_t e2 )
        {
            int old_self = progs.GlobalStruct.self;
            int old_other = progs.GlobalStruct.other;

            progs.GlobalStruct.time = (float)sv.time;
            if( e1.v.touch != 0 && e1.v.solid != Solids.SOLID_NOT )
            {
                progs.GlobalStruct.self = EdictToProg( e1 );
                progs.GlobalStruct.other = EdictToProg( e2 );
                progs.Execute( e1.v.touch );
            }

            if( e2.v.touch != 0 && e2.v.solid != Solids.SOLID_NOT )
            {
                progs.GlobalStruct.self = EdictToProg( e2 );
                progs.GlobalStruct.other = EdictToProg( e1 );
                progs.Execute( e2.v.touch );
            }

            progs.GlobalStruct.self = old_self;
            progs.GlobalStruct.other = old_other;
        }

        /// <summary>
        /// SV_PushMove
        /// </summary>
        private static void PushMove( edict_t pusher, float movetime )
        {
            if( pusher.v.velocity.IsEmpty )
            {
                pusher.v.ltime += movetime;
                return;
            }

            v3f move, mins, maxs;
            mathlib.VectorScale( ref pusher.v.velocity, movetime, out move );
            mathlib.VectorAdd( ref pusher.v.absmin, ref move, out mins );
            mathlib.VectorAdd( ref pusher.v.absmax, ref move, out maxs );

            v3f pushorig = pusher.v.origin;

            edict_t[] moved_edict = new edict_t[QDef.MAX_EDICTS];
            v3f[] moved_from = new v3f[QDef.MAX_EDICTS];

            // move the pusher to it's final position

            mathlib.VectorAdd( ref pusher.v.origin, ref move, out pusher.v.origin );
            pusher.v.ltime += movetime;
            LinkEdict( pusher, false );

            // see if any solid entities are inside the final position
            int num_moved = 0;
            for( int e = 1; e < sv.num_edicts; e++ )
            {
                edict_t check = sv.edicts[e];
                if( check.free )
                    continue;
                if( check.v.movetype == Movetypes.MOVETYPE_PUSH ||
                    check.v.movetype == Movetypes.MOVETYPE_NONE ||
                    check.v.movetype == Movetypes.MOVETYPE_NOCLIP )
                    continue;

                // if the entity is standing on the pusher, it will definately be moved
                if( !( ( (int)check.v.flags & EdictFlags.FL_ONGROUND ) != 0 && ProgToEdict( check.v.groundentity ) == pusher ) )
                {
                    if( check.v.absmin.x >= maxs.x || check.v.absmin.y >= maxs.y ||
                        check.v.absmin.z >= maxs.z || check.v.absmax.x <= mins.x ||
                        check.v.absmax.y <= mins.y || check.v.absmax.z <= mins.z )
                        continue;

                    // see if the ent's bbox is inside the pusher's final position
                    if( TestEntityPosition( check ) == null )
                        continue;
                }

                // remove the onground flag for non-players
                if( check.v.movetype != Movetypes.MOVETYPE_WALK )
                    check.v.flags = (int)check.v.flags & ~EdictFlags.FL_ONGROUND;

                v3f entorig = check.v.origin;
                moved_from[num_moved] = entorig;
                moved_edict[num_moved] = check;
                num_moved++;

                // try moving the contacted entity
                pusher.v.solid = Solids.SOLID_NOT;
                PushEntity( check, ref move );
                pusher.v.solid = Solids.SOLID_BSP;

                // if it is still inside the pusher, block
                edict_t block = TestEntityPosition( check );
                if( block != null )
                {
                    // fail the move
                    if( check.v.mins.x == check.v.maxs.x )
                        continue;
                    if( check.v.solid == Solids.SOLID_NOT || check.v.solid == Solids.SOLID_TRIGGER )
                    {
                        // corpse
                        check.v.mins.x = check.v.mins.y = 0;
                        check.v.maxs = check.v.mins;
                        continue;
                    }

                    check.v.origin = entorig;
                    LinkEdict( check, true );

                    pusher.v.origin = pushorig;
                    LinkEdict( pusher, false );
                    pusher.v.ltime -= movetime;

                    // if the pusher has a "blocked" function, call it
                    // otherwise, just stay in place until the obstacle is gone
                    if( pusher.v.blocked != 0 )
                    {
                        progs.GlobalStruct.self = EdictToProg( pusher );
                        progs.GlobalStruct.other = EdictToProg( check );
                        progs.Execute( pusher.v.blocked );
                    }

                    // move back any entities we already moved
                    for( int i = 0; i < num_moved; i++ )
                    {
                        moved_edict[i].v.origin = moved_from[i];
                        LinkEdict( moved_edict[i], false );
                    }
                    return;
                }
            }
        }
    }
}
