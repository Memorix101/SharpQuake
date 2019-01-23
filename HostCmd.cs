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
using System.Globalization;
using System.IO;
using System.Text;

// host_cmd.c

namespace SharpQuake
{
    partial class Host
    {
        /// <summary>
        /// Host_Quit_f
        /// </summary>
        public static void Quit_f()
        {
            if( Key.Destination != keydest_t.key_console && Client.cls.state != cactive_t.ca_dedicated )
            {
                MenuBase.QuitMenu.Show();
                return;
            }
            Client.Disconnect();
            Host.ShutdownServer( false );
            Sys.Quit();
        }

        // Host_InitCommands
        private static void InitCommands()
        {
            Cmd.Add( "status", Status_f );
            Cmd.Add( "quit", Quit_f );
            Cmd.Add( "god", God_f );
            Cmd.Add( "notarget", Notarget_f );
            Cmd.Add( "fly", Fly_f );
            Cmd.Add( "map", Map_f );
            Cmd.Add( "restart", Restart_f );
            Cmd.Add( "changelevel", Changelevel_f );
            Cmd.Add( "connect", Connect_f );
            Cmd.Add( "reconnect", Reconnect_f );
            Cmd.Add( "name", Name_f );
            Cmd.Add( "noclip", Noclip_f );
            Cmd.Add( "version", Version_f );
            Cmd.Add( "say", Say_f );
            Cmd.Add( "say_team", Say_Team_f );
            Cmd.Add( "tell", Tell_f );
            Cmd.Add( "color", Color_f );
            Cmd.Add( "kill", Kill_f );
            Cmd.Add( "pause", Pause_f );
            Cmd.Add( "spawn", Spawn_f );
            Cmd.Add( "begin", Begin_f );
            Cmd.Add( "prespawn", PreSpawn_f );
            Cmd.Add( "kick", Kick_f );
            Cmd.Add( "ping", Ping_f );
            Cmd.Add( "load", Loadgame_f );
            Cmd.Add( "save", Savegame_f );
            Cmd.Add( "give", Give_f );

            Cmd.Add( "startdemos", Startdemos_f );
            Cmd.Add( "demos", Demos_f );
            Cmd.Add( "stopdemo", Stopdemo_f );

            Cmd.Add( "viewmodel", Viewmodel_f );
            Cmd.Add( "viewframe", Viewframe_f );
            Cmd.Add( "viewnext", Viewnext_f );
            Cmd.Add( "viewprev", Viewprev_f );

            Cmd.Add( "mcache", Mod.Print );
        }

        /// <summary>
        /// Host_Status_f
        /// </summary>
        private static void Status_f()
        {
            bool flag = true;
            if( Cmd.Source == cmd_source_t.src_command )
            {
                if( !Server.sv.active )
                {
                    Cmd.ForwardToServer();
                    return;
                }
            }
            else
                flag = false;

            StringBuilder sb = new StringBuilder( 256 );
            sb.Append( String.Format( "host:    {0}\n", Cvar.GetString( "hostname" ) ) );
            sb.Append( String.Format( "version: {0:F2}\n", QDef.VERSION ) );
            if( Net.TcpIpAvailable )
            {
                sb.Append( "tcp/ip:  " );
                sb.Append( Net.MyTcpIpAddress );
                sb.Append( '\n' );
            }

            sb.Append( "map:     " );
            sb.Append( Server.sv.name );
            sb.Append( '\n' );
            sb.Append( String.Format( "players: {0} active ({1} max)\n\n", Net.ActiveConnections, Server.svs.maxclients ) );
            for( int j = 0; j < Server.svs.maxclients; j++ )
            {
                client_t client = Server.svs.clients[j];
                if( !client.active )
                    continue;

                int seconds = (int)( Net.Time - client.netconnection.connecttime );
                int hours, minutes = seconds / 60;
                if( minutes > 0 )
                {
                    seconds -= ( minutes * 60 );
                    hours = minutes / 60;
                    if( hours > 0 )
                        minutes -= ( hours * 60 );
                }
                else
                    hours = 0;
                sb.Append( String.Format( "#{0,-2} {1,-16}  {2}  {2}:{4,2}:{5,2}",
                    j + 1, client.name, (int)client.edict.v.frags, hours, minutes, seconds ) );
                sb.Append( "   " );
                sb.Append( client.netconnection.address );
                sb.Append( '\n' );
            }

            if( flag )
                Con.Print( sb.ToString() );
            else
                Server.ClientPrint( sb.ToString() );
        }

        /// <summary>
        /// Host_God_f
        /// Sets client to godmode
        /// </summary>
        private static void God_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Progs.GlobalStruct.deathmatch != 0 && !Host.HostClient.privileged )
                return;

            Server.Player.v.flags = (int)Server.Player.v.flags ^ EdictFlags.FL_GODMODE;
            if( ( (int)Server.Player.v.flags & EdictFlags.FL_GODMODE ) == 0 )
                Server.ClientPrint( "godmode OFF\n" );
            else
                Server.ClientPrint( "godmode ON\n" );
        }

        /// <summary>
        /// Host_Notarget_f
        /// </summary>
        private static void Notarget_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Progs.GlobalStruct.deathmatch != 0 && !Host.HostClient.privileged )
                return;

            Server.Player.v.flags = (int)Server.Player.v.flags ^ EdictFlags.FL_NOTARGET;
            if( ( (int)Server.Player.v.flags & EdictFlags.FL_NOTARGET ) == 0 )
                Server.ClientPrint( "notarget OFF\n" );
            else
                Server.ClientPrint( "notarget ON\n" );
        }

        /// <summary>
        /// Host_Noclip_f
        /// </summary>
        private static void Noclip_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Progs.GlobalStruct.deathmatch > 0 && !Host.HostClient.privileged )
                return;

            if( Server.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                Host.NoClipAngleHack = true;
                Server.Player.v.movetype = Movetypes.MOVETYPE_NOCLIP;
                Server.ClientPrint( "noclip ON\n" );
            }
            else
            {
                Host.NoClipAngleHack = false;
                Server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                Server.ClientPrint( "noclip OFF\n" );
            }
        }

        /// <summary>
        /// Host_Fly_f
        /// Sets client to flymode
        /// </summary>
        private static void Fly_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Progs.GlobalStruct.deathmatch > 0 && !Host.HostClient.privileged )
                return;

            if( Server.Player.v.movetype != Movetypes.MOVETYPE_FLY )
            {
                Server.Player.v.movetype = Movetypes.MOVETYPE_FLY;
                Server.ClientPrint( "flymode ON\n" );
            }
            else
            {
                Server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                Server.ClientPrint( "flymode OFF\n" );
            }
        }

        /// <summary>
        /// Host_Ping_f
        /// </summary>
        private static void Ping_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            Server.ClientPrint( "Client ping times:\n" );
            for( int i = 0; i < Server.svs.maxclients; i++ )
            {
                client_t client = Server.svs.clients[i];
                if( !client.active )
                    continue;
                float total = 0;
                for( int j = 0; j < Server.NUM_PING_TIMES; j++ )
                    total += client.ping_times[j];
                total /= Server.NUM_PING_TIMES;
                Server.ClientPrint( "{0,4} {1}\n", (int)( total * 1000 ), client.name );
            }
        }

        // Host_Map_f
        //
        // handle a
        // map <servername>
        // command from the console.  Active clients are kicked off.
        private static void Map_f()
        {
            if( Cmd.Source != cmd_source_t.src_command )
                return;

            Client.cls.demonum = -1;		// stop demo loop in case this fails

            Client.Disconnect();
            ShutdownServer( false );

            Key.Destination = keydest_t.key_game;			// remove console or menu
            Scr.BeginLoadingPlaque();

            Client.cls.mapstring = Cmd.JoinArgv() + "\n";

            Server.svs.serverflags = 0;			// haven't completed an episode yet
            string name = Cmd.Argv( 1 );
            Server.SpawnServer( name );

            if( !Server.IsActive )
                return;

            if( Client.cls.state != cactive_t.ca_dedicated )
            {
                Client.cls.spawnparms = Cmd.JoinArgv();
                Cmd.ExecuteString( "connect local", cmd_source_t.src_command );
            }
        }

        /// <summary>
        /// Host_Changelevel_f
        /// Goes to a new map, taking all clients along
        /// </summary>
        private static void Changelevel_f()
        {
            if( Cmd.Argc != 2 )
            {
                Con.Print( "changelevel <levelname> : continue game on a new level\n" );
                return;
            }
            if( !Server.sv.active || Client.cls.demoplayback )
            {
                Con.Print( "Only the server may changelevel\n" );
                return;
            }
            Server.SaveSpawnparms();
            string level = Cmd.Argv( 1 );
            Server.SpawnServer( level );
        }

        // Host_Restart_f
        //
        // Restarts the current server for a dead player
        private static void Restart_f()
        {
            if( Client.cls.demoplayback || !Server.IsActive )
                return;

            if( Cmd.Source != cmd_source_t.src_command )
                return;

            string mapname = Server.sv.name; // must copy out, because it gets cleared
                                             // in sv_spawnserver
            Server.SpawnServer( mapname );
        }

        /// <summary>
        /// Host_Reconnect_f
        /// This command causes the client to wait for the signon messages again.
        /// This is sent just before a server changes levels
        /// </summary>
        private static void Reconnect_f()
        {
            Scr.BeginLoadingPlaque();
            Client.cls.signon = 0;		// need new connection messages
        }

        /// <summary>
        /// Host_Connect_f
        /// User command to connect to server
        /// </summary>
        private static void Connect_f()
        {
            Client.cls.demonum = -1;		// stop demo loop in case this fails
            if( Client.cls.demoplayback )
            {
                Client.StopPlayback();
                Client.Disconnect();
            }
            string name = Cmd.Argv( 1 );
            Client.EstablishConnection( name );
            Reconnect_f();
        }

        /// <summary>
        /// Host_SavegameComment
        /// Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current
        /// </summary>
        private static string SavegameComment()
        {
            string result = String.Format( "{0} kills:{1,3}/{2,3}", Client.cl.levelname,
                Client.cl.stats[QStats.STAT_MONSTERS], Client.cl.stats[QStats.STAT_TOTALMONSTERS] );

            // convert space to _ to make stdio happy
            result = result.Replace( ' ', '_' );

            if( result.Length < QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.PadRight( QDef.SAVEGAME_COMMENT_LENGTH - 1, '_' );

            if( result.Length > QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.Remove( QDef.SAVEGAME_COMMENT_LENGTH - 2 );

            return result + '\0';
        }

        /// <summary>
        /// Host_Savegame_f
        /// </summary>
        private static void Savegame_f()
        {
            if( Cmd.Source != cmd_source_t.src_command )
                return;

            if( !Server.sv.active )
            {
                Con.Print( "Not playing a local game.\n" );
                return;
            }

            if( Client.cl.intermission != 0 )
            {
                Con.Print( "Can't save in intermission.\n" );
                return;
            }

            if( Server.svs.maxclients != 1 )
            {
                Con.Print( "Can't save multiplayer games.\n" );
                return;
            }

            if( Cmd.Argc != 2 )
            {
                Con.Print( "save <savename> : save a game\n" );
                return;
            }

            if( Cmd.Argv( 1 ).Contains( ".." ) )
            {
                Con.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            for( int i = 0; i < Server.svs.maxclients; i++ )
            {
                if( Server.svs.clients[i].active && ( Server.svs.clients[i].edict.v.health <= 0 ) )
                {
                    Con.Print( "Can't savegame with a dead player\n" );
                    return;
                }
            }

            string name = Path.ChangeExtension( Path.Combine( Common.GameDir, Cmd.Argv( 1 ) ), ".sav" );

            Con.Print( "Saving game to {0}...\n", name );
            FileStream fs = Sys.FileOpenWrite( name, true );
            if( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }
            using( StreamWriter writer = new StreamWriter( fs, Encoding.ASCII ) )
            {
                writer.WriteLine( SAVEGAME_VERSION );
                writer.WriteLine( SavegameComment() );

                for( int i = 0; i < Server.NUM_SPAWN_PARMS; i++ )
                    writer.WriteLine( Server.svs.clients[0].spawn_parms[i].ToString( "F6",
                        CultureInfo.InvariantCulture.NumberFormat ) );

                writer.WriteLine( Host.CurrentSkill );
                writer.WriteLine( Server.sv.name );
                writer.WriteLine( Server.sv.time.ToString( "F6",
                    CultureInfo.InvariantCulture.NumberFormat ) );

                // write the light styles

                for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    if( !String.IsNullOrEmpty( Server.sv.lightstyles[i] ) )
                        writer.WriteLine( Server.sv.lightstyles[i] );
                    else
                        writer.WriteLine( "m" );
                }

                Progs.WriteGlobals( writer );
                for( int i = 0; i < Server.sv.num_edicts; i++ )
                {
                    Progs.WriteEdict( writer, Server.EdictNum( i ) );
                    writer.Flush();
                }
            }
            Con.Print( "done.\n" );
        }

        /// <summary>
        /// Host_Loadgame_f
        /// </summary>
        private static void Loadgame_f()
        {
            if( Cmd.Source != cmd_source_t.src_command )
                return;

            if( Cmd.Argc != 2 )
            {
                Con.Print( "load <savename> : load a game\n" );
                return;
            }

            Client.cls.demonum = -1;		// stop demo loop in case this fails

            string name = Path.ChangeExtension( Path.Combine( Common.GameDir, Cmd.Argv( 1 ) ), ".sav" );

            // we can't call SCR_BeginLoadingPlaque, because too much stack space has
            // been used.  The menu calls it before stuffing loadgame command
            //	SCR_BeginLoadingPlaque ();

            Con.Print( "Loading game from {0}...\n", name );
            FileStream fs = Sys.FileOpenRead( name );
            if( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }

            using( StreamReader reader = new StreamReader( fs, Encoding.ASCII ) )
            {
                string line = reader.ReadLine();
                int version = Common.atoi( line );
                if( version != SAVEGAME_VERSION )
                {
                    Con.Print( "Savegame is version {0}, not {1}\n", version, SAVEGAME_VERSION );
                    return;
                }
                line = reader.ReadLine();

                float[] spawn_parms = new float[Server.NUM_SPAWN_PARMS];
                for( int i = 0; i < spawn_parms.Length; i++ )
                {
                    line = reader.ReadLine();
                    spawn_parms[i] = Common.atof( line );
                }
                // this silliness is so we can load 1.06 save files, which have float skill values
                line = reader.ReadLine();
                float tfloat = Common.atof( line );
                Host.CurrentSkill = (int)( tfloat + 0.1 );
                Cvar.Set( "skill", (float)Host.CurrentSkill );

                string mapname = reader.ReadLine();
                line = reader.ReadLine();
                float time = Common.atof( line );

                Client.Disconnect_f();
                Server.SpawnServer( mapname );

                if( !Server.sv.active )
                {
                    Con.Print( "Couldn't load map\n" );
                    return;
                }
                Server.sv.paused = true;		// pause until all clients connect
                Server.sv.loadgame = true;

                // load the light styles

                for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    line = reader.ReadLine();
                    Server.sv.lightstyles[i] = line;
                }

                // load the edicts out of the savegame file
                int entnum = -1;		// -1 is the globals
                StringBuilder sb = new StringBuilder( 32768 );
                while( !reader.EndOfStream )
                {
                    line = reader.ReadLine();
                    if( line == null )
                        Sys.Error( "EOF without closing brace" );

                    sb.AppendLine( line );
                    int idx = line.IndexOf( '}' );
                    if( idx != -1 )
                    {
                        int length = 1 + sb.Length - ( line.Length - idx );
                        string data = Common.Parse( sb.ToString( 0, length ) );
                        if( String.IsNullOrEmpty( Common.Token ) )
                            break; // end of file
                        if( Common.Token != "{" )
                            Sys.Error( "First token isn't a brace" );

                        if( entnum == -1 )
                        {
                            // parse the global vars
                            Progs.ParseGlobals( data );
                        }
                        else
                        {
                            // parse an edict
                            edict_t ent = Server.EdictNum( entnum );
                            ent.Clear();
                            Progs.ParseEdict( data, ent );

                            // link it into the bsp tree
                            if( !ent.free )
                                Server.LinkEdict( ent, false );
                        }

                        entnum++;
                        sb.Remove( 0, length );
                    }
                }

                Server.sv.num_edicts = entnum;
                Server.sv.time = time;

                for( int i = 0; i < Server.NUM_SPAWN_PARMS; i++ )
                    Server.svs.clients[0].spawn_parms[i] = spawn_parms[i];
            }

            if( Client.cls.state != cactive_t.ca_dedicated )
            {
                Client.EstablishConnection( "local" );
                Reconnect_f();
            }
        }

        // Host_Name_f
        private static void Name_f()
        {
            if( Cmd.Argc == 1 )
            {
                Con.Print( "\"name\" is \"{0}\"\n", Client.Name );
                return;
            }

            string newName;
            if( Cmd.Argc == 2 )
                newName = Cmd.Argv( 1 );
            else
                newName = Cmd.Args;

            if( newName.Length > 16 )
                newName = newName.Remove( 15 );

            if( Cmd.Source == cmd_source_t.src_command )
            {
                if( Client.Name == newName )
                    return;
                Cvar.Set( "_cl_name", newName );
                if( Client.cls.state == cactive_t.ca_connected )
                    Cmd.ForwardToServer();
                return;
            }

            if( !String.IsNullOrEmpty( Host.HostClient.name ) && Host.HostClient.name != "unconnected" )
                if( Host.HostClient.name != newName )
                    Con.Print( "{0} renamed to {1}\n", Host.HostClient.name, newName );

            Host.HostClient.name = newName;
            Host.HostClient.edict.v.netname = Progs.NewString( newName );

            // send notification to all clients
            MsgWriter msg = Server.sv.reliable_datagram;
            msg.WriteByte( Protocol.svc_updatename );
            msg.WriteByte( Host.ClientNum );
            msg.WriteString( newName );
        }

        // Host_Version_f
        private static void Version_f()
        {
            Con.Print( "Version {0}\n", QDef.VERSION );
            Con.Print( "Exe hash code: {0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetHashCode() );
        }

        /// <summary>
        /// Host_Say
        /// </summary>
        private static void Say( bool teamonly )
        {
            bool fromServer = false;
            if( Cmd.Source == cmd_source_t.src_command )
            {
                if( Client.cls.state == cactive_t.ca_dedicated )
                {
                    fromServer = true;
                    teamonly = false;
                }
                else
                {
                    Cmd.ForwardToServer();
                    return;
                }
            }

            if( Cmd.Argc < 2 )
                return;

            client_t save = Host.HostClient;

            string p = Cmd.Args;
            // remove quotes if present
            if( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            // turn on color set 1
            string text;
            if( !fromServer )
                text = (char)1 + save.name + ": ";
            else
                text = (char)1 + "<" + Net.HostName + "> ";

            text += p + "\n";

            for( int j = 0; j < Server.svs.maxclients; j++ )
            {
                client_t client = Server.svs.clients[j];
                if( client == null || !client.active || !client.spawned )
                    continue;
                if( Host.TeamPlay != 0 && teamonly && client.edict.v.team != save.edict.v.team )
                    continue;
                Host.HostClient = client;
                Server.ClientPrint( text );
            }
            Host.HostClient = save;
        }

        // Host_Say_f
        private static void Say_f()
        {
            Say( false );
        }

        // Host_Say_Team_f
        private static void Say_Team_f()
        {
            Say( true );
        }

        // Host_Tell_f
        private static void Tell_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Cmd.Argc < 3 )
                return;

            string text = Host.HostClient.name + ": ";
            string p = Cmd.Args;

            // remove quotes if present
            if( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            text += p + "\n";

            client_t save = Host.HostClient;
            for( int j = 0; j < Server.svs.maxclients; j++ )
            {
                client_t client = Server.svs.clients[j];
                if( !client.active || !client.spawned )
                    continue;
                if( client.name == Cmd.Argv( 1 ) )
                    continue;
                Host.HostClient = client;
                Server.ClientPrint( text );
                break;
            }
            Host.HostClient = save;
        }

        // Host_Color_f
        private static void Color_f()
        {
            if( Cmd.Argc == 1 )
            {
                Con.Print( "\"color\" is \"{0} {1}\"\n", ( (int)Client.Color ) >> 4, ( (int)Client.Color ) & 0x0f );
                Con.Print( "color <0-13> [0-13]\n" );
                return;
            }

            int top, bottom;
            if( Cmd.Argc == 2 )
                top = bottom = Common.atoi( Cmd.Argv( 1 ) );
            else
            {
                top = Common.atoi( Cmd.Argv( 1 ) );
                bottom = Common.atoi( Cmd.Argv( 2 ) );
            }

            top &= 15;
            if( top > 13 )
                top = 13;
            bottom &= 15;
            if( bottom > 13 )
                bottom = 13;

            int playercolor = top * 16 + bottom;

            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cvar.Set( "_cl_color", playercolor );
                if( Client.cls.state == cactive_t.ca_connected )
                    Cmd.ForwardToServer();
                return;
            }

            Host.HostClient.colors = playercolor;
            Host.HostClient.edict.v.team = bottom + 1;

            // send notification to all clients
            MsgWriter msg = Server.sv.reliable_datagram;
            msg.WriteByte( Protocol.svc_updatecolors );
            msg.WriteByte( Host.ClientNum );
            msg.WriteByte( Host.HostClient.colors );
        }

        /// <summary>
        /// Host_Kill_f
        /// </summary>
        private static void Kill_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Server.Player.v.health <= 0 )
            {
                Server.ClientPrint( "Can't suicide -- allready dead!\n" );
                return;
            }

            Progs.GlobalStruct.time = (float)Server.sv.time;
            Progs.GlobalStruct.self = Server.EdictToProg( Server.Player );
            Progs.Execute( Progs.GlobalStruct.ClientKill );
        }

        /// <summary>
        /// Host_Pause_f
        /// </summary>
        private static void Pause_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }
            if( _Pausable.Value == 0 )
                Server.ClientPrint( "Pause not allowed.\n" );
            else
            {
                Server.sv.paused = !Server.sv.paused;

                if( Server.sv.paused )
                {
                    Server.BroadcastPrint( "{0} paused the game\n", Progs.GetString( Server.Player.v.netname ) );
                }
                else
                {
                    Server.BroadcastPrint( "{0} unpaused the game\n", Progs.GetString( Server.Player.v.netname ) );
                }

                // send notification to all clients
                Server.sv.reliable_datagram.WriteByte( Protocol.svc_setpause );
                Server.sv.reliable_datagram.WriteByte( Server.sv.paused ? 1 : 0 );
            }
        }

        /// <summary>
        /// Host_PreSpawn_f
        /// </summary>
        private static void PreSpawn_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "prespawn is not valid from the console\n" );
                return;
            }

            if( Host.HostClient.spawned )
            {
                Con.Print( "prespawn not valid -- allready spawned\n" );
                return;
            }

            MsgWriter msg = Host.HostClient.message;
            msg.Write( Server.sv.signon.Data, 0, Server.sv.signon.Length );
            msg.WriteByte( Protocol.svc_signonnum );
            msg.WriteByte( 2 );
            Host.HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Spawn_f
        /// </summary>
        private static void Spawn_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "spawn is not valid from the console\n" );
                return;
            }

            if( Host.HostClient.spawned )
            {
                Con.Print( "Spawn not valid -- allready spawned\n" );
                return;
            }

            edict_t ent;

            // run the entrance script
            if( Server.sv.loadgame )
            {
                // loaded games are fully inited allready
                // if this is the last client to be connected, unpause
                Server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = Host.HostClient.edict;

                ent.Clear(); //memset(&ent.v, 0, progs.entityfields * 4);
                ent.v.colormap = Server.NumForEdict( ent );
                ent.v.team = ( Host.HostClient.colors & 15 ) + 1;
                ent.v.netname = Progs.NewString( Host.HostClient.name );

                // copy spawn parms out of the client_t
                Progs.GlobalStruct.SetParams( Host.HostClient.spawn_parms );

                // call the spawn function

                Progs.GlobalStruct.time = (float)Server.sv.time;
                Progs.GlobalStruct.self = Server.EdictToProg( Server.Player );
                Progs.Execute( Progs.GlobalStruct.ClientConnect );

                if( ( Sys.GetFloatTime() - Host.HostClient.netconnection.connecttime ) <= Server.sv.time )
                    Con.DPrint( "{0} entered the game\n", Host.HostClient.name );

                Progs.Execute( Progs.GlobalStruct.PutClientInServer );
            }

            // send all current names, colors, and frag counts
            MsgWriter msg = Host.HostClient.message;
            msg.Clear();

            // send time of update
            msg.WriteByte( Protocol.svc_time );
            msg.WriteFloat( (float)Server.sv.time );

            for( int i = 0; i < Server.svs.maxclients; i++ )
            {
                client_t client = Server.svs.clients[i];
                msg.WriteByte( Protocol.svc_updatename );
                msg.WriteByte( i );
                msg.WriteString( client.name );
                msg.WriteByte( Protocol.svc_updatefrags );
                msg.WriteByte( i );
                msg.WriteShort( client.old_frags );
                msg.WriteByte( Protocol.svc_updatecolors );
                msg.WriteByte( i );
                msg.WriteByte( client.colors );
            }

            // send all current light styles
            for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
            {
                msg.WriteByte( Protocol.svc_lightstyle );
                msg.WriteByte( (char)i );
                msg.WriteString( Server.sv.lightstyles[i] );
            }

            //
            // send some stats
            //
            msg.WriteByte( Protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_TOTALSECRETS );
            msg.WriteLong( (int)Progs.GlobalStruct.total_secrets );

            msg.WriteByte( Protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_TOTALMONSTERS );
            msg.WriteLong( (int)Progs.GlobalStruct.total_monsters );

            msg.WriteByte( Protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_SECRETS );
            msg.WriteLong( (int)Progs.GlobalStruct.found_secrets );

            msg.WriteByte( Protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_MONSTERS );
            msg.WriteLong( (int)Progs.GlobalStruct.killed_monsters );

            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the client to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = Server.EdictNum( 1 + Host.ClientNum );
            msg.WriteByte( Protocol.svc_setangle );
            msg.WriteAngle( ent.v.angles.x );
            msg.WriteAngle( ent.v.angles.y );
            msg.WriteAngle( 0 );

            Server.WriteClientDataToMessage( Server.Player, Host.HostClient.message );

            msg.WriteByte( Protocol.svc_signonnum );
            msg.WriteByte( 3 );
            Host.HostClient.sendsignon = true;
        }

        // Host_Begin_f
        private static void Begin_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "begin is not valid from the console\n" );
                return;
            }

            Host.HostClient.spawned = true;
        }

        /// <summary>
        /// Host_Kick_f
        /// Kicks a user off of the server
        /// </summary>
        private static void Kick_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                if( !Server.sv.active )
                {
                    Cmd.ForwardToServer();
                    return;
                }
            }
            else if( Progs.GlobalStruct.deathmatch != 0 && !Host.HostClient.privileged )
                return;

            client_t save = Host.HostClient;
            bool byNumber = false;
            int i;
            if( Cmd.Argc > 2 && Cmd.Argv( 1 ) == "#" )
            {
                i = (int)Common.atof( Cmd.Argv( 2 ) ) - 1;
                if( i < 0 || i >= Server.svs.maxclients )
                    return;
                if( !Server.svs.clients[i].active )
                    return;

                Host.HostClient = Server.svs.clients[i];
                byNumber = true;
            }
            else
            {
                for( i = 0; i < Server.svs.maxclients; i++ )
                {
                    Host.HostClient = Server.svs.clients[i];
                    if( !Host.HostClient.active )
                        continue;
                    if( Common.SameText( Host.HostClient.name, Cmd.Argv( 1 ) ) )
                        break;
                }
            }

            if( i < Server.svs.maxclients )
            {
                string who;
                if( Cmd.Source == cmd_source_t.src_command )
                    if( Client.cls.state == cactive_t.ca_dedicated )
                        who = "Console";
                    else
                        who = Client.Name;
                else
                    who = save.name;

                // can't kick yourself!
                if( Host.HostClient == save )
                    return;

                string message = null;
                if( Cmd.Argc > 2 )
                {
                    message = Common.Parse( Cmd.Args );
                    if( byNumber )
                    {
                        message = message.Substring( 1 ); // skip the #
                        message = message.Trim(); // skip white space
                        message = message.Substring( Cmd.Argv( 2 ).Length );	// skip the number
                    }
                    message = message.Trim();
                }
                if( !String.IsNullOrEmpty( message ) )
                    Server.ClientPrint( "Kicked by {0}: {1}\n", who, message );
                else
                    Server.ClientPrint( "Kicked by {0}\n", who );
                Server.DropClient( false );
            }

            Host.HostClient = save;
        }

        /// <summary>
        /// Host_Give_f
        /// </summary>
        private static void Give_f()
        {
            if( Cmd.Source == cmd_source_t.src_command )
            {
                Cmd.ForwardToServer();
                return;
            }

            if( Progs.GlobalStruct.deathmatch != 0 && !Host.HostClient.privileged )
                return;

            string t = Cmd.Argv( 1 );
            int v = Common.atoi( Cmd.Argv( 2 ) );

            if( String.IsNullOrEmpty( t ) )
                return;

            switch( t[0] )
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    // MED 01/04/97 added hipnotic give stuff
                    if( Common.GameKind == GameKind.Hipnotic )
                    {
                        if( t[0] == '6' )
                        {
                            if( t[1] == 'a' )
                                Server.Player.v.items = (int)Server.Player.v.items | QItems.HIT_PROXIMITY_GUN;
                            else
                                Server.Player.v.items = (int)Server.Player.v.items | QItems.IT_GRENADE_LAUNCHER;
                        }
                        else if( t[0] == '9' )
                            Server.Player.v.items = (int)Server.Player.v.items | QItems.HIT_LASER_CANNON;
                        else if( t[0] == '0' )
                            Server.Player.v.items = (int)Server.Player.v.items | QItems.HIT_MJOLNIR;
                        else if( t[0] >= '2' )
                            Server.Player.v.items = (int)Server.Player.v.items | ( QItems.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    else
                    {
                        if( t[0] >= '2' )
                            Server.Player.v.items = (int)Server.Player.v.items | ( QItems.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    break;

                case 's':
                    if( Common.GameKind == GameKind.Rogue )
                        Progs.SetEdictFieldFloat( Server.Player, "ammo_shells1", v );

                    Server.Player.v.ammo_shells = v;
                    break;

                case 'n':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_nails1", v ) )
                            if( Server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_nails = v;
                    }
                    else
                        Server.Player.v.ammo_nails = v;
                    break;

                case 'l':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_lava_nails", v ) )
                            if( Server.Player.v.weapon > QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_nails = v;
                    }
                    break;

                case 'r':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_rockets1", v ) )
                            if( Server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_rockets = v;
                    }
                    else
                    {
                        Server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'm':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_multi_rockets", v ) )
                            if( Server.Player.v.weapon > QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'h':
                    Server.Player.v.health = v;
                    break;

                case 'c':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_cells1", v ) )
                            if( Server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_cells = v;
                    }
                    else
                    {
                        Server.Player.v.ammo_cells = v;
                    }
                    break;

                case 'p':
                    if( Common.GameKind == GameKind.Rogue )
                    {
                        if( Progs.SetEdictFieldFloat( Server.Player, "ammo_plasma", v ) )
                            if( Server.Player.v.weapon > QItems.IT_LIGHTNING )
                                Server.Player.v.ammo_cells = v;
                    }
                    break;
            }
        }

        private static edict_t FindViewthing()
        {
            for( int i = 0; i < Server.sv.num_edicts; i++ )
            {
                edict_t e = Server.EdictNum( i );
                if( Progs.GetString( e.v.classname ) == "viewthing" )
                    return e;
            }
            Con.Print( "No viewthing on map\n" );
            return null;
        }

        // Host_Viewmodel_f
        private static void Viewmodel_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = Mod.ForName( Cmd.Argv( 1 ), false );
            if( m == null )
            {
                Con.Print( "Can't load {0}\n", Cmd.Argv( 1 ) );
                return;
            }

            e.v.frame = 0;
            Client.cl.model_precache[(int)e.v.modelindex] = m;
        }

        /// <summary>
        /// Host_Viewframe_f
        /// </summary>
        private static void Viewframe_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = Client.cl.model_precache[(int)e.v.modelindex];

            int f = Common.atoi( Cmd.Argv( 1 ) );
            if( f >= m.numframes )
                f = m.numframes - 1;

            e.v.frame = f;
        }

        private static void PrintFrameName( model_t m, int frame )
        {
            aliashdr_t hdr = Mod.GetExtraData( m );
            if( hdr == null )
                return;

            Con.Print( "frame {0}: {1}\n", frame, hdr.frames[frame].name );
        }

        /// <summary>
        /// Host_Viewnext_f
        /// </summary>
        private static void Viewnext_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = Client.cl.model_precache[(int)e.v.modelindex];

            e.v.frame = e.v.frame + 1;
            if( e.v.frame >= m.numframes )
                e.v.frame = m.numframes - 1;

            PrintFrameName( m, (int)e.v.frame );
        }

        /// <summary>
        /// Host_Viewprev_f
        /// </summary>
        private static void Viewprev_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = Client.cl.model_precache[(int)e.v.modelindex];

            e.v.frame = e.v.frame - 1;
            if( e.v.frame < 0 )
                e.v.frame = 0;

            PrintFrameName( m, (int)e.v.frame );
        }

        // Host_Startdemos_f
        private static void Startdemos_f()
        {
            if( Client.cls.state == cactive_t.ca_dedicated )
            {
                if( !Server.sv.active )
                    Cbuf.AddText( "map start\n" );
                return;
            }

            int c = Cmd.Argc - 1;
            if( c > Client.MAX_DEMOS )
            {
                Con.Print( "Max {0} demos in demoloop\n", Client.MAX_DEMOS );
                c = Client.MAX_DEMOS;
            }
            Con.Print( "{0} demo(s) in loop\n", c );

            for( int i = 1; i < c + 1; i++ )
                Client.cls.demos[i - 1] = Common.Copy( Cmd.Argv( i ), Client.MAX_DEMONAME );

            if( !Server.sv.active && Client.cls.demonum != -1 && !Client.cls.demoplayback )
            {
                Client.cls.demonum = 0;
                Client.NextDemo();
            }
            else
                Client.cls.demonum = -1;
        }

        /// <summary>
        /// Host_Demos_f
        /// Return to looping demos
        /// </summary>
        private static void Demos_f()
        {
            if( Client.cls.state == cactive_t.ca_dedicated )
                return;
            if( Client.cls.demonum == -1 )
                Client.cls.demonum = 1;
            Client.Disconnect_f();
            Client.NextDemo();
        }

        /// <summary>
        /// Host_Stopdemo_f
        /// Return to looping demos
        /// </summary>
        private static void Stopdemo_f()
        {
            if( Client.cls.state == cactive_t.ca_dedicated )
                return;
            if( !Client.cls.demoplayback )
                return;
            Client.StopPlayback();
            Client.Disconnect();
        }
    }
}
