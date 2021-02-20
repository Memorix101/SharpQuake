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

using SharpQuake.Framework.IO;

namespace SharpQuake.Sys
{
    /// <summary>
    /// Global store for all cvar references
    /// </summary>
	public class Cvars
	{
        // Input
		public ClientVariable MouseFilter;// = { "m_filter", "0" };
       
        // Client
        public ClientVariable Name;// = { "_cl_name", "player", true };
        public ClientVariable Color;// = { "_cl_color", "0", true };
        public ClientVariable ShowNet;// = { "cl_shownet", "0" };	// can be 0, 1, or 2
        public ClientVariable NoLerp;// = { "cl_nolerp", "0" };
        public ClientVariable LookSpring;// = { "lookspring", "0", true };
        public ClientVariable LookStrafe;// = { "lookstrafe", "0", true };
        public ClientVariable Sensitivity;// = { "sensitivity", "3", true };
        public ClientVariable MPitch;// = { "m_pitch", "0.022", true };
        public ClientVariable MYaw;// = { "m_yaw", "0.022", true };
        public ClientVariable MForward;// = { "m_forward", "1", true };
        public ClientVariable MSide;// = { "m_side", "0.8", true };
        public ClientVariable UpSpeed;// = { "cl_upspeed", "200" };
        public ClientVariable ForwardSpeed;// = { "cl_forwardspeed", "200", true };
        public ClientVariable BackSpeed;// = { "cl_backspeed", "200", true };
        public ClientVariable SideSpeed;// = { "cl_sidespeed", "350" };
        public ClientVariable MoveSpeedKey;// = { "cl_movespeedkey", "2.0" };
        public ClientVariable YawSpeed;// = { "cl_yawspeed", "140" };
        public ClientVariable PitchSpeed;// = { "cl_pitchspeed", "150" };
        public ClientVariable AngleSpeedKey;// = { "cl_anglespeedkey", "1.5" };
        public ClientVariable AnimationBlend;

        // Network
        public ClientVariable MessageTimeout; // = { "net_messagetimeout", "300" };
        public ClientVariable HostName;

        // Server
        public ClientVariable Friction;// = { "sv_friction", "4", false, true };
        public ClientVariable EdgeFriction;// = { "edgefriction", "2" };
        public ClientVariable StopSpeed;// = { "sv_stopspeed", "100" };
        public ClientVariable Gravity;// = { "sv_gravity", "800", false, true };
        public ClientVariable MaxVelocity;// = { "sv_maxvelocity", "2000" };
        public ClientVariable NoStep;// = { "sv_nostep", "0" };
        public ClientVariable MaxSpeed;// = { "sv_maxspeed", "320", false, true };
        public ClientVariable Accelerate;// = { "sv_accelerate", "10" };
        public ClientVariable Aim;// = { "sv_aim", "0.93" };
        public ClientVariable IdealPitchScale;// = { "sv_idealpitchscale", "0.8" };

        // Chase View
        public ClientVariable Back;// = { "chase_back", "100" };
        public ClientVariable Up;// = { "chase_up", "16" };
        public ClientVariable Right;// = { "chase_right", "0" };
        public ClientVariable Active;// = { "chase_active", "0" };

        // Draw
        public ClientVariable glNoBind; // = {"gl_nobind", "0"};
        public ClientVariable glMaxSize; // = {"gl_max_size", "1024"};
        public ClientVariable glPicMip;

        // Render
        public ClientVariable NoRefresh;// = { "r_norefresh", "0" };
        public ClientVariable DrawEntities;// = { "r_drawentities", "1" };
        public ClientVariable DrawViewModel;// = { "r_drawviewmodel", "1" };
        public ClientVariable Speeds;// = { "r_speeds", "0" };
        public ClientVariable FullBright;// = { "r_fullbright", "0" };
        public ClientVariable LightMap;// = { "r_lightmap", "0" };
        public ClientVariable Shadows;// = { "r_shadows", "0" };
        //public CVar _MirrorAlpha;// = { "r_mirroralpha", "1" };
        public ClientVariable WaterAlpha;// = { "r_wateralpha", "1" };
        public ClientVariable Dynamic;// = { "r_dynamic", "1" };
        public ClientVariable NoVis;// = { "r_novis", "0" };
        public ClientVariable glFinish;// = { "gl_finish", "0" };
        public ClientVariable glClear;// = { "gl_clear", "0" };
        public ClientVariable glCull;// = { "gl_cull", "1" };
        public ClientVariable glTexSort;// = { "gl_texsort", "1" };
        public ClientVariable glSmoothModels;// = { "gl_smoothmodels", "1" };
        public ClientVariable glAffineModels;// = { "gl_affinemodels", "0" };
        public ClientVariable glPolyBlend;// = { "gl_polyblend", "1" };
        public ClientVariable glFlashBlend;// = { "gl_flashblend", "1" };
        public ClientVariable glPlayerMip;// = { "gl_playermip", "0" };
        public ClientVariable glNoColors;// = { "gl_nocolors", "0" };
        public ClientVariable glKeepTJunctions;// = { "gl_keeptjunctions", "0" };
        public ClientVariable glReportTJunctions;// = { "gl_reporttjunctions", "0" };
        public ClientVariable glDoubleEyes;// = { "gl_doubleeys", "1" };

        // Screen
        public ClientVariable ViewSize; // = { "viewsize", "100", true };
        public ClientVariable Fov;// = { "fov", "90" };	// 10 - 170
        public ClientVariable ConSpeed;// = { "scr_conspeed", "300" };
        public ClientVariable CenterTime;// = { "scr_centertime", "2" };
        public ClientVariable ShowRam;// = { "showram", "1" };
        public ClientVariable ShowTurtle;// = { "showturtle", "0" };
        public ClientVariable ShowPause;// = { "showpause", "1" };
        public ClientVariable PrintSpeed;// = { "scr_printspeed", "8" };
        public ClientVariable glTripleBuffer;// = { "gl_triplebuffer", "1", true };

        // Console
        public ClientVariable NotifyTime; // con_notifytime = { "con_notifytime", "3" };		//seconds

        // Vid
        public ClientVariable glZTrick;// = { "gl_ztrick", "1" };
        public ClientVariable Mode;// = { "vid_mode", "0", false };
        // Note that 0 is MODE_WINDOWED
        public ClientVariable DefaultMode;// = { "_vid_default_mode", "0", true };
        // Note that 3 is MODE_FULLSCREEN_DEFAULT
        public ClientVariable DefaultModeWin;// = { "_vid_default_mode_win", "3", true };
        public ClientVariable Wait;// = { "vid_wait", "0" };
        public ClientVariable NoPageFlip;// = { "vid_nopageflip", "0", true };
        public ClientVariable WaitOverride;// = { "_vid_wait_override", "0", true };
        public ClientVariable ConfigX;// = { "vid_config_x", "800", true };
        public ClientVariable ConfigY;// = { "vid_config_y", "600", true };
        public ClientVariable StretchBy2;// = { "vid_stretch_by_2", "1", true };
        public ClientVariable WindowedMouse;// = { "_windowed_mouse", "1", true };

        // View
        public ClientVariable LcdX; // = { "lcd_x", "0" };
        public ClientVariable LcdYaw; // = { "lcd_yaw", "0" };

        public ClientVariable ScrOfsX; // = { "scr_ofsx", "0", false };
        public ClientVariable ScrOfsY; // = { "scr_ofsy", "0", false };
        public ClientVariable ScrOfsZ; // = { "scr_ofsz", "0", false };

        public ClientVariable ClRollSpeed; // = { "cl_rollspeed", "200" };
        public ClientVariable ClRollAngle; // = { "cl_rollangle", "2.0" };

        public ClientVariable ClBob; // = { "cl_bob", "0.02", false };
        public ClientVariable ClBobCycle; // = { "cl_bobcycle", "0.6", false };
        public ClientVariable ClBobUp; // = { "cl_bobup", "0.5", false };

        public ClientVariable KickTime; // = { "v_kicktime", "0.5", false };
        public ClientVariable KickRoll; // = { "v_kickroll", "0.6", false };
        public ClientVariable KickPitch; // = { "v_kickpitch", "0.6", false };

        public ClientVariable IYawCycle; // = { "v_iyaw_cycle", "2", false };
        public ClientVariable IRollCycle; // = { "v_iroll_cycle", "0.5", false };
        public ClientVariable IPitchCycle;// = { "v_ipitch_cycle", "1", false };
        public ClientVariable IYawLevel;// = { "v_iyaw_level", "0.3", false };
        public ClientVariable IRollLevel;// = { "v_iroll_level", "0.1", false };
        public ClientVariable IPitchLevel;// = { "v_ipitch_level", "0.3", false };

        public ClientVariable IdleScale;// = { "v_idlescale", "0", false };

        public ClientVariable Crosshair;// = { "crosshair", "0", true };
        public ClientVariable ClCrossX;// = { "cl_crossx", "0", false };
        public ClientVariable ClCrossY;// = { "cl_crossy", "0", false };

        public ClientVariable glCShiftPercent;// = { "gl_cshiftpercent", "100", false };

        public ClientVariable Gamma;// = { "gamma", "1", true };
        public ClientVariable CenterMove;// = { "v_centermove", "0.15", false };
        public ClientVariable CenterSpeed;// = { "v_centerspeed", "500" };

        // Sound
        public ClientVariable BgmVolume;
        public ClientVariable Volume;
        public ClientVariable NoSound;
        public ClientVariable Precache;
        public ClientVariable LoadAs8bit;
        public ClientVariable BgmBuffer;
        public ClientVariable AmbientLevel;
        public ClientVariable AmbientFade;
        public ClientVariable NoExtraUpdate;
        public ClientVariable Show;
        public ClientVariable MixAhead;

        // Common
        public ClientVariable Registered;
        public ClientVariable CmdLine;

        // Programs Edict
        public ClientVariable NoMonsters;// = { "nomonsters", "0" };
        public ClientVariable GameCfg;// = { "gamecfg", "0" };
        public ClientVariable Scratch1;// = { "scratch1", "0" };
        public ClientVariable Scratch2;// = { "scratch2", "0" };
        public ClientVariable Scratch3;// = { "scratch3", "0" };
        public ClientVariable Scratch4;// = { "scratch4", "0" };
        public ClientVariable SavedGameCfg;// = { "savedgamecfg", "0", true };
        public ClientVariable Saved1;// = { "saved1", "0", true };
        public ClientVariable Saved2;// = { "saved2", "0", true };
        public ClientVariable Saved3;// = { "saved3", "0", true };
        public ClientVariable Saved4;// = { "saved4", "0", true };

        // Host
        public ClientVariable SystemTickRate;
        public ClientVariable Developer;
        public ClientVariable FrameRate;
        public ClientVariable HostSpeeds;
        public ClientVariable ServerProfile;
        public ClientVariable FragLimit;
        public ClientVariable TimeLimit;
        public ClientVariable TeamPlay;
        public ClientVariable SameLevel;
        public ClientVariable NoExit;
        public ClientVariable Skill;
        public ClientVariable Deathmatch;
        public ClientVariable Coop;
        public ClientVariable Pausable;
        public ClientVariable Temp1;
    }
}
