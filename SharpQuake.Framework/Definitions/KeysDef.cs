/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake Rewritten in C# by Yury Kiselev, 2010.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public static class KeysDef
    {
        //
        // these are the key numbers that should be passed to Key_Event
        //
        public const Int32 K_TAB = 9;

        public const Int32 K_ENTER = 13;

        public const Int32 K_ESCAPE = 27;

        public const Int32 K_SPACE = 32;

        public const Int32 K_BACKSPACE = 127;

        // normal keys should be passed as lowercased ascii
        public const Int32 K_UPARROW = 128;

        public const Int32 K_DOWNARROW = 129;

        public const Int32 K_LEFTARROW = 130;

        public const Int32 K_RIGHTARROW = 131;

        public const Int32 K_ALT = 132;

        public const Int32 K_CTRL = 133;

        public const Int32 K_SHIFT = 134;

        public const Int32 K_F1 = 135;

        public const Int32 K_F2 = 136;

        public const Int32 K_F3 = 137;

        public const Int32 K_F4 = 138;

        public const Int32 K_F5 = 139;

        public const Int32 K_F6 = 140;

        public const Int32 K_F7 = 141;

        public const Int32 K_F8 = 142;

        public const Int32 K_F9 = 143;

        public const Int32 K_F10 = 144;

        public const Int32 K_F11 = 145;

        public const Int32 K_F12 = 146;

        public const Int32 K_INS = 147;

        public const Int32 K_DEL = 148;

        public const Int32 K_PGDN = 149;

        public const Int32 K_PGUP = 150;

        public const Int32 K_HOME = 151;

        public const Int32 K_END = 152;

        public const Int32 K_PAUSE = 255;

        //
        // mouse buttons generate virtual keys
        //
        public const Int32 K_MOUSE1 = 200;

        public const Int32 K_MOUSE2 = 201;

        public const Int32 K_MOUSE3 = 202;

        //
        // joystick buttons
        //
        public const Int32 K_JOY1 = 203;

        public const Int32 K_JOY2 = 204;

        public const Int32 K_JOY3 = 205;

        public const Int32 K_JOY4 = 206;

        //
        // aux keys are for multi-buttoned joysticks to generate so they can use
        // the normal binding process
        //
        public const Int32 K_AUX1 = 207;

        public const Int32 K_AUX2 = 208;

        public const Int32 K_AUX3 = 209;

        public const Int32 K_AUX4 = 210;

        public const Int32 K_AUX5 = 211;

        public const Int32 K_AUX6 = 212;

        public const Int32 K_AUX7 = 213;

        public const Int32 K_AUX8 = 214;

        public const Int32 K_AUX9 = 215;

        public const Int32 K_AUX10 = 216;

        public const Int32 K_AUX11 = 217;

        public const Int32 K_AUX12 = 218;

        public const Int32 K_AUX13 = 219;

        public const Int32 K_AUX14 = 220;

        public const Int32 K_AUX15 = 221;

        public const Int32 K_AUX16 = 222;

        public const Int32 K_AUX17 = 223;

        public const Int32 K_AUX18 = 224;

        public const Int32 K_AUX19 = 225;

        public const Int32 K_AUX20 = 226;

        public const Int32 K_AUX21 = 227;

        public const Int32 K_AUX22 = 228;

        public const Int32 K_AUX23 = 229;

        public const Int32 K_AUX24 = 230;

        public const Int32 K_AUX25 = 231;

        public const Int32 K_AUX26 = 232;

        public const Int32 K_AUX27 = 233;

        public const Int32 K_AUX28 = 234;

        public const Int32 K_AUX29 = 235;

        public const Int32 K_AUX30 = 236;

        public const Int32 K_AUX31 = 237;

        public const Int32 K_AUX32 = 238;

        public const Int32 K_MWHEELUP = 239;

        // JACK: Intellimouse(c) Mouse Wheel Support
        public const Int32 K_MWHEELDOWN = 240;

        public const Int32 MAXCMDLINE = 256;

        public static KeyName[] KeyNames = new KeyName[]
        {
            new KeyName("TAB", K_TAB),
            new KeyName("ENTER", K_ENTER),
            new KeyName("ESCAPE", K_ESCAPE),
            new KeyName("SPACE", K_SPACE),
            new KeyName("BACKSPACE", K_BACKSPACE),
            new KeyName("UPARROW", K_UPARROW),
            new KeyName("DOWNARROW", K_DOWNARROW),
            new KeyName("LEFTARROW", K_LEFTARROW),
            new KeyName("RIGHTARROW", K_RIGHTARROW),

            new KeyName("ALT", K_ALT),
            new KeyName("CTRL", K_CTRL),
            new KeyName("SHIFT", K_SHIFT),

            new KeyName("F1", K_F1),
            new KeyName("F2", K_F2),
            new KeyName("F3", K_F3),
            new KeyName("F4", K_F4),
            new KeyName("F5", K_F5),
            new KeyName("F6", K_F6),
            new KeyName("F7", K_F7),
            new KeyName("F8", K_F8),
            new KeyName("F9", K_F9),
            new KeyName("F10", K_F10),
            new KeyName("F11", K_F11),
            new KeyName("F12", K_F12),

            new KeyName("INS", K_INS),
            new KeyName("DEL", K_DEL),
            new KeyName("PGDN", K_PGDN),
            new KeyName("PGUP", K_PGUP),
            new KeyName("HOME", K_HOME),
            new KeyName("END", K_END),

            new KeyName("MOUSE1", K_MOUSE1),
            new KeyName("MOUSE2", K_MOUSE2),
            new KeyName("MOUSE3", K_MOUSE3),

            new KeyName("JOY1", K_JOY1),
            new KeyName("JOY2", K_JOY2),
            new KeyName("JOY3", K_JOY3),
            new KeyName("JOY4", K_JOY4),

            new KeyName("AUX1", K_AUX1),
            new KeyName("AUX2", K_AUX2),
            new KeyName("AUX3", K_AUX3),
            new KeyName("AUX4", K_AUX4),
            new KeyName("AUX5", K_AUX5),
            new KeyName("AUX6", K_AUX6),
            new KeyName("AUX7", K_AUX7),
            new KeyName("AUX8", K_AUX8),
            new KeyName("AUX9", K_AUX9),
            new KeyName("AUX10", K_AUX10),
            new KeyName("AUX11", K_AUX11),
            new KeyName("AUX12", K_AUX12),
            new KeyName("AUX13", K_AUX13),
            new KeyName("AUX14", K_AUX14),
            new KeyName("AUX15", K_AUX15),
            new KeyName("AUX16", K_AUX16),
            new KeyName("AUX17", K_AUX17),
            new KeyName("AUX18", K_AUX18),
            new KeyName("AUX19", K_AUX19),
            new KeyName("AUX20", K_AUX20),
            new KeyName("AUX21", K_AUX21),
            new KeyName("AUX22", K_AUX22),
            new KeyName("AUX23", K_AUX23),
            new KeyName("AUX24", K_AUX24),
            new KeyName("AUX25", K_AUX25),
            new KeyName("AUX26", K_AUX26),
            new KeyName("AUX27", K_AUX27),
            new KeyName("AUX28", K_AUX28),
            new KeyName("AUX29", K_AUX29),
            new KeyName("AUX30", K_AUX30),
            new KeyName("AUX31", K_AUX31),
            new KeyName("AUX32", K_AUX32),

            new KeyName("PAUSE", K_PAUSE),

            new KeyName("MWHEELUP", K_MWHEELUP),
            new KeyName("MWHEELDOWN", K_MWHEELDOWN),

            new KeyName("SEMICOLON", ';'),	// because a raw semicolon seperates commands
        };
    }
}
