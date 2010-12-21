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
    partial class Server
    {
        static int _FatBytes; // fatbytes
        static byte[] _FatPvs = new byte[BspFile.MAX_MAP_LEAFS/8]; // fatpvs

        
        // SV_Init
        public static void Init()
        {
            for (int i = 0; i < _BoxClipNodes.Length; i++)
            {
                _BoxClipNodes[i].children = new short[2];
            }
            for (int i = 0; i < _BoxPlanes.Length; i++)
            {
                _BoxPlanes[i] = new mplane_t();
            }
            for (int i = 0; i < _AreaNodes.Length; i++)
            {
                _AreaNodes[i] = new areanode_t();
            }
            
            if (_Friction == null)
            {
                _Friction = new Cvar("sv_friction", "4", false, true);
                _EdgeFriction = new Cvar("edgefriction", "2");
                _StopSpeed = new Cvar("sv_stopspeed", "100");
                _Gravity = new Cvar("sv_gravity", "800", false, true);
                _MaxVelocity = new Cvar("sv_maxvelocity", "2000");
                _NoStep = new Cvar("sv_nostep", "0");
                _MaxSpeed = new Cvar("sv_maxspeed", "320", false, true);
                _Accelerate = new Cvar("sv_accelerate", "10");
                _Aim = new Cvar("sv_aim", "0.93");
                _IdealPitchScale = new Cvar("sv_idealpitchscale", "0.8");
            }

            for (int i = 0; i < QDef.MAX_MODELS; i++)
                _LocalModels[i] = "*" + i.ToString();
        }

        /// <summary>
        /// SV_StartParticle
        /// Make sure the event gets sent to all clients
        /// </summary>
        public static void StartParticle(ref Vector3 org, ref Vector3 dir, int color, int count)
        {
            if (sv.datagram.Length > QDef.MAX_DATAGRAM - 16)
                return;

            sv.datagram.WriteByte(Protocol.svc_particle);
            sv.datagram.WriteCoord(org.X);
            sv.datagram.WriteCoord(org.Y);
            sv.datagram.WriteCoord(org.Z);

            Vector3 max = Vector3.One * 127;
            Vector3 min = Vector3.One * -128;
            Vector3 v = Vector3.Clamp(dir * 16, min, max);
            sv.datagram.WriteChar((int)v.X);
            sv.datagram.WriteChar((int)v.Y);
            sv.datagram.WriteChar((int)v.Z);
            sv.datagram.WriteByte(count);
            sv.datagram.WriteByte(color);

        }

        /// <summary>
        /// SV_StartSound
        /// Each entity can have eight independant sound sources, like voice,
        /// weapon, feet, etc.
        ///
        /// Channel 0 is an auto-allocate channel, the others override anything
        /// allready running on that entity/channel pair.
        ///
        /// An attenuation of 0 will play full volume everywhere in the level.
        /// Larger attenuations will drop off.  (max 4 attenuation)
        /// </summary>
        public static void StartSound(edict_t entity, int channel, string sample, int volume, float attenuation)
        {
            if (volume < 0 || volume > 255)
                Sys.Error("SV_StartSound: volume = {0}", volume);

            if (attenuation < 0 || attenuation > 4)
                Sys.Error("SV_StartSound: attenuation = {0}", attenuation);

            if (channel < 0 || channel > 7)
                Sys.Error("SV_StartSound: channel = {0}", channel);

            if (sv.datagram.Length > QDef.MAX_DATAGRAM - 16)
                return;

            // find precache number for sound
            int sound_num;
            for (sound_num = 1; sound_num < QDef.MAX_SOUNDS && sv.sound_precache[sound_num] != null; sound_num++)
                if (sample == sv.sound_precache[sound_num])
                    break;

            if (sound_num == QDef.MAX_SOUNDS || String.IsNullOrEmpty(sv.sound_precache[sound_num]))
            {
                Con.Print("SV_StartSound: {0} not precacheed\n", sample);
                return;
            }

            int ent = NumForEdict(entity);

            channel = (ent << 3) | channel;

            int field_mask = 0;
            if (volume != Sound.DEFAULT_SOUND_PACKET_VOLUME)
                field_mask |= Protocol.SND_VOLUME;
            if (attenuation != Sound.DEFAULT_SOUND_PACKET_ATTENUATION)
                field_mask |= Protocol.SND_ATTENUATION;

            // directed messages go only to the entity the are targeted on
            sv.datagram.WriteByte(Protocol.svc_sound);
            sv.datagram.WriteByte(field_mask);
            if ((field_mask & Protocol.SND_VOLUME) != 0)
                sv.datagram.WriteByte(volume);
            if ((field_mask & Protocol.SND_ATTENUATION) != 0)
                sv.datagram.WriteByte((int)(attenuation * 64));
            sv.datagram.WriteShort(channel);
            sv.datagram.WriteByte(sound_num);
            v3f v;
            Mathlib.VectorAdd(ref entity.v.mins, ref entity.v.maxs, out v);
            Mathlib.VectorMA(ref entity.v.origin, 0.5f, ref v, out v);
            sv.datagram.WriteCoord(v.x);
            sv.datagram.WriteCoord(v.y);
            sv.datagram.WriteCoord(v.z);
        }

        /// <summary>
        /// SV_DropClient
        /// Called when the player is getting totally kicked off the host
        /// if (crash = true), don't bother sending signofs
        /// </summary>
        public static void DropClient(bool crash)
        {
            client_t client = Host.HostClient;

            if (!crash)
            {
                // send any final messages (don't check for errors)
                if (Net.CanSendMessage(client.netconnection))
                {
                    MsgWriter msg = client.message;
                    msg.WriteByte(Protocol.svc_disconnect);
                    Net.SendMessage(client.netconnection, msg);
                }

                if (client.edict != null && client.spawned)
                {
                    // call the prog function for removing a client
                    // this will set the body to a dead frame, among other things
                    int saveSelf = Progs.GlobalStruct.self;
                    Progs.GlobalStruct.self = EdictToProg(client.edict);
                    Progs.Execute(Progs.GlobalStruct.ClientDisconnect);
                    Progs.GlobalStruct.self = saveSelf;
                }

                Con.DPrint("Client {0} removed\n", client.name);
            }

            // break the net connection
            Net.Close(client.netconnection);
            client.netconnection = null;

            // free the client (the body stays around)
            client.active = false;
            client.name = null;
            client.old_frags = -999999;
            Net.ActiveConnections--;

            // send notification to all clients
            for (int i = 0; i < Server.svs.maxclients; i++)
            {
                client_t cl = Server.svs.clients[i];
                if (!cl.active)
                    continue;

                cl.message.WriteByte(Protocol.svc_updatename);
                cl.message.WriteByte(Host.ClientNum);
                cl.message.WriteString("");
                cl.message.WriteByte(Protocol.svc_updatefrags);
                cl.message.WriteByte(Host.ClientNum);
                cl.message.WriteShort(0);
                cl.message.WriteByte(Protocol.svc_updatecolors);
                cl.message.WriteByte(Host.ClientNum);
                cl.message.WriteByte(0);
            }
        }

        /// <summary>
        /// SV_SendClientMessages
        /// </summary>
        public static void SendClientMessages()
        {
            // update frags, names, etc
            UpdateToReliableMessages();

            // build individual updates
            for (int i = 0; i < svs.maxclients; i++)
            {
                Host.HostClient = svs.clients[i];

                if (!Host.HostClient.active)
                    continue;

                if (Host.HostClient.spawned)
                {
                    if (!SendClientDatagram(Host.HostClient))
                        continue;
                }
                else
                {
                    // the player isn't totally in the game yet
                    // send small keepalive messages if too much time has passed
                    // send a full message when the next signon stage has been requested
                    // some other message data (name changes, etc) may accumulate 
                    // between signon stages
                    if (!Host.HostClient.sendsignon)
                    {
                        if (Host.RealTime - Host.HostClient.last_message > 5)
                            SendNop(Host.HostClient);
                        continue;	// don't send out non-signon messages
                    }
                }

                // check for an overflowed message.  Should only happen
                // on a very fucked up connection that backs up a lot, then
                // changes level
                if (Host.HostClient.message.IsOveflowed)
                {
                    DropClient(true);
                    Host.HostClient.message.IsOveflowed = false;
                    continue;
                }

                if (Host.HostClient.message.Length > 0 || Host.HostClient.dropasap)
                {
                    if (!Net.CanSendMessage(Host.HostClient.netconnection))
                        continue;

                    if (Host.HostClient.dropasap)
                        DropClient(false);	// went to another level
                    else
                    {
                        if (Net.SendMessage(Host.HostClient.netconnection, Host.HostClient.message) == -1)
                            DropClient(true);	// if the message couldn't send, kick off
                        Host.HostClient.message.Clear();
                        Host.HostClient.last_message = Host.RealTime;
                        Host.HostClient.sendsignon = false;
                    }
                }
            }

            // clear muzzle flashes
            CleanupEnts();
        }

        /// <summary>
        /// SV_CleanupEnts
        /// </summary>
        static void CleanupEnts()
        {
            for (int i = 1; i < sv.num_edicts; i++)
            {
                edict_t ent = sv.edicts[i];
                ent.v.effects = (int)ent.v.effects & ~EntityEffects.EF_MUZZLEFLASH;
            }
        }

        /// <summary>
        /// SV_SendNop
        /// Send a nop message without trashing or sending the accumulated client
        /// message buffer
        /// </summary>
        static void SendNop(client_t client)
        {
            MsgWriter msg = new MsgWriter(4);
            msg.WriteChar(Protocol.svc_nop);

            if (Net.SendUnreliableMessage(client.netconnection, msg) == -1)
                DropClient(true);	// if the message couldn't send, kick off
            client.last_message = Host.RealTime;
        }


        /// <summary>
        /// SV_SendClientDatagram
        /// </summary>
        static bool SendClientDatagram(client_t client)
        {
            MsgWriter msg = new MsgWriter(QDef.MAX_DATAGRAM); // Uze todo: make static?

            msg.WriteByte(Protocol.svc_time);
            msg.WriteFloat((float)sv.time);

            // add the client specific data to the datagram
            WriteClientDataToMessage(client.edict, msg);

            WriteEntitiesToClient(client.edict, msg);

            // copy the server datagram if there is space
            if (msg.Length + sv.datagram.Length < msg.Capacity)
                msg.Write(sv.datagram.Data, 0, sv.datagram.Length);

            // send the datagram
            if (Net.SendUnreliableMessage(client.netconnection, msg) == -1)
            {
                DropClient(true);// if the message couldn't send, kick off
                return false;
            }

            return true;
        }

        /// <summary>
        /// SV_WriteEntitiesToClient
        /// </summary>
        static void WriteEntitiesToClient(edict_t clent, MsgWriter msg)
        {
            // find the client's PVS
            Vector3 org = Common.ToVector(ref clent.v.origin) + Common.ToVector(ref clent.v.view_ofs);
            byte[] pvs = FatPVS(ref org);

            // send over all entities (except the client) that touch the pvs
            for (int e = 1; e < sv.num_edicts; e++)
            {
                edict_t ent = sv.edicts[e];
                // ignore if not touching a PV leaf
                if (ent != clent)	// clent is ALLWAYS sent
                {
                    // ignore ents without visible models
                    string mname = Progs.GetString(ent.v.model);
                    if (String.IsNullOrEmpty(mname))
                        continue;

                    int i;
                    for (i = 0; i < ent.num_leafs; i++)
                        if ((pvs[ent.leafnums[i] >> 3] & (1 << (ent.leafnums[i] & 7))) != 0)
                            break;

                    if (i == ent.num_leafs)
                        continue;		// not visible
                }

                if (msg.Capacity - msg.Length < 16)
                {
                    Con.Print("packet overflow\n");
                    return;
                }

                // send an update
                int bits = 0;
                v3f miss;
                Mathlib.VectorSubtract(ref ent.v.origin, ref ent.baseline.origin, out miss);
                if (miss.x < -0.1f || miss.x > 0.1f) bits |= Protocol.U_ORIGIN1;
                if (miss.y < -0.1f || miss.y > 0.1f) bits |= Protocol.U_ORIGIN2;
                if (miss.z < -0.1f || miss.z > 0.1f) bits |= Protocol.U_ORIGIN3;

                if (ent.v.angles.x != ent.baseline.angles.x)
                    bits |= Protocol.U_ANGLE1;

                if (ent.v.angles.y != ent.baseline.angles.y)
                    bits |= Protocol.U_ANGLE2;

                if (ent.v.angles.z != ent.baseline.angles.z)
                    bits |= Protocol.U_ANGLE3;

                if (ent.v.movetype == Movetypes.MOVETYPE_STEP)
                    bits |= Protocol.U_NOLERP;	// don't mess up the step animation

                if (ent.baseline.colormap != ent.v.colormap)
                    bits |= Protocol.U_COLORMAP;

                if (ent.baseline.skin != ent.v.skin)
                    bits |= Protocol.U_SKIN;

                if (ent.baseline.frame != ent.v.frame)
                    bits |= Protocol.U_FRAME;

                if (ent.baseline.effects != ent.v.effects)
                    bits |= Protocol.U_EFFECTS;

                if (ent.baseline.modelindex != ent.v.modelindex)
                    bits |= Protocol.U_MODEL;

                if (e >= 256)
                    bits |= Protocol.U_LONGENTITY;

                if (bits >= 256)
                    bits |= Protocol.U_MOREBITS;

                //
                // write the message
                //
                msg.WriteByte(bits | Protocol.U_SIGNAL);

                if ((bits & Protocol.U_MOREBITS) != 0)
                    msg.WriteByte(bits >> 8);
                if ((bits & Protocol.U_LONGENTITY) != 0)
                    msg.WriteShort(e);
                else
                    msg.WriteByte(e);

                if ((bits & Protocol.U_MODEL) != 0)
                    msg.WriteByte((int)ent.v.modelindex);
                if ((bits & Protocol.U_FRAME) != 0)
                    msg.WriteByte((int)ent.v.frame);
                if ((bits & Protocol.U_COLORMAP) != 0)
                    msg.WriteByte((int)ent.v.colormap);
                if ((bits & Protocol.U_SKIN) != 0)
                    msg.WriteByte((int)ent.v.skin);
                if ((bits & Protocol.U_EFFECTS) != 0)
                    msg.WriteByte((int)ent.v.effects);
                if ((bits & Protocol.U_ORIGIN1) != 0)
                    msg.WriteCoord(ent.v.origin.x);
                if ((bits & Protocol.U_ANGLE1) != 0)
                    msg.WriteAngle(ent.v.angles.x);
                if ((bits & Protocol.U_ORIGIN2) != 0)
                    msg.WriteCoord(ent.v.origin.y);
                if ((bits & Protocol.U_ANGLE2) != 0)
                    msg.WriteAngle(ent.v.angles.y);
                if ((bits & Protocol.U_ORIGIN3) != 0)
                    msg.WriteCoord(ent.v.origin.z);
                if ((bits & Protocol.U_ANGLE3) != 0)
                    msg.WriteAngle(ent.v.angles.z);
            }
        }

        /// <summary>
        /// SV_FatPVS
        /// Calculates a PVS that is the inclusive or of all leafs within 8 pixels of the
        /// given point.
        /// </summary>
        static byte[] FatPVS(ref Vector3 org)
        {
            _FatBytes = (sv.worldmodel.numleafs + 31) >> 3;
            Array.Clear(_FatPvs, 0, _FatPvs.Length);
            AddToFatPVS(ref org, sv.worldmodel.nodes[0]);
            return _FatPvs;
        }

        /// <summary>
        /// SV_AddToFatPVS
        /// The PVS must include a small area around the client to allow head bobbing
        /// or other small motion on the client side.  Otherwise, a bob might cause an
        /// entity that should be visible to not show up, especially when the bob
        /// crosses a waterline.
        /// </summary>
        static void AddToFatPVS(ref Vector3 org, mnodebase_t node)
        {
            while (true)
            {
                // if this is a leaf, accumulate the pvs bits
                if (node.contents < 0)
                {
                    if (node.contents != Contents.CONTENTS_SOLID)
                    {
                        byte[] pvs = Mod.LeafPVS((mleaf_t)node, sv.worldmodel);
                        for (int i = 0; i < _FatBytes; i++)
                            _FatPvs[i] |= pvs[i];
                    }
                    return;
                }

                mnode_t n = (mnode_t)node;
                mplane_t plane = n.plane;
                float d = Vector3.Dot(org, plane.normal) - plane.dist;
                if (d > 8)
                    node = n.children[0];
                else if (d < -8)
                    node = n.children[1];
                else
                {	// go down both
                    AddToFatPVS(ref org, n.children[0]);
                    node = n.children[1];
                }
            }
        }

        /// <summary>
        /// SV_UpdateToReliableMessages
        /// </summary>
        static void UpdateToReliableMessages()
        {
            // check for changes to be sent over the reliable streams
            for (int i = 0; i < svs.maxclients; i++)
            {
                Host.HostClient = svs.clients[i];
                if (Host.HostClient.old_frags != Host.HostClient.edict.v.frags)
                {
                    for (int j = 0; j < svs.maxclients; j++)
                    {
                        client_t client = svs.clients[j];
                        if (!client.active)
                            continue;

                        client.message.WriteByte(Protocol.svc_updatefrags);
                        client.message.WriteByte(i);
                        client.message.WriteShort((int)Host.HostClient.edict.v.frags);
                    }

                    Host.HostClient.old_frags = (int)Host.HostClient.edict.v.frags;
                }
            }

            for (int j = 0; j < svs.maxclients; j++)
            {
                client_t client = svs.clients[j];
                if (!client.active)
                    continue;
                client.message.Write(sv.reliable_datagram.Data, 0, sv.reliable_datagram.Length);
            }

            sv.reliable_datagram.Clear();
        }

        /// <summary>
        /// SV_ClearDatagram
        /// </summary>
        public static void ClearDatagram()
        {
            sv.datagram.Clear();
        }

        /// <summary>
        /// SV_ModelIndex
        /// </summary>
        public static int ModelIndex(string name)
        {
            if (String.IsNullOrEmpty(name))
                return 0;

            int i;
            for (i = 0; i < QDef.MAX_MODELS && sv.model_precache[i] != null; i++)
                if (sv.model_precache[i] == name)
                    return i;

            if (i == QDef.MAX_MODELS || String.IsNullOrEmpty(sv.model_precache[i]))
                Sys.Error("SV_ModelIndex: model {0} not precached", name);
            return i;
        }

        /// <summary>
        /// SV_ClientPrintf
        /// Sends text across to be displayed 
        /// FIXME: make this just a stuffed echo?
        /// </summary>
        public static void ClientPrint(string fmt, params object[] args)
        {
            string tmp = String.Format(fmt, args);
            Host.HostClient.message.WriteByte(Protocol.svc_print);
            Host.HostClient.message.WriteString(tmp);
        }

        /// <summary>
        /// SV_BroadcastPrint
        /// </summary>
        public static void BroadcastPrint(string fmt, params object[] args)
        {
            string tmp = args.Length > 0 ? String.Format(fmt, args) : fmt;
            for (int i = 0; i < svs.maxclients; i++)
                if (svs.clients[i].active && svs.clients[i].spawned)
                {
                    MsgWriter msg = svs.clients[i].message;
                    msg.WriteByte(Protocol.svc_print);
                    msg.WriteString(tmp);
                }
        }

        /// <summary>
        /// SV_WriteClientdataToMessage
        /// </summary>
        public static void WriteClientDataToMessage(edict_t ent, MsgWriter msg)
        {
            //
            // send a damage message
            //
            if (ent.v.dmg_take != 0 || ent.v.dmg_save != 0)
            {
                edict_t other = ProgToEdict(ent.v.dmg_inflictor);
                msg.WriteByte(Protocol.svc_damage);
                msg.WriteByte((int)ent.v.dmg_save);
                msg.WriteByte((int)ent.v.dmg_take);
                msg.WriteCoord(other.v.origin.x + 0.5f * (other.v.mins.x + other.v.maxs.x));
                msg.WriteCoord(other.v.origin.y + 0.5f * (other.v.mins.y + other.v.maxs.y));
                msg.WriteCoord(other.v.origin.z + 0.5f * (other.v.mins.z + other.v.maxs.z));

                ent.v.dmg_take = 0;
                ent.v.dmg_save = 0;
            }

            //
            // send the current viewpos offset from the view entity
            //
            SetIdealPitch();		// how much to look up / down ideally

            // a fixangle might get lost in a dropped packet.  Oh well.
            if (ent.v.fixangle != 0)
            {
                msg.WriteByte(Protocol.svc_setangle);
                msg.WriteAngle(ent.v.angles.x);
                msg.WriteAngle(ent.v.angles.y);
                msg.WriteAngle(ent.v.angles.z);
                ent.v.fixangle = 0;
            }

            int bits = 0;

            if (ent.v.view_ofs.z != Protocol.DEFAULT_VIEWHEIGHT)
                bits |= Protocol.SU_VIEWHEIGHT;

            if (ent.v.idealpitch != 0)
                bits |= Protocol.SU_IDEALPITCH;

            // stuff the sigil bits into the high bits of items for sbar, or else
            // mix in items2
            float val = Progs.GetEdictFieldFloat(ent, "items2", 0);
            int items;
            if (val != 0)
                items = (int)ent.v.items | ((int)val << 23);
            else
                items = (int)ent.v.items | ((int)Progs.GlobalStruct.serverflags << 28);

            bits |= Protocol.SU_ITEMS;

            if (((int)ent.v.flags & EdictFlags.FL_ONGROUND) != 0)
                bits |= Protocol.SU_ONGROUND;

            if (ent.v.waterlevel >= 2)
                bits |= Protocol.SU_INWATER;

            if (ent.v.punchangle.x != 0) bits |= Protocol.SU_PUNCH1;
            if (ent.v.punchangle.y != 0) bits |= Protocol.SU_PUNCH2;
            if (ent.v.punchangle.z != 0) bits |= Protocol.SU_PUNCH3;

            if (ent.v.velocity.x != 0) bits |= Protocol.SU_VELOCITY1;
            if (ent.v.velocity.y != 0) bits |= Protocol.SU_VELOCITY2;
            if (ent.v.velocity.z != 0) bits |= Protocol.SU_VELOCITY3;

            if (ent.v.weaponframe != 0)
                bits |= Protocol.SU_WEAPONFRAME;

            if (ent.v.armorvalue != 0)
                bits |= Protocol.SU_ARMOR;

            //	if (ent.v.weapon)
            bits |= Protocol.SU_WEAPON;

            // send the data

            msg.WriteByte(Protocol.svc_clientdata);
            msg.WriteShort(bits);

            if ((bits & Protocol.SU_VIEWHEIGHT) != 0)
                msg.WriteChar((int)ent.v.view_ofs.z);

            if ((bits & Protocol.SU_IDEALPITCH) != 0)
                msg.WriteChar((int)ent.v.idealpitch);

            if ((bits & Protocol.SU_PUNCH1) != 0) msg.WriteChar((int)ent.v.punchangle.x);
            if ((bits & Protocol.SU_VELOCITY1) != 0) msg.WriteChar((int)(ent.v.velocity.x / 16));

            if ((bits & Protocol.SU_PUNCH2) != 0) msg.WriteChar((int)ent.v.punchangle.y);
            if ((bits & Protocol.SU_VELOCITY2) != 0) msg.WriteChar((int)(ent.v.velocity.y / 16));

            if ((bits & Protocol.SU_PUNCH3) != 0) msg.WriteChar((int)ent.v.punchangle.z);
            if ((bits & Protocol.SU_VELOCITY3) != 0) msg.WriteChar((int)(ent.v.velocity.z / 16));

            // always sent
            msg.WriteLong(items);

            if ((bits & Protocol.SU_WEAPONFRAME) != 0)
                msg.WriteByte((int)ent.v.weaponframe);
            if ((bits & Protocol.SU_ARMOR) != 0)
                msg.WriteByte((int)ent.v.armorvalue);
            if ((bits & Protocol.SU_WEAPON) != 0)
                msg.WriteByte(ModelIndex(Progs.GetString(ent.v.weaponmodel)));

            msg.WriteShort((int)ent.v.health);
            msg.WriteByte((int)ent.v.currentammo);
            msg.WriteByte((int)ent.v.ammo_shells);
            msg.WriteByte((int)ent.v.ammo_nails);
            msg.WriteByte((int)ent.v.ammo_rockets);
            msg.WriteByte((int)ent.v.ammo_cells);

            if (Common.GameKind == GameKind.StandardQuake)
            {
                msg.WriteByte((int)ent.v.weapon);
            }
            else
            {
                for (int i = 0; i < 32; i++)
                {
                    if ((((int)ent.v.weapon) & (1 << i)) != 0)
                    {
                        msg.WriteByte(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// SV_CheckForNewClients
        /// </summary>
        public static void CheckForNewClients()
        {
            //
            // check for new connections
            //
            while (true)
            {
                qsocket_t ret = Net.CheckNewConnections();
                if (ret == null)
                    break;

                // 
                // init a new client structure
                //	
                int i;
                for (i = 0; i < svs.maxclients; i++)
                    if (!svs.clients[i].active)
                        break;
                if (i == svs.maxclients)
                    Sys.Error("Host_CheckForNewClients: no free clients");

                svs.clients[i].netconnection = ret;
                ConnectClient(i);

                Net.ActiveConnections++;
            }
        }

        /// <summary>
        /// SV_ConnectClient
        /// Initializes a client_t for a new net connection.  This will only be called
        /// once for a player each game, not once for each level change.
        /// </summary>
        static void ConnectClient(int clientnum)
        {
            client_t client = svs.clients[clientnum];

            Con.DPrint("Client {0} connected\n", client.netconnection.address);

            int edictnum = clientnum + 1;
            edict_t ent = EdictNum(edictnum);

            // set up the client_t
            qsocket_t netconnection = client.netconnection;

            float[] spawn_parms = new float[NUM_SPAWN_PARMS];
            if (sv.loadgame)
            {
                Array.Copy(client.spawn_parms, spawn_parms, spawn_parms.Length);
            }

            client.Clear();
            client.netconnection = netconnection;
            client.name = "unconnected";
            client.active = true;
            client.spawned = false;
            client.edict = ent;
            client.message.AllowOverflow = true; // we can catch it
            client.privileged = false;

            if (sv.loadgame)
            {
                Array.Copy(spawn_parms, client.spawn_parms, spawn_parms.Length);
            }
            else
            {
                // call the progs to get default spawn parms for the new client
                Progs.Execute(Progs.GlobalStruct.SetNewParms);

                AssignGlobalSpawnparams(client);
            }

            SendServerInfo(client);
        }

        private static void AssignGlobalSpawnparams(client_t client)
        {
            client.spawn_parms[0] = Progs.GlobalStruct.parm1;
            client.spawn_parms[1] = Progs.GlobalStruct.parm2;
            client.spawn_parms[2] = Progs.GlobalStruct.parm3;
            client.spawn_parms[3] = Progs.GlobalStruct.parm4;

            client.spawn_parms[4] = Progs.GlobalStruct.parm5;
            client.spawn_parms[5] = Progs.GlobalStruct.parm6;
            client.spawn_parms[6] = Progs.GlobalStruct.parm7;
            client.spawn_parms[7] = Progs.GlobalStruct.parm8;

            client.spawn_parms[8] = Progs.GlobalStruct.parm9;
            client.spawn_parms[9] = Progs.GlobalStruct.parm10;
            client.spawn_parms[10] = Progs.GlobalStruct.parm11;
            client.spawn_parms[11] = Progs.GlobalStruct.parm12;

            client.spawn_parms[12] = Progs.GlobalStruct.parm13;
            client.spawn_parms[13] = Progs.GlobalStruct.parm14;
            client.spawn_parms[14] = Progs.GlobalStruct.parm15;
            client.spawn_parms[15] = Progs.GlobalStruct.parm16;
        }

        /// <summary>
        /// SV_SaveSpawnparms
        /// Grabs the current state of each client for saving across the
        /// transition to another level
        /// </summary>
        public static void SaveSpawnparms()
        {
            Server.svs.serverflags = (int)Progs.GlobalStruct.serverflags;

            for (int i = 0; i < svs.maxclients; i++)
            {
                Host.HostClient = Server.svs.clients[i];
                if (!Host.HostClient.active)
                    continue;

                // call the progs to get default spawn parms for the new client
                Progs.GlobalStruct.self = EdictToProg(Host.HostClient.edict);
                Progs.Execute(Progs.GlobalStruct.SetChangeParms);
                AssignGlobalSpawnparams(Host.HostClient);
            }
        }

        /// <summary>
        /// SV_SpawnServer
        /// </summary>
        public static void SpawnServer(string server)
        {
            // let's not have any servers with no name
            if (String.IsNullOrEmpty(Net.HostName))
                Cvar.Set("hostname", "UNNAMED");
            
            Scr.CenterTimeOff = 0;

            Con.DPrint("SpawnServer: {0}\n", server);
            svs.changelevel_issued = false;		// now safe to issue another

            //
            // tell all connected clients that we are going to a new level
            //
            if (sv.active)
            {
                SendReconnect();
            }

            //
            // make cvars consistant
            //
            if (Host.IsCoop)
                Cvar.Set("deathmatch", 0);

            Host.CurrentSkill = (int)(Host.Skill + 0.5);
            if (Host.CurrentSkill < 0)
                Host.CurrentSkill = 0;
            if (Host.CurrentSkill > 3)
                Host.CurrentSkill = 3;

            Cvar.Set("skill", (float)Host.CurrentSkill);

            //
            // set up the new server
            //
            Host.ClearMemory();

            sv.Clear();

            sv.name = server;

            // load progs to get entity field count
            Progs.LoadProgs();

            // allocate server memory
            sv.max_edicts = QDef.MAX_EDICTS;

            sv.edicts = new edict_t[sv.max_edicts];
            for (int i = 0; i < sv.edicts.Length; i++)
            {
                sv.edicts[i] = new edict_t();
            }
            
            // leave slots at start for clients only
            sv.num_edicts = svs.maxclients + 1;
            edict_t ent;
            for (int i = 0; i < svs.maxclients; i++)
            {
                ent = EdictNum(i + 1);
                svs.clients[i].edict = ent;
            }

            sv.state = server_state_t.Loading;
            sv.paused = false;
            sv.time = 1.0;
            sv.modelname = String.Format("maps/{0}.bsp", server);
            sv.worldmodel = Mod.ForName(sv.modelname, false);
            if (sv.worldmodel == null)
            {
                Con.Print("Couldn't spawn server {0}\n", sv.modelname);
                sv.active = false;
                return;
            }
            sv.models[1] = sv.worldmodel;

            //
            // clear world interaction links
            //
            ClearWorld();
            
            sv.sound_precache[0] = String.Empty;
            sv.model_precache[0] = String.Empty;
            
            sv.model_precache[1] = sv.modelname;
            for (int i = 1; i < sv.worldmodel.numsubmodels; i++)
            {
                sv.model_precache[1 + i] = _LocalModels[i];
                sv.models[i + 1] = Mod.ForName(_LocalModels[i], false);
            }

            //
            // load the rest of the entities
            //	
            ent = EdictNum(0);
            ent.Clear();
            ent.v.model = Progs.StringOffset(sv.worldmodel.name);
            if (ent.v.model == -1)
            {
                ent.v.model = Progs.NewString(sv.worldmodel.name);
            }
            ent.v.modelindex = 1;		// world model
            ent.v.solid = Solids.SOLID_BSP;
            ent.v.movetype = Movetypes.MOVETYPE_PUSH;

            if (Host.IsCoop)
                Progs.GlobalStruct.coop = 1; //coop.value;
            else
                Progs.GlobalStruct.deathmatch = Host.Deathmatch;

            int offset = Progs.NewString(sv.name);
            Progs.GlobalStruct.mapname = offset;

            // serverflags are for cross level information (sigils)
            Progs.GlobalStruct.serverflags = svs.serverflags;

            Progs.LoadFromFile(sv.worldmodel.entities);

            sv.active = true;

            // all setup is completed, any further precache statements are errors
            sv.state = server_state_t.Active;

            // run two frames to allow everything to settle
            Host.FrameTime = 0.1;
            Physics();
            Physics();

            // create a baseline for more efficient communications
            CreateBaseline();

            // send serverinfo to all connected clients
            for (int i = 0; i < svs.maxclients; i++)
            {
                Host.HostClient = svs.clients[i];
                if (Host.HostClient.active)
                    SendServerInfo(Host.HostClient);
            }

            GC.Collect();
            Con.DPrint("Server spawned.\n");
        }

        /// <summary>
        /// SV_SendServerinfo
        /// Sends the first message from the server to a connected client.
        /// This will be sent on the initial connection and upon each server load.
        /// </summary>
        static void SendServerInfo(client_t client)
        {
            MsgWriter writer = client.message;

            writer.WriteByte(Protocol.svc_print);
            writer.WriteString(String.Format("{0}\nVERSION {1,4:F2} SERVER ({2} CRC)", (char)2, QDef.VERSION, Progs.Crc));

            writer.WriteByte(Protocol.svc_serverinfo);
            writer.WriteLong(Protocol.PROTOCOL_VERSION);
            writer.WriteByte(svs.maxclients);

            if (!Host.IsCoop && Host.Deathmatch != 0)
                writer.WriteByte(Protocol.GAME_DEATHMATCH);
            else
                writer.WriteByte(Protocol.GAME_COOP);

            string message = Progs.GetString(sv.edicts[0].v.message);

            writer.WriteString(message);

            for (int i = 1; i < sv.model_precache.Length; i++)
            {
                string tmp = sv.model_precache[i];
                if (String.IsNullOrEmpty(tmp))
                    break;
                writer.WriteString(tmp);
            }
            writer.WriteByte(0);

            for (int i = 1; i < sv.sound_precache.Length; i++)
            {
                string tmp = sv.sound_precache[i];
                if (tmp == null)
                    break;
                writer.WriteString(tmp);
            }
            writer.WriteByte(0);

            // send music
            writer.WriteByte(Protocol.svc_cdtrack);
            writer.WriteByte((int)sv.edicts[0].v.sounds);
            writer.WriteByte((int)sv.edicts[0].v.sounds);

            // set view	
            writer.WriteByte(Protocol.svc_setview);
            writer.WriteShort(NumForEdict(client.edict));

            writer.WriteByte(Protocol.svc_signonnum);
            writer.WriteByte(1);

            client.sendsignon = true;
            client.spawned = false;		// need prespawn, spawn, etc
        }

        /// <summary>
        /// SV_SendReconnect
        /// Tell all the clients that the server is changing levels
        /// </summary>
        private static void SendReconnect()
        {
            MsgWriter msg = new MsgWriter(128);

            msg.WriteChar(Protocol.svc_stufftext);
            msg.WriteString("reconnect\n");
            Net.SendToAll(msg, 5);

            if (Client.cls.state != cactive_t.ca_dedicated)
                Cmd.ExecuteString("reconnect\n", cmd_source_t.src_command);
        }

        /// <summary>
        /// SV_CreateBaseline
        /// </summary>
        static void CreateBaseline()
        {
            for (int entnum = 0; entnum < sv.num_edicts; entnum++)
            {
                // get the current server version
                edict_t svent = EdictNum(entnum);
                if (svent.free)
                    continue;
                if (entnum > svs.maxclients && svent.v.modelindex == 0)
                    continue;

                //
                // create entity baseline
                //
                svent.baseline.origin = svent.v.origin;
                svent.baseline.angles = svent.v.angles;
                svent.baseline.frame = (int)svent.v.frame;
                svent.baseline.skin = (int)svent.v.skin;
                if (entnum > 0 && entnum <= svs.maxclients)
                {
                    svent.baseline.colormap = entnum;
                    svent.baseline.modelindex = ModelIndex("progs/player.mdl");
                }
                else
                {
                    svent.baseline.colormap = 0;
                    svent.baseline.modelindex = ModelIndex(Progs.GetString(svent.v.model));
                }

                //
                // add to the message
                //
                sv.signon.WriteByte(Protocol.svc_spawnbaseline);
                sv.signon.WriteShort(entnum);

                sv.signon.WriteByte(svent.baseline.modelindex);
                sv.signon.WriteByte(svent.baseline.frame);
                sv.signon.WriteByte(svent.baseline.colormap);
                sv.signon.WriteByte(svent.baseline.skin);

                sv.signon.WriteCoord(svent.baseline.origin.x);
                sv.signon.WriteAngle(svent.baseline.angles.x);
                sv.signon.WriteCoord(svent.baseline.origin.y);
                sv.signon.WriteAngle(svent.baseline.angles.y);
                sv.signon.WriteCoord(svent.baseline.origin.z);
                sv.signon.WriteAngle(svent.baseline.angles.z);
            }
        }
    }
}
