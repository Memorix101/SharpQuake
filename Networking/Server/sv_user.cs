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

namespace SharpQuake
{
    partial class server
    {
        public static edict_t Player
        {
            get
            {
                return _Player;
            }
        }

        private const Int32 MAX_FORWARD = 6;

        private static edict_t _Player; // sv_player
        private static Boolean _OnGround; // onground

        // world
        //static v3f angles - this must be a reference to _Player.v.angles
        //static v3f origin  - this must be a reference to _Player.v.origin
        //static Vector3 velocity - this must be a reference to _Player.v.velocity

        private static usercmd_t _Cmd; // cmd

        private static Vector3 _Forward; // forward
        private static Vector3 _Right; // right
        private static Vector3 _Up; // up

        private static Vector3 _WishDir; // wishdir
        private static Single _WishSpeed; // wishspeed

        /// <summary>
        /// SV_RunClients
        /// </summary>
        public static void RunClients()
        {
            for( var i = 0; i < svs.maxclients; i++ )
            {
                host.HostClient = svs.clients[i];
                if( !host.HostClient.active )
                    continue;

                _Player = host.HostClient.edict;

                if( !ReadClientMessage() )
                {
                    DropClient( false );	// client misbehaved...
                    continue;
                }

                if( !host.HostClient.spawned )
                {
                    // clear client movement until a new packet is received
                    host.HostClient.cmd.Clear();
                    continue;
                }

                // always pause in single player if in console or menus
                if( !sv.paused && ( svs.maxclients > 1 || Key.Destination == keydest_t.key_game ) )
                    ClientThink();
            }
        }

        /// <summary>
        /// SV_SetIdealPitch
        /// </summary>
        public static void SetIdealPitch()
        {
            if( ( ( Int32 ) _Player.v.flags & EdictFlags.FL_ONGROUND ) == 0 )
                return;

            var angleval = _Player.v.angles.y * Math.PI * 2 / 360;
            var sinval = Math.Sin( angleval );
            var cosval = Math.Cos( angleval );
            Single[] z = new Single[MAX_FORWARD];
            for( var i = 0; i < MAX_FORWARD; i++ )
            {
                Vector3f top = _Player.v.origin;
                top.x += ( Single ) ( cosval * ( i + 3 ) * 12 );
                top.y += ( Single ) ( sinval * ( i + 3 ) * 12 );
                top.z += _Player.v.view_ofs.z;

                Vector3f bottom = top;
                bottom.z -= 160;

                trace_t tr = Move( ref top, ref Common.ZeroVector3f, ref Common.ZeroVector3f, ref bottom, 1, _Player );
                if( tr.allsolid )
                    return;	// looking at a wall, leave ideal the way is was

                if( tr.fraction == 1 )
                    return;	// near a dropoff

                z[i] = top.z + tr.fraction * ( bottom.z - top.z );
            }

            Single dir = 0; // Uze: int in original code???
            var steps = 0;
            for( var j = 1; j < MAX_FORWARD; j++ )
            {
                var step = z[j] - z[j - 1]; // Uze: int in original code???
                if( step > -QDef.ON_EPSILON && step < QDef.ON_EPSILON ) // Uze: comparing int with ON_EPSILON (0.1)???
                    continue;

                if( dir != 0 && ( step - dir > QDef.ON_EPSILON || step - dir < -QDef.ON_EPSILON ) )
                    return;		// mixed changes

                steps++;
                dir = step;
            }

            if( dir == 0 )
            {
                _Player.v.idealpitch = 0;
                return;
            }

            if( steps < 2 )
                return;
            _Player.v.idealpitch = -dir * _IdealPitchScale.Value;
        }

        /// <summary>
        /// SV_ReadClientMessage
        /// Returns false if the client should be killed
        /// </summary>
        private static Boolean ReadClientMessage()
        {
            while( true )
            {
                var ret = net.GetMessage( host.HostClient.netconnection );
                if( ret == -1 )
                {
                    Con.DPrint( "SV_ReadClientMessage: NET_GetMessage failed\n" );
                    return false;
                }
                if( ret == 0 )
                    return true;

                net.Reader.Reset();

                var flag = true;
                while( flag )
                {
                    if( !host.HostClient.active )
                        return false;	// a command caused an error

                    if( net.Reader.IsBadRead )
                    {
                        Con.DPrint( "SV_ReadClientMessage: badread\n" );
                        return false;
                    }

                    var cmd = net.Reader.ReadChar();
                    switch( cmd )
                    {
                        case -1:
                            flag = false; // end of message
                            ret = 1;
                            break;

                        case protocol.clc_nop:
                            break;

                        case protocol.clc_stringcmd:
                            var s = net.Reader.ReadString();
                            if( host.HostClient.privileged )
                                ret = 2;
                            else
                                ret = 0;
                            if( Common.SameText( s, "status", 6 ) )
                                ret = 1;
                            else if( Common.SameText( s, "god", 3 ) )
                                ret = 1;
                            else if( Common.SameText( s, "notarget", 8 ) )
                                ret = 1;
                            else if( Common.SameText( s, "fly", 3 ) )
                                ret = 1;
                            else if( Common.SameText( s, "name", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "noclip", 6 ) )
                                ret = 1;
                            else if( Common.SameText( s, "say", 3 ) )
                                ret = 1;
                            else if( Common.SameText( s, "say_team", 8 ) )
                                ret = 1;
                            else if( Common.SameText( s, "tell", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "color", 5 ) )
                                ret = 1;
                            else if( Common.SameText( s, "kill", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "pause", 5 ) )
                                ret = 1;
                            else if( Common.SameText( s, "spawn", 5 ) )
                                ret = 1;
                            else if( Common.SameText( s, "begin", 5 ) )
                                ret = 1;
                            else if( Common.SameText( s, "prespawn", 8 ) )
                                ret = 1;
                            else if( Common.SameText( s, "kick", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "ping", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "give", 4 ) )
                                ret = 1;
                            else if( Common.SameText( s, "ban", 3 ) )
                                ret = 1;
                            if( ret == 2 )
                                Cbuf.InsertText( s );
                            else if( ret == 1 )
                                SharpQuake.Command.ExecuteString( s, cmd_source_t.src_client );
                            else
                                Con.DPrint( "{0} tried to {1}\n", host.HostClient.name, s );
                            break;

                        case protocol.clc_disconnect:
                            return false;

                        case protocol.clc_move:
                            ReadClientMove( ref host.HostClient.cmd );
                            break;

                        default:
                            Con.DPrint( "SV_ReadClientMessage: unknown command char\n" );
                            return false;
                    }
                }

                if( ret != 1 )
                    break;
            }

            return true;
        }

        /// <summary>
        /// SV_ReadClientMove
        /// </summary>
        private static void ReadClientMove( ref usercmd_t move )
        {
            client_t client = host.HostClient;

            // read ping time
            client.ping_times[client.num_pings % NUM_PING_TIMES] = ( Single ) ( sv.time - net.Reader.ReadFloat() );
            client.num_pings++;

            // read current angles
            Vector3 angles = net.Reader.ReadAngles();
            MathLib.Copy( ref angles, out client.edict.v.v_angle );

            // read movement
            move.forwardmove = net.Reader.ReadShort();
            move.sidemove = net.Reader.ReadShort();
            move.upmove = net.Reader.ReadShort();

            // read buttons
            var bits = net.Reader.ReadByte();
            client.edict.v.button0 = bits & 1;
            client.edict.v.button2 = ( bits & 2 ) >> 1;

            var i = net.Reader.ReadByte();
            if( i != 0 )
                client.edict.v.impulse = i;
        }

        /// <summary>
        /// SV_ClientThink
        /// the move fields specify an intended velocity in pix/sec
        /// the angle fields specify an exact angular motion in degrees
        /// </summary>
        private static void ClientThink()
        {
            if( _Player.v.movetype == Movetypes.MOVETYPE_NONE )
                return;

            _OnGround = ( ( Int32 ) _Player.v.flags & EdictFlags.FL_ONGROUND ) != 0;

            DropPunchAngle();

            //
            // if dead, behave differently
            //
            if( _Player.v.health <= 0 )
                return;

            //
            // angles
            // show 1/3 the pitch angle and all the roll angle
            _Cmd = host.HostClient.cmd;

            Vector3f v_angle;
            MathLib.VectorAdd( ref _Player.v.v_angle, ref _Player.v.punchangle, out v_angle );
            Vector3 pang = Common.ToVector( ref _Player.v.angles );
            Vector3 pvel = Common.ToVector( ref _Player.v.velocity );
            _Player.v.angles.z = view.CalcRoll( ref pang, ref pvel ) * 4;
            if( _Player.v.fixangle == 0 )
            {
                _Player.v.angles.x = -v_angle.x / 3;
                _Player.v.angles.y = v_angle.y;
            }

            if( ( ( Int32 ) _Player.v.flags & EdictFlags.FL_WATERJUMP ) != 0 )
            {
                WaterJump();
                return;
            }
            //
            // walk
            //
            if( ( _Player.v.waterlevel >= 2 ) && ( _Player.v.movetype != Movetypes.MOVETYPE_NOCLIP ) )
            {
                WaterMove();
                return;
            }

            AirMove();
        }

        private static void DropPunchAngle()
        {
            Vector3 v = Common.ToVector( ref _Player.v.punchangle );
            var len = MathLib.Normalize( ref v ) - 10 * host.FrameTime;
            if( len < 0 )
                len = 0;
            v *= ( Single ) len;
            MathLib.Copy( ref v, out _Player.v.punchangle );
        }

        /// <summary>
        /// SV_WaterJump
        /// </summary>
        private static void WaterJump()
        {
            if( sv.time > _Player.v.teleport_time || _Player.v.waterlevel == 0 )
            {
                _Player.v.flags = ( Int32 ) _Player.v.flags & ~EdictFlags.FL_WATERJUMP;
                _Player.v.teleport_time = 0;
            }
            _Player.v.velocity.x = _Player.v.movedir.x;
            _Player.v.velocity.y = _Player.v.movedir.y;
        }

        /// <summary>
        /// SV_WaterMove
        /// </summary>
        private static void WaterMove()
        {
            //
            // user intentions
            //
            Vector3 pangle = Common.ToVector( ref _Player.v.v_angle );
            MathLib.AngleVectors( ref pangle, out _Forward, out _Right, out _Up );
            Vector3 wishvel = _Forward * _Cmd.forwardmove + _Right * _Cmd.sidemove;

            if( _Cmd.forwardmove == 0 && _Cmd.sidemove == 0 && _Cmd.upmove == 0 )
                wishvel.Z -= 60;		// drift towards bottom
            else
                wishvel.Z += _Cmd.upmove;

            var wishspeed = wishvel.Length;
            if( wishspeed > _MaxSpeed.Value )
            {
                wishvel *= _MaxSpeed.Value / wishspeed;
                wishspeed = _MaxSpeed.Value;
            }
            wishspeed *= 0.7f;

            //
            // water friction
            //
            Single newspeed, speed = MathLib.Length( ref _Player.v.velocity );
            if( speed != 0 )
            {
                newspeed = ( Single ) ( speed - host.FrameTime * speed * _Friction.Value );
                if( newspeed < 0 )
                    newspeed = 0;
                MathLib.VectorScale( ref _Player.v.velocity, newspeed / speed, out _Player.v.velocity );
            }
            else
                newspeed = 0;

            //
            // water acceleration
            //
            if( wishspeed == 0 )
                return;

            var addspeed = wishspeed - newspeed;
            if( addspeed <= 0 )
                return;

            MathLib.Normalize( ref wishvel );
            var accelspeed = ( Single ) ( _Accelerate.Value * wishspeed * host.FrameTime );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            wishvel *= accelspeed;
            _Player.v.velocity.x += wishvel.X;
            _Player.v.velocity.y += wishvel.Y;
            _Player.v.velocity.z += wishvel.Z;
        }

        /// <summary>
        /// SV_AirMove
        /// </summary>
        private static void AirMove()
        {
            Vector3 pangles = Common.ToVector( ref _Player.v.angles );
            MathLib.AngleVectors( ref pangles, out _Forward, out _Right, out _Up );

            var fmove = _Cmd.forwardmove;
            var smove = _Cmd.sidemove;

            // hack to not let you back into teleporter
            if( sv.time < _Player.v.teleport_time && fmove < 0 )
                fmove = 0;

            Vector3 wishvel = _Forward * fmove + _Right * smove;

            if( ( Int32 ) _Player.v.movetype != Movetypes.MOVETYPE_WALK )
                wishvel.Z = _Cmd.upmove;
            else
                wishvel.Z = 0;

            _WishDir = wishvel;
            _WishSpeed = MathLib.Normalize( ref _WishDir );
            if( _WishSpeed > _MaxSpeed.Value )
            {
                wishvel *= _MaxSpeed.Value / _WishSpeed;
                _WishSpeed = _MaxSpeed.Value;
            }

            if( _Player.v.movetype == Movetypes.MOVETYPE_NOCLIP )
            {
                // noclip
                MathLib.Copy( ref wishvel, out _Player.v.velocity );
            }
            else if( _OnGround )
            {
                UserFriction();
                Accelerate();
            }
            else
            {	// not on ground, so little effect on velocity
                AirAccelerate( wishvel );
            }
        }

        /// <summary>
        /// SV_UserFriction
        /// </summary>
        private static void UserFriction()
        {
            var speed = MathLib.LengthXY( ref _Player.v.velocity );
            if( speed == 0 )
                return;

            // if the leading edge is over a dropoff, increase friction
            Vector3 start, stop;
            start.X = stop.X = _Player.v.origin.x + _Player.v.velocity.x / speed * 16;
            start.Y = stop.Y = _Player.v.origin.y + _Player.v.velocity.y / speed * 16;
            start.Z = _Player.v.origin.z + _Player.v.mins.z;
            stop.Z = start.Z - 34;

            trace_t trace = Move( ref start, ref Common.ZeroVector, ref Common.ZeroVector, ref stop, 1, _Player );
            var friction = _Friction.Value;
            if( trace.fraction == 1.0 )
                friction *= _EdgeFriction.Value;

            // apply friction
            var control = speed < _StopSpeed.Value ? _StopSpeed.Value : speed;
            var newspeed = ( Single ) ( speed - host.FrameTime * control * friction );

            if( newspeed < 0 )
                newspeed = 0;
            newspeed /= speed;

            MathLib.VectorScale( ref _Player.v.velocity, newspeed, out _Player.v.velocity );
        }

        /// <summary>
        /// SV_Accelerate
        /// </summary>
        private static void Accelerate()
        {
            var currentspeed = Vector3.Dot( Common.ToVector( ref _Player.v.velocity ), _WishDir );
            var addspeed = _WishSpeed - currentspeed;
            if( addspeed <= 0 )
                return;

            var accelspeed = ( Single ) ( _Accelerate.Value * host.FrameTime * _WishSpeed );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            _Player.v.velocity.x += _WishDir.X * accelspeed;
            _Player.v.velocity.y += _WishDir.Y * accelspeed;
            _Player.v.velocity.z += _WishDir.Z * accelspeed;
        }

        /// <summary>
        /// SV_AirAccelerate
        /// </summary>
        private static void AirAccelerate( Vector3 wishveloc )
        {
            var wishspd = MathLib.Normalize( ref wishveloc );
            if( wishspd > 30 )
                wishspd = 30;
            var currentspeed = Vector3.Dot( Common.ToVector( ref _Player.v.velocity ), wishveloc );
            var addspeed = wishspd - currentspeed;
            if( addspeed <= 0 )
                return;
            var accelspeed = ( Single ) ( _Accelerate.Value * _WishSpeed * host.FrameTime );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            wishveloc *= accelspeed;
            _Player.v.velocity.x += wishveloc.X;
            _Player.v.velocity.y += wishveloc.Y;
            _Player.v.velocity.z += wishveloc.Z;
        }
    }
}
