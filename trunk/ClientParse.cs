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

// cl_parse.c

namespace SharpQuake
{
    partial class Client
    {
        const string ConsoleBar = "\n\n\u001D\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001F\n\n";
        
        static string[] _SvcStrings = new string[]
        {
	        "svc_bad",
	        "svc_nop",
	        "svc_disconnect",
	        "svc_updatestat",
	        "svc_version",		// [long] server version
	        "svc_setview",		// [short] entity number
	        "svc_sound",			// <see code>
	        "svc_time",			// [float] server time
	        "svc_print",			// [string] null terminated string
	        "svc_stufftext",		// [string] stuffed into client's console buffer
						        // the string should be \n terminated
	        "svc_setangle",		// [vec3] set the view angle to this absolute value
	
	        "svc_serverinfo",		// [long] version
						        // [string] signon string
						        // [string]..[0]model cache [string]...[0]sounds cache
						        // [string]..[0]item cache
	        "svc_lightstyle",		// [byte] [string]
	        "svc_updatename",		// [byte] [string]
	        "svc_updatefrags",	// [byte] [short]
	        "svc_clientdata",		// <shortbits + data>
	        "svc_stopsound",		// <see code>
	        "svc_updatecolors",	// [byte] [byte]
	        "svc_particle",		// [vec3] <variable>
	        "svc_damage",			// [byte] impact [byte] blood [vec3] from
	
	        "svc_spawnstatic",
	        "OBSOLETE svc_spawnbinary",
	        "svc_spawnbaseline",
	
	        "svc_temp_entity",		// <variable>
	        "svc_setpause",
	        "svc_signonnum",
	        "svc_centerprint",
	        "svc_killedmonster",
	        "svc_foundsecret",
	        "svc_spawnstaticsound",
	        "svc_intermission",
	        "svc_finale",			// [string] music [string] text
	        "svc_cdtrack",			// [byte] track [byte] looptrack
	        "svc_sellscreen",
	        "svc_cutscene"
        };

        static int[] _BitCounts = new int[16]; // bitcounts
        static object _MsgState; // used by KeepaliveMessage function
        static float _LastMsg; // static float lastmsg from CL_KeepaliveMessage

        
        /// <summary>
        /// CL_ParseServerMessage
        /// </summary>
        static void ParseServerMessage()
        {
            //
            // if recording demos, copy the message out
            //
            if (_ShowNet.Value == 1)
                Con.Print("{0} ", Net.Message.Length);
            else if (_ShowNet.Value == 2)
                Con.Print("------------------\n");

            cl.onground = false;	// unless the server says otherwise	

            //
            // parse the message
            //
            Net.Reader.Reset();
            int i;
            while (true)
            {
                if (Net.Reader.IsBadRead)
                    Host.Error("CL_ParseServerMessage: Bad server message");

                int cmd = Net.Reader.ReadByte();
                if (cmd == -1)
                {
                    ShowNet("END OF MESSAGE");
                    return;	// end of message
                }

                // if the high bit of the command byte is set, it is a fast update
                if ((cmd & 128) != 0)
                {
                    ShowNet("fast update");
                    ParseUpdate(cmd & 127);
                    continue;
                }

                ShowNet(_SvcStrings[cmd]);

                // other commands
                switch (cmd)
                {
                    default:
                        Host.Error("CL_ParseServerMessage: Illegible server message\n");
                        break;

                    case Protocol.svc_nop:
                        break;

                    case Protocol.svc_time:
                        cl.mtime[1] = cl.mtime[0];
                        cl.mtime[0] = Net.Reader.ReadFloat();
                        break;

                    case Protocol.svc_clientdata:
                        i = Net.Reader.ReadShort();
                        ParseClientData(i);
                        break;

                    case Protocol.svc_version:
                        i = Net.Reader.ReadLong();
                        if (i != Protocol.PROTOCOL_VERSION)
                            Host.Error("CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, Protocol.PROTOCOL_VERSION);
                        break;

                    case Protocol.svc_disconnect:
                        Host.EndGame("Server disconnected\n");
                        break;

                    case Protocol.svc_print:
                        Con.Print(Net.Reader.ReadString());
                        break;

                    case Protocol.svc_centerprint:
                        Scr.CenterPrint(Net.Reader.ReadString());
                        break;

                    case Protocol.svc_stufftext:
                        Cbuf.AddText(Net.Reader.ReadString());
                        break;

                    case Protocol.svc_damage:
                        View.ParseDamage();
                        break;

                    case Protocol.svc_serverinfo:
                        ParseServerInfo();
                        Scr.vid.recalc_refdef = true;	// leave intermission full screen
                        break;

                    case Protocol.svc_setangle:
                        cl.viewangles.X = Net.Reader.ReadAngle();
                        cl.viewangles.Y = Net.Reader.ReadAngle();
                        cl.viewangles.Z = Net.Reader.ReadAngle();
                        break;

                    case Protocol.svc_setview:
                        cl.viewentity = Net.Reader.ReadShort();
                        break;

                    case Protocol.svc_lightstyle:
                        i = Net.Reader.ReadByte();
                        if (i >= QDef.MAX_LIGHTSTYLES)
                            Sys.Error("svc_lightstyle > MAX_LIGHTSTYLES");
                        _LightStyle[i].map = Net.Reader.ReadString();
                        break;

                    case Protocol.svc_sound:
                        ParseStartSoundPacket();
                        break;

                    case Protocol.svc_stopsound:
                        i = Net.Reader.ReadShort();
                        Sound.StopSound(i >> 3, i & 7);
                        break;

                    case Protocol.svc_updatename:
                        Sbar.Changed();
                        i = Net.Reader.ReadByte();
                        if (i >= cl.maxclients)
                            Host.Error("CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD");
                        cl.scores[i].name = Net.Reader.ReadString();
                        break;

                    case Protocol.svc_updatefrags:
                        Sbar.Changed();
                        i = Net.Reader.ReadByte();
                        if (i >= cl.maxclients)
                            Host.Error("CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD");
                        cl.scores[i].frags = Net.Reader.ReadShort();
                        break;

                    case Protocol.svc_updatecolors:
                        Sbar.Changed();
                        i = Net.Reader.ReadByte();
                        if (i >= cl.maxclients)
                            Host.Error("CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD");
                        cl.scores[i].colors = Net.Reader.ReadByte();
                        NewTranslation(i);
                        break;

                    case Protocol.svc_particle:
                        Render.ParseParticleEffect();
                        break;

                    case Protocol.svc_spawnbaseline:
                        i = Net.Reader.ReadShort();
                        // must use CL_EntityNum() to force cl.num_entities up
                        ParseBaseline(EntityNum(i));
                        break;

                    case Protocol.svc_spawnstatic:
                        ParseStatic();
                        break;

                    case Protocol.svc_temp_entity:
                        ParseTempEntity();
                        break;

                    case Protocol.svc_setpause:
                        {
                            cl.paused = Net.Reader.ReadByte() != 0;

                            if (cl.paused)
                            {
                                CDAudio.Pause();
                            }
                            else
                            {
                                CDAudio.Resume();
                            }
                        }
                        break;

                    case Protocol.svc_signonnum:
                        i = Net.Reader.ReadByte();
                        if (i <= cls.signon)
                            Host.Error("Received signon {0} when at {1}", i, cls.signon);
                        cls.signon = i;
                        SignonReply();
                        break;

                    case Protocol.svc_killedmonster:
                        cl.stats[QStats.STAT_MONSTERS]++;
                        break;

                    case Protocol.svc_foundsecret:
                        cl.stats[QStats.STAT_SECRETS]++;
                        break;

                    case Protocol.svc_updatestat:
                        i = Net.Reader.ReadByte();
                        if (i < 0 || i >= QStats.MAX_CL_STATS)
                            Sys.Error("svc_updatestat: {0} is invalid", i);
                        cl.stats[i] = Net.Reader.ReadLong();
                        break;

                    case Protocol.svc_spawnstaticsound:
                        ParseStaticSound();
                        break;

                    case Protocol.svc_cdtrack:
                        cl.cdtrack = Net.Reader.ReadByte();
                        cl.looptrack = Net.Reader.ReadByte();
                        if ((cls.demoplayback || cls.demorecording) && (cls.forcetrack != -1))
                            CDAudio.Play((byte)cls.forcetrack, true);
                        else
                            CDAudio.Play((byte)cl.cdtrack, true);
                        break;

                    case Protocol.svc_intermission:
                        cl.intermission = 1;
                        cl.completed_time = (int)cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        break;

                    case Protocol.svc_finale:
                        cl.intermission = 2;
                        cl.completed_time = (int)cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        Scr.CenterPrint(Net.Reader.ReadString());
                        break;

                    case Protocol.svc_cutscene:
                        cl.intermission = 3;
                        cl.completed_time = (int)cl.time;
                        Scr.vid.recalc_refdef = true;	// go to full screen
                        Scr.CenterPrint(Net.Reader.ReadString());
                        break;

                    case Protocol.svc_sellscreen:
                        Cmd.ExecuteString("help", cmd_source_t.src_command);
                        break;
                }
            }
        }

        static void ShowNet(string s)
        {
            if (_ShowNet.Value == 2)
                Con.Print("{0,3}:{1}\n", Net.Reader.Position - 1, s);
        }

        /// <summary>
        /// CL_ParseUpdate
        /// 
        /// Parse an entity update message from the server
        /// If an entities model or origin changes from frame to frame, it must be
        /// relinked.  Other attributes can change without relinking.
        /// </summary>
        static void ParseUpdate(int bits)
        {
            int i;

            if (cls.signon == SIGNONS - 1)
            {
                // first update is the final signon stage
                cls.signon = SIGNONS;
                SignonReply();
            }

            if ((bits & Protocol.U_MOREBITS) != 0)
            {
                i = Net.Reader.ReadByte();
                bits |= (i << 8);
            }

            int num;

            if ((bits & Protocol.U_LONGENTITY) != 0)
                num = Net.Reader.ReadShort();
            else
                num = Net.Reader.ReadByte();

            entity_t ent = EntityNum(num);
            for (i = 0; i < 16; i++)
                if ((bits & (1 << i)) != 0)
                    _BitCounts[i]++;

            bool forcelink = false;
            if (ent.msgtime != cl.mtime[1])
                forcelink = true;	// no previous frame to lerp from

            ent.msgtime = cl.mtime[0];
            int modnum;
            if ((bits & Protocol.U_MODEL) != 0)
            {
                modnum = Net.Reader.ReadByte();
                if (modnum >= QDef.MAX_MODELS)
                    Host.Error("CL_ParseModel: bad modnum");
            }
            else
                modnum = ent.baseline.modelindex;

            model_t model = cl.model_precache[modnum];
            if (model != ent.model)
            {
                ent.model = model;
                // automatic animation (torches, etc) can be either all together
                // or randomized
                if (model != null)
                {
                    if (model.synctype == synctype_t.ST_RAND)
                        ent.syncbase = (float)(Sys.Random() & 0x7fff) / 0x7fff;
                    else
                        ent.syncbase = 0;
                }
                else
                    forcelink = true;	// hack to make null model players work

                if (num > 0 && num <= cl.maxclients)
                    Render.TranslatePlayerSkin(num - 1);
            }

            if ((bits & Protocol.U_FRAME) != 0)
                ent.frame = Net.Reader.ReadByte();
            else
                ent.frame = ent.baseline.frame;

            if ((bits & Protocol.U_COLORMAP) != 0)
                i = Net.Reader.ReadByte();
            else
                i = ent.baseline.colormap;
            if (i == 0)
                ent.colormap = Scr.vid.colormap;
            else
            {
                if (i > cl.maxclients)
                    Sys.Error("i >= cl.maxclients");
                ent.colormap = cl.scores[i - 1].translations;
            }

            int skin;
            if ((bits & Protocol.U_SKIN) != 0)
                skin = Net.Reader.ReadByte();
            else
                skin = ent.baseline.skin;
            if (skin != ent.skinnum)
            {
                ent.skinnum = skin;
                if (num > 0 && num <= cl.maxclients)
                    Render.TranslatePlayerSkin(num - 1);
            }

            if ((bits & Protocol.U_EFFECTS) != 0)
                ent.effects = Net.Reader.ReadByte();
            else
                ent.effects = ent.baseline.effects;

            // shift the known values for interpolation
            ent.msg_origins[1] = ent.msg_origins[0];
            ent.msg_angles[1] = ent.msg_angles[0];

            if ((bits & Protocol.U_ORIGIN1) != 0)
                ent.msg_origins[0].X = Net.Reader.ReadCoord();
            else
                ent.msg_origins[0].X = ent.baseline.origin.x;
            if ((bits & Protocol.U_ANGLE1) != 0)
                ent.msg_angles[0].X = Net.Reader.ReadAngle();
            else
                ent.msg_angles[0].X = ent.baseline.angles.x;

            if ((bits & Protocol.U_ORIGIN2) != 0)
                ent.msg_origins[0].Y = Net.Reader.ReadCoord();
            else
                ent.msg_origins[0].Y = ent.baseline.origin.y;
            if ((bits & Protocol.U_ANGLE2) != 0)
                ent.msg_angles[0].Y = Net.Reader.ReadAngle();
            else
                ent.msg_angles[0].Y = ent.baseline.angles.y;

            if ((bits & Protocol.U_ORIGIN3) != 0)
                ent.msg_origins[0].Z = Net.Reader.ReadCoord();
            else
                ent.msg_origins[0].Z = ent.baseline.origin.z;
            if ((bits & Protocol.U_ANGLE3) != 0)
                ent.msg_angles[0].Z = Net.Reader.ReadAngle();
            else
                ent.msg_angles[0].Z = ent.baseline.angles.z;

            if ((bits & Protocol.U_NOLERP) != 0)
                ent.forcelink = true;

            if (forcelink)
            {	// didn't have an update last message
                ent.msg_origins[1] = ent.msg_origins[0];
                ent.origin = ent.msg_origins[0];
                ent.msg_angles[1] = ent.msg_angles[0];
                ent.angles = ent.msg_angles[0];
                ent.forcelink = true;
            }
        }

        
        /// <summary>
        /// CL_ParseClientdata
        /// Server information pertaining to this client only
        /// </summary>
        static void ParseClientData(int bits)
        {
            if ((bits & Protocol.SU_VIEWHEIGHT) != 0)
                cl.viewheight = Net.Reader.ReadChar();
            else
                cl.viewheight = Protocol.DEFAULT_VIEWHEIGHT;

            if ((bits & Protocol.SU_IDEALPITCH) != 0)
                cl.idealpitch = Net.Reader.ReadChar();
            else
                cl.idealpitch = 0;

            cl.mvelocity[1] = cl.mvelocity[0];
            for (int i = 0; i < 3; i++)
            {
                if ((bits & (Protocol.SU_PUNCH1 << i)) != 0)
                    Mathlib.SetComp(ref cl.punchangle, i, Net.Reader.ReadChar());
                else
                    Mathlib.SetComp(ref cl.punchangle, i, 0);
                if ((bits & (Protocol.SU_VELOCITY1 << i)) != 0)
                    Mathlib.SetComp(ref cl.mvelocity[0], i, Net.Reader.ReadChar() * 16);
                else
                    Mathlib.SetComp(ref cl.mvelocity[0], i, 0);
            }

            // [always sent]	if (bits & SU_ITEMS)
            int i2 = Net.Reader.ReadLong();

            if (cl.items != i2)
            {	// set flash times
                Sbar.Changed();
                for (int j = 0; j < 32; j++)
                    if ((i2 & (1 << j)) != 0 && (cl.items & (1 << j)) == 0)
                        cl.item_gettime[j] = (float)cl.time;
                cl.items = i2;
            }

            cl.onground = (bits & Protocol.SU_ONGROUND) != 0;
            cl.inwater = (bits & Protocol.SU_INWATER) != 0;

            if ((bits & Protocol.SU_WEAPONFRAME) != 0)
                cl.stats[QStats.STAT_WEAPONFRAME] = Net.Reader.ReadByte();
            else
                cl.stats[QStats.STAT_WEAPONFRAME] = 0;

            if ((bits & Protocol.SU_ARMOR) != 0)
                i2 = Net.Reader.ReadByte();
            else
                i2 = 0;
            if (cl.stats[QStats.STAT_ARMOR] != i2)
            {
                cl.stats[QStats.STAT_ARMOR] = i2;
                Sbar.Changed();
            }

            if ((bits & Protocol.SU_WEAPON) != 0)
                i2 = Net.Reader.ReadByte();
            else
                i2 = 0;
            if (cl.stats[QStats.STAT_WEAPON] != i2)
            {
                cl.stats[QStats.STAT_WEAPON] = i2;
                Sbar.Changed();
            }

            i2 = Net.Reader.ReadShort();
            if (cl.stats[QStats.STAT_HEALTH] != i2)
            {
                cl.stats[QStats.STAT_HEALTH] = i2;
                Sbar.Changed();
            }

            i2 = Net.Reader.ReadByte();
            if (cl.stats[QStats.STAT_AMMO] != i2)
            {
                cl.stats[QStats.STAT_AMMO] = i2;
                Sbar.Changed();
            }

            for (i2 = 0; i2 < 4; i2++)
            {
                int j = Net.Reader.ReadByte();
                if (cl.stats[QStats.STAT_SHELLS + i2] != j)
                {
                    cl.stats[QStats.STAT_SHELLS + i2] = j;
                    Sbar.Changed();
                }
            }

            i2 = Net.Reader.ReadByte();

            if (Common.GameKind == GameKind.StandardQuake)
            {
                if (cl.stats[QStats.STAT_ACTIVEWEAPON] != i2)
                {
                    cl.stats[QStats.STAT_ACTIVEWEAPON] = i2;
                    Sbar.Changed();
                }
            }
            else
            {
                if (cl.stats[QStats.STAT_ACTIVEWEAPON] != (1 << i2))
                {
                    cl.stats[QStats.STAT_ACTIVEWEAPON] = (1 << i2);
                    Sbar.Changed();
                }
            }
        }


        /// <summary>
        /// CL_ParseServerInfo
        /// </summary>
        static void ParseServerInfo()
        {
            Con.DPrint("Serverinfo packet received.\n");

            //
            // wipe the client_state_t struct
            //
            ClearState();

            // parse protocol version number
            int i = Net.Reader.ReadLong();
            if (i != Protocol.PROTOCOL_VERSION)
            {
                Con.Print("Server returned version {0}, not {1}", i, Protocol.PROTOCOL_VERSION);
                return;
            }

            // parse maxclients
            cl.maxclients = Net.Reader.ReadByte();
            if (cl.maxclients < 1 || cl.maxclients > QDef.MAX_SCOREBOARD)
            {
                Con.Print("Bad maxclients ({0}) from server\n", cl.maxclients);
                return;
            }
            cl.scores = new scoreboard_t[cl.maxclients];// Hunk_AllocName (cl.maxclients*sizeof(*cl.scores), "scores");
            for (i = 0; i < cl.scores.Length; i++)
                cl.scores[i] = new scoreboard_t();

            // parse gametype
            cl.gametype = Net.Reader.ReadByte();

            // parse signon message
            string str = Net.Reader.ReadString();
            cl.levelname = Common.Copy(str, 40);

            // seperate the printfs so the server message can have a color
            Con.Print(ConsoleBar);
            Con.Print("{0}{1}\n", (char)2, str);

            //
            // first we go through and touch all of the precache data that still
            // happens to be in the cache, so precaching something else doesn't
            // needlessly purge it
            //

            // precache models
            Array.Clear(cl.model_precache, 0, cl.model_precache.Length);
            int nummodels;
            string[] model_precache = new string[QDef.MAX_MODELS];
            for (nummodels = 1; ; nummodels++)
            {
                str = Net.Reader.ReadString();
                if (String.IsNullOrEmpty(str))
                    break;

                if (nummodels == QDef.MAX_MODELS)
                {
                    Con.Print("Server sent too many model precaches\n");
                    return;
                }
                model_precache[nummodels] = str;
                Mod.TouchModel(str);
            }

            // precache sounds
            Array.Clear(cl.sound_precache, 0, cl.sound_precache.Length);
            int numsounds;
            string[] sound_precache = new string[QDef.MAX_SOUNDS];
            for (numsounds = 1; ; numsounds++)
            {
                str = Net.Reader.ReadString();
                if (String.IsNullOrEmpty(str))
                    break;
                if (numsounds == QDef.MAX_SOUNDS)
                {
                    Con.Print("Server sent too many sound precaches\n");
                    return;
                }
                sound_precache[numsounds] = str;
                Sound.TouchSound(str);
            }

            //
            // now we try to load everything else until a cache allocation fails
            //
            for (i = 1; i < nummodels; i++)
            {
                cl.model_precache[i] = Mod.ForName(model_precache[i], false);
                if (cl.model_precache[i] == null)
                {
                    Con.Print("Model {0} not found\n", model_precache[i]);
                    return;
                }
                KeepaliveMessage();
            }

            Sound.BeginPrecaching();
            for (i = 1; i < numsounds; i++)
            {
                cl.sound_precache[i] = Sound.PrecacheSound(sound_precache[i]);
                KeepaliveMessage();
            }
            Sound.EndPrecaching();

            // local state
            _Entities[0].model = cl.worldmodel = cl.model_precache[1];

            Render.NewMap();

            Host.NoClipAngleHack = false; // noclip is turned off at start	

            GC.Collect();
        }

        // CL_ParseStartSoundPacket
        static void ParseStartSoundPacket()
        {
            int field_mask = Net.Reader.ReadByte();
            int volume;
            float attenuation;

            if ((field_mask & Protocol.SND_VOLUME) != 0)
                volume = Net.Reader.ReadByte();
            else
                volume = Sound.DEFAULT_SOUND_PACKET_VOLUME;

            if ((field_mask & Protocol.SND_ATTENUATION) != 0)
                attenuation = Net.Reader.ReadByte() / 64.0f;
            else
                attenuation = Sound.DEFAULT_SOUND_PACKET_ATTENUATION;

            int channel = Net.Reader.ReadShort();
            int sound_num = Net.Reader.ReadByte();

            int ent = channel >> 3;
            channel &= 7;

            if (ent > QDef.MAX_EDICTS)
                Host.Error("CL_ParseStartSoundPacket: ent = {0}", ent);

            Vector3 pos = Net.Reader.ReadCoords();
            Sound.StartSound(ent, channel, cl.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation);
        }


        // CL_NewTranslation
        static void NewTranslation(int slot)
        {
            if (slot > cl.maxclients)
                Sys.Error("CL_NewTranslation: slot > cl.maxclients");

            byte[] dest = cl.scores[slot].translations;
            byte[] source = Scr.vid.colormap;
            Array.Copy(source, dest, dest.Length);

            int top = cl.scores[slot].colors & 0xf0;
            int bottom = (cl.scores[slot].colors & 15) << 4;

            Render.TranslatePlayerSkin(slot);

            for (int i = 0, offset = 0; i < Vid.VID_GRADES; i++)//, dest += 256, source+=256)
            {
                if (top < 128)	// the artists made some backwards ranges.  sigh.
                    Buffer.BlockCopy(source, offset + top, dest, offset + Render.TOP_RANGE, 16);  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
                else
                    for (int j = 0; j < 16; j++)
                        dest[offset + Render.TOP_RANGE + j] = source[offset + top + 15 - j];

                if (bottom < 128)
                    Buffer.BlockCopy(source, offset + bottom, dest, offset + Render.BOTTOM_RANGE, 16); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
                else
                    for (int j = 0; j < 16; j++)
                        dest[offset + Render.BOTTOM_RANGE + j] = source[offset + bottom + 15 - j];

                offset += 256;
            }
        }

        
        /// <summary>
        /// CL_EntityNum
        /// 
        /// This error checks and tracks the total number of entities
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        static entity_t EntityNum(int num)
        {
            if (num >= cl.num_entities)
            {
                if (num >= QDef.MAX_EDICTS)
                    Host.Error("CL_EntityNum: %i is an invalid number", num);
                while (cl.num_entities <= num)
                {
                    _Entities[cl.num_entities].colormap = Scr.vid.colormap;
                    cl.num_entities++;
                }
            }

            return _Entities[num];
        }


        /// <summary>
        /// CL_ParseBaseline
        /// </summary>
        /// <param name="ent"></param>
        static void ParseBaseline(entity_t ent)
        {
            ent.baseline.modelindex = Net.Reader.ReadByte();
            ent.baseline.frame = Net.Reader.ReadByte();
            ent.baseline.colormap = Net.Reader.ReadByte();
            ent.baseline.skin = Net.Reader.ReadByte();
            ent.baseline.origin.x = Net.Reader.ReadCoord();
            ent.baseline.angles.x = Net.Reader.ReadAngle();
            ent.baseline.origin.y = Net.Reader.ReadCoord();
            ent.baseline.angles.y = Net.Reader.ReadAngle();
            ent.baseline.origin.z = Net.Reader.ReadCoord();
            ent.baseline.angles.z = Net.Reader.ReadAngle();
        }

        /// <summary>
        /// CL_ParseStatic
        /// </summary>
        static void ParseStatic()
        {
            int i = cl.num_statics;
            if (i >= MAX_STATIC_ENTITIES)
                Host.Error("Too many static entities");

            entity_t ent = _StaticEntities[i];
            cl.num_statics++;
            ParseBaseline(ent);

            // copy it to the current state
            ent.model = cl.model_precache[ent.baseline.modelindex];
            ent.frame = ent.baseline.frame;
            ent.colormap = Scr.vid.colormap;
            ent.skinnum = ent.baseline.skin;
            ent.effects = ent.baseline.effects;
            ent.origin = Common.ToVector(ref ent.baseline.origin);
            ent.angles = Common.ToVector(ref ent.baseline.angles);
            Render.AddEfrags(ent);
        }

        /// <summary>
        /// CL_ParseStaticSound 
        /// </summary>
        static void ParseStaticSound()
        {
            Vector3 org = Net.Reader.ReadCoords();
            int sound_num = Net.Reader.ReadByte();
            int vol = Net.Reader.ReadByte();
            int atten = Net.Reader.ReadByte();

            Sound.StaticSound(cl.sound_precache[sound_num], ref org, vol, atten);
        }

        /// <summary>
        /// CL_KeepaliveMessage
        /// When the client is taking a long time to load stuff, send keepalive messages
        /// so the server doesn't disconnect.
        /// </summary>
        static void KeepaliveMessage()
        {
            if (Server.IsActive)
                return;	// no need if server is local
            if (cls.demoplayback)
                return;

            // read messages from server, should just be nops
            Net.Message.SaveState(ref _MsgState);

            int ret;
            do
            {
                ret = GetMessage();
                switch (ret)
                {
                    default:
                        Host.Error("CL_KeepaliveMessage: CL_GetMessage failed");
                        break;
                    
                    case 0:
                        break;	// nothing waiting
                    
                    case 1:
                        Host.Error("CL_KeepaliveMessage: received a message");
                        break;
                    
                    case 2:
                        if (Net.Reader.ReadByte() != Protocol.svc_nop)
                            Host.Error("CL_KeepaliveMessage: datagram wasn't a nop");
                        break;
                }
            } while (ret != 0);

            Net.Message.RestoreState(_MsgState);

            // check time
            float time = (float)Sys.GetFloatTime();
            if (time - _LastMsg < 5)
                return;
            
            _LastMsg = time;

            // write out a nop
            Con.Print("--> client to server keepalive\n");

            cls.message.WriteByte(Protocol.clc_nop);
            Net.SendMessage(cls.netcon, cls.message);
            cls.message.Clear();
        }

    }
}
