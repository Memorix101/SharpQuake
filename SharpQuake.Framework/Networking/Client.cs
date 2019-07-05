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
using OpenTK;

namespace SharpQuake.Framework
{
    public class client_t
    {
        public Boolean active;             // false = client is free
        public Boolean spawned;            // false = don't send datagrams
        public Boolean dropasap;           // has been told to go to another level
        public Boolean privileged;         // can execute any host command
        public Boolean sendsignon;         // only valid before spawned

        public Double last_message;     // reliable messages must be sent

        // periodically
        public qsocket_t netconnection; // communications handle

        public usercmd_t cmd;               // movement
        public Vector3 wishdir;			// intended motion calced from cmd

        public MessageWriter message;
        //public sizebuf_t		message;			// can be added to at any time,
        // copied and clear once per frame
        //public byte[] msgbuf;//[MAX_MSGLEN];

        public MemoryEdict edict; // edict_t *edict	// EDICT_NUM(clientnum+1)
        public String name;//[32];			// for printing to other people
        public Int32 colors;

        public Single[] ping_times;//[NUM_PING_TIMES];
        public Int32 num_pings;           // ping_times[num_pings%NUM_PING_TIMES]

        // spawn parms are carried from level to level
        public Single[] spawn_parms;//[NUM_SPAWN_PARMS];

        // client known data for deltas
        public Int32 old_frags;

        public void Clear( )
        {
            this.active = false;
            this.spawned = false;
            this.dropasap = false;
            this.privileged = false;
            this.sendsignon = false;
            this.last_message = 0;
            this.netconnection = null;
            this.cmd.Clear( );
            this.wishdir = Vector3.Zero;
            this.message.Clear( );
            this.edict = null;
            this.name = null;
            this.colors = 0;
            Array.Clear( this.ping_times, 0, this.ping_times.Length );
            this.num_pings = 0;
            Array.Clear( this.spawn_parms, 0, this.spawn_parms.Length );
            this.old_frags = 0;
        }

        public client_t( )
        {
            this.ping_times = new Single[ServerDef.NUM_PING_TIMES];
            this.spawn_parms = new Single[ServerDef.NUM_SPAWN_PARMS];
            this.message = new MessageWriter( QDef.MAX_MSGLEN );
        }
    }// client_t;
}
