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
using SharpQuake.Framework.IO;

// cl_input.c

namespace SharpQuake
{
    internal static class client_input
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

        public static Int32 Impulse;

        public static Host Host
        {
            get;
            private set;
        }

        public static void Init( Host host )
        {
            Host = host;

            Host.Commands.Add( "+moveup", UpDown );
            Host.Commands.Add( "-moveup", UpUp );
            Host.Commands.Add( "+movedown", DownDown );
            Host.Commands.Add( "-movedown", DownUp );
            Host.Commands.Add( "+left", LeftDown );
            Host.Commands.Add( "-left", LeftUp );
            Host.Commands.Add( "+right", RightDown );
            Host.Commands.Add( "-right", RightUp );
            Host.Commands.Add( "+forward", ForwardDown );
            Host.Commands.Add( "-forward", ForwardUp );
            Host.Commands.Add( "+back", BackDown );
            Host.Commands.Add( "-back", BackUp );
            Host.Commands.Add( "+lookup", LookupDown );
            Host.Commands.Add( "-lookup", LookupUp );
            Host.Commands.Add( "+lookdown", LookdownDown );
            Host.Commands.Add( "-lookdown", LookdownUp );
            Host.Commands.Add( "+strafe", StrafeDown );
            Host.Commands.Add( "-strafe", StrafeUp );
            Host.Commands.Add( "+moveleft", MoveleftDown );
            Host.Commands.Add( "-moveleft", MoveleftUp );
            Host.Commands.Add( "+moveright", MoverightDown );
            Host.Commands.Add( "-moveright", MoverightUp );
            Host.Commands.Add( "+speed", SpeedDown );
            Host.Commands.Add( "-speed", SpeedUp );
            Host.Commands.Add( "+attack", AttackDown );
            Host.Commands.Add( "-attack", AttackUp );
            Host.Commands.Add( "+use", UseDown );
            Host.Commands.Add( "-use", UseUp );
            Host.Commands.Add( "+jump", JumpDown );
            Host.Commands.Add( "-jump", JumpUp );
            Host.Commands.Add( "impulse", ImpulseCmd );
            Host.Commands.Add( "+klook", KLookDown );
            Host.Commands.Add( "-klook", KLookUp );
            Host.Commands.Add( "+mlook", MLookDown );
            Host.Commands.Add( "-mlook", MLookUp );
        }

        private static void KeyDown( CommandMessage msg, ref kbutton_t b )
        {
            Int32 k;
            if ( msg.Parameters?.Length > 0 && !String.IsNullOrEmpty( msg.Parameters[0] ) )
                k = Int32.Parse( msg.Parameters[0] );
            else
                k = -1;	// typed manually at the console for continuous down

            if ( k == b.down0 || k == b.down1 )
                return;		// repeating key

            if ( b.down0 == 0 )
                b.down0 = k;
            else if ( b.down1 == 0 )
                b.down1 = k;
            else
            {
                Host.Console.Print( "Three keys down for a button!\n" );
                return;
            }

            if ( ( b.state & 1 ) != 0 )
                return;	// still down
            b.state |= 1 + 2; // down + impulse down
        }

        private static void KeyUp( CommandMessage msg, ref kbutton_t b )
        {
            Int32 k;
            if ( msg.Parameters?.Length > 0 && !String.IsNullOrEmpty( msg.Parameters[0] ) )
                k = Int32.Parse( msg.Parameters[0] );
            else
            {
                // typed manually at the console, assume for unsticking, so clear all
                b.down0 = b.down1 = 0;
                b.state = 4;	// impulse up
                return;
            }

            if ( b.down0 == k )
                b.down0 = 0;
            else if ( b.down1 == k )
                b.down1 = 0;
            else
                return;	// key up without coresponding down (menu pass through)

            if ( b.down0 != 0 || b.down1 != 0 )
                return;	// some other key is still holding it down

            if ( ( b.state & 1 ) == 0 )
                return;		// still up (this should not happen)
            b.state &= ~1;		// now up
            b.state |= 4; 		// impulse up
        }

        private static void KLookDown( CommandMessage msg )
        {
            KeyDown( msg, ref KLookBtn );
        }

        private static void KLookUp( CommandMessage msg )
        {
            KeyUp( msg, ref KLookBtn );
        }

        private static void MLookDown( CommandMessage msg )
        {
            KeyDown( msg, ref MLookBtn );
        }

        private static void MLookUp( CommandMessage msg )
        {
            KeyUp( msg, ref MLookBtn );

            if ( ( MLookBtn.state & 1 ) == 0 && Host.Client.LookSpring )
                Host.View.StartPitchDrift( null );
        }

        private static void UpDown( CommandMessage msg )
        {
            KeyDown( msg, ref UpBtn );
        }

        private static void UpUp( CommandMessage msg )
        {
            KeyUp( msg, ref UpBtn );
        }

        private static void DownDown( CommandMessage msg )
        {
            KeyDown( msg, ref DownBtn );
        }

        private static void DownUp( CommandMessage msg )
        {
            KeyUp( msg, ref DownBtn );
        }

        private static void LeftDown( CommandMessage msg )
        {
            KeyDown( msg, ref LeftBtn );
        }

        private static void LeftUp( CommandMessage msg )
        {
            KeyUp( msg, ref LeftBtn );
        }

        private static void RightDown( CommandMessage msg )
        {
            KeyDown( msg, ref RightBtn );
        }

        private static void RightUp( CommandMessage msg )
        {
            KeyUp( msg, ref RightBtn );
        }

        private static void ForwardDown( CommandMessage msg )
        {
            KeyDown( msg, ref ForwardBtn );
        }

        private static void ForwardUp( CommandMessage msg )
        {
            KeyUp( msg, ref ForwardBtn );
        }

        private static void BackDown( CommandMessage msg )
        {
            KeyDown( msg, ref BackBtn );
        }

        private static void BackUp( CommandMessage msg )
        {
            KeyUp( msg, ref BackBtn );
        }

        private static void LookupDown( CommandMessage msg )
        {
            KeyDown( msg, ref LookUpBtn );
        }

        private static void LookupUp( CommandMessage msg )
        {
            KeyUp( msg, ref LookUpBtn );
        }

        private static void LookdownDown( CommandMessage msg )
        {
            KeyDown( msg, ref LookDownBtn );
        }

        private static void LookdownUp( CommandMessage msg )
        {
            KeyUp( msg, ref LookDownBtn );
        }

        private static void MoveleftDown( CommandMessage msg )
        {
            KeyDown( msg, ref MoveLeftBtn );
        }

        private static void MoveleftUp( CommandMessage msg )
        {
            KeyUp( msg, ref MoveLeftBtn );
        }

        private static void MoverightDown( CommandMessage msg )
        {
            KeyDown( msg, ref MoveRightBtn );
        }

        private static void MoverightUp( CommandMessage msg )
        {
            KeyUp( msg, ref MoveRightBtn );
        }

        private static void SpeedDown( CommandMessage msg )
        {
            KeyDown( msg, ref SpeedBtn );
        }

        private static void SpeedUp( CommandMessage msg )
        {
            KeyUp( msg, ref SpeedBtn );
        }

        private static void StrafeDown( CommandMessage msg )
        {
            KeyDown( msg, ref StrafeBtn );
        }

        private static void StrafeUp( CommandMessage msg )
        {
            KeyUp( msg, ref StrafeBtn );
        }

        private static void AttackDown( CommandMessage msg )
        {
            KeyDown( msg, ref AttackBtn );
        }

        private static void AttackUp( CommandMessage msg )
        {
            KeyUp( msg, ref AttackBtn );
        }

        private static void UseDown( CommandMessage msg )
        {
            KeyDown( msg, ref UseBtn );
        }

        private static void UseUp( CommandMessage msg )
        {
            KeyUp( msg, ref UseBtn );
        }

        private static void JumpDown( CommandMessage msg )
        {
            KeyDown( msg, ref JumpBtn );
        }

        private static void JumpUp( CommandMessage msg )
        {
            KeyUp( msg, ref JumpBtn );
        }

        private static void ImpulseCmd( CommandMessage msg )
        {
            Impulse = MathLib.atoi( msg.Parameters[0] );
        }
    }

    partial class client
    {
        // CL_SendMove
        public void SendMove( ref usercmd_t cmd )
        {
            cl.cmd = cmd; // cl.cmd = *cmd - struct copying!!!

            var msg = new MessageWriter( 128 );

            //
            // send the movement message
            //
            msg.WriteByte( ProtocolDef.clc_move );

            msg.WriteFloat( ( Single ) cl.mtime[0] );	// so server can get ping times

            msg.WriteAngle( cl.viewangles.X );
            msg.WriteAngle( cl.viewangles.Y );
            msg.WriteAngle( cl.viewangles.Z );

            msg.WriteShort( ( Int16 ) cmd.forwardmove );
            msg.WriteShort( ( Int16 ) cmd.sidemove );
            msg.WriteShort( ( Int16 ) cmd.upmove );

            //
            // send button bits
            //
            var bits = 0;

            if ( ( client_input.AttackBtn.state & 3 ) != 0 )
                bits |= 1;
            client_input.AttackBtn.state &= ~2;

            if ( ( client_input.JumpBtn.state & 3 ) != 0 )
                bits |= 2;
            client_input.JumpBtn.state &= ~2;

            msg.WriteByte( bits );

            msg.WriteByte( client_input.Impulse );
            client_input.Impulse = 0;

            //
            // deliver the message
            //
            if ( cls.demoplayback )
                return;

            //
            // allways dump the first two message, because it may contain leftover inputs
            // from the last level
            //
            if ( ++cl.movemessages <= 2 )
                return;

            if ( Host.Network.SendUnreliableMessage( cls.netcon, msg ) == -1 )
            {
                Host.Console.Print( "CL_SendMove: lost server connection\n" );
                Disconnect( );
            }
        }

        // CL_InitInput
        private void InitInput( Host host )
        {
            client_input.Init( host );
        }

        /// <summary>
        /// CL_BaseMove
        /// Send the intended movement message to the server
        /// </summary>
        private void BaseMove( ref usercmd_t cmd )
        {
            if ( cls.signon != ClientDef.SIGNONS )
                return;

            AdjustAngles( );

            cmd.Clear( );

            if ( client_input.StrafeBtn.IsDown )
            {
                cmd.sidemove += _SideSpeed.Get<Single>( ) * KeyState( ref client_input.RightBtn );
                cmd.sidemove -= _SideSpeed.Get<Single>( ) * KeyState( ref client_input.LeftBtn );
            }

            cmd.sidemove += _SideSpeed.Get<Single>( ) * KeyState( ref client_input.MoveRightBtn );
            cmd.sidemove -= _SideSpeed.Get<Single>( ) * KeyState( ref client_input.MoveLeftBtn );

            var upBtn = KeyState( ref client_input.UpBtn );
            if ( upBtn > 0 )
                Console.WriteLine( "asd" );
            cmd.upmove += _UpSpeed.Get<Single>( ) * KeyState( ref client_input.UpBtn );
            cmd.upmove -= _UpSpeed.Get<Single>( ) * KeyState( ref client_input.DownBtn );

            if ( !client_input.KLookBtn.IsDown )
            {
                cmd.forwardmove += _ForwardSpeed.Get<Single>( ) * KeyState( ref client_input.ForwardBtn );
                cmd.forwardmove -= _BackSpeed.Get<Single>( ) * KeyState( ref client_input.BackBtn );
            }

            //
            // adjust for speed key
            //
            if ( client_input.SpeedBtn.IsDown )
            {
                cmd.forwardmove *= _MoveSpeedKey.Get<Single>( );
                cmd.sidemove *= _MoveSpeedKey.Get<Single>( );
                cmd.upmove *= _MoveSpeedKey.Get<Single>( );
            }
        }

        // CL_AdjustAngles
        //
        // Moves the local angle positions
        private void AdjustAngles( )
        {
            var speed = ( Single ) Host.FrameTime;

            if ( client_input.SpeedBtn.IsDown )
                speed *= _AngleSpeedKey.Get<Single>( );

            if ( !client_input.StrafeBtn.IsDown )
            {
                cl.viewangles.Y -= speed * _YawSpeed.Get<Single>( ) * KeyState( ref client_input.RightBtn );
                cl.viewangles.Y += speed * _YawSpeed.Get<Single>( ) * KeyState( ref client_input.LeftBtn );
                cl.viewangles.Y = MathLib.AngleMod( cl.viewangles.Y );
            }

            if ( client_input.KLookBtn.IsDown )
            {
                Host.View.StopPitchDrift( );
                cl.viewangles.X -= speed * _PitchSpeed.Get<Single>( ) * KeyState( ref client_input.ForwardBtn );
                cl.viewangles.X += speed * _PitchSpeed.Get<Single>( ) * KeyState( ref client_input.BackBtn );
            }

            var up = KeyState( ref client_input.LookUpBtn );
            var down = KeyState( ref client_input.LookDownBtn );

            cl.viewangles.X -= speed * _PitchSpeed.Get<Single>( ) * up;
            cl.viewangles.X += speed * _PitchSpeed.Get<Single>( ) * down;

            if ( up != 0 || down != 0 )
                Host.View.StopPitchDrift( );

            if ( cl.viewangles.X > 80 )
                cl.viewangles.X = 80;
            if ( cl.viewangles.X < -70 )
                cl.viewangles.X = -70;

            if ( cl.viewangles.Z > 50 )
                cl.viewangles.Z = 50;
            if ( cl.viewangles.Z < -50 )
                cl.viewangles.Z = -50;
        }

        // CL_KeyState
        //
        // Returns 0.25 if a key was pressed and released during the frame,
        // 0.5 if it was pressed and held
        // 0 if held then released, and
        // 1.0 if held for the entire time
        private Single KeyState( ref kbutton_t key )
        {
            var impulsedown = ( key.state & 2 ) != 0;
            var impulseup = ( key.state & 4 ) != 0;
            var down = key.IsDown;// ->state & 1;
            Single val = 0;

            if ( impulsedown && !impulseup )
                if ( down )
                    val = 0.5f;	// pressed and held this frame
                else
                    val = 0;	//	I_Error ();
            if ( impulseup && !impulsedown )
                if ( down )
                    val = 0;	//	I_Error ();
                else
                    val = 0;	// released this frame
            if ( !impulsedown && !impulseup )
                if ( down )
                    val = 1.0f;	// held the entire frame
                else
                    val = 0;	// up the entire frame
            if ( impulsedown && impulseup )
                if ( down )
                    val = 0.75f;	// released and re-pressed this frame
                else
                    val = 0.25f;	// pressed and released this frame

            key.state &= 1;		// clear impulses

            return val;
        }
    }
}
