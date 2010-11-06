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
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;

// view.h
// view.c -- player eye positioning

// The view is allowed to move slightly from it's true position for bobbing,
// but if it exceeds 8 pixels linear distance (spherical, not box), the list of
// entities sent from the server may not include everything in the pvs, especially
// when crossing a water boudnary.


namespace SharpQuake
{
    /// <summary>
    /// V_functions
    /// </summary>
    static class View
    {
        static readonly Vector3 SmallOffset = Vector3.One / 32f;

        static Cvar _LcdX; // = { "lcd_x", "0" };
        static Cvar _LcdYaw; // = { "lcd_yaw", "0" };

        static Cvar _ScrOfsX; // = { "scr_ofsx", "0", false };
        static Cvar _ScrOfsY; // = { "scr_ofsy", "0", false };
        static Cvar _ScrOfsZ; // = { "scr_ofsz", "0", false };

        static Cvar _ClRollSpeed; // = { "cl_rollspeed", "200" };
        static Cvar _ClRollAngle; // = { "cl_rollangle", "2.0" };

        static Cvar _ClBob; // = { "cl_bob", "0.02", false };
        static Cvar _ClBobCycle; // = { "cl_bobcycle", "0.6", false };
        static Cvar _ClBobUp; // = { "cl_bobup", "0.5", false };

        static Cvar _KickTime; // = { "v_kicktime", "0.5", false };
        static Cvar _KickRoll; // = { "v_kickroll", "0.6", false };
        static Cvar _KickPitch; // = { "v_kickpitch", "0.6", false };

        static Cvar _IYawCycle; // = { "v_iyaw_cycle", "2", false };
        static Cvar _IRollCycle; // = { "v_iroll_cycle", "0.5", false };
        static Cvar _IPitchCycle;// = { "v_ipitch_cycle", "1", false };
        static Cvar _IYawLevel;// = { "v_iyaw_level", "0.3", false };
        static Cvar _IRollLevel;// = { "v_iroll_level", "0.1", false };
        static Cvar _IPitchLevel;// = { "v_ipitch_level", "0.3", false };

        static Cvar _IdleScale;// = { "v_idlescale", "0", false };

        static Cvar _Crosshair;// = { "crosshair", "0", true };
        static Cvar _ClCrossX;// = { "cl_crossx", "0", false };
        static Cvar _ClCrossY;// = { "cl_crossy", "0", false };

        static Cvar _glCShiftPercent;// = { "gl_cshiftpercent", "100", false };

        static Cvar _Gamma;// = { "gamma", "1", true };
        static Cvar _CenterMove;// = { "v_centermove", "0.15", false };
        static Cvar _CenterSpeed;// = { "v_centerspeed", "500" };

        static byte[] _GammaTable; // [256];	// palette is sent through this
        static cshift_t _CShift_empty;// = { { 130, 80, 50 }, 0 };
        static cshift_t _CShift_water;// = { { 130, 80, 50 }, 128 };
        static cshift_t _CShift_slime;// = { { 0, 25, 5 }, 150 };
        static cshift_t _CShift_lava;// = { { 255, 80, 0 }, 150 };
        
        public static Color4 Blend; // v_blend[4]		// rgba 0.0 - 1.0
        static byte[,] _Ramps = new byte[3, 256]; // ramps[3][256]

        static Vector3 _Forward; // vec3_t forward
        static Vector3 _Right; // vec3_t right
        static Vector3 _Up; // vec3_t up
        
        static float _DmgTime; // v_dmg_time
        static float _DmgRoll; // v_dmg_roll
        static float _DmgPitch; // v_dmg_pitch
        
        static float _OldZ = 0; // static oldz  from CalcRefdef()
        static float _OldYaw = 0; // static oldyaw from CalcGunAngle
        static float _OldPitch = 0; // static oldpitch from CalcGunAngle
        static float _OldGammaValue; // static float oldgammavalue from CheckGamma

        public static float Crosshair
        {
            get { return _Crosshair.Value; }
        }
        public static float Gamma
        {
            get { return _Gamma.Value; }
        }

        static View()
        {
            _GammaTable = new byte[256];

            _CShift_empty = new cshift_t(new[] { 130, 80, 50 }, 0);
            _CShift_water = new cshift_t(new[] { 130, 80, 50 }, 128);
            _CShift_slime = new cshift_t(new[] { 0, 25, 5 }, 150);
            _CShift_lava = new cshift_t(new[] { 255, 80, 0 }, 150);
        }

        // V_Init
        public static void Init()
        {
            Cmd.Add("v_cshift", CShift_f);
            Cmd.Add("bf", BonusFlash_f);
            Cmd.Add("centerview", StartPitchDrift);

            if (_LcdX == null)
            {
                _LcdX = new Cvar("lcd_x", "0");
                _LcdYaw = new Cvar("lcd_yaw", "0");

                _ScrOfsX = new Cvar("scr_ofsx", "0", false);
                _ScrOfsY = new Cvar("scr_ofsy", "0", false);
                _ScrOfsZ = new Cvar("scr_ofsz", "0", false);

                _ClRollSpeed = new Cvar("cl_rollspeed", "200");
                _ClRollAngle = new Cvar("cl_rollangle", "2.0");

                _ClBob = new Cvar("cl_bob", "0.02", false);
                _ClBobCycle = new Cvar("cl_bobcycle", "0.6", false);
                _ClBobUp = new Cvar("cl_bobup", "0.5", false);

                _KickTime = new Cvar("v_kicktime", "0.5", false);
                _KickRoll = new Cvar("v_kickroll", "0.6", false);
                _KickPitch = new Cvar("v_kickpitch", "0.6", false);

                _IYawCycle = new Cvar("v_iyaw_cycle", "2", false);
                _IRollCycle = new Cvar("v_iroll_cycle", "0.5", false);
                _IPitchCycle = new Cvar("v_ipitch_cycle", "1", false);
                _IYawLevel = new Cvar("v_iyaw_level", "0.3", false);
                _IRollLevel = new Cvar("v_iroll_level", "0.1", false);
                _IPitchLevel = new Cvar("v_ipitch_level", "0.3", false);

                _IdleScale = new Cvar("v_idlescale", "0", false);

                _Crosshair = new Cvar("crosshair", "0", true);
                _ClCrossX = new Cvar("cl_crossx", "0", false);
                _ClCrossY = new Cvar("cl_crossy", "0", false);

                _glCShiftPercent = new Cvar("gl_cshiftpercent", "100", false);
            
                _CenterMove = new Cvar("v_centermove", "0.15", false);
                _CenterSpeed = new Cvar("v_centerspeed", "500");
                
                BuildGammaTable(1.0f);	// no gamma yet
                _Gamma = new Cvar("gamma", "1", true);
            }
        }

        /// <summary>
        /// V_RenderView
        /// The player's clipping box goes from (-16 -16 -24) to (16 16 32) from
        /// the entity origin, so any view position inside that will be valid
        /// </summary>
        public static void RenderView()
        {
            if (Con.ForcedUp)
                return;

            // don't allow cheats in multiplayer
            if (Client.cl.maxclients > 1)
            {
                Cvar.Set("scr_ofsx", "0");
                Cvar.Set("scr_ofsy", "0");
                Cvar.Set("scr_ofsz", "0");
            }

            if (Client.cl.intermission > 0)
            {
                // intermission / finale rendering
                CalcIntermissionRefDef();
            }
            else if (!Client.cl.paused)
                CalcRefDef();

            Render.PushDlights();

            if (_LcdX.Value != 0)
            {
                //
                // render two interleaved views
                //
                viddef_t vid = Scr.vid;
                refdef_t rdef = Render.RefDef;

                vid.rowbytes <<= 1;
                vid.aspect *= 0.5f;

                rdef.viewangles.Y -= _LcdYaw.Value;
                rdef.vieworg -= _Right * _LcdX.Value;

                Render.RenderView();

                // ???????? vid.buffer += vid.rowbytes>>1;

                Render.PushDlights();

                rdef.viewangles.Y += _LcdYaw.Value * 2;
                rdef.vieworg += _Right * _LcdX.Value * 2;

                Render.RenderView();

                // ????????? vid.buffer -= vid.rowbytes>>1;

                rdef.vrect.height <<= 1;

                vid.rowbytes >>= 1;
                vid.aspect *= 2;
            }
            else
            {
                Render.RenderView();
            }
        }

        /// <summary>
        /// V_CalcRoll
        /// Used by view and sv_user
        /// </summary>
        public static float CalcRoll(ref Vector3 angles, ref Vector3 velocity)
        {
            Mathlib.AngleVectors(ref angles, out _Forward, out _Right, out _Up);
            float side = Vector3.Dot(velocity, _Right);
            float sign = side < 0 ? -1 : 1;
            side = Math.Abs(side);

            float value = _ClRollAngle.Value;
            if (side < _ClRollSpeed.Value)
                side = side * value / _ClRollSpeed.Value;
            else
                side = value;

            return side * sign;
        }

        // V_UpdatePalette
        public static void UpdatePalette()
        {
            CalcPowerupCshift();

            bool isnew = false;

            client_state_t cl = Client.cl;
            for (int i = 0; i < ColorShift.NUM_CSHIFTS; i++)
            {
                if (cl.cshifts[i].percent != cl.prev_cshifts[i].percent)
                {
                    isnew = true;
                    cl.prev_cshifts[i].percent = cl.cshifts[i].percent;
                }
                for (int j = 0; j < 3; j++)
                    if (cl.cshifts[i].destcolor[j] != cl.prev_cshifts[i].destcolor[j])
                    {
                        isnew = true;
                        cl.prev_cshifts[i].destcolor[j] = cl.cshifts[i].destcolor[j];
                    }
            }

            // drop the damage value
            cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent -= (int)(Host.FrameTime * 150);
            if (cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0)
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;

            // drop the bonus value
            cl.cshifts[ColorShift.CSHIFT_BONUS].percent -= (int)(Host.FrameTime * 100);
            if (cl.cshifts[ColorShift.CSHIFT_BONUS].percent < 0)
                cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 0;

            bool force = CheckGamma();
            if (!isnew && !force)
                return;

            CalcBlend();

            float a = Blend.A;
            float r = 255 * Blend.R * a;
            float g = 255 * Blend.G * a;
            float b = 255 * Blend.B * a;

            a = 1 - a;
            for (int i = 0; i < 256; i++)
            {
                int ir = (int)(i * a + r);
                int ig = (int)(i * a + g);
                int ib = (int)(i * a + b);
                if (ir > 255)
                    ir = 255;
                if (ig > 255)
                    ig = 255;
                if (ib > 255)
                    ib = 255;

                _Ramps[0, i] = _GammaTable[ir];
                _Ramps[1, i] = _GammaTable[ig];
                _Ramps[2, i] = _GammaTable[ib];
            }

            byte[] basepal = Host.BasePal;
            int offset = 0;
            byte[] newpal = new byte[768];

            for (int i = 0; i < 256; i++)
            {
                int ir = basepal[offset + 0];
                int ig = basepal[offset + 1];
                int ib = basepal[offset + 2];

                newpal[offset + 0] = _Ramps[0, ir];
                newpal[offset + 1] = _Ramps[1, ig];
                newpal[offset + 2] = _Ramps[2, ib];

                offset += 3;
            }

            ShiftPalette(newpal);
        }

        // BuildGammaTable
        static void BuildGammaTable(float g)
        {
            if (g == 1.0f)
            {
                for (int i = 0; i < 256; i++)
                {
                    _GammaTable[i] = (byte)i;
                }
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    int inf = (int)(255 * Math.Pow((i + 0.5) / 255.5, g) + 0.5);
                    if (inf < 0) inf = 0;
                    if (inf > 255) inf = 255;
                    _GammaTable[i] = (byte)inf;
                }
            }
        }

        // V_StartPitchDrift
        public static void StartPitchDrift()
        {
            client_state_t cl = Client.cl;
	        if (cl.laststop == cl.time)
	        {
		        return;	// something else is keeping it from drifting
	        }
	        if (cl.nodrift || cl.pitchvel == 0)
	        {
		        cl.pitchvel = _CenterSpeed.Value;
		        cl.nodrift = false;
		        cl.driftmove = 0;
	        }
        }

        // V_StopPitchDrift
        public static void StopPitchDrift()
        {
            client_state_t cl = Client.cl;
            cl.laststop = cl.time;
            cl.nodrift = true;
            cl.pitchvel = 0;
        }


        // V_cshift_f
        static void CShift_f()
        {
            int.TryParse(Cmd.Argv(1), out _CShift_empty.destcolor[0]);
            int.TryParse(Cmd.Argv(2), out _CShift_empty.destcolor[1]);
            int.TryParse(Cmd.Argv(3), out _CShift_empty.destcolor[2]);
            int.TryParse(Cmd.Argv(4), out _CShift_empty.percent);
        }


        // V_BonusFlash_f
        //
        // When you run over an item, the server sends this command
        static void BonusFlash_f()
        {
            client_state_t cl = Client.cl;
            cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[0] = 215;
            cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[1] = 186;
            cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[2] = 69;
            cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 50;
        }

        
        // V_CalcIntermissionRefdef
        static void CalcIntermissionRefDef()
        {
            // ent is the player model (visible when out of body)
	        entity_t ent = Client.ViewEntity;

            // view is the weapon model (only visible from inside body)
	        entity_t view = Client.ViewEnt;
            
            refdef_t rdef = Render.RefDef;
	        rdef.vieworg = ent.origin;
            rdef.viewangles = ent.angles;
	        view.model = null;

            // allways idle in intermission
            AddIdle(1);
        }

        
        // V_CalcRefdef
        static void CalcRefDef()
        {
            DriftPitch();

            // ent is the player model (visible when out of body)
            entity_t ent = Client.ViewEntity;
            // view is the weapon model (only visible from inside body)
            entity_t view = Client.ViewEnt;

            // transform the view offset by the model's matrix to get the offset from
            // model origin for the view
            ent.angles.Y = Client.cl.viewangles.Y;	// the model should face the view dir
            ent.angles.X = -Client.cl.viewangles.X;	// the model should face the view dir

            float bob = CalcBob();

            refdef_t rdef = Render.RefDef;
            client_state_t cl = Client.cl;

            // refresh position
            rdef.vieworg = ent.origin;
            rdef.vieworg.Z += cl.viewheight + bob;

            // never let it sit exactly on a node line, because a water plane can
            // dissapear when viewed with the eye exactly on it.
            // the server protocol only specifies to 1/16 pixel, so add 1/32 in each axis
            rdef.vieworg += SmallOffset;
            rdef.viewangles = cl.viewangles;

            CalcViewRoll();
            AddIdle(_IdleScale.Value);

            // offsets
            Vector3 angles = ent.angles;
            angles.X = -angles.X; // because entity pitches are actually backward

            Vector3 forward, right, up;
            Mathlib.AngleVectors(ref angles, out forward, out right, out up);

            rdef.vieworg += forward * _ScrOfsX.Value + right * _ScrOfsY.Value + up * _ScrOfsZ.Value;

            BoundOffsets();

            // set up gun position
            view.angles = cl.viewangles;

            CalcGunAngle();

            view.origin = ent.origin;
            view.origin.Z += cl.viewheight;
            view.origin += forward * bob * 0.4f;
            view.origin.Z += bob;

            // fudge position around to keep amount of weapon visible
            // roughly equal with different FOV
            float viewSize = Scr.ViewSize.Value; // scr_viewsize

            if (viewSize == 110)
                view.origin.Z += 1;
            else if (viewSize == 100)
                view.origin.Z += 2;
            else if (viewSize == 90)
                view.origin.Z += 1;
            else if (viewSize == 80)
                view.origin.Z += 0.5f;

            view.model = cl.model_precache[cl.stats[QStats.STAT_WEAPON]];
            view.frame = cl.stats[QStats.STAT_WEAPONFRAME];
            view.colormap = Scr.vid.colormap;

            // set up the refresh position
            rdef.viewangles += cl.punchangle;

            // smooth out stair step ups
            if (cl.onground && ent.origin.Z - _OldZ > 0)
            {
                float steptime = (float)(cl.time - cl.oldtime);
                if (steptime < 0)
                    steptime = 0;

                _OldZ += steptime * 80;
                if (_OldZ > ent.origin.Z)
                    _OldZ = ent.origin.Z;
                if (ent.origin.Z - _OldZ > 12)
                    _OldZ = ent.origin.Z - 12;
                rdef.vieworg.Z += _OldZ - ent.origin.Z;
                view.origin.Z += _OldZ - ent.origin.Z;
            }
            else
                _OldZ = ent.origin.Z;

            if (Chase.IsActive)
                Chase.Update();
        }


        // V_AddIdle
        //
        // Idle swaying
        static void AddIdle(float idleScale)
        {
            double time = Client.cl.time;
            Vector3 v = new Vector3(
                (float)(Math.Sin(time * _IPitchCycle.Value) * _IPitchLevel.Value),
                (float)(Math.Sin(time * _IYawCycle.Value) * _IYawLevel.Value),
                (float)(Math.Sin(time * _IRollCycle.Value) * _IRollLevel.Value));
            Render.RefDef.viewangles += v * idleScale;
        }

        
        // V_DriftPitch
        // 
        // Moves the client pitch angle towards cl.idealpitch sent by the server.
        //
        // If the user is adjusting pitch manually, either with lookup/lookdown,
        // mlook and mouse, or klook and keyboard, pitch drifting is constantly stopped.
        //
        // Drifting is enabled when the center view key is hit, mlook is released and
        // lookspring is non 0, or when 
        static void DriftPitch()
        {
            client_state_t cl = Client.cl;
            if (Host.NoClipAngleHack || !cl.onground || Client.cls.demoplayback)
            {
                cl.driftmove = 0;
                cl.pitchvel = 0;
                return;
            }

            // don't count small mouse motion
            if (cl.nodrift)
            {
                if (Math.Abs(cl.cmd.forwardmove) < Client.ForwardSpeed)
                    cl.driftmove = 0;
                else
                    cl.driftmove += (float)Host.FrameTime;

                if (cl.driftmove > _CenterMove.Value)
                {
                    StartPitchDrift();
                }
                return;
            }

            float delta = cl.idealpitch - cl.viewangles.X;
            if (delta == 0)
            {
                cl.pitchvel = 0;
                return;
            }

            float move = (float)Host.FrameTime * cl.pitchvel;
            cl.pitchvel += (float)Host.FrameTime * _CenterSpeed.Value;

            if (delta > 0)
            {
                if (move > delta)
                {
                    cl.pitchvel = 0;
                    move = delta;
                }
                cl.viewangles.X += move;
            }
            else if (delta < 0)
            {
                if (move > -delta)
                {
                    cl.pitchvel = 0;
                    move = -delta;
                }
                cl.viewangles.X -= move;
            }
        }

        
        // V_CalcBob
        static float CalcBob()
        {
            client_state_t cl = Client.cl;
            float bobCycle = _ClBobCycle.Value;
            float bobUp = _ClBobUp.Value;
            float cycle = (float)(cl.time - (int)(cl.time / bobCycle) * bobCycle);
            cycle /= bobCycle;
            if (cycle < bobUp)
                cycle = (float)Math.PI * cycle / bobUp;
            else
                cycle = (float)(Math.PI + Math.PI * (cycle - bobUp) / (1.0 - bobUp));

            // bob is proportional to velocity in the xy plane
            // (don't count Z, or jumping messes it up)
            Vector2 tmp = cl.velocity.Xy;
            double bob = tmp.Length * _ClBob.Value;
            bob = bob * 0.3 + bob * 0.7 * Math.Sin(cycle);
            if (bob > 4)
                bob = 4;
            else if (bob < -7)
                bob = -7;
            return (float)bob;
        }

        // V_CalcViewRoll
        //
        // Roll is induced by movement and damage
        static void CalcViewRoll()
        {
            client_state_t cl = Client.cl;
            refdef_t rdef = Render.RefDef;
            float side = CalcRoll(ref Client.ViewEntity.angles, ref cl.velocity);
            rdef.viewangles.Z += side;

            if (_DmgTime > 0)
            {
                rdef.viewangles.Z += _DmgTime / _KickTime.Value * _DmgRoll;
                rdef.viewangles.X += _DmgTime / _KickTime.Value * _DmgPitch;
                _DmgTime -= (float)Host.FrameTime;
            }

            if (cl.stats[QStats.STAT_HEALTH] <= 0)
            {
                rdef.viewangles.Z = 80;	// dead view angle
                return;
            }
        }

        
        // V_BoundOffsets
        static void BoundOffsets()
        {
            entity_t ent = Client.ViewEntity;

            // absolutely bound refresh reletive to entity clipping hull
            // so the view can never be inside a solid wall
            refdef_t rdef = Render.RefDef;
            if (rdef.vieworg.X < ent.origin.X - 14)
                rdef.vieworg.X = ent.origin.X - 14;
            else if (rdef.vieworg.X > ent.origin.X + 14)
                rdef.vieworg.X = ent.origin.X + 14;

            if (rdef.vieworg.Y < ent.origin.Y - 14)
                rdef.vieworg.Y = ent.origin.Y - 14;
            else if (rdef.vieworg.Y > ent.origin.Y + 14)
                rdef.vieworg.Y = ent.origin.Y + 14;

            if (rdef.vieworg.Z < ent.origin.Z - 22)
                rdef.vieworg.Z = ent.origin.Z - 22;
            else if (rdef.vieworg.Z > ent.origin.Z + 30)
                rdef.vieworg.Z = ent.origin.Z + 30;
        }


        /// <summary>
        /// CalcGunAngle
        /// </summary>
        static void CalcGunAngle()
        {
            refdef_t rdef = Render.RefDef;
            float yaw = rdef.viewangles.Y;
            float pitch = -rdef.viewangles.X;

            yaw = AngleDelta(yaw - rdef.viewangles.Y) * 0.4f;
            if (yaw > 10)
                yaw = 10;
            if (yaw < -10)
                yaw = -10;
            pitch = AngleDelta(-pitch - rdef.viewangles.X) * 0.4f;
            if (pitch > 10)
                pitch = 10;
            if (pitch < -10)
                pitch = -10;
            float move = (float)Host.FrameTime * 20;
            if (yaw > _OldYaw)
            {
                if (_OldYaw + move < yaw)
                    yaw = _OldYaw + move;
            }
            else
            {
                if (_OldYaw - move > yaw)
                    yaw = _OldYaw - move;
            }

            if (pitch > _OldPitch)
            {
                if (_OldPitch + move < pitch)
                    pitch = _OldPitch + move;
            }
            else
            {
                if (_OldPitch - move > pitch)
                    pitch = _OldPitch - move;
            }

            _OldYaw = yaw;
            _OldPitch = pitch;

            client_state_t cl = Client.cl;
            cl.viewent.angles.Y = rdef.viewangles.Y +yaw;
            cl.viewent.angles.X = -(rdef.viewangles.X + pitch);

            float idleScale = _IdleScale.Value;
            cl.viewent.angles.Z -= (float)(idleScale * Math.Sin(cl.time * _IRollCycle.Value) * _IRollLevel.Value);
            cl.viewent.angles.X -= (float)(idleScale * Math.Sin(cl.time * _IPitchCycle.Value) * _IPitchLevel.Value);
            cl.viewent.angles.Y -= (float)(idleScale * Math.Sin(cl.time * _IYawCycle.Value) * _IYawLevel.Value);
        }

        // angledelta()
        static float AngleDelta(float a)
        {
            a = Mathlib.AngleMod(a);
            if (a > 180)
                a -= 360;
            return a;
        }

        // V_CalcPowerupCshift
        static void CalcPowerupCshift()
        {
            client_state_t cl = Client.cl;
            if (cl.HasItems(QItems.IT_QUAD))
            {
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 0;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 255;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
            }
            else if (cl.HasItems(QItems.IT_SUIT))
            {
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 20;
            }
            else if (cl.HasItems(QItems.IT_INVISIBILITY))
            {
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 100;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 100;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 100;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 100;
            }
            else if (cl.HasItems(QItems.IT_INVULNERABILITY))
            {
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 255;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
                cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
            }
            else
                cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 0;
        }

        // V_CheckGamma
        static bool CheckGamma()
        {
            if (_Gamma.Value == _OldGammaValue)
                return false;

            _OldGammaValue = _Gamma.Value;

            BuildGammaTable(_Gamma.Value);
            Scr.vid.recalc_refdef = true;	// force a surface cache flush

            return true;
        }

        /// <summary>
        /// V_CalcBlend
        /// </summary>
        public static void CalcBlend()
        {
            float r = 0;
            float g = 0;
            float b = 0;
            float a = 0;

            cshift_t[] cshifts = Client.cl.cshifts;

            if (_glCShiftPercent.Value != 0)
            {
                for (int j = 0; j < ColorShift.NUM_CSHIFTS; j++)
                {
                    float a2 = ((cshifts[j].percent * _glCShiftPercent.Value) / 100.0f) / 255.0f;

                    if (a2 == 0)
                        continue;

                    a = a + a2 * (1 - a);

                    a2 = a2 / a;
                    r = r * (1 - a2) + cshifts[j].destcolor[0] * a2;
                    g = g * (1 - a2) + cshifts[j].destcolor[1] * a2;
                    b = b * (1 - a2) + cshifts[j].destcolor[2] * a2;
                }
            }

            Blend.R = r / 255.0f;
            Blend.G = g / 255.0f;
            Blend.B = b / 255.0f;
            Blend.A = a;
            if (Blend.A > 1)
                Blend.A = 1;
            if (Blend.A < 0)
                Blend.A = 0;
        }

        // VID_ShiftPalette from gl_vidnt.c 
        static void ShiftPalette(byte[] palette)
        {
            //	VID_SetPalette (palette);
            //	gammaworks = SetDeviceGammaRamp (maindc, ramps);
        }

        
        // V_ParseDamage
        public static void ParseDamage()
        {
            int armor = Net.Reader.ReadByte();
            int blood = Net.Reader.ReadByte();
            Vector3 from = Net.Reader.ReadCoords();

            float count = blood * 0.5f + armor * 0.5f;
            if (count < 10)
                count = 10;

            client_state_t cl = Client.cl;
            cl.faceanimtime = (float)cl.time + 0.2f; // put sbar face into pain frame

            cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent += (int)(3 * count);
            if (cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0)
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;
            if (cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent > 150)
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 150;

            if (armor > blood)
            {
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 200;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 100;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 100;
            }
            else if (armor != 0)
            {
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 220;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 50;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 50;
            }
            else
            {
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 255;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 0;
                cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 0;
            }

            //
            // calculate view angle kicks
            //
            entity_t ent = Client.Entities[cl.viewentity];

            from -= ent.origin; //  VectorSubtract (from, ent->origin, from);
            Mathlib.Normalize(ref from);

            Vector3 forward, right, up;
            Mathlib.AngleVectors(ref ent.angles, out forward, out right, out up);

            float side = Vector3.Dot(from, right);

            _DmgRoll = count * side * _KickRoll.Value;

            side = Vector3.Dot(from, forward);
            _DmgPitch = count * side * _KickPitch.Value;

            _DmgTime = _KickTime.Value;
        }

        /// <summary>
        /// V_SetContentsColor
        /// Underwater, lava, etc each has a color shift
        /// </summary>
        public static void SetContentsColor(int contents)
        {
            switch (contents)
            {
                case Contents.CONTENTS_EMPTY:
                case Contents.CONTENTS_SOLID:
                    Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_empty;
                    break;
                
                case Contents.CONTENTS_LAVA:
                    Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_lava;
                    break;
                
                case Contents.CONTENTS_SLIME:
                    Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_slime;
                    break;
                
                default:
                    Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = _CShift_water;
                    break;
            }
        }
    }
}
