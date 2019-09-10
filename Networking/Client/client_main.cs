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
using SharpQuake.Game.Rendering;
using SharpQuake.Game.World;
using SharpQuake.Framework.IO;

namespace SharpQuake
{
    partial class client
    {
        // Instance
        public Host Host
        {
            get;
            private set;
        }

        // CL_Init
        public void Initialise( )
        {
            InitInput( Host );
            InitTempEntities();

            if( _Name == null )
            {
                _Name = Host.CVars.Add( "_cl_name", "player", ClientVariableFlags.Archive );
                _Color = Host.CVars.Add( "_cl_color", 0f, ClientVariableFlags.Archive );
                _ShowNet = Host.CVars.Add( "cl_shownet", 0 );	// can be 0, 1, or 2
                _NoLerp = Host.CVars.Add( "cl_nolerp", false );
                _LookSpring = Host.CVars.Add( "lookspring", false, ClientVariableFlags.Archive );
                _LookStrafe = Host.CVars.Add( "lookstrafe", false, ClientVariableFlags.Archive );
                _Sensitivity = Host.CVars.Add( "sensitivity", 3f, ClientVariableFlags.Archive );
                _MPitch = Host.CVars.Add( "m_pitch", 0.022f, ClientVariableFlags.Archive );
                _MYaw = Host.CVars.Add( "m_yaw", 0.022f, ClientVariableFlags.Archive );
                _MForward = Host.CVars.Add( "m_forward", 1f, ClientVariableFlags.Archive );
                _MSide = Host.CVars.Add( "m_side", 0.8f, ClientVariableFlags.Archive );
                _UpSpeed = Host.CVars.Add( "cl_upspeed", 200f );
                _ForwardSpeed = Host.CVars.Add( "cl_forwardspeed", 200f, ClientVariableFlags.Archive );
                _BackSpeed = Host.CVars.Add( "cl_backspeed", 200f, ClientVariableFlags.Archive );
                _SideSpeed = Host.CVars.Add( "cl_sidespeed", 350f );
                _MoveSpeedKey = Host.CVars.Add( "cl_movespeedkey", 2.0f );
                _YawSpeed = Host.CVars.Add( "cl_yawspeed", 140f );
                _PitchSpeed = Host.CVars.Add( "cl_pitchspeed", 150f );
                _AngleSpeedKey = Host.CVars.Add( "cl_anglespeedkey", 1.5f );
            }

            for( var i = 0; i < _EFrags.Length; i++ )
                _EFrags[i] = new EFrag();

            for( var i = 0; i < _Entities.Length; i++ )
                _Entities[i] = new Entity();

            for( var i = 0; i < _StaticEntities.Length; i++ )
                _StaticEntities[i] = new Entity();

            for( var i = 0; i < _DLights.Length; i++ )
                _DLights[i] = new dlight_t();

            //
            // register our commands
            //
            Host.Commands.Add( "cmd", ForwardToServer_f );
            Host.Commands.Add( "entities", PrintEntities_f );
            Host.Commands.Add( "disconnect", Disconnect_f );
            Host.Commands.Add( "record", Record_f );
            Host.Commands.Add( "stop", Stop_f );
            Host.Commands.Add( "playdemo", PlayDemo_f );
            Host.Commands.Add( "timedemo", TimeDemo_f );
        }

        // void	Cmd_ForwardToServer (void);
        // adds the current command line as a clc_stringcmd to the client message.
        // things like godmode, noclip, etc, are commands directed to the server,
        // so when they are typed in at the console, they will need to be forwarded.
        //
        // Sends the entire command line over to the server
        public void ForwardToServer_f( CommandMessage msg )
        {
            if ( Host.Client.cls.state != cactive_t.ca_connected )
            {
                Host.Console.Print( $"Can't \"{msg.Name}\", not connected\n" );
                return;
            }

            if ( Host.Client.cls.demoplayback )
                return;		// not really connected

            var writer = Host.Client.cls.message;
            writer.WriteByte( ProtocolDef.clc_stringcmd );
            if ( !msg.Name.Equals( "cmd" ) )
            {
                writer.Print( msg.Name + " " );
            }
            if ( msg.HasParameters )
            {
                writer.Print( msg.StringParameters );
            }
            else
            {
                writer.Print( "\n" );
            }
        }

        /// <summary>
        /// CL_EstablishConnection
        /// </summary>
        public void EstablishConnection( String host )
        {
            if( cls.state == cactive_t.ca_dedicated )
                return;

            if( cls.demoplayback )
                return;

            Disconnect();

            cls.netcon = Host.Network.Connect( host );
            if( cls.netcon == null )
                Host.Error( "CL_Connect: connect failed\n" );

            Host.Console.DPrint( "CL_EstablishConnection: connected to {0}\n", host );

            cls.demonum = -1;			// not in the demo loop now
            cls.state = cactive_t.ca_connected;
            cls.signon = 0;				// need all the signon messages before playing
        }

        /// <summary>
        /// CL_NextDemo
        ///
        /// Called to play the next demo in the demo loop
        /// </summary>
        public void NextDemo()
        {
            if( cls.demonum == -1 )
                return;		// don't play demos

            Host.Screen.BeginLoadingPlaque();

            if( String.IsNullOrEmpty( cls.demos[cls.demonum] ) || cls.demonum == ClientDef.MAX_DEMOS )
            {
                cls.demonum = 0;
                if( String.IsNullOrEmpty( cls.demos[cls.demonum] ) )
                {
                    Host.Console.Print( "No demos listed with startdemos\n" );
                    cls.demonum = -1;
                    return;
                }
            }

            Host.Commands.Buffer.Insert( String.Format( "playdemo {0}\n", cls.demos[cls.demonum] ) );
            cls.demonum++;
        }

        /// <summary>
        /// CL_AllocDlight
        /// </summary>
        public dlight_t AllocDlight( Int32 key )
        {
            dlight_t dl;

            // first look for an exact key match
            if( key != 0 )
            {
                for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
                {
                    dl = _DLights[i];
                    if( dl.key == key )
                    {
                        dl.Clear();
                        dl.key = key;
                        return dl;
                    }
                }
            }

            // then look for anything else
            //dl = cl_dlights;
            for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
            {
                dl = _DLights[i];
                if( dl.die < cl.time )
                {
                    dl.Clear();
                    dl.key = key;
                    return dl;
                }
            }

            dl = _DLights[0];
            dl.Clear();
            dl.key = key;
            return dl;
        }

        /// <summary>
        /// CL_DecayLights
        /// </summary>
        public void DecayLights()
        {
            var time = ( Single ) ( cl.time - cl.oldtime );

            for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
            {
                var dl = _DLights[i];
                if( dl.die < cl.time || dl.radius == 0 )
                    continue;

                dl.radius -= time * dl.decay;
                if( dl.radius < 0 )
                    dl.radius = 0;
            }
        }

        // CL_Disconnect_f
        public void Disconnect_f( CommandMessage msg )
        {
            Disconnect();
            if( Host.Server.IsActive )
                Host.ShutdownServer( false );
        }

        // CL_SendCmd
        public void SendCmd()
        {
            if( cls.state != cactive_t.ca_connected )
                return;

            if( cls.signon == ClientDef.SIGNONS )
            {
                var cmd = new usercmd_t();

                // get basic movement from keyboard
                BaseMove( ref cmd );

                // allow mice or other external controllers to add to the move
                MainWindow.Input.Move( cmd );

                // send the unreliable message
                Host.Client.SendMove( ref cmd );
            }

            if( cls.demoplayback )
            {
                cls.message.Clear();//    SZ_Clear (cls.message);
                return;
            }

            // send the reliable message
            if( cls.message.IsEmpty )
                return;		// no message at all

            if( !Host.Network.CanSendMessage( cls.netcon ) )
            {
                Host.Console.DPrint( "CL_WriteToServer: can't send\n" );
                return;
            }

            if( Host.Network.SendMessage( cls.netcon, cls.message ) == -1 )
                Host.Error( "CL_WriteToServer: lost server connection" );

            cls.message.Clear();
        }

        // CL_ReadFromServer
        //
        // Read all incoming data from the server
        public Int32 ReadFromServer()
        {
            cl.oldtime = cl.time;
            cl.time += Host.FrameTime;

            Int32 ret;
            do
            {
                ret = GetMessage();
                if( ret == -1 )
                    Host.Error( "CL_ReadFromServer: lost server connection" );
                if( ret == 0 )
                    break;

                cl.last_received_message = ( Single ) Host.RealTime;
                ParseServerMessage();
            } while( ret != 0 && cls.state == cactive_t.ca_connected );

            if( _ShowNet.Get<Int32>( ) != 0 )
                Host.Console.Print( "\n" );

            //
            // bring the links up to date
            //
            RelinkEntities();
            UpdateTempEntities();

            return 0;
        }

        /// <summary>
        /// CL_Disconnect
        ///
        /// Sends a disconnect message to the server
        /// This is also called on Host_Error, so it shouldn't cause any errors
        /// </summary>
        public void Disconnect()
        {
            // stop sounds (especially looping!)
            Host.Sound.StopAllSounds( true );

            // bring the console down and fade the colors back to normal
            //	SCR_BringDownConsole ();

            // if running a local server, shut it down
            if( cls.demoplayback )
                StopPlayback();
            else if( cls.state == cactive_t.ca_connected )
            {
                if( cls.demorecording )
                    Stop_f( null );

                Host.Console.DPrint( "Sending clc_disconnect\n" );
                cls.message.Clear();
                cls.message.WriteByte( ProtocolDef.clc_disconnect );
                Host.Network.SendUnreliableMessage( cls.netcon, cls.message );
                cls.message.Clear();
                Host.Network.Close( cls.netcon );

                cls.state = cactive_t.ca_disconnected;
                if( Host.Server.sv.active )
                    Host.ShutdownServer( false );
            }

            cls.demoplayback = cls.timedemo = false;
            cls.signon = 0;
        }

        // CL_PrintEntities_f
        private void PrintEntities_f( CommandMessage msg )
        {
            for( var i = 0; i < _State.num_entities; i++ )
            {
                var ent = _Entities[i];
                Host.Console.Print( "{0:d3}:", i );
                if( ent.model == null )
                {
                    Host.Console.Print( "EMPTY\n" );
                    continue;
                }
                Host.Console.Print( "{0}:{1:d2}  ({2}) [{3}]\n", ent.model.Name, ent.frame, ent.origin, ent.angles );
            }
        }

        /// <summary>
        /// CL_RelinkEntities
        /// </summary>
        private void RelinkEntities()
        {
            // determine partial update time
            var frac = LerpPoint();

            NumVisEdicts = 0;

            //
            // interpolate player info
            //
            cl.velocity = cl.mvelocity[1] + frac * ( cl.mvelocity[0] - cl.mvelocity[1] );

            if( cls.demoplayback )
            {
                // interpolate the angles
                var angleDelta = cl.mviewangles[0] - cl.mviewangles[1];
                MathLib.CorrectAngles180( ref angleDelta );
                cl.viewangles = cl.mviewangles[1] + frac * angleDelta;
            }

            var bobjrotate = MathLib.AngleMod( 100 * cl.time );

            // start on the entity after the world
            for( var i = 1; i < cl.num_entities; i++ )
            {
                var ent = _Entities[i];
                if( ent.model == null )
                {
                    // empty slot
                    if( ent.forcelink )
                        Host.RenderContext.RemoveEfrags( ent );	// just became empty
                    continue;
                }

                // if the object wasn't included in the last packet, remove it
                if( ent.msgtime != cl.mtime[0] )
                {
                    ent.model = null;
                    continue;
                }

                var oldorg = ent.origin;

                if( ent.forcelink )
                {
                    // the entity was not updated in the last message
                    // so move to the final spot
                    ent.origin = ent.msg_origins[0];
                    ent.angles = ent.msg_angles[0];
                }
                else
                {
                    // if the delta is large, assume a teleport and don't lerp
                    var f = frac;
                    var delta = ent.msg_origins[0] - ent.msg_origins[1];
                    if( Math.Abs( delta.X ) > 100 || Math.Abs( delta.Y ) > 100 || Math.Abs( delta.Z ) > 100 )
                        f = 1; // assume a teleportation, not a motion

                    // interpolate the origin and angles
                    ent.origin = ent.msg_origins[1] + f * delta;
                    var angleDelta = ent.msg_angles[0] - ent.msg_angles[1];
                    MathLib.CorrectAngles180( ref angleDelta );
                    ent.angles = ent.msg_angles[1] + f * angleDelta;
                }

                // rotate binary objects locally
                if( ( ent.model.Flags & EF.EF_ROTATE ) != 0 )
                    ent.angles.Y = bobjrotate;

                if( ( ent.effects & EntityEffects.EF_BRIGHTFIELD ) != 0 )
                    Host.RenderContext.EntityParticles( ent );

                if( ( ent.effects & EntityEffects.EF_MUZZLEFLASH ) != 0 )
                {
                    var dl = AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    Vector3 fv, rv, uv;
                    MathLib.AngleVectors( ref ent.angles, out fv, out rv, out uv );
                    dl.origin += fv * 18;
                    dl.radius = 200 + ( MathLib.Random() & 31 );
                    dl.minlight = 32;
                    dl.die = ( Single ) cl.time + 0.1f;
                }
                if( ( ent.effects & EntityEffects.EF_BRIGHTLIGHT ) != 0 )
                {
                    var dl = AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    dl.radius = 400 + ( MathLib.Random() & 31 );
                    dl.die = ( Single ) cl.time + 0.001f;
                }
                if( ( ent.effects & EntityEffects.EF_DIMLIGHT ) != 0 )
                {
                    var dl = AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.radius = 200 + ( MathLib.Random() & 31 );
                    dl.die = ( Single ) cl.time + 0.001f;
                }

                if( ( ent.model.Flags & EF.EF_GIB ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 2 );
                else if( ( ent.model.Flags & EF.EF_ZOMGIB ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 4 );
                else if( ( ent.model.Flags & EF.EF_TRACER ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 3 );
                else if( ( ent.model.Flags & EF.EF_TRACER2 ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 5 );
                else if( ( ent.model.Flags & EF.EF_ROCKET ) != 0 )
                {
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 0 );
                    var dl = AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.radius = 200;
                    dl.die = ( Single ) cl.time + 0.01f;
                }
                else if( ( ent.model.Flags & EF.EF_GRENADE ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 1 );
                else if( ( ent.model.Flags & EF.EF_TRACER3 ) != 0 )
                    Host.RenderContext.RocketTrail( ref oldorg, ref ent.origin, 6 );

                ent.forcelink = false;

                if( i == cl.viewentity && !Host.ChaseView.IsActive )
                    continue;

                if( NumVisEdicts < ClientDef.MAX_VISEDICTS )
                {
                    _VisEdicts[NumVisEdicts] = ent;
                    NumVisEdicts++;
                }
            }
        }

        /// <summary>
        /// CL_SignonReply
        ///
        /// An svc_signonnum has been received, perform a client side setup
        /// </summary>
        private void SignonReply()
        {
            Host.Console.DPrint( "CL_SignonReply: {0}\n", cls.signon );

            switch( cls.signon )
            {
                case 1:
                    cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    cls.message.WriteString( "prespawn" );
                    break;

                case 2:
                    cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    cls.message.WriteString( String.Format( "name \"{0}\"\n", _Name.Get<String>( ) ) );

                    cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    cls.message.WriteString( String.Format( "color {0} {1}\n", ( ( Int32 ) _Color.Get<Single>( ) ) >> 4, ( ( Int32 ) _Color.Get<Single>( ) ) & 15 ) );

                    cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    cls.message.WriteString( "spawn " + cls.spawnparms );
                    break;

                case 3:
                    cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    cls.message.WriteString( "begin" );
                    Host.Cache.Report();	// print remaining memory
                    break;

                case 4:
                    Host.Screen.EndLoadingPlaque();		// allow normal screen updates
                    break;
            }
        }

        /// <summary>  
        /// CL_ClearState
        /// </summary>
        private void ClearState()
        {
            if( !Host.Server.sv.active )
                Host.ClearMemory();

            // wipe the entire cl structure
            _State.Clear();

            cls.message.Clear();

            // clear other arrays
            foreach( var ef in _EFrags )
                ef.Clear();
            foreach( var et in _Entities )
                et.Clear();

            foreach( var dl in _DLights )
                dl.Clear();

            Array.Clear( _LightStyle, 0, _LightStyle.Length );

            foreach( var et in _TempEntities )
                et.Clear();

            foreach( var b in _Beams )
                b.Clear();

            //
            // allocate the efrags and chain together into a free list
            //
            cl.free_efrags = _EFrags[0];// cl_efrags;
            for( var i = 0; i < ClientDef.MAX_EFRAGS - 1; i++ )
                _EFrags[i].entnext = _EFrags[i + 1];
            _EFrags[ClientDef.MAX_EFRAGS - 1].entnext = null;
        }

        /// <summary>
        /// CL_LerpPoint
        /// Determines the fraction between the last two messages that the objects
        /// should be put at.
        /// </summary>
        private Single LerpPoint()
        {
            var f = cl.mtime[0] - cl.mtime[1];
            if( f == 0 || _NoLerp.Get<Boolean>( ) || cls.timedemo || Host.Server.IsActive )
            {
                cl.time = cl.mtime[0];
                return 1;
            }

            if( f > 0.1 )
            {	// dropped packet, or start of demo
                cl.mtime[1] = cl.mtime[0] - 0.1;
                f = 0.1;
            }
            var frac = ( cl.time - cl.mtime[1] ) / f;
            if( frac < 0 )
            {
                if( frac < -0.01 )
                {
                    cl.time = cl.mtime[1];
                }
                frac = 0;
            }
            else if( frac > 1 )
            {
                if( frac > 1.01 )
                {
                    cl.time = cl.mtime[0];
                }
                frac = 1;
            }
            return ( Single ) frac;
        }
    }
}
