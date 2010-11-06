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

// cl_input.c

namespace SharpQuake
{
    partial class Client
    {
        // CL_InitInput
        static void InitInput()
        {
            ClientInput.Init();
        }

        
        /// <summary>
        /// CL_BaseMove
        /// Send the intended movement message to the server
        /// </summary>
        static void BaseMove(ref usercmd_t cmd)
        {
            if (cls.signon != SIGNONS)
                return;

            AdjustAngles();

            cmd.Clear();

            if (ClientInput.StrafeBtn.IsDown)
            {
                cmd.sidemove += _SideSpeed.Value * KeyState(ref ClientInput.RightBtn);
                cmd.sidemove -= _SideSpeed.Value * KeyState(ref ClientInput.LeftBtn);
            }

            cmd.sidemove += _SideSpeed.Value * KeyState(ref ClientInput.MoveRightBtn);
            cmd.sidemove -= _SideSpeed.Value * KeyState(ref ClientInput.MoveLeftBtn);

            cmd.upmove += _UpSpeed.Value * KeyState(ref ClientInput.UpBtn);
            cmd.upmove -= _UpSpeed.Value * KeyState(ref ClientInput.DownBtn);

            if (!ClientInput.KLookBtn.IsDown)
            {
                cmd.forwardmove += _ForwardSpeed.Value * KeyState(ref ClientInput.ForwardBtn);
                cmd.forwardmove -= _BackSpeed.Value * KeyState(ref ClientInput.BackBtn);
            }

            //
            // adjust for speed key
            //
            if (ClientInput.SpeedBtn.IsDown)
            {
                cmd.forwardmove *= _MoveSpeedKey.Value;
                cmd.sidemove *= _MoveSpeedKey.Value;
                cmd.upmove *= _MoveSpeedKey.Value;
            }
        }

        
        // CL_AdjustAngles
        //
        // Moves the local angle positions
        static void AdjustAngles()
        {
            float speed = (float)Host.FrameTime;

            if (ClientInput.SpeedBtn.IsDown)
                speed *= _AngleSpeedKey.Value;

            if (!ClientInput.StrafeBtn.IsDown)
            {
                cl.viewangles.Y -= speed * _YawSpeed.Value * KeyState(ref ClientInput.RightBtn);
                cl.viewangles.Y += speed * _YawSpeed.Value * KeyState(ref ClientInput.LeftBtn);
                cl.viewangles.Y = Mathlib.AngleMod(cl.viewangles.Y);
            }

            if (ClientInput.KLookBtn.IsDown)
            {
                View.StopPitchDrift();
                cl.viewangles.X -= speed * _PitchSpeed.Value * KeyState(ref ClientInput.ForwardBtn);
                cl.viewangles.X += speed * _PitchSpeed.Value * KeyState(ref ClientInput.BackBtn);
            }

            float up = KeyState(ref ClientInput.LookUpBtn);
            float down = KeyState(ref ClientInput.LookDownBtn);

            cl.viewangles.X -= speed * _PitchSpeed.Value * up;
            cl.viewangles.X += speed * _PitchSpeed.Value * down;

            if (up != 0 || down != 0)
                View.StopPitchDrift();

            if (cl.viewangles.X > 80)
                cl.viewangles.X = 80;
            if (cl.viewangles.X < -70)
                cl.viewangles.X = -70;

            if (cl.viewangles.Z > 50)
                cl.viewangles.Z = 50;
            if (cl.viewangles.Z < -50)
                cl.viewangles.Z = -50;
        }

        // CL_KeyState
        //
        // Returns 0.25 if a key was pressed and released during the frame,
        // 0.5 if it was pressed and held
        // 0 if held then released, and
        // 1.0 if held for the entire time
        static float KeyState(ref kbutton_t key)
        {
            bool impulsedown = (key.state & 2) != 0;
            bool impulseup = (key.state & 4) != 0;
            bool down = key.IsDown;// ->state & 1;
            float val = 0;

            if (impulsedown && !impulseup)
                if (down)
                    val = 0.5f;	// pressed and held this frame
                else
                    val = 0;	//	I_Error ();
            if (impulseup && !impulsedown)
                if (down)
                    val = 0;	//	I_Error ();
                else
                    val = 0;	// released this frame
            if (!impulsedown && !impulseup)
                if (down)
                    val = 1.0f;	// held the entire frame
                else
                    val = 0;	// up the entire frame
            if (impulsedown && impulseup)
                if (down)
                    val = 0.75f;	// released and re-pressed this frame
                else
                    val = 0.25f;	// pressed and released this frame

            key.state &= 1;		// clear impulses

            return val;
        }

        // CL_SendMove
        public static void SendMove(ref usercmd_t cmd)
        {
            cl.cmd = cmd; // cl.cmd = *cmd - struct copying!!!

            MsgWriter msg = new MsgWriter(128);

            //
            // send the movement message
            //
            msg.WriteByte(Protocol.clc_move);

            msg.WriteFloat((float)cl.mtime[0]);	// so server can get ping times

            msg.WriteAngle(cl.viewangles.X);
            msg.WriteAngle(cl.viewangles.Y);
            msg.WriteAngle(cl.viewangles.Z);

            msg.WriteShort((short)cmd.forwardmove);
            msg.WriteShort((short)cmd.sidemove);
            msg.WriteShort((short)cmd.upmove);

            //
            // send button bits
            //
            int bits = 0;

            if ((ClientInput.AttackBtn.state & 3) != 0)
                bits |= 1;
            ClientInput.AttackBtn.state &= ~2;

            if ((ClientInput.JumpBtn.state & 3) != 0)
                bits |= 2;
            ClientInput.JumpBtn.state &= ~2;

            msg.WriteByte(bits);

            msg.WriteByte(ClientInput.Impulse);
            ClientInput.Impulse = 0;

            //
            // deliver the message
            //
            if (cls.demoplayback)
                return;

            //
            // allways dump the first two message, because it may contain leftover inputs
            // from the last level
            //
            if (++cl.movemessages <= 2)
                return;

            if (Net.SendUnreliableMessage(cls.netcon, msg) == -1)
            {
                Con.Print("CL_SendMove: lost server connection\n");
                Disconnect();
            }
        }

    }

    static class ClientInput
    {
        // kbutton_t in_xxx
        public static kbutton_t MLookBtn;
        public static kbutton_t KLookBtn;
        public static kbutton_t LeftBtn;
        public static kbutton_t RightBtn;
        public static kbutton_t ForwardBtn;
        public static kbutton_t BackBtn;
        public static kbutton_t LookUpBtn;
        public static kbutton_t LookDownBtn;
        public static kbutton_t MoveLeftBtn;
        public static kbutton_t MoveRightBtn;
        public static kbutton_t StrafeBtn;
        public static kbutton_t SpeedBtn;
        public static kbutton_t UseBtn;
        public static kbutton_t JumpBtn;
        public static kbutton_t AttackBtn;
        public static kbutton_t UpBtn;
        public static kbutton_t DownBtn;

        public static int Impulse;

        public static void Init()
        {
            Cmd.Add("+moveup", UpDown);
            Cmd.Add("-moveup", UpUp);
            Cmd.Add("+movedown", DownDown);
            Cmd.Add("-movedown", DownUp);
            Cmd.Add("+left", LeftDown);
            Cmd.Add("-left", LeftUp);
            Cmd.Add("+right", RightDown);
            Cmd.Add("-right", RightUp);
            Cmd.Add("+forward", ForwardDown);
            Cmd.Add("-forward", ForwardUp);
            Cmd.Add("+back", BackDown);
            Cmd.Add("-back", BackUp);
            Cmd.Add("+lookup", LookupDown);
            Cmd.Add("-lookup", LookupUp);
            Cmd.Add("+lookdown", LookdownDown);
            Cmd.Add("-lookdown", LookdownUp);
            Cmd.Add("+strafe", StrafeDown);
            Cmd.Add("-strafe", StrafeUp);
            Cmd.Add("+moveleft", MoveleftDown);
            Cmd.Add("-moveleft", MoveleftUp);
            Cmd.Add("+moveright", MoverightDown);
            Cmd.Add("-moveright", MoverightUp);
            Cmd.Add("+speed", SpeedDown);
            Cmd.Add("-speed", SpeedUp);
            Cmd.Add("+attack", AttackDown);
            Cmd.Add("-attack", AttackUp);
            Cmd.Add("+use", UseDown);
            Cmd.Add("-use", UseUp);
            Cmd.Add("+jump", JumpDown);
            Cmd.Add("-jump", JumpUp);
            Cmd.Add("impulse", ImpulseCmd);
            Cmd.Add("+klook", KLookDown);
            Cmd.Add("-klook", KLookUp);
            Cmd.Add("+mlook", MLookDown);
            Cmd.Add("-mlook", MLookUp);
        }

        static void KeyDown(ref kbutton_t b)
        {
            int k;
            string c = Cmd.Argv(1);
            if (!String.IsNullOrEmpty(c))
                k = int.Parse(c);
            else
                k = -1;	// typed manually at the console for continuous down

            if (k == b.down0 || k == b.down1)
                return;		// repeating key

            if (b.down0 == 0)
                b.down0 = k;
            else if (b.down1 == 0)
                b.down1 = k;
            else
            {
                Con.Print("Three keys down for a button!\n");
                return;
            }

            if ((b.state & 1) != 0)
                return;	// still down
            b.state |= 1 + 2; // down + impulse down
        }

        static void KeyUp(ref kbutton_t b)
        {
            int k;
            string c = Cmd.Argv(1);
            if (!String.IsNullOrEmpty(c))
                k = int.Parse(c);
            else
            {
                // typed manually at the console, assume for unsticking, so clear all
                b.down0 = b.down1 = 0;
                b.state = 4;	// impulse up
                return;
            }

            if (b.down0 == k)
                b.down0 = 0;
            else if (b.down1 == k)
                b.down1 = 0;
            else
                return;	// key up without coresponding down (menu pass through)

            if (b.down0 != 0 || b.down1 != 0)
                return;	// some other key is still holding it down

            if ((b.state & 1) == 0)
                return;		// still up (this should not happen)
            b.state &= ~1;		// now up
            b.state |= 4; 		// impulse up
        }

        static void KLookDown()
        {
            KeyDown(ref KLookBtn);
        }
        static void KLookUp()
        {
            KeyUp(ref KLookBtn);
        }
        static void MLookDown()
        {
            KeyDown(ref MLookBtn);
        }
        static void MLookUp()
        {
            KeyUp(ref MLookBtn);

            if ((MLookBtn.state & 1) == 0 && Client.LookSpring)
                View.StartPitchDrift();
        }
        static void UpDown()
        {
            KeyDown(ref UpBtn);
        }
        static void UpUp()
        {
            KeyUp(ref UpBtn);
        }
        static void DownDown()
        {
            KeyDown(ref DownBtn);
        }
        static void DownUp()
        {
            KeyUp(ref DownBtn);
        }
        static void LeftDown()
        {
            KeyDown(ref LeftBtn);
        }
        static void LeftUp()
        {
            KeyUp(ref LeftBtn);
        }
        static void RightDown()
        {
            KeyDown(ref RightBtn);
        }
        static void RightUp()
        {
            KeyUp(ref RightBtn);
        }
        static void ForwardDown()
        {
            KeyDown(ref ForwardBtn);
        }
        static void ForwardUp()
        {
            KeyUp(ref ForwardBtn);
        }
        static void BackDown()
        {
            KeyDown(ref BackBtn);
        }
        static void BackUp()
        {
            KeyUp(ref BackBtn);
        }
        static void LookupDown()
        {
            KeyDown(ref LookUpBtn);
        }
        static void LookupUp()
        {
            KeyUp(ref LookUpBtn);
        }
        static void LookdownDown()
        {
            KeyDown(ref LookDownBtn);
        }
        static void LookdownUp()
        {
            KeyUp(ref LookDownBtn);
        }
        static void MoveleftDown()
        {
            KeyDown(ref MoveLeftBtn);
        }
        static void MoveleftUp()
        {
            KeyUp(ref MoveLeftBtn);
        }
        static void MoverightDown()
        {
            KeyDown(ref MoveRightBtn);
        }
        static void MoverightUp()
        {
            KeyUp(ref MoveRightBtn);
        }
        static void SpeedDown()
        {
            KeyDown(ref SpeedBtn);
        }
        static void SpeedUp()
        {
            KeyUp(ref SpeedBtn);
        }
        static void StrafeDown()
        {
            KeyDown(ref StrafeBtn);
        }
        static void StrafeUp()
        {
            KeyUp(ref StrafeBtn);
        }
        static void AttackDown()
        {
            KeyDown(ref AttackBtn);
        }
        static void AttackUp()
        {
            KeyUp(ref AttackBtn);
        }
        static void UseDown()
        {
            KeyDown(ref UseBtn);
        }
        static void UseUp()
        {
            KeyUp(ref UseBtn);
        }
        static void JumpDown()
        {
            KeyDown(ref JumpBtn);
        }
        static void JumpUp()
        {
            KeyUp(ref JumpBtn);
        }
        static void ImpulseCmd()
        {
            Impulse = Common.atoi(Cmd.Argv(1));
        }
    }
}
