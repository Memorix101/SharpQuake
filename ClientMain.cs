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
    partial class Client
    {
        // CL_Init
        public static void Init()
        {
            InitInput();
            InitTempEntities();

            if (_Name == null)
            {
                _Name = new Cvar("_cl_name", "player", true);
                _Color = new Cvar("_cl_color", "0", true);
                _ShowNet = new Cvar("cl_shownet", "0");	// can be 0, 1, or 2
                _NoLerp = new Cvar("cl_nolerp", "0");
                _LookSpring = new Cvar("lookspring", "0", true);
                _LookStrafe = new Cvar("lookstrafe", "0", true);
                _Sensitivity = new Cvar("sensitivity", "3", true);
                _MPitch = new Cvar("m_pitch", "0.022", true);
                _MYaw = new Cvar("m_yaw", "0.022", true);
                _MForward = new Cvar("m_forward", "1", true);
                _MSide = new Cvar("m_side", "0.8", true);
                _UpSpeed = new Cvar("cl_upspeed", "200");
                _ForwardSpeed = new Cvar("cl_forwardspeed", "200", true);
                _BackSpeed = new Cvar("cl_backspeed", "200", true);
                _SideSpeed = new Cvar("cl_sidespeed", "350");
                _MoveSpeedKey = new Cvar("cl_movespeedkey", "2.0");
                _YawSpeed = new Cvar("cl_yawspeed", "140");
                _PitchSpeed = new Cvar("cl_pitchspeed", "150");
                _AngleSpeedKey = new Cvar("cl_anglespeedkey", "1.5");
            }

            for (int i = 0; i < _EFrags.Length; i++)
                _EFrags[i] = new efrag_t();

            for (int i = 0; i < _Entities.Length; i++)
                _Entities[i] = new entity_t();

            for (int i = 0; i < _StaticEntities.Length; i++)
                _StaticEntities[i] = new entity_t();

            for (int i = 0; i < _DLights.Length; i++)
                _DLights[i] = new dlight_t();

            //
            // register our commands
            //
            Cmd.Add("entities", PrintEntities_f);
            Cmd.Add("disconnect", Disconnect_f);
            Cmd.Add("record", Record_f);
            Cmd.Add("stop", Stop_f);
            Cmd.Add("playdemo", PlayDemo_f);
            Cmd.Add("timedemo", TimeDemo_f);
        }

        /// <summary>
        /// CL_EstablishConnection
        /// </summary>
        public static void EstablishConnection(string host)
        {
            if (cls.state == cactive_t.ca_dedicated)
                return;

            if (cls.demoplayback)
                return;

            Disconnect();

            cls.netcon = Net.Connect(host);
            if (cls.netcon == null)
                Host.Error("CL_Connect: connect failed\n");

            Con.DPrint("CL_EstablishConnection: connected to {0}\n", host);

            cls.demonum = -1;			// not in the demo loop now
            cls.state = cactive_t.ca_connected;
            cls.signon = 0;				// need all the signon messages before playing
        }

        /// <summary>
        /// CL_NextDemo
        /// 
        /// Called to play the next demo in the demo loop
        /// </summary>
        public static void NextDemo()
        {
            if (cls.demonum == -1)
                return;		// don't play demos

            Scr.BeginLoadingPlaque();

            if (String.IsNullOrEmpty(cls.demos[cls.demonum]) || cls.demonum == MAX_DEMOS)
            {
                cls.demonum = 0;
                if (String.IsNullOrEmpty(cls.demos[cls.demonum]))
                {
                    Con.Print("No demos listed with startdemos\n");
                    cls.demonum = -1;
                    return;
                }
            }

            Cbuf.InsertText(String.Format("playdemo {0}\n", cls.demos[cls.demonum]));
            cls.demonum++;
        }

        /// <summary>
        /// CL_AllocDlight
        /// </summary>
        public static dlight_t AllocDlight(int key)
        {
            dlight_t dl;

            // first look for an exact key match
            if (key != 0)
            {
                for (int i = 0; i < MAX_DLIGHTS; i++)
                {
                    dl = _DLights[i];
                    if (dl.key == key)
                    {
                        dl.Clear();
                        dl.key = key;
                        return dl;
                    }
                }
            }

            // then look for anything else
            //dl = cl_dlights;
            for (int i = 0; i < MAX_DLIGHTS; i++)
            {
                dl = _DLights[i];
                if (dl.die < cl.time)
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
        public static void DecayLights()
        {
            float time = (float)(cl.time - cl.oldtime);

            for (int i = 0; i < MAX_DLIGHTS; i++)
            {
                dlight_t dl = _DLights[i];
                if (dl.die < cl.time || dl.radius == 0)
                    continue;

                dl.radius -= time * dl.decay;
                if (dl.radius < 0)
                    dl.radius = 0;
            }
        }

        // CL_PrintEntities_f
        static void PrintEntities_f()
        {
	        for (int i=0; i< _State.num_entities; i++)
	        {
                entity_t ent = _Entities[i];
                Con.Print("{0:d3}:", i);
		        if (ent.model == null)
		        {
			        Con.Print("EMPTY\n");
			        continue;
		        }
		        Con.Print("{0}:{1:d2}  ({2}) [{3}]\n", ent.model.name, ent.frame, ent.origin, ent.angles);
	        }
        }

        // CL_Disconnect_f
        public static void Disconnect_f()
        {
            Disconnect();
            if (Server.IsActive)
                Host.ShutdownServer(false);
        }

        // CL_SendCmd
        public static void SendCmd()
        {
            if (cls.state != cactive_t.ca_connected)
                return;

            if (cls.signon == SIGNONS)
            {
                usercmd_t cmd = new usercmd_t();

                // get basic movement from keyboard
                BaseMove(ref cmd);

                // allow mice or other external controllers to add to the move
                Input.Move(cmd);

                // send the unreliable message
                Client.SendMove(ref cmd);

            }

            if (cls.demoplayback)
            {
                cls.message.Clear();//    SZ_Clear (cls.message);
                return;
            }

            // send the reliable message
            if (cls.message.IsEmpty)
                return;		// no message at all

            if (!Net.CanSendMessage(cls.netcon))
            {
                Con.DPrint("CL_WriteToServer: can't send\n");
                return;
            }

            if (Net.SendMessage(cls.netcon, cls.message) == -1)
                Host.Error("CL_WriteToServer: lost server connection");

            cls.message.Clear();
        }

        // CL_ReadFromServer
        //
        // Read all incoming data from the server
        public static int ReadFromServer()
        {
            cl.oldtime = cl.time;
            cl.time += Host.FrameTime;

            int ret;
            do
            {
                ret = GetMessage();
                if (ret == -1)
                    Host.Error("CL_ReadFromServer: lost server connection");
                if (ret == 0)
                    break;

                cl.last_received_message = (float)Host.RealTime;
                ParseServerMessage();
            } while (ret != 0 && cls.state == cactive_t.ca_connected);

            if (_ShowNet.Value != 0)
                Con.Print("\n");


            //
            // bring the links up to date
            //
            RelinkEntities();
            UpdateTempEntities();

            return 0;
        }


        /// <summary>
        /// CL_RelinkEntities 
        /// </summary>
        static void RelinkEntities()
        {
            // determine partial update time
            float frac = LerpPoint();

            NumVisEdicts = 0;

            //
            // interpolate player info
            //
            cl.velocity = cl.mvelocity[1] + frac * (cl.mvelocity[0] - cl.mvelocity[1]);

            if (cls.demoplayback)
            {
                // interpolate the angles
                Vector3 angleDelta = cl.mviewangles[0] - cl.mviewangles[1];
                Mathlib.CorrectAngles180(ref angleDelta);
                cl.viewangles = cl.mviewangles[1] + frac * angleDelta;
            }

            float bobjrotate = Mathlib.AngleMod(100 * cl.time);
            
            // start on the entity after the world
            for (int i = 1; i < cl.num_entities; i++)
            {
                entity_t ent = _Entities[i];
                if (ent.model == null)
                {
                    // empty slot
                    if (ent.forcelink)
                        Render.RemoveEfrags(ent);	// just became empty
                    continue;
                }

                // if the object wasn't included in the last packet, remove it
                if (ent.msgtime != cl.mtime[0])
                {
                    ent.model = null;
                    continue;
                }

                Vector3 oldorg = ent.origin;

                if (ent.forcelink)
                {	
                    // the entity was not updated in the last message
                    // so move to the final spot
                    ent.origin = ent.msg_origins[0];
                    ent.angles = ent.msg_angles[0];
                }
                else
                {
                    // if the delta is large, assume a teleport and don't lerp
                    float f = frac;
                    Vector3 delta = ent.msg_origins[0] - ent.msg_origins[1];
                    if (Math.Abs(delta.X) > 100 || Math.Abs(delta.Y) > 100 || Math.Abs(delta.Z) > 100)
                        f = 1; // assume a teleportation, not a motion

                    // interpolate the origin and angles
                    ent.origin = ent.msg_origins[1] + f * delta;
                    Vector3 angleDelta = ent.msg_angles[0] - ent.msg_angles[1];
                    Mathlib.CorrectAngles180(ref angleDelta);
                    ent.angles = ent.msg_angles[1] + f * angleDelta;
                }

                // rotate binary objects locally
                if ((ent.model.flags & EF.EF_ROTATE) != 0)
                    ent.angles.Y = bobjrotate;

                if ((ent.effects & EntityEffects.EF_BRIGHTFIELD) != 0)
                    Render.EntityParticles(ent);

                if ((ent.effects & EntityEffects.EF_MUZZLEFLASH) != 0)
                {
                    dlight_t dl = AllocDlight(i);
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    Vector3 fv, rv, uv;
                    Mathlib.AngleVectors(ref ent.angles, out fv, out rv, out uv);
                    dl.origin += fv * 18;
                    dl.radius = 200 + (Sys.Random() & 31);
                    dl.minlight = 32;
                    dl.die = (float)cl.time + 0.1f;
                }
                if ((ent.effects & EntityEffects.EF_BRIGHTLIGHT) != 0)
                {
                    dlight_t dl = AllocDlight(i);
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    dl.radius = 400 + (Sys.Random() & 31);
                    dl.die = (float)cl.time + 0.001f;
                }
                if ((ent.effects & EntityEffects.EF_DIMLIGHT) != 0)
                {
                    dlight_t dl = AllocDlight(i);
                    dl.origin = ent.origin;
                    dl.radius = 200 + (Sys.Random() & 31);
                    dl.die = (float)cl.time + 0.001f;
                }

                if ((ent.model.flags & EF.EF_GIB) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 2);
                else if ((ent.model.flags & EF.EF_ZOMGIB) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 4);
                else if ((ent.model.flags & EF.EF_TRACER) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 3);
                else if ((ent.model.flags & EF.EF_TRACER2) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 5);
                else if ((ent.model.flags & EF.EF_ROCKET) != 0)
                {
                    Render.RocketTrail(ref oldorg, ref ent.origin, 0);
                    dlight_t dl = AllocDlight(i);
                    dl.origin = ent.origin;
                    dl.radius = 200;
                    dl.die = (float)cl.time + 0.01f;
                }
                else if ((ent.model.flags & EF.EF_GRENADE) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 1);
                else if ((ent.model.flags & EF.EF_TRACER3) != 0)
                    Render.RocketTrail(ref oldorg, ref ent.origin, 6);

                ent.forcelink = false;

                if (i == cl.viewentity && !Chase.IsActive)
                    continue;

                if (NumVisEdicts < MAX_VISEDICTS)
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
        static void SignonReply()
        {
            Con.DPrint("CL_SignonReply: {0}\n", cls.signon);

            switch (cls.signon)
            {
                case 1:
                    cls.message.WriteByte(Protocol.clc_stringcmd);
                    cls.message.WriteString("prespawn");
                    break;

                case 2:
                    cls.message.WriteByte(Protocol.clc_stringcmd);
                    cls.message.WriteString(String.Format("name \"{0}\"\n", _Name.String));

                    cls.message.WriteByte(Protocol.clc_stringcmd);
                    cls.message.WriteString(String.Format("color {0} {1}\n", ((int)_Color.Value) >> 4, ((int)_Color.Value) & 15));

                    cls.message.WriteByte(Protocol.clc_stringcmd);
                    cls.message.WriteString("spawn " + cls.spawnparms);
                    break;

                case 3:
                    cls.message.WriteByte(Protocol.clc_stringcmd);
                    cls.message.WriteString("begin");
                    Cache.Report();	// print remaining memory
                    break;

                case 4:
                    Scr.EndLoadingPlaque();		// allow normal screen updates
                    break;
            }
        }

        /// <summary>
        /// CL_ClearState
        /// </summary>
        static void ClearState()
        {
            if (!Server.sv.active)
                Host.ClearMemory();

            // wipe the entire cl structure
            _State.Clear();

            cls.message.Clear();

            // clear other arrays
            foreach (efrag_t ef in _EFrags)
                ef.Clear();
            foreach (entity_t et in _Entities)
                et.Clear();

            foreach (dlight_t dl in _DLights)
                dl.Clear();
            
            Array.Clear(_LightStyle, 0, _LightStyle.Length);

            foreach (entity_t et in _TempEntities)
                et.Clear();

            foreach (beam_t b in _Beams)
                b.Clear();

            //
            // allocate the efrags and chain together into a free list
            //
            cl.free_efrags = _EFrags[0];// cl_efrags;
            for (int i = 0; i < MAX_EFRAGS - 1; i++)
                _EFrags[i].entnext = _EFrags[i + 1];
            _EFrags[MAX_EFRAGS - 1].entnext = null;
        }

        /// <summary>
        /// CL_Disconnect
        /// 
        /// Sends a disconnect message to the server
        /// This is also called on Host_Error, so it shouldn't cause any errors
        /// </summary>
        public static void Disconnect()
        {
            // stop sounds (especially looping!)
            Sound.StopAllSounds(true);

            // bring the console down and fade the colors back to normal
            //	SCR_BringDownConsole ();

            // if running a local server, shut it down
            if (cls.demoplayback)
                StopPlayback();
            else if (cls.state == cactive_t.ca_connected)
            {
                if (cls.demorecording)
                    Stop_f();

                Con.DPrint("Sending clc_disconnect\n");
                cls.message.Clear();
                cls.message.WriteByte(Protocol.clc_disconnect);
                Net.SendUnreliableMessage(cls.netcon, cls.message);
                cls.message.Clear();
                Net.Close(cls.netcon);

                cls.state = cactive_t.ca_disconnected;
                if (Server.sv.active)
                    Host.ShutdownServer(false);
            }

            cls.demoplayback = cls.timedemo = false;
            cls.signon = 0;
        }

        /// <summary>
        /// CL_LerpPoint
        /// Determines the fraction between the last two messages that the objects
        /// should be put at.
        /// </summary>
        static float LerpPoint()
        {
            double f = cl.mtime[0] - cl.mtime[1];
            if (f == 0 || _NoLerp.Value != 0 || cls.timedemo || Server.IsActive)
            {
                cl.time = cl.mtime[0];
                return 1;
            }

            if (f > 0.1)
            {	// dropped packet, or start of demo
                cl.mtime[1] = cl.mtime[0] - 0.1;
                f = 0.1;
            }
            double frac = (cl.time - cl.mtime[1]) / f;
            if (frac < 0)
            {
                if (frac < -0.01)
                {
                    cl.time = cl.mtime[1];
                }
                frac = 0;
            }
            else if (frac > 1)
            {
                if (frac > 1.01)
                {
                    cl.time = cl.mtime[0];
                }
                frac = 1;
            }
            return (float)frac;
        }

    }
}
