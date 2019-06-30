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
    }
}
