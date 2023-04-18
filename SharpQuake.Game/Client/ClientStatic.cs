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

using SharpQuake.Framework;
using System;

namespace SharpQuake.Game.Client
{
    //
    // the client_static_t structure is persistant through an arbitrary number
    // of server connections
    //
    public class client_static_t
    {
        public cactive_t state;

        // personalization data sent to server
        public String mapstring; // [MAX_QPATH];

        public String spawnparms;//[MAX_MAPSTRING];	// to restart a level

        // demo loop control
        public Int32 demonum;		// -1 = don't play demos

        public String[] demos; // [MAX_DEMOS][MAX_DEMONAME];		// when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing client_state_t)
        public Boolean demorecording;

        public Boolean demoplayback;
        public Boolean timedemo;
        public Int32 forcetrack;			// -1 = use normal cd track
        public IDisposable demofile; // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
        public Int32 td_lastframe;		// to meter out one message a frame
        public Int32 td_startframe;		// host_framecount at start
        public Single td_starttime;		// realtime at second frame of timedemo

        // connection information
        public Int32 signon;			// 0 to SIGNONS

        public qsocket_t netcon; // qsocket_t	*netcon;
        public MessageWriter message; // sizebuf_t	message;		// writing buffer to send to server

        public client_static_t( )
        {
            demos = new String[ClientDef.MAX_DEMOS];
            message = new MessageWriter( 1024 ); // like in Client_Init()
        }
    } // client_static_t;
}
