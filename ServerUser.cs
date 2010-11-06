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

namespace SharpQuake
{
    partial class Server
    {
        const int MAX_FORWARD = 6;
        
        static edict_t _Player; // sv_player
        static bool _OnGround; // onground

        // world
        //static v3f angles - this must be a reference to _Player.v.angles
        //static v3f origin  - this must be a reference to _Player.v.origin
        //static Vector3 velocity - this must be a reference to _Player.v.velocity
        
        static usercmd_t _Cmd; // cmd

        static Vector3 _Forward; // forward
        static Vector3 _Right; // right
        static Vector3 _Up; // up

        static Vector3 _WishDir; // wishdir
        static float _WishSpeed; // wishspeed


        public static edict_t Player
        {
            get { return _Player; }
        }

        
        /// <summary>
        /// SV_RunClients
        /// </summary>
        public static void RunClients()
        {
            for (int i = 0; i < svs.maxclients; i++)
            {
                Host.HostClient = svs.clients[i];
                if (!Host.HostClient.active)
                    continue;

                _Player = Host.HostClient.edict;

                if (!ReadClientMessage())
                {
                    DropClient(false);	// client misbehaved...
                    continue;
                }

                if (!Host.HostClient.spawned)
                {
                    // clear client movement until a new packet is received
                    Host.HostClient.cmd.Clear();
                    continue;
                }

                // always pause in single player if in console or menus
                if (!sv.paused && (svs.maxclients > 1 || Key.Destination == keydest_t.key_game))
                    ClientThink();
            }
        }

        /// <summary>
        /// SV_ReadClientMessage
        /// Returns false if the client should be killed
        /// </summary>
        static bool ReadClientMessage()
        {
            while (true)
            {
                int ret = Net.GetMessage(Host.HostClient.netconnection);
                if (ret == -1)
                {
                    Con.DPrint("SV_ReadClientMessage: NET_GetMessage failed\n");
                    return false;
                }
                if (ret == 0)
                    return true;

                Net.Reader.Reset();

                bool flag = true;
                while (flag)
                {
                    if (!Host.HostClient.active)
                        return false;	// a command caused an error

                    if (Net.Reader.IsBadRead)
                    {
                        Con.DPrint("SV_ReadClientMessage: badread\n");
                        return false;
                    }

                    int cmd = Net.Reader.ReadChar();
                    switch (cmd)
                    {
                        case -1:
                            flag = false; // end of message
                            ret = 1;
                            break;

                        case Protocol.clc_nop:
                            break;

                        case Protocol.clc_stringcmd:
                            string s = Net.Reader.ReadString();
                            if (Host.HostClient.privileged)
                                ret = 2;
                            else
                                ret = 0;
                            if (Common.SameText(s, "status", 6))
                                ret = 1;
                            else if (Common.SameText(s, "god", 3))
                                ret = 1;
                            else if (Common.SameText(s, "notarget", 8))
                                ret = 1;
                            else if (Common.SameText(s, "fly", 3))
                                ret = 1;
                            else if (Common.SameText(s, "name", 4))
                                ret = 1;
                            else if (Common.SameText(s, "noclip", 6))
                                ret = 1;
                            else if (Common.SameText(s, "say", 3))
                                ret = 1;
                            else if (Common.SameText(s, "say_team", 8))
                                ret = 1;
                            else if (Common.SameText(s, "tell", 4))
                                ret = 1;
                            else if (Common.SameText(s, "color", 5))
                                ret = 1;
                            else if (Common.SameText(s, "kill", 4))
                                ret = 1;
                            else if (Common.SameText(s, "pause", 5))
                                ret = 1;
                            else if (Common.SameText(s, "spawn", 5))
                                ret = 1;
                            else if (Common.SameText(s, "begin", 5))
                                ret = 1;
                            else if (Common.SameText(s, "prespawn", 8))
                                ret = 1;
                            else if (Common.SameText(s, "kick", 4))
                                ret = 1;
                            else if (Common.SameText(s, "ping", 4))
                                ret = 1;
                            else if (Common.SameText(s, "give", 4))
                                ret = 1;
                            else if (Common.SameText(s, "ban", 3))
                                ret = 1;
                            if (ret == 2)
                                Cbuf.InsertText(s);
                            else if (ret == 1)
                                Cmd.ExecuteString(s, cmd_source_t.src_client);
                            else
                                Con.DPrint("{0} tried to {1}\n", Host.HostClient.name, s);
                            break;

                        case Protocol.clc_disconnect:
                            return false;

                        case Protocol.clc_move:
                            ReadClientMove(ref Host.HostClient.cmd);
                            break;

                        default:
                            Con.DPrint("SV_ReadClientMessage: unknown command char\n");
                            return false;
                    }
                }
                
                if (ret != 1)
                    break;
            }

            return true;
        }

        /// <summary>
        /// SV_ReadClientMove
        /// </summary>
        static void ReadClientMove(ref usercmd_t move)
        {
            client_t client = Host.HostClient;

            // read ping time
            client.ping_times[client.num_pings % NUM_PING_TIMES] = (float)(sv.time - Net.Reader.ReadFloat());
            client.num_pings++;

            // read current angles	
            Vector3 angles = Net.Reader.ReadAngles();
            Mathlib.Copy(ref angles, out client.edict.v.v_angle);

            // read movement
            move.forwardmove = Net.Reader.ReadShort();
            move.sidemove = Net.Reader.ReadShort();
            move.upmove = Net.Reader.ReadShort();

            // read buttons
            int bits = Net.Reader.ReadByte();
            client.edict.v.button0 = bits & 1;
            client.edict.v.button2 = (bits & 2) >> 1;

            int i = Net.Reader.ReadByte();
            if (i != 0)
                client.edict.v.impulse = i;
        }

        /// <summary>
        /// SV_SetIdealPitch
        /// </summary>
        public static void SetIdealPitch()
        {
            if (((int)_Player.v.flags & EdictFlags.FL_ONGROUND) == 0)
                return;

            double angleval = _Player.v.angles.y * Math.PI * 2 / 360;
            double sinval = Math.Sin(angleval);
            double cosval = Math.Cos(angleval);
            float[] z = new float[MAX_FORWARD];
            for (int i = 0; i < MAX_FORWARD; i++)
            {
                v3f top = _Player.v.origin;
                top.x += (float)(cosval * (i + 3) * 12);
                top.y += (float)(sinval * (i + 3) * 12);
                top.z += _Player.v.view_ofs.z;

                v3f bottom = top;
                bottom.z -= 160;

                trace_t tr = Move(ref top, ref Common.ZeroVector3f, ref Common.ZeroVector3f, ref bottom, 1, _Player);
                if (tr.allsolid)
                    return;	// looking at a wall, leave ideal the way is was

                if (tr.fraction == 1)
                    return;	// near a dropoff

                z[i] = top.z + tr.fraction * (bottom.z - top.z);
            }

            float dir = 0; // Uze: int in original code???
            int steps = 0;
            for (int j = 1; j < MAX_FORWARD; j++)
            {
                float step = z[j] - z[j - 1]; // Uze: int in original code???
                if (step > -QDef.ON_EPSILON && step < QDef.ON_EPSILON) // Uze: comparing int with ON_EPSILON (0.1)???
                    continue;

                if (dir != 0 && (step - dir > QDef.ON_EPSILON || step - dir < -QDef.ON_EPSILON))
                    return;		// mixed changes

                steps++;
                dir = step;
            }

            if (dir == 0)
            {
                _Player.v.idealpitch = 0;
                return;
            }

            if (steps < 2)
                return;
            _Player.v.idealpitch = -dir * _IdealPitchScale.Value;
        }

        /// <summary>
        /// SV_ClientThink
        /// the move fields specify an intended velocity in pix/sec
        /// the angle fields specify an exact angular motion in degrees
        /// </summary>
        static void ClientThink()
        {
            if (_Player.v.movetype == Movetypes.MOVETYPE_NONE)
                return;

            _OnGround = ((int)_Player.v.flags & EdictFlags.FL_ONGROUND) != 0;

            DropPunchAngle();

            //
            // if dead, behave differently
            //
            if (_Player.v.health <= 0)
                return;

            //
            // angles
            // show 1/3 the pitch angle and all the roll angle
            _Cmd = Host.HostClient.cmd;

            v3f v_angle;
            Mathlib.VectorAdd(ref _Player.v.v_angle, ref _Player.v.punchangle, out v_angle);
            Vector3 pang = Common.ToVector(ref _Player.v.angles);
            Vector3 pvel = Common.ToVector(ref _Player.v.velocity);
            _Player.v.angles.z = View.CalcRoll(ref pang, ref pvel) * 4;
            if (_Player.v.fixangle == 0)
            {
                _Player.v.angles.x = -v_angle.x / 3;
                _Player.v.angles.y = v_angle.y;
            }

            if (((int)_Player.v.flags & EdictFlags.FL_WATERJUMP) != 0)
            {
                WaterJump();
                return;
            }
            //
            // walk
            //
            if ((_Player.v.waterlevel >= 2) && (_Player.v.movetype != Movetypes.MOVETYPE_NOCLIP))
            {
                WaterMove();
                return;
            }

            AirMove();
        }

        static void DropPunchAngle()
        {
            Vector3 v = Common.ToVector(ref _Player.v.punchangle);
            double len = Mathlib.Normalize(ref v) - 10 * Host.FrameTime;
            if (len < 0)
                len = 0;
            v *= (float)len;
            Mathlib.Copy(ref v, out _Player.v.punchangle);
        }

        /// <summary>
        /// SV_WaterJump
        /// </summary>
        static void WaterJump()
        {
            if (sv.time > _Player.v.teleport_time || _Player.v.waterlevel == 0)
            {
                _Player.v.flags = (int)_Player.v.flags & ~EdictFlags.FL_WATERJUMP;
                _Player.v.teleport_time = 0;
            }
            _Player.v.velocity.x = _Player.v.movedir.x;
            _Player.v.velocity.y = _Player.v.movedir.y;
        }

        /// <summary>
        /// SV_WaterMove
        /// </summary>
        static void WaterMove()
        {
            //
            // user intentions
            //
            Vector3 pangle = Common.ToVector(ref _Player.v.v_angle);
            Mathlib.AngleVectors(ref pangle, out _Forward, out _Right, out _Up);
            Vector3 wishvel = _Forward * _Cmd.forwardmove + _Right * _Cmd.sidemove;

            if (_Cmd.forwardmove == 0 && _Cmd.sidemove == 0 && _Cmd.upmove == 0)
                wishvel.Z -= 60;		// drift towards bottom
            else
                wishvel.Z += _Cmd.upmove;

            float wishspeed = wishvel.Length;
            if (wishspeed > _MaxSpeed.Value)
            {
                wishvel *= _MaxSpeed.Value / wishspeed;
                wishspeed = _MaxSpeed.Value;
            }
            wishspeed *= 0.7f;

            //
            // water friction
            //
            float newspeed, speed = Mathlib.Length(ref _Player.v.velocity);
            if (speed != 0)
            {
                newspeed = (float)(speed - Host.FrameTime * speed * _Friction.Value);
                if (newspeed < 0)
                    newspeed = 0;
                Mathlib.VectorScale(ref _Player.v.velocity, newspeed / speed, out _Player.v.velocity);
            }
            else
                newspeed = 0;

            //
            // water acceleration
            //
            if (wishspeed == 0)
                return;

            float addspeed = wishspeed - newspeed;
            if (addspeed <= 0)
                return;

            Mathlib.Normalize(ref wishvel);
            float accelspeed = (float)(_Accelerate.Value * wishspeed * Host.FrameTime);
            if (accelspeed > addspeed)
                accelspeed = addspeed;

            wishvel *= accelspeed;
            _Player.v.velocity.x += wishvel.X;
            _Player.v.velocity.y += wishvel.Y;
            _Player.v.velocity.z += wishvel.Z;
        }

        /// <summary>
        /// SV_AirMove
        /// </summary>
        static void AirMove()
        {
            Vector3 pangles = Common.ToVector(ref _Player.v.angles);
            Mathlib.AngleVectors(ref pangles, out _Forward, out _Right, out _Up);

            float fmove = _Cmd.forwardmove;
            float smove = _Cmd.sidemove;

            // hack to not let you back into teleporter
            if (sv.time < _Player.v.teleport_time && fmove < 0)
                fmove = 0;

            Vector3 wishvel = _Forward * fmove + _Right * smove;

            if ((int)_Player.v.movetype != Movetypes.MOVETYPE_WALK)
                wishvel.Z = _Cmd.upmove;
            else
                wishvel.Z = 0;

            _WishDir = wishvel;
            _WishSpeed = Mathlib.Normalize(ref _WishDir);
            if (_WishSpeed > _MaxSpeed.Value)
            {
                wishvel *= _MaxSpeed.Value / _WishSpeed;
                _WishSpeed = _MaxSpeed.Value;
            }

            if (_Player.v.movetype == Movetypes.MOVETYPE_NOCLIP)
            {
                // noclip
                Mathlib.Copy(ref wishvel, out _Player.v.velocity);
            }
            else if (_OnGround)
            {
                UserFriction();
                Accelerate();
            }
            else
            {	// not on ground, so little effect on velocity
                AirAccelerate(wishvel);
            }
        }

        /// <summary>
        /// SV_UserFriction
        /// </summary>
        static void UserFriction()
        {
            float speed = Mathlib.LengthXY(ref _Player.v.velocity);
            if (speed == 0)
                return;

            // if the leading edge is over a dropoff, increase friction
            Vector3 start, stop;
            start.X = stop.X = _Player.v.origin.x + _Player.v.velocity.x / speed * 16;
            start.Y = stop.Y = _Player.v.origin.y + _Player.v.velocity.y / speed * 16;
            start.Z = _Player.v.origin.z + _Player.v.mins.z;
            stop.Z = start.Z - 34;

            trace_t trace = Move(ref start, ref Common.ZeroVector, ref Common.ZeroVector, ref stop, 1, _Player);
            float friction = _Friction.Value;
            if (trace.fraction == 1.0)
                friction *= _EdgeFriction.Value;

            // apply friction	
            float control = speed < _StopSpeed.Value ? _StopSpeed.Value : speed;
            float newspeed = (float)(speed - Host.FrameTime * control * friction);

            if (newspeed < 0)
                newspeed = 0;
            newspeed /= speed;

            Mathlib.VectorScale(ref _Player.v.velocity, newspeed, out _Player.v.velocity);
        }

        /// <summary>
        /// SV_Accelerate
        /// </summary>
        static void Accelerate()
        {
            float currentspeed = Vector3.Dot(Common.ToVector(ref _Player.v.velocity), _WishDir);
            float addspeed = _WishSpeed - currentspeed;
            if (addspeed <= 0)
                return;

            float accelspeed = (float)(_Accelerate.Value * Host.FrameTime * _WishSpeed);
            if (accelspeed > addspeed)
                accelspeed = addspeed;

            _Player.v.velocity.x += _WishDir.X * accelspeed;
            _Player.v.velocity.y += _WishDir.Y * accelspeed;
            _Player.v.velocity.z += _WishDir.Z * accelspeed;
        }

        /// <summary>
        /// SV_AirAccelerate
        /// </summary>
        static void AirAccelerate(Vector3 wishveloc)
        {
            float wishspd = Mathlib.Normalize(ref wishveloc);
            if (wishspd > 30)
                wishspd = 30;
            float currentspeed = Vector3.Dot(Common.ToVector(ref _Player.v.velocity), wishveloc);
            float addspeed = wishspd - currentspeed;
            if (addspeed <= 0)
                return;
            float accelspeed = (float)(_Accelerate.Value * _WishSpeed * Host.FrameTime);
            if (accelspeed > addspeed)
                accelspeed = addspeed;

            wishveloc *= accelspeed;
            _Player.v.velocity.x += wishveloc.X;
            _Player.v.velocity.y += wishveloc.Y;
            _Player.v.velocity.z += wishveloc.Z;
        }
    }
}
