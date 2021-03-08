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

using System;
using System.Globalization;
using System.IO;
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Game.Data.Models;
using SharpQuake.Rendering.UI;

namespace SharpQuake
{
	public partial class Host
    {
        public UInt32 FPSCounter = 0;
        public UInt32 FPS = 0;
        public DateTime LastFPSUpdate;

        public Boolean ShowFPS
        {
            get;
            private set;
        }

        public void ShowFPS_f( CommandMessage msg )
        {
            ShowFPS = !ShowFPS;
        }

        /// <summary>
        /// Host_Quit_f
        /// </summary>
        public void Quit_f( CommandMessage msg )
        {
            if ( Keyboard.Destination != KeyDestination.key_console && Client.cls.state != cactive_t.ca_dedicated )
            {
                Menus.Show( "menu_quit" );
                return;
            }

            Client.Disconnect( );
            ShutdownServer( false );
            MainWindow.Quit( );
        }

        /// <summary>
        /// Host_InitCommands
        /// </summary>
        private void InititaliseCommands( )
        {
            Commands.Add( "status", Status_f );
            Commands.Add( "quit", Quit_f );
            Commands.Add( "god", God_f );
            Commands.Add( "notarget", Notarget_f );
            Commands.Add( "fly", Fly_f );
            Commands.Add( "map", Map_f );
            Commands.Add( "restart", Restart_f );
            Commands.Add( "changelevel", Changelevel_f );
            Commands.Add( "connect", Connect_f );
            Commands.Add( "reconnect", Reconnect_f );
            Commands.Add( "name", Name_f );
            Commands.Add( "noclip", Noclip_f );
            Commands.Add( "version", Version_f );
            Commands.Add( "say", Say_f );
            Commands.Add( "say_team", Say_Team_f );
            Commands.Add( "tell", Tell_f );
            Commands.Add( "color", Color_f );
            Commands.Add( "kill", Kill_f );
            Commands.Add( "pause", Pause_f );
            Commands.Add( "spawn", Spawn_f );
            Commands.Add( "begin", Begin_f );
            Commands.Add( "prespawn", PreSpawn_f );
            Commands.Add( "kick", Kick_f );
            Commands.Add( "ping", Ping_f );
            Commands.Add( "load", Loadgame_f );
            Commands.Add( "save", Savegame_f );
            Commands.Add( "give", Give_f );

            Commands.Add( "startdemos", Startdemos_f );
            Commands.Add( "demos", Demos_f );
            Commands.Add( "stopdemo", Stopdemo_f );

            Commands.Add( "viewmodel", Viewmodel_f );
            Commands.Add( "viewframe", Viewframe_f );
            Commands.Add( "viewnext", Viewnext_f );
            Commands.Add( "viewprev", Viewprev_f );

            Commands.Add( "mcache", Model.Print );

            // New
            Commands.Add( "showfps", ShowFPS_f );
        }

        /// <summary>
        /// Host_Viewmodel_f
        /// </summary>
        /// <param name="msg"></param>
        private void Viewmodel_f( CommandMessage msg )
        {
            var e = FindViewthing( );
            if ( e == null )
                return;

            var m = Model.ForName( msg.Parameters[0], false, ModelType.Alias );
            if ( m == null )
            {
                Console.Print( "Can't load {0}\n", msg.Parameters[0] );
                return;
            }

            e.v.frame = 0;
            Client.cl.model_precache[( Int32 ) e.v.modelindex] = m;
        }

        /// <summary>
        /// Host_Viewframe_f
        /// </summary>
        private void Viewframe_f( CommandMessage msg )
        {
            var e = FindViewthing( );
            if ( e == null )
                return;

            var m = Client.cl.model_precache[( Int32 ) e.v.modelindex];

            var f = MathLib.atoi( msg.Parameters[0] );
            if ( f >= m.FrameCount )
                f = m.FrameCount - 1;

            e.v.frame = f;
        }

        private void PrintFrameName( ModelData m, Int32 frame )
        {
            var hdr = Model.GetExtraData( m );
            if ( hdr == null )
                return;

            Console.Print( "frame {0}: {1}\n", frame, hdr.frames[frame].name );
        }

        /// <summary>
        /// Host_Viewnext_f
        /// </summary>
        private void Viewnext_f( CommandMessage msg )
        {
            var e = FindViewthing( );
            if ( e == null )
                return;

            var m = Client.cl.model_precache[( Int32 ) e.v.modelindex];

            e.v.frame = e.v.frame + 1;
            if ( e.v.frame >= m.FrameCount )
                e.v.frame = m.FrameCount - 1;

            PrintFrameName( m, ( Int32 ) e.v.frame );
        }

        /// <summary>
        /// Host_Viewprev_f
        /// </summary>
        private void Viewprev_f( CommandMessage msg )
        {
            var e = FindViewthing( );
            if ( e == null )
                return;

            var m = Client.cl.model_precache[( Int32 ) e.v.modelindex];

            e.v.frame = e.v.frame - 1;
            if ( e.v.frame < 0 )
                e.v.frame = 0;

            PrintFrameName( m, ( Int32 ) e.v.frame );
        }

        /// <summary>
        /// Host_Status_f
        /// </summary>
        private void Status_f( CommandMessage msg )
        {
            var flag = true;
            if ( msg.Source == CommandSource.Command )
            {
                if ( !Server.sv.active )
                {
                    Client.ForwardToServer_f( msg );
                    return;
                }
            }
            else
                flag = false;

            var sb = new StringBuilder( 256 );
            sb.Append( String.Format( "host:    {0}\n", CVars.Get( "hostname" ).Get<String>( ) ) );
            sb.Append( String.Format( "version: {0:F2}\n", QDef.VERSION ) );
            if ( Network.TcpIpAvailable )
            {
                sb.Append( "tcp/ip:  " );
                sb.Append( Network.MyTcpIpAddress );
                sb.Append( '\n' );
            }

            sb.Append( "map:     " );
            sb.Append( Server.sv.name );
            sb.Append( '\n' );
            sb.Append( String.Format( "players: {0} active ({1} max)\n\n", Network.ActiveConnections, Server.svs.maxclients ) );
            for ( var j = 0; j < Server.svs.maxclients; j++ )
            {
                var client = Server.svs.clients[j];
                if ( !client.active )
                    continue;

                var seconds = ( Int32 ) ( Network.Time - client.netconnection.connecttime );
                Int32 hours, minutes = seconds / 60;
                if ( minutes > 0 )
                {
                    seconds -= ( minutes * 60 );
                    hours = minutes / 60;
                    if ( hours > 0 )
                        minutes -= ( hours * 60 );
                }
                else
                    hours = 0;
                sb.Append( String.Format( "#{0,-2} {1,-16}  {2}  {2}:{4,2}:{5,2}",
                    j + 1, client.name, ( Int32 ) client.edict.v.frags, hours, minutes, seconds ) );
                sb.Append( "   " );
                sb.Append( client.netconnection.address );
                sb.Append( '\n' );
            }

            if ( flag )
                Console.Print( sb.ToString( ) );
            else
                Server.ClientPrint( sb.ToString( ) );
        }

        /// <summary>
        /// Host_God_f
        /// Sets client to godmode
        /// </summary>
        private void God_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Programs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            Server.Player.v.flags = ( Int32 ) Server.Player.v.flags ^ EdictFlags.FL_GODMODE;
            if ( ( ( Int32 ) Server.Player.v.flags & EdictFlags.FL_GODMODE ) == 0 )
                Server.ClientPrint( "godmode OFF\n" );
            else
                Server.ClientPrint( "godmode ON\n" );
        }

        /// <summary>
        /// Host_Notarget_f
        /// </summary>
        private void Notarget_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Programs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            Server.Player.v.flags = ( Int32 ) Server.Player.v.flags ^ EdictFlags.FL_NOTARGET;
            if ( ( ( Int32 ) Server.Player.v.flags & EdictFlags.FL_NOTARGET ) == 0 )
                Server.ClientPrint( "notarget OFF\n" );
            else
                Server.ClientPrint( "notarget ON\n" );
        }

        /// <summary>
        /// Host_Noclip_f
        /// </summary>
        private void Noclip_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Programs.GlobalStruct.deathmatch > 0 && !HostClient.privileged )
                return;

            if ( Server.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                NoClipAngleHack = true;
                Server.Player.v.movetype = Movetypes.MOVETYPE_NOCLIP;
                Server.ClientPrint( "noclip ON\n" );
            }
            else
            {
                NoClipAngleHack = false;
                Server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                Server.ClientPrint( "noclip OFF\n" );
            }
        }

        /// <summary>
        /// Host_Fly_f
        /// Sets client to flymode
        /// </summary>
        private void Fly_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Programs.GlobalStruct.deathmatch > 0 && !HostClient.privileged )
                return;

            if ( Server.Player.v.movetype != Movetypes.MOVETYPE_FLY )
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
        private void Ping_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            Server.ClientPrint( "Client ping times:\n" );
            for ( var i = 0; i < Server.svs.maxclients; i++ )
            {
                var client = Server.svs.clients[i];
                if ( !client.active )
                    continue;
                Single total = 0;
                for ( var j = 0; j < ServerDef.NUM_PING_TIMES; j++ )
                    total += client.ping_times[j];
                total /= ServerDef.NUM_PING_TIMES;
                Server.ClientPrint( "{0,4} {1}\n", ( Int32 ) ( total * 1000 ), client.name );
            }
        }

        /// <summary>
        /// Host_Map_f
        ///
        /// handle a
        /// map [servername]
        /// command from the console.  Active clients are kicked off.
        /// </summary>
        /// <param name="msg"></param>
        private void Map_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            Client.cls.demonum = -1;		// stop demo loop in case this fails

            Client.Disconnect( );
            ShutdownServer( false );

            Keyboard.Destination = KeyDestination.key_game;			// remove console or menu
            Screen.BeginLoadingPlaque( );

            Client.cls.mapstring = msg.FullCommand + "\n";

            Server.svs.serverflags = 0;			// haven't completed an episode yet
            var name = msg.Parameters[0];
            Server.SpawnServer( name );

            if ( !Server.IsActive )
                return;

            if ( Client.cls.state != cactive_t.ca_dedicated )
            {
                Client.cls.spawnparms = msg.FullCommand;
                Commands.ExecuteString( "connect local", CommandSource.Command );
            }
        }

        /// <summary>
        /// Host_Changelevel_f
        /// Goes to a new map, taking all clients along
        /// </summary>
        private void Changelevel_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                Console.Print( "changelevel <levelname> : continue game on a new level\n" );
                return;
            }
            if ( !Server.sv.active || Client.cls.demoplayback )
            {
                Console.Print( "Only the server may changelevel\n" );
                return;
            }
            Server.SaveSpawnparms( );
            var level = msg.Parameters[0];
            Server.SpawnServer( level );
        }

        // Host_Restart_f
        //
        // Restarts the current server for a dead player
        private void Restart_f( CommandMessage msg )
        {
            if ( Client.cls.demoplayback || !Server.IsActive )
                return;

            if ( msg.Source != CommandSource.Command )
                return;

            var mapname = Server.sv.name; // must copy out, because it gets cleared
                                          // in sv_spawnserver
            Server.SpawnServer( mapname );
        }

        /// <summary>
        /// Host_Reconnect_f
        /// This command causes the client to wait for the signon messages again.
        /// This is sent just before a server changes levels
        /// </summary>
        private void Reconnect_f( CommandMessage msg )
        {
            Screen.BeginLoadingPlaque( );
            Client.cls.signon = 0;		// need new connection messages
        }

        /// <summary>
        /// Host_Connect_f
        /// User command to connect to server
        /// </summary>
        private void Connect_f( CommandMessage msg )
        {
            Client.cls.demonum = -1;		// stop demo loop in case this fails
            if ( Client.cls.demoplayback )
            {
                Client.StopPlayback( );
                Client.Disconnect( );
            }
            var name = msg.Parameters[0];
            Client.EstablishConnection( name );
            Reconnect_f( null );
        }

        /// <summary>
        /// Host_SavegameComment
        /// Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current
        /// </summary>
        private String SavegameComment( )
        {
            var result = String.Format( "{0} kills:{1,3}/{2,3}", Client.cl.levelname,
                Client.cl.stats[QStatsDef.STAT_MONSTERS], Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );

            // convert space to _ to make stdio happy
            result = result.Replace( ' ', '_' );

            if ( result.Length < QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.PadRight( QDef.SAVEGAME_COMMENT_LENGTH - 1, '_' );

            if ( result.Length > QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.Remove( QDef.SAVEGAME_COMMENT_LENGTH - 2 );

            return result + '\0';
        }

        /// <summary>
        /// Host_Savegame_f
        /// </summary>
        private void Savegame_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( !Server.sv.active )
            {
                Console.Print( "Not playing a local game.\n" );
                return;
            }

            if ( Client.cl.intermission != 0 )
            {
                Console.Print( "Can't save in intermission.\n" );
                return;
            }

            if ( Server.svs.maxclients != 1 )
            {
                Console.Print( "Can't save multiplayer games.\n" );
                return;
            }

            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                Console.Print( "save <savename> : save a game\n" );
                return;
            }

            if ( msg.Parameters[0].Contains( ".." ) )
            {
                Console.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            for ( var i = 0; i < Server.svs.maxclients; i++ )
            {
                if ( Server.svs.clients[i].active && ( Server.svs.clients[i].edict.v.health <= 0 ) )
                {
                    Console.Print( "Can't savegame with a dead player\n" );
                    return;
                }
            }

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, msg.Parameters[0] ), ".sav" );

            Console.Print( "Saving game to {0}...\n", name );
            var fs = FileSystem.OpenWrite( name, true );
            if ( fs == null )
            {
                Console.Print( "ERROR: couldn't open.\n" );
                return;
            }
            using ( var writer = new StreamWriter( fs, Encoding.ASCII ) )
            {
                writer.WriteLine( HostDef.SAVEGAME_VERSION );
                writer.WriteLine( SavegameComment( ) );

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                    writer.WriteLine( Server.svs.clients[0].spawn_parms[i].ToString( "F6",
                        CultureInfo.InvariantCulture.NumberFormat ) );

                writer.WriteLine( CurrentSkill );
                writer.WriteLine( Server.sv.name );
                writer.WriteLine( Server.sv.time.ToString( "F6",
                    CultureInfo.InvariantCulture.NumberFormat ) );

                // write the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    if ( !String.IsNullOrEmpty( Server.sv.lightstyles[i] ) )
                        writer.WriteLine( Server.sv.lightstyles[i] );
                    else
                        writer.WriteLine( "m" );
                }

                Programs.WriteGlobals( writer );
                for ( var i = 0; i < Server.sv.num_edicts; i++ )
                {
                    Programs.WriteEdict( writer, Server.EdictNum( i ) );
                    writer.Flush( );
                }
            }
            Console.Print( "done.\n" );
        }

        /// <summary>
        /// Host_Loadgame_f
        /// </summary>
        private void Loadgame_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                Console.Print( "load <savename> : load a game\n" );
                return;
            }

            Client.cls.demonum = -1;		// stop demo loop in case this fails

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, msg.Parameters[0] ), ".sav" );

            // we can't call SCR_BeginLoadingPlaque, because too much stack space has
            // been used.  The menu calls it before stuffing loadgame command
            //	SCR_BeginLoadingPlaque ();

            Console.Print( "Loading game from {0}...\n", name );
            var fs = FileSystem.OpenRead( name );
            if ( fs == null )
            {
                Console.Print( "ERROR: couldn't open.\n" );
                return;
            }

            using ( var reader = new StreamReader( fs, Encoding.ASCII ) )
            {
                var line = reader.ReadLine( );
                var version = MathLib.atoi( line );
                if ( version != HostDef.SAVEGAME_VERSION )
                {
                    Console.Print( "Savegame is version {0}, not {1}\n", version, HostDef.SAVEGAME_VERSION );
                    return;
                }
                line = reader.ReadLine( );

                var spawn_parms = new Single[ServerDef.NUM_SPAWN_PARMS];
                for ( var i = 0; i < spawn_parms.Length; i++ )
                {
                    line = reader.ReadLine( );
                    spawn_parms[i] = MathLib.atof( line );
                }
                // this silliness is so we can load 1.06 save files, which have float skill values
                line = reader.ReadLine( );
                var tfloat = MathLib.atof( line );
                CurrentSkill = ( Int32 ) ( tfloat + 0.1 );
                CVars.Set( "skill", ( Single ) CurrentSkill );

                var mapname = reader.ReadLine( );
                line = reader.ReadLine( );
                var time = MathLib.atof( line );

                Client.Disconnect_f( null );
                Server.SpawnServer( mapname );

                if ( !Server.sv.active )
                {
                    Console.Print( "Couldn't load map\n" );
                    return;
                }
                Server.sv.paused = true;		// pause until all clients connect
                Server.sv.loadgame = true;

                // load the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    line = reader.ReadLine( );
                    Server.sv.lightstyles[i] = line;
                }

                // load the edicts out of the savegame file
                var entnum = -1;		// -1 is the globals
                var sb = new StringBuilder( 32768 );
                while ( !reader.EndOfStream )
                {
                    line = reader.ReadLine( );
                    if ( line == null )
                        Utilities.Error( "EOF without closing brace" );

                    sb.AppendLine( line );
                    var idx = line.IndexOf( '}' );
                    if ( idx != -1 )
                    {
                        var length = 1 + sb.Length - ( line.Length - idx );
                        var data = Tokeniser.Parse( sb.ToString( 0, length ) );
                        if ( String.IsNullOrEmpty( Tokeniser.Token ) )
                            break; // end of file
                        if ( Tokeniser.Token != "{" )
                            Utilities.Error( "First token isn't a brace" );

                        if ( entnum == -1 )
                        {
                            // parse the global vars
                            Programs.ParseGlobals( data );
                        }
                        else
                        {
                            // parse an edict
                            var ent = Server.EdictNum( entnum );
                            ent.Clear( );
                            Programs.ParseEdict( data, ent );

                            // link it into the bsp tree
                            if ( !ent.free )
                                Server.LinkEdict( ent, false );
                        }

                        entnum++;
                        sb.Remove( 0, length );
                    }
                }

                Server.sv.num_edicts = entnum;
                Server.sv.time = time;

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                    Server.svs.clients[0].spawn_parms[i] = spawn_parms[i];
            }

            if ( Client.cls.state != cactive_t.ca_dedicated )
            {
                Client.EstablishConnection( "local" );
                Reconnect_f( null );
            }
        }

        /// <summary>
        /// Host_Name_f
        /// </summary>
        /// <param name="msg"></param>
        private void Name_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length <= 0 )
            {
                Console.Print( "\"name\" is \"{0}\"\n", Client.Name );
                return;
            }

            String newName;
            if ( msg.Parameters.Length == 1 )
                newName = msg.Parameters[0];
            else
                newName = msg.StringParameters;

            if ( newName.Length > 16 )
                newName = newName.Remove( 15 );

            if ( msg.Source == CommandSource.Command )
            {
                if ( Client.Name == newName )
                    return;
                CVars.Set( "_cl_name", newName );
                if ( Client.cls.state == cactive_t.ca_connected )
                    Client.ForwardToServer_f( msg );
                return;
            }

            if ( !String.IsNullOrEmpty( HostClient.name ) && HostClient.name != "unconnected" )
                if ( HostClient.name != newName )
                    Console.Print( "{0} renamed to {1}\n", HostClient.name, newName );

            HostClient.name = newName;
            HostClient.edict.v.netname = Programs.NewString( newName );

            // send notification to all clients
            var m = Server.sv.reliable_datagram;
            m.WriteByte( ProtocolDef.svc_updatename );
            m.WriteByte( ClientNum );
            m.WriteString( newName );
        }

        /// <summary>
        /// Host_Version_f
        /// </summary>
        /// <param name="msg"></param>
        private void Version_f( CommandMessage msg )
        {
            Console.Print( "Version {0}\n", QDef.VERSION );
            Console.Print( "Exe hash code: {0}\n", System.Reflection.Assembly.GetExecutingAssembly( ).GetHashCode( ) );
        }

        /// <summary>
        /// Host_Say
        /// </summary>
        private void Say( CommandMessage msg, Boolean teamonly )
        {
            var fromServer = false;
            if ( msg.Source == CommandSource.Command )
            {
                if ( Client.cls.state == cactive_t.ca_dedicated )
                {
                    fromServer = true;
                    teamonly = false;
                }
                else
                {
                    Client.ForwardToServer_f( msg );
                    return;
                }
            }

            if ( msg.Parameters == null || msg.Parameters.Length < 1 )
                return;

            var save = HostClient;

            var p = msg.StringParameters;
            // remove quotes if present
            if ( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            // turn on color set 1
            String text;
            if ( !fromServer )
                text = ( Char ) 1 + save.name + ": ";
            else
                text = ( Char ) 1 + "<" + Network.HostName + "> ";

            text += p + "\n";

            for ( var j = 0; j < Server.svs.maxclients; j++ )
            {
                var client = Server.svs.clients[j];
                if ( client == null || !client.active || !client.spawned )
                    continue;
                if ( Cvars.TeamPlay.Get<Int32>( ) != 0 && teamonly && client.edict.v.team != save.edict.v.team )
                    continue;
                HostClient = client;
                Server.ClientPrint( text );
            }
            HostClient = save;
        }

        /// <summary>
        /// Host_Say_f
        /// </summary>
        /// <param name="msg"></param>
        private void Say_f( CommandMessage msg )
        {
            Say( msg, false );
        }

        /// <summary>
        /// Host_Say_Team_f
        /// </summary>
        /// <param name="msg"></param>
        private void Say_Team_f( CommandMessage msg )
        {
            Say( msg, true );
        }

        /// <summary>
        /// Host_Tell_f
        /// </summary>
        /// <param name="msg"></param>
        private void Tell_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( msg.Parameters == null || msg.Parameters.Length < 2 )
                return;

            var text = HostClient.name + ": ";
            var p = msg.StringParameters;

            // remove quotes if present
            if ( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            text += p + "\n";

            var save = HostClient;
            for ( var j = 0; j < Server.svs.maxclients; j++ )
            {
                var client = Server.svs.clients[j];
                if ( !client.active || !client.spawned )
                    continue;
                if ( client.name == msg.Parameters[0] )
                    continue;
                HostClient = client;
                Server.ClientPrint( text );
                break;
            }
            HostClient = save;
        }

        /// <summary>
        /// Host_Color_f
        /// </summary>
        /// <param name="msg"></param>
        private void Color_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length <= 0 )
            {
                Console.Print( "\"color\" is \"{0} {1}\"\n", ( ( Int32 ) Client.Color ) >> 4, ( ( Int32 ) Client.Color ) & 0x0f );
                Console.Print( "color <0-13> [0-13]\n" );
                return;
            }

            Int32 top, bottom;
            if ( msg.Parameters?.Length == 1 )
                top = bottom = MathLib.atoi( msg.Parameters[0] );
            else
            {
                top = MathLib.atoi( msg.Parameters[0] );
                bottom = MathLib.atoi( msg.Parameters[1] );
            }

            top &= 15;
            if ( top > 13 )
                top = 13;
            bottom &= 15;
            if ( bottom > 13 )
                bottom = 13;

            var playercolor = top * 16 + bottom;

            if ( msg.Source == CommandSource.Command )
            {
                CVars.Set( "_cl_color", playercolor );
                if ( Client.cls.state == cactive_t.ca_connected )
                    Client.ForwardToServer_f( msg );
                return;
            }

            HostClient.colors = playercolor;
            HostClient.edict.v.team = bottom + 1;

            // send notification to all clients
            var m = Server.sv.reliable_datagram;
            m.WriteByte( ProtocolDef.svc_updatecolors );
            m.WriteByte( ClientNum );
            m.WriteByte( HostClient.colors );
        }

        /// <summary>
        /// Host_Kill_f
        /// </summary>
        private void Kill_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Server.Player.v.health <= 0 )
            {
                Server.ClientPrint( "Can't suicide -- allready dead!\n" );
                return;
            }

            Programs.GlobalStruct.time = ( Single ) Server.sv.time;
            Programs.GlobalStruct.self = Server.EdictToProg( Server.Player );
            Programs.Execute( Programs.GlobalStruct.ClientKill );
        }

        /// <summary>
        /// Host_Pause_f
        /// </summary>
        private void Pause_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }
            if ( !Cvars.Pausable.Get<Boolean>( ) )
                Server.ClientPrint( "Pause not allowed.\n" );
            else
            {
                Server.sv.paused = !Server.sv.paused;

                if ( Server.sv.paused )
                {
                    Server.BroadcastPrint( "{0} paused the game\n", Programs.GetString( Server.Player.v.netname ) );
                }
                else
                {
                    Server.BroadcastPrint( "{0} unpaused the game\n", Programs.GetString( Server.Player.v.netname ) );
                }

                // send notification to all clients
                Server.sv.reliable_datagram.WriteByte( ProtocolDef.svc_setpause );
                Server.sv.reliable_datagram.WriteByte( Server.sv.paused ? 1 : 0 );
            }
        }

        /// <summary>
        /// Host_PreSpawn_f
        /// </summary>
        private void PreSpawn_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Console.Print( "prespawn is not valid from the console\n" );
                return;
            }

            if ( HostClient.spawned )
            {
                Console.Print( "prespawn not valid -- allready spawned\n" );
                return;
            }

            var m = HostClient.message;
            m.Write( Server.sv.signon.Data, 0, Server.sv.signon.Length );
            m.WriteByte( ProtocolDef.svc_signonnum );
            m.WriteByte( 2 );
            HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Spawn_f
        /// </summary>
        private void Spawn_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Console.Print( "spawn is not valid from the console\n" );
                return;
            }

            if ( HostClient.spawned )
            {
                Console.Print( "Spawn not valid -- allready spawned\n" );
                return;
            }

            MemoryEdict ent;

            // run the entrance script
            if ( Server.sv.loadgame )
            {
                // loaded games are fully inited allready
                // if this is the last client to be connected, unpause
                Server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = HostClient.edict;

                ent.Clear( ); //memset(&ent.v, 0, Programs.entityfields * 4);
                ent.v.colormap = Server.NumForEdict( ent );
                ent.v.team = ( HostClient.colors & 15 ) + 1;
                ent.v.netname = Programs.NewString( HostClient.name );

                // copy spawn parms out of the client_t
                Programs.GlobalStruct.SetParams( HostClient.spawn_parms );

                // call the spawn function

                Programs.GlobalStruct.time = ( Single ) Server.sv.time;
                Programs.GlobalStruct.self = Server.EdictToProg( Server.Player );
                Programs.Execute( Programs.GlobalStruct.ClientConnect );

                if ( ( Timer.GetFloatTime( ) - HostClient.netconnection.connecttime ) <= Server.sv.time )
                    Console.DPrint( "{0} entered the game\n", HostClient.name );

                Programs.Execute( Programs.GlobalStruct.PutClientInServer );
            }

            // send all current names, colors, and frag counts
            var m = HostClient.message;
            m.Clear( );

            // send time of update
            m.WriteByte( ProtocolDef.svc_time );
            m.WriteFloat( ( Single ) Server.sv.time );

            for ( var i = 0; i < Server.svs.maxclients; i++ )
            {
                var client = Server.svs.clients[i];
                m.WriteByte( ProtocolDef.svc_updatename );
                m.WriteByte( i );
                m.WriteString( client.name );
                m.WriteByte( ProtocolDef.svc_updatefrags );
                m.WriteByte( i );
                m.WriteShort( client.old_frags );
                m.WriteByte( ProtocolDef.svc_updatecolors );
                m.WriteByte( i );
                m.WriteByte( client.colors );
            }

            // send all current light styles
            for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
            {
                m.WriteByte( ProtocolDef.svc_lightstyle );
                m.WriteByte( ( Char ) i );
                m.WriteString( Server.sv.lightstyles[i] );
            }

            //
            // send some stats
            //
            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_TOTALSECRETS );
            m.WriteLong( ( Int32 ) Programs.GlobalStruct.total_secrets );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_TOTALMONSTERS );
            m.WriteLong( ( Int32 ) Programs.GlobalStruct.total_monsters );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_SECRETS );
            m.WriteLong( ( Int32 ) Programs.GlobalStruct.found_secrets );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_MONSTERS );
            m.WriteLong( ( Int32 ) Programs.GlobalStruct.killed_monsters );

            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the client to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = Server.EdictNum( 1 + ClientNum );
            m.WriteByte( ProtocolDef.svc_setangle );
            m.WriteAngle( ent.v.angles.x );
            m.WriteAngle( ent.v.angles.y );
            m.WriteAngle( 0 );

            Server.WriteClientDataToMessage( Server.Player, HostClient.message );

            m.WriteByte( ProtocolDef.svc_signonnum );
            m.WriteByte( 3 );
            HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Begin_f
        /// </summary>
        /// <param name="msg"></param>
        private void Begin_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Console.Print( "begin is not valid from the console\n" );
                return;
            }

            HostClient.spawned = true;
        }

        /// <summary>
        /// Host_Kick_f
        /// Kicks a user off of the server
        /// </summary>
        private void Kick_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                if ( !Server.sv.active )
                {
                    Client.ForwardToServer_f( msg );
                    return;
                }
            }
            else if ( Programs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            var save = HostClient;
            var byNumber = false;
            Int32 i;
            if ( msg.Parameters?.Length > 1 && msg.Parameters[0] == "#" )
            {
                i = ( Int32 ) MathLib.atof( msg.Parameters[1] ) - 1;
                if ( i < 0 || i >= Server.svs.maxclients )
                    return;
                if ( !Server.svs.clients[i].active )
                    return;

                HostClient = Server.svs.clients[i];
                byNumber = true;
            }
            else
            {
                for ( i = 0; i < Server.svs.maxclients; i++ )
                {
                    HostClient = Server.svs.clients[i];
                    if ( !HostClient.active )
                        continue;
                    if ( Utilities.SameText( HostClient.name, msg.Parameters[0] ) )
                        break;
                }
            }

            if ( i < Server.svs.maxclients )
            {
                String who;
                if ( msg.Source == CommandSource.Command )
                    if ( Client.cls.state == cactive_t.ca_dedicated )
                        who = "Console";
                    else
                        who = Client.Name;
                else
                    who = save.name;

                // can't kick yourself!
                if ( HostClient == save )
                    return;

                String message = null;
                if ( msg.Parameters?.Length > 1 )
                {
                    message = Tokeniser.Parse( msg.StringParameters );
                    if ( byNumber )
                    {
                        message = message.Substring( 1 ); // skip the #
                        message = message.Trim( ); // skip white space
                        message = message.Substring( msg.Parameters[1].Length );	// skip the number
                    }
                    message = message.Trim( );
                }
                if ( !String.IsNullOrEmpty( message ) )
                    Server.ClientPrint( "Kicked by {0}: {1}\n", who, message );
                else
                    Server.ClientPrint( "Kicked by {0}\n", who );
                Server.DropClient( false );
            }

            HostClient = save;
        }

        /// <summary>
        /// Host_Give_f
        /// </summary>
        private void Give_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                Client.ForwardToServer_f( msg );
                return;
            }

            if ( Programs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            var t =  msg.Parameters[0];
            var v = MathLib.atoi(  msg.Parameters[1] );

            if ( String.IsNullOrEmpty( t ) )
                return;

            switch ( t[0] )
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
                    // CHANGE
                    // MED 01/04/97 added hipnotic give stuff
                    if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                    {
                        if ( t[0] == '6' )
                        {
                            if ( t[1] == 'a' )
                                Server.Player.v.items = ( Int32 ) Server.Player.v.items | QItemsDef.HIT_PROXIMITY_GUN;
                            else
                                Server.Player.v.items = ( Int32 ) Server.Player.v.items | QItemsDef.IT_GRENADE_LAUNCHER;
                        }
                        else if ( t[0] == '9' )
                            Server.Player.v.items = ( Int32 ) Server.Player.v.items | QItemsDef.HIT_LASER_CANNON;
                        else if ( t[0] == '0' )
                            Server.Player.v.items = ( Int32 ) Server.Player.v.items | QItemsDef.HIT_MJOLNIR;
                        else if ( t[0] >= '2' )
                            Server.Player.v.items = ( Int32 ) Server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    else
                    {
                        if ( t[0] >= '2' )
                            Server.Player.v.items = ( Int32 ) Server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    break;

                case 's':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        Programs.SetEdictFieldFloat( Server.Player, "ammo_shells1", v );

                    Server.Player.v.ammo_shells = v;
                    break;

                case 'n':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_nails1", v ) )
                            if ( Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_nails = v;
                    }
                    else
                        Server.Player.v.ammo_nails = v;
                    break;

                case 'l':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_lava_nails", v ) )
                            if ( Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_nails = v;
                    }
                    break;

                case 'r':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_rockets1", v ) )
                            if ( Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_rockets = v;
                    }
                    else
                    {
                        Server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'm':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_multi_rockets", v ) )
                            if ( Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'h':
                    Server.Player.v.health = v;
                    break;

                case 'c':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_cells1", v ) )
                            if ( Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_cells = v;
                    }
                    else
                    {
                        Server.Player.v.ammo_cells = v;
                    }
                    break;

                case 'p':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( Programs.SetEdictFieldFloat( Server.Player, "ammo_plasma", v ) )
                            if ( Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                Server.Player.v.ammo_cells = v;
                    }
                    break;
            }
        }

        private MemoryEdict FindViewthing( )
        {
            for ( var i = 0; i < Server.sv.num_edicts; i++ )
            {
                var e = Server.EdictNum( i );
                if ( Programs.GetString( e.v.classname ) == "viewthing" )
                    return e;
            }
            Console.Print( "No viewthing on map\n" );
            return null;
        }

        /// <summary>
        /// Host_Startdemos_f
        /// </summary>
        /// <param name="msg"></param>
        private void Startdemos_f( CommandMessage msg )
        {
            if ( Client.cls.state == cactive_t.ca_dedicated )
            {
                if ( !Server.sv.active )
                    Commands.Buffer.Append( "map start\n" );
                return;
            }

            var c = msg.Parameters.Length;
            if ( c > ClientDef.MAX_DEMOS )
            {
                Console.Print( "Max {0} demos in demoloop\n", ClientDef.MAX_DEMOS );
                c = ClientDef.MAX_DEMOS;
            }
            Console.Print( "{0} demo(s) in loop\n", c );

            for ( var i = 0; i < c; i++ )
                Client.cls.demos[i] = Utilities.Copy( msg.Parameters[i], ClientDef.MAX_DEMONAME );

            if ( !Server.sv.active && Client.cls.demonum != -1 && !Client.cls.demoplayback )
            {
                Client.cls.demonum = 0;
                Client.NextDemo( );
            }
            else
                Client.cls.demonum = -1;
        }

        /// <summary>
        /// Host_Demos_f
        /// Return to looping demos
        /// </summary>
        private void Demos_f( CommandMessage msg )
        {
            if ( Client.cls.state == cactive_t.ca_dedicated )
                return;
            if ( Client.cls.demonum == -1 )
                Client.cls.demonum = 1;
            Client.Disconnect_f( null );
            Client.NextDemo( );
        }

        /// <summary>
        /// Host_Stopdemo_f
        /// Return to looping demos
        /// </summary>
        private void Stopdemo_f( CommandMessage msg )
        {
            if ( Client.cls.state == cactive_t.ca_dedicated || !Client.cls.demoplayback )
                return;
            Client.StopPlayback( );
            Client.Disconnect( );
        }
    }
}
