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

// cl_tent.c

namespace SharpQuake
{
    partial class Client
    {
        static int _NumTempEntities; // num_temp_entities
        static entity_t[] _TempEntities = new entity_t[MAX_TEMP_ENTITIES]; // cl_temp_entities[MAX_TEMP_ENTITIES]
        static beam_t[] _Beams = new beam_t[MAX_BEAMS]; // cl_beams[MAX_BEAMS]

        static sfx_t _SfxWizHit; // cl_sfx_wizhit
        static sfx_t _SfxKnigtHit; // cl_sfx_knighthit
        static sfx_t _SfxTink1; // cl_sfx_tink1
        static sfx_t _SfxRic1; // cl_sfx_ric1
        static sfx_t _SfxRic2; // cl_sfx_ric2
        static sfx_t _SfxRic3; // cl_sfx_ric3
        static sfx_t _SfxRExp3; // cl_sfx_r_exp3

        // CL_InitTEnts
        static void InitTempEntities()
        {
	        _SfxWizHit =  Sound.PrecacheSound ("wizard/hit.wav");
	        _SfxKnigtHit = Sound.PrecacheSound ("hknight/hit.wav");
	        _SfxTink1 = Sound.PrecacheSound ("weapons/tink1.wav");
	        _SfxRic1 = Sound.PrecacheSound ("weapons/ric1.wav");
	        _SfxRic2 = Sound.PrecacheSound ("weapons/ric2.wav");
	        _SfxRic3 = Sound.PrecacheSound ("weapons/ric3.wav");
	        _SfxRExp3 = Sound.PrecacheSound ("weapons/r_exp3.wav");

            for (int i = 0; i < _TempEntities.Length; i++)
                _TempEntities[i] = new entity_t();

            for (int i = 0; i < _Beams.Length; i++)
                _Beams[i] = new beam_t();
        }

        // CL_UpdateTEnts
        static void UpdateTempEntities()
        {
            _NumTempEntities = 0;

            // update lightning
            for (int i = 0; i < MAX_BEAMS; i++)
            {
                beam_t b = _Beams[i];
                if (b.model == null || b.endtime < cl.time)
                    continue;

                // if coming from the player, update the start position
                if (b.entity == cl.viewentity)
                {
                    b.start = _Entities[cl.viewentity].origin;
                }

                // calculate pitch and yaw
                Vector3 dist = b.end - b.start;
                float yaw, pitch, forward;

                if (dist.Y == 0 && dist.X == 0)
                {
                    yaw = 0;
                    if (dist.Z > 0)
                        pitch = 90;
                    else
                        pitch = 270;
                }
                else
                {
                    yaw = (int)(Math.Atan2(dist.Y, dist.X) * 180 / Math.PI);
                    if (yaw < 0)
                        yaw += 360;

                    forward = (float)Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
                    pitch = (int)(Math.Atan2(dist.Z, forward) * 180 / Math.PI);
                    if (pitch < 0)
                        pitch += 360;
                }

                // add new entities for the lightning
                Vector3 org = b.start;
                float d = Mathlib.Normalize(ref dist);
                while (d > 0)
                {
                    entity_t ent = NewTempEntity();
                    if (ent == null)
                        return;

                    ent.origin = org;
                    ent.model = b.model;
                    ent.angles.X = pitch;
                    ent.angles.Y = yaw;
                    ent.angles.Z = Sys.Random() % 360;

                    org += dist * 30;
                    // Uze: is this code bug (i is outer loop variable!!!) or what??????????????
                    //for (i=0 ; i<3 ; i++)
                    //    org[i] += dist[i]*30;
                    d -= 30;
                }
            }
        }

        /// <summary>
        /// CL_NewTempEntity
        /// </summary>
        static entity_t NewTempEntity()
        {
            if (NumVisEdicts == MAX_VISEDICTS)
                return null;
            if (_NumTempEntities == MAX_TEMP_ENTITIES)
                return null;

            entity_t ent = _TempEntities[_NumTempEntities];
            _NumTempEntities++;
            _VisEdicts[NumVisEdicts] = ent;
            NumVisEdicts++;

            ent.colormap = Scr.vid.colormap;
            
            return ent;
        }

        
        /// <summary>
        /// CL_ParseTEnt
        /// </summary>
        static void ParseTempEntity()
        {
            Vector3 pos;
            dlight_t dl;
            int type = Net.Reader.ReadByte();
            switch (type)
            {
                case Protocol.TE_WIZSPIKE:			// spike hitting wall
                    pos = Net.Reader.ReadCoords();
                    Render.RunParticleEffect(ref pos, ref Common.ZeroVector, 20, 30);
                    Sound.StartSound(-1, 0, _SfxWizHit, ref pos, 1, 1);
                    break;

                case Protocol.TE_KNIGHTSPIKE:			// spike hitting wall
                    pos = Net.Reader.ReadCoords();
                    Render.RunParticleEffect(ref pos, ref Common.ZeroVector, 226, 20);
                    Sound.StartSound(-1, 0, _SfxKnigtHit, ref pos, 1, 1);
                    break;

                case Protocol.TE_SPIKE:			// spike hitting wall
                    pos = Net.Reader.ReadCoords();
#if GLTEST
                    Test_Spawn (pos);
#else
                    Render.RunParticleEffect(ref pos, ref Common.ZeroVector, 0, 10);
#endif
                    if ((Sys.Random() % 5) != 0)
                        Sound.StartSound(-1, 0, _SfxTink1, ref pos, 1, 1);
                    else
                    {
                        int rnd = Sys.Random() & 3;
                        if (rnd == 1)
                            Sound.StartSound(-1, 0, _SfxRic1, ref pos, 1, 1);
                        else if (rnd == 2)
                            Sound.StartSound(-1, 0, _SfxRic2, ref pos, 1, 1);
                        else
                            Sound.StartSound(-1, 0, _SfxRic3, ref pos, 1, 1);
                    }
                    break;

                case Protocol.TE_SUPERSPIKE:			// super spike hitting wall
                    pos = Net.Reader.ReadCoords();
                    Render.RunParticleEffect(ref pos, ref Common.ZeroVector, 0, 20);

                    if ((Sys.Random() % 5) != 0)
                        Sound.StartSound(-1, 0, _SfxTink1, ref pos, 1, 1);
                    else
                    {
                        int rnd = Sys.Random() & 3;
                        if (rnd == 1)
                            Sound.StartSound(-1, 0, _SfxRic1, ref pos, 1, 1);
                        else if (rnd == 2)
                            Sound.StartSound(-1, 0, _SfxRic2, ref pos, 1, 1);
                        else
                            Sound.StartSound(-1, 0, _SfxRic3, ref pos, 1, 1);
                    }
                    break;

                case Protocol.TE_GUNSHOT:			// bullet hitting wall
                    pos = Net.Reader.ReadCoords();
                    Render.RunParticleEffect(ref pos, ref Common.ZeroVector, 0, 20);
                    break;

                case Protocol.TE_EXPLOSION:			// rocket explosion
                    pos = Net.Reader.ReadCoords();
                    Render.ParticleExplosion(ref pos);
                    dl = AllocDlight(0);
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = (float)Client.cl.time + 0.5f;
                    dl.decay = 300;
                    Sound.StartSound(-1, 0, _SfxRExp3, ref pos, 1, 1);
                    break;

                case Protocol.TE_TAREXPLOSION:			// tarbaby explosion
                    pos = Net.Reader.ReadCoords();
                    Render.BlobExplosion(ref pos);
                    Sound.StartSound(-1, 0, _SfxRExp3, ref pos, 1, 1);
                    break;

                case Protocol.TE_LIGHTNING1:				// lightning bolts
                    ParseBeam(Mod.ForName("progs/bolt.mdl", true));
                    break;

                case Protocol.TE_LIGHTNING2:				// lightning bolts
                    ParseBeam(Mod.ForName("progs/bolt2.mdl", true));
                    break;

                case Protocol.TE_LIGHTNING3:				// lightning bolts
                    ParseBeam(Mod.ForName("progs/bolt3.mdl", true));
                    break;

                // PGM 01/21/97 
                case Protocol.TE_BEAM:				// grappling hook beam
                    ParseBeam(Mod.ForName("progs/beam.mdl", true));
                    break;
                // PGM 01/21/97

                case Protocol.TE_LAVASPLASH:
                    pos = Net.Reader.ReadCoords();
                    Render.LavaSplash(ref pos);
                    break;

                case Protocol.TE_TELEPORT:
                    pos = Net.Reader.ReadCoords();
                    Render.TeleportSplash(ref pos);
                    break;

                case Protocol.TE_EXPLOSION2:				// color mapped explosion
                    pos = Net.Reader.ReadCoords();
                    int colorStart = Net.Reader.ReadByte();
                    int colorLength = Net.Reader.ReadByte();
                    Render.ParticleExplosion(ref pos, colorStart, colorLength);
                    dl = AllocDlight(0);
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = (float)cl.time + 0.5f;
                    dl.decay = 300;
                    Sound.StartSound(-1, 0, _SfxRExp3, ref pos, 1, 1);
                    break;

                default:
                    Sys.Error("CL_ParseTEnt: bad type");
                    break;
            }
        }

        /// <summary>
        /// CL_ParseBeam
        /// </summary>
        static void ParseBeam(model_t m)
        {
            int ent = Net.Reader.ReadShort();

            Vector3 start = Net.Reader.ReadCoords();
            Vector3 end = Net.Reader.ReadCoords();

            // override any beam with the same entity
            for (int i = 0; i < MAX_BEAMS; i++)
            {
                beam_t b = _Beams[i];
                if (b.entity == ent)
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = (float)(cl.time + 0.2);
                    b.start = start;
                    b.end = end;
                    return;
                }
            }

            // find a free beam
            for (int i = 0; i < MAX_BEAMS; i++)
            {
                beam_t b = _Beams[i];
                if (b.model == null || b.endtime < cl.time)
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = (float)(cl.time + 0.2);
                    b.start = start;
                    b.end = end;
                    return;
                }
            }
            Con.Print("beam list overflow!\n");
        }
    }
}
