using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public partial class Host
    {
        /// <summary>
        /// Host_Quit_f
        /// </summary>
        public void Quit_f( )
        {
            if ( Key.Destination != keydest_t.key_console && client.cls.state != cactive_t.ca_dedicated )
            {
                MenuBase.QuitMenu.Show( this );
                return;
            }
            client.Disconnect( );
            ShutdownServer( false );
            sys.Quit( );
            Dispose( );
        }

        // Host_InitCommands
        private void InititaliseCommands( )
        {
            Command.Add( "status", Status_f );
            Command.Add( "quit", Quit_f );
            Command.Add( "god", God_f );
            Command.Add( "notarget", Notarget_f );
            Command.Add( "fly", Fly_f );
            Command.Add( "map", Map_f );
            Command.Add( "restart", Restart_f );
            Command.Add( "changelevel", Changelevel_f );
            Command.Add( "connect", Connect_f );
            Command.Add( "reconnect", Reconnect_f );
            Command.Add( "name", Name_f );
            Command.Add( "noclip", Noclip_f );
            Command.Add( "version", Version_f );
            Command.Add( "say", Say_f );
            Command.Add( "say_team", Say_Team_f );
            Command.Add( "tell", Tell_f );
            Command.Add( "color", Color_f );
            Command.Add( "kill", Kill_f );
            Command.Add( "pause", Pause_f );
            Command.Add( "spawn", Spawn_f );
            Command.Add( "begin", Begin_f );
            Command.Add( "prespawn", PreSpawn_f );
            Command.Add( "kick", Kick_f );
            Command.Add( "ping", Ping_f );
            Command.Add( "load", Loadgame_f );
            Command.Add( "save", Savegame_f );
            Command.Add( "give", Give_f );

            Command.Add( "startdemos", Startdemos_f );
            Command.Add( "demos", Demos_f );
            Command.Add( "stopdemo", Stopdemo_f );

            Command.Add( "viewmodel", Viewmodel_f );
            Command.Add( "viewframe", Viewframe_f );
            Command.Add( "viewnext", Viewnext_f );
            Command.Add( "viewprev", Viewprev_f );

            Command.Add( "mcache", Mod.Print );
        }

        // Host_Viewmodel_f
        private void Viewmodel_f( )
        {
            MemoryEdict e = FindViewthing( );
            if ( e == null )
                return;

            Model m = Mod.ForName( Command.Argv( 1 ), false );
            if ( m == null )
            {
                Con.Print( "Can't load {0}\n", Command.Argv( 1 ) );
                return;
            }

            e.v.frame = 0;
            client.cl.model_precache[( Int32 ) e.v.modelindex] = m;
        }

        /// <summary>
        /// Host_Viewframe_f
        /// </summary>
        private void Viewframe_f( )
        {
            MemoryEdict e = FindViewthing( );
            if ( e == null )
                return;

            Model m = client.cl.model_precache[( Int32 ) e.v.modelindex];

            var f = MathLib.atoi( Command.Argv( 1 ) );
            if ( f >= m.numframes )
                f = m.numframes - 1;

            e.v.frame = f;
        }

        private static void PrintFrameName( Model m, Int32 frame )
        {
            aliashdr_t hdr = Mod.GetExtraData( m );
            if ( hdr == null )
                return;

            Con.Print( "frame {0}: {1}\n", frame, hdr.frames[frame].name );
        }

        /// <summary>
        /// Host_Viewnext_f
        /// </summary>
        private void Viewnext_f( )
        {
            MemoryEdict e = FindViewthing( );
            if ( e == null )
                return;

            Model m = client.cl.model_precache[( Int32 ) e.v.modelindex];

            e.v.frame = e.v.frame + 1;
            if ( e.v.frame >= m.numframes )
                e.v.frame = m.numframes - 1;

            PrintFrameName( m, ( Int32 ) e.v.frame );
        }

        /// <summary>
        /// Host_Viewprev_f
        /// </summary>
        private void Viewprev_f( )
        {
            MemoryEdict e = FindViewthing( );
            if ( e == null )
                return;

            Model m = client.cl.model_precache[( Int32 ) e.v.modelindex];

            e.v.frame = e.v.frame - 1;
            if ( e.v.frame < 0 )
                e.v.frame = 0;

            PrintFrameName( m, ( Int32 ) e.v.frame );
        }

        /// <summary>
        /// Host_Status_f
        /// </summary>
        private void Status_f( )
        {
            var flag = true;
            if ( Command.Source == CommandSource.src_command )
            {
                if ( !server.sv.active )
                {
                    Command.ForwardToServer( );
                    return;
                }
            }
            else
                flag = false;

            StringBuilder sb = new StringBuilder( 256 );
            sb.Append( String.Format( "host:    {0}\n", CVar.GetString( "hostname" ) ) );
            sb.Append( String.Format( "version: {0:F2}\n", QDef.VERSION ) );
            if ( net.TcpIpAvailable )
            {
                sb.Append( "tcp/ip:  " );
                sb.Append( net.MyTcpIpAddress );
                sb.Append( '\n' );
            }

            sb.Append( "map:     " );
            sb.Append( server.sv.name );
            sb.Append( '\n' );
            sb.Append( String.Format( "players: {0} active ({1} max)\n\n", net.ActiveConnections, server.svs.maxclients ) );
            for ( var j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if ( !client.active )
                    continue;

                var seconds = ( Int32 ) ( net.Time - client.netconnection.connecttime );
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
                Con.Print( sb.ToString( ) );
            else
                server.ClientPrint( sb.ToString( ) );
        }

        /// <summary>
        /// Host_God_f
        /// Sets client to godmode
        /// </summary>
        private void God_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( progs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            server.Player.v.flags = ( Int32 ) server.Player.v.flags ^ EdictFlags.FL_GODMODE;
            if ( ( ( Int32 ) server.Player.v.flags & EdictFlags.FL_GODMODE ) == 0 )
                server.ClientPrint( "godmode OFF\n" );
            else
                server.ClientPrint( "godmode ON\n" );
        }

        /// <summary>
        /// Host_Notarget_f
        /// </summary>
        private void Notarget_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( progs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            server.Player.v.flags = ( Int32 ) server.Player.v.flags ^ EdictFlags.FL_NOTARGET;
            if ( ( ( Int32 ) server.Player.v.flags & EdictFlags.FL_NOTARGET ) == 0 )
                server.ClientPrint( "notarget OFF\n" );
            else
                server.ClientPrint( "notarget ON\n" );
        }

        /// <summary>
        /// Host_Noclip_f
        /// </summary>
        private void Noclip_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( progs.GlobalStruct.deathmatch > 0 && !HostClient.privileged )
                return;

            if ( server.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                NoClipAngleHack = true;
                server.Player.v.movetype = Movetypes.MOVETYPE_NOCLIP;
                server.ClientPrint( "noclip ON\n" );
            }
            else
            {
                NoClipAngleHack = false;
                server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                server.ClientPrint( "noclip OFF\n" );
            }
        }

        /// <summary>
        /// Host_Fly_f
        /// Sets client to flymode
        /// </summary>
        private void Fly_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( progs.GlobalStruct.deathmatch > 0 && !HostClient.privileged )
                return;

            if ( server.Player.v.movetype != Movetypes.MOVETYPE_FLY )
            {
                server.Player.v.movetype = Movetypes.MOVETYPE_FLY;
                server.ClientPrint( "flymode ON\n" );
            }
            else
            {
                server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                server.ClientPrint( "flymode OFF\n" );
            }
        }

        /// <summary>
        /// Host_Ping_f
        /// </summary>
        private void Ping_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            server.ClientPrint( "Client ping times:\n" );
            for ( var i = 0; i < server.svs.maxclients; i++ )
            {
                client_t client = server.svs.clients[i];
                if ( !client.active )
                    continue;
                Single total = 0;
                for ( var j = 0; j < ServerDef.NUM_PING_TIMES; j++ )
                    total += client.ping_times[j];
                total /= ServerDef.NUM_PING_TIMES;
                server.ClientPrint( "{0,4} {1}\n", ( Int32 ) ( total * 1000 ), client.name );
            }
        }

        // Host_Map_f
        //
        // handle a
        // map <servername>
        // command from the console.  Active clients are kicked off.
        private void Map_f( )
        {
            if ( Command.Source != CommandSource.src_command )
                return;

            client.cls.demonum = -1;		// stop demo loop in case this fails

            client.Disconnect( );
            ShutdownServer( false );

            Key.Destination = keydest_t.key_game;			// remove console or menu
            Scr.BeginLoadingPlaque( );

            client.cls.mapstring = Command.JoinArgv( ) + "\n";

            server.svs.serverflags = 0;			// haven't completed an episode yet
            var name = Command.Argv( 1 );
            server.SpawnServer( name );

            if ( !server.IsActive )
                return;

            if ( client.cls.state != cactive_t.ca_dedicated )
            {
                client.cls.spawnparms = Command.JoinArgv( );
                Command.ExecuteString( "connect local", CommandSource.src_command );
            }
        }

        /// <summary>
        /// Host_Changelevel_f
        /// Goes to a new map, taking all clients along
        /// </summary>
        private void Changelevel_f( )
        {
            if ( Command.Argc != 2 )
            {
                Con.Print( "changelevel <levelname> : continue game on a new level\n" );
                return;
            }
            if ( !server.sv.active || client.cls.demoplayback )
            {
                Con.Print( "Only the server may changelevel\n" );
                return;
            }
            server.SaveSpawnparms( );
            var level = Command.Argv( 1 );
            server.SpawnServer( level );
        }

        // Host_Restart_f
        //
        // Restarts the current server for a dead player
        private void Restart_f( )
        {
            if ( client.cls.demoplayback || !server.IsActive )
                return;

            if ( Command.Source != CommandSource.src_command )
                return;

            var mapname = server.sv.name; // must copy out, because it gets cleared
                                          // in sv_spawnserver
            server.SpawnServer( mapname );
        }

        /// <summary>
        /// Host_Reconnect_f
        /// This command causes the client to wait for the signon messages again.
        /// This is sent just before a server changes levels
        /// </summary>
        private void Reconnect_f( )
        {
            Scr.BeginLoadingPlaque( );
            client.cls.signon = 0;		// need new connection messages
        }

        /// <summary>
        /// Host_Connect_f
        /// User command to connect to server
        /// </summary>
        private void Connect_f( )
        {
            client.cls.demonum = -1;		// stop demo loop in case this fails
            if ( client.cls.demoplayback )
            {
                client.StopPlayback( );
                client.Disconnect( );
            }
            var name = Command.Argv( 1 );
            client.EstablishConnection( name );
            Reconnect_f( );
        }

        /// <summary>
        /// Host_SavegameComment
        /// Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current
        /// </summary>
        private String SavegameComment( )
        {
            var result = String.Format( "{0} kills:{1,3}/{2,3}", client.cl.levelname,
                client.cl.stats[QStatsDef.STAT_MONSTERS], client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );

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
        private void Savegame_f( )
        {
            if ( Command.Source != CommandSource.src_command )
                return;

            if ( !server.sv.active )
            {
                Con.Print( "Not playing a local game.\n" );
                return;
            }

            if ( client.cl.intermission != 0 )
            {
                Con.Print( "Can't save in intermission.\n" );
                return;
            }

            if ( server.svs.maxclients != 1 )
            {
                Con.Print( "Can't save multiplayer games.\n" );
                return;
            }

            if ( Command.Argc != 2 )
            {
                Con.Print( "save <savename> : save a game\n" );
                return;
            }

            if ( Command.Argv( 1 ).Contains( ".." ) )
            {
                Con.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            for ( var i = 0; i < server.svs.maxclients; i++ )
            {
                if ( server.svs.clients[i].active && ( server.svs.clients[i].edict.v.health <= 0 ) )
                {
                    Con.Print( "Can't savegame with a dead player\n" );
                    return;
                }
            }

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, Command.Argv( 1 ) ), ".sav" );

            Con.Print( "Saving game to {0}...\n", name );
            FileStream fs = FileSystem.OpenWrite( name, true );
            if ( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }
            using ( StreamWriter writer = new StreamWriter( fs, Encoding.ASCII ) )
            {
                writer.WriteLine( HostDef.SAVEGAME_VERSION );
                writer.WriteLine( SavegameComment( ) );

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                    writer.WriteLine( server.svs.clients[0].spawn_parms[i].ToString( "F6",
                        CultureInfo.InvariantCulture.NumberFormat ) );

                writer.WriteLine( CurrentSkill );
                writer.WriteLine( server.sv.name );
                writer.WriteLine( server.sv.time.ToString( "F6",
                    CultureInfo.InvariantCulture.NumberFormat ) );

                // write the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    if ( !String.IsNullOrEmpty( server.sv.lightstyles[i] ) )
                        writer.WriteLine( server.sv.lightstyles[i] );
                    else
                        writer.WriteLine( "m" );
                }

                progs.WriteGlobals( writer );
                for ( var i = 0; i < server.sv.num_edicts; i++ )
                {
                    progs.WriteEdict( writer, server.EdictNum( i ) );
                    writer.Flush( );
                }
            }
            Con.Print( "done.\n" );
        }

        /// <summary>
        /// Host_Loadgame_f
        /// </summary>
        private void Loadgame_f( )
        {
            if ( Command.Source != CommandSource.src_command )
                return;

            if ( Command.Argc != 2 )
            {
                Con.Print( "load <savename> : load a game\n" );
                return;
            }

            client.cls.demonum = -1;		// stop demo loop in case this fails

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, Command.Argv( 1 ) ), ".sav" );

            // we can't call SCR_BeginLoadingPlaque, because too much stack space has
            // been used.  The menu calls it before stuffing loadgame command
            //	SCR_BeginLoadingPlaque ();

            Con.Print( "Loading game from {0}...\n", name );
            FileStream fs = FileSystem.OpenRead( name );
            if ( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }

            using ( StreamReader reader = new StreamReader( fs, Encoding.ASCII ) )
            {
                var line = reader.ReadLine( );
                var version = MathLib.atoi( line );
                if ( version != HostDef.SAVEGAME_VERSION )
                {
                    Con.Print( "Savegame is version {0}, not {1}\n", version, HostDef.SAVEGAME_VERSION );
                    return;
                }
                line = reader.ReadLine( );

                Single[] spawn_parms = new Single[ServerDef.NUM_SPAWN_PARMS];
                for ( var i = 0; i < spawn_parms.Length; i++ )
                {
                    line = reader.ReadLine( );
                    spawn_parms[i] = MathLib.atof( line );
                }
                // this silliness is so we can load 1.06 save files, which have float skill values
                line = reader.ReadLine( );
                var tfloat = MathLib.atof( line );
                CurrentSkill = ( Int32 ) ( tfloat + 0.1 );
                CVar.Set( "skill", ( Single ) CurrentSkill );

                var mapname = reader.ReadLine( );
                line = reader.ReadLine( );
                var time = MathLib.atof( line );

                client.Disconnect_f( );
                server.SpawnServer( mapname );

                if ( !server.sv.active )
                {
                    Con.Print( "Couldn't load map\n" );
                    return;
                }
                server.sv.paused = true;		// pause until all clients connect
                server.sv.loadgame = true;

                // load the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    line = reader.ReadLine( );
                    server.sv.lightstyles[i] = line;
                }

                // load the edicts out of the savegame file
                var entnum = -1;		// -1 is the globals
                StringBuilder sb = new StringBuilder( 32768 );
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
                            progs.ParseGlobals( data );
                        }
                        else
                        {
                            // parse an edict
                            MemoryEdict ent = server.EdictNum( entnum );
                            ent.Clear( );
                            progs.ParseEdict( data, ent );

                            // link it into the bsp tree
                            if ( !ent.free )
                                server.LinkEdict( ent, false );
                        }

                        entnum++;
                        sb.Remove( 0, length );
                    }
                }

                server.sv.num_edicts = entnum;
                server.sv.time = time;

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                    server.svs.clients[0].spawn_parms[i] = spawn_parms[i];
            }

            if ( client.cls.state != cactive_t.ca_dedicated )
            {
                client.EstablishConnection( "local" );
                Reconnect_f( );
            }
        }

        // Host_Name_f
        private void Name_f( )
        {
            if ( Command.Argc == 1 )
            {
                Con.Print( "\"name\" is \"{0}\"\n", client.Name );
                return;
            }

            String newName;
            if ( Command.Argc == 2 )
                newName = Command.Argv( 1 );
            else
                newName = Command.Args;

            if ( newName.Length > 16 )
                newName = newName.Remove( 15 );

            if ( Command.Source == CommandSource.src_command )
            {
                if ( client.Name == newName )
                    return;
                CVar.Set( "_cl_name", newName );
                if ( client.cls.state == cactive_t.ca_connected )
                    Command.ForwardToServer( );
                return;
            }

            if ( !String.IsNullOrEmpty( HostClient.name ) && HostClient.name != "unconnected" )
                if ( HostClient.name != newName )
                    Con.Print( "{0} renamed to {1}\n", HostClient.name, newName );

            HostClient.name = newName;
            HostClient.edict.v.netname = progs.NewString( newName );

            // send notification to all clients
            MessageWriter msg = server.sv.reliable_datagram;
            msg.WriteByte( protocol.svc_updatename );
            msg.WriteByte( ClientNum );
            msg.WriteString( newName );
        }

        // Host_Version_f
        private void Version_f( )
        {
            Con.Print( "Version {0}\n", QDef.VERSION );
            Con.Print( "Exe hash code: {0}\n", System.Reflection.Assembly.GetExecutingAssembly( ).GetHashCode( ) );
        }

        /// <summary>
        /// Host_Say
        /// </summary>
        private void Say( Boolean teamonly )
        {
            var fromServer = false;
            if ( Command.Source == CommandSource.src_command )
            {
                if ( client.cls.state == cactive_t.ca_dedicated )
                {
                    fromServer = true;
                    teamonly = false;
                }
                else
                {
                    Command.ForwardToServer( );
                    return;
                }
            }

            if ( Command.Argc < 2 )
                return;

            client_t save = HostClient;

            var p = Command.Args;
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
                text = ( Char ) 1 + "<" + net.HostName + "> ";

            text += p + "\n";

            for ( var j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if ( client == null || !client.active || !client.spawned )
                    continue;
                if ( TeamPlay != 0 && teamonly && client.edict.v.team != save.edict.v.team )
                    continue;
                HostClient = client;
                server.ClientPrint( text );
            }
            HostClient = save;
        }

        // Host_Say_f
        private void Say_f( )
        {
            Say( false );
        }

        // Host_Say_Team_f
        private void Say_Team_f( )
        {
            Say( true );
        }

        // Host_Tell_f
        private void Tell_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( Command.Argc < 3 )
                return;

            var text = HostClient.name + ": ";
            var p = Command.Args;

            // remove quotes if present
            if ( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            text += p + "\n";

            client_t save = HostClient;
            for ( var j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if ( !client.active || !client.spawned )
                    continue;
                if ( client.name == Command.Argv( 1 ) )
                    continue;
                HostClient = client;
                server.ClientPrint( text );
                break;
            }
            HostClient = save;
        }

        // Host_Color_f
        private void Color_f( )
        {
            if ( Command.Argc == 1 )
            {
                Con.Print( "\"color\" is \"{0} {1}\"\n", ( ( Int32 ) client.Color ) >> 4, ( ( Int32 ) client.Color ) & 0x0f );
                Con.Print( "color <0-13> [0-13]\n" );
                return;
            }

            Int32 top, bottom;
            if ( Command.Argc == 2 )
                top = bottom = MathLib.atoi( Command.Argv( 1 ) );
            else
            {
                top = MathLib.atoi( Command.Argv( 1 ) );
                bottom = MathLib.atoi( Command.Argv( 2 ) );
            }

            top &= 15;
            if ( top > 13 )
                top = 13;
            bottom &= 15;
            if ( bottom > 13 )
                bottom = 13;

            var playercolor = top * 16 + bottom;

            if ( Command.Source == CommandSource.src_command )
            {
                CVar.Set( "_cl_color", playercolor );
                if ( client.cls.state == cactive_t.ca_connected )
                    Command.ForwardToServer( );
                return;
            }

            HostClient.colors = playercolor;
            HostClient.edict.v.team = bottom + 1;

            // send notification to all clients
            MessageWriter msg = server.sv.reliable_datagram;
            msg.WriteByte( protocol.svc_updatecolors );
            msg.WriteByte( ClientNum );
            msg.WriteByte( HostClient.colors );
        }

        /// <summary>
        /// Host_Kill_f
        /// </summary>
        private void Kill_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( server.Player.v.health <= 0 )
            {
                server.ClientPrint( "Can't suicide -- allready dead!\n" );
                return;
            }

            progs.GlobalStruct.time = ( Single ) server.sv.time;
            progs.GlobalStruct.self = server.EdictToProg( server.Player );
            progs.Execute( progs.GlobalStruct.ClientKill );
        }

        /// <summary>
        /// Host_Pause_f
        /// </summary>
        private void Pause_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }
            if ( _Pausable.Value == 0 )
                server.ClientPrint( "Pause not allowed.\n" );
            else
            {
                server.sv.paused = !server.sv.paused;

                if ( server.sv.paused )
                {
                    server.BroadcastPrint( "{0} paused the game\n", progs.GetString( server.Player.v.netname ) );
                }
                else
                {
                    server.BroadcastPrint( "{0} unpaused the game\n", progs.GetString( server.Player.v.netname ) );
                }

                // send notification to all clients
                server.sv.reliable_datagram.WriteByte( protocol.svc_setpause );
                server.sv.reliable_datagram.WriteByte( server.sv.paused ? 1 : 0 );
            }
        }

        /// <summary>
        /// Host_PreSpawn_f
        /// </summary>
        private void PreSpawn_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Con.Print( "prespawn is not valid from the console\n" );
                return;
            }

            if ( HostClient.spawned )
            {
                Con.Print( "prespawn not valid -- allready spawned\n" );
                return;
            }

            MessageWriter msg = HostClient.message;
            msg.Write( server.sv.signon.Data, 0, server.sv.signon.Length );
            msg.WriteByte( protocol.svc_signonnum );
            msg.WriteByte( 2 );
            HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Spawn_f
        /// </summary>
        private void Spawn_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Con.Print( "spawn is not valid from the console\n" );
                return;
            }

            if ( HostClient.spawned )
            {
                Con.Print( "Spawn not valid -- allready spawned\n" );
                return;
            }

            MemoryEdict ent;

            // run the entrance script
            if ( server.sv.loadgame )
            {
                // loaded games are fully inited allready
                // if this is the last client to be connected, unpause
                server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = HostClient.edict;

                ent.Clear( ); //memset(&ent.v, 0, progs.entityfields * 4);
                ent.v.colormap = server.NumForEdict( ent );
                ent.v.team = ( HostClient.colors & 15 ) + 1;
                ent.v.netname = progs.NewString( HostClient.name );

                // copy spawn parms out of the client_t
                progs.GlobalStruct.SetParams( HostClient.spawn_parms );

                // call the spawn function

                progs.GlobalStruct.time = ( Single ) server.sv.time;
                progs.GlobalStruct.self = server.EdictToProg( server.Player );
                progs.Execute( progs.GlobalStruct.ClientConnect );

                if ( ( Timer.GetFloatTime( ) - HostClient.netconnection.connecttime ) <= server.sv.time )
                    Con.DPrint( "{0} entered the game\n", HostClient.name );

                progs.Execute( progs.GlobalStruct.PutClientInServer );
            }

            // send all current names, colors, and frag counts
            MessageWriter msg = HostClient.message;
            msg.Clear( );

            // send time of update
            msg.WriteByte( protocol.svc_time );
            msg.WriteFloat( ( Single ) server.sv.time );

            for ( var i = 0; i < server.svs.maxclients; i++ )
            {
                client_t client = server.svs.clients[i];
                msg.WriteByte( protocol.svc_updatename );
                msg.WriteByte( i );
                msg.WriteString( client.name );
                msg.WriteByte( protocol.svc_updatefrags );
                msg.WriteByte( i );
                msg.WriteShort( client.old_frags );
                msg.WriteByte( protocol.svc_updatecolors );
                msg.WriteByte( i );
                msg.WriteByte( client.colors );
            }

            // send all current light styles
            for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
            {
                msg.WriteByte( protocol.svc_lightstyle );
                msg.WriteByte( ( Char ) i );
                msg.WriteString( server.sv.lightstyles[i] );
            }

            //
            // send some stats
            //
            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStatsDef.STAT_TOTALSECRETS );
            msg.WriteLong( ( Int32 ) progs.GlobalStruct.total_secrets );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStatsDef.STAT_TOTALMONSTERS );
            msg.WriteLong( ( Int32 ) progs.GlobalStruct.total_monsters );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStatsDef.STAT_SECRETS );
            msg.WriteLong( ( Int32 ) progs.GlobalStruct.found_secrets );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStatsDef.STAT_MONSTERS );
            msg.WriteLong( ( Int32 ) progs.GlobalStruct.killed_monsters );

            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the client to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = server.EdictNum( 1 + ClientNum );
            msg.WriteByte( protocol.svc_setangle );
            msg.WriteAngle( ent.v.angles.x );
            msg.WriteAngle( ent.v.angles.y );
            msg.WriteAngle( 0 );

            server.WriteClientDataToMessage( server.Player, HostClient.message );

            msg.WriteByte( protocol.svc_signonnum );
            msg.WriteByte( 3 );
            HostClient.sendsignon = true;
        }

        // Host_Begin_f
        private void Begin_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Con.Print( "begin is not valid from the console\n" );
                return;
            }

            HostClient.spawned = true;
        }

        /// <summary>
        /// Host_Kick_f
        /// Kicks a user off of the server
        /// </summary>
        private void Kick_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                if ( !server.sv.active )
                {
                    Command.ForwardToServer( );
                    return;
                }
            }
            else if ( progs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            client_t save = HostClient;
            var byNumber = false;
            Int32 i;
            if ( Command.Argc > 2 && Command.Argv( 1 ) == "#" )
            {
                i = ( Int32 ) MathLib.atof( Command.Argv( 2 ) ) - 1;
                if ( i < 0 || i >= server.svs.maxclients )
                    return;
                if ( !server.svs.clients[i].active )
                    return;

                HostClient = server.svs.clients[i];
                byNumber = true;
            }
            else
            {
                for ( i = 0; i < server.svs.maxclients; i++ )
                {
                    HostClient = server.svs.clients[i];
                    if ( !HostClient.active )
                        continue;
                    if ( Utilities.SameText( HostClient.name, Command.Argv( 1 ) ) )
                        break;
                }
            }

            if ( i < server.svs.maxclients )
            {
                String who;
                if ( Command.Source == CommandSource.src_command )
                    if ( client.cls.state == cactive_t.ca_dedicated )
                        who = "Console";
                    else
                        who = client.Name;
                else
                    who = save.name;

                // can't kick yourself!
                if ( HostClient == save )
                    return;

                String message = null;
                if ( Command.Argc > 2 )
                {
                    message = Tokeniser.Parse( Command.Args );
                    if ( byNumber )
                    {
                        message = message.Substring( 1 ); // skip the #
                        message = message.Trim( ); // skip white space
                        message = message.Substring( Command.Argv( 2 ).Length );	// skip the number
                    }
                    message = message.Trim( );
                }
                if ( !String.IsNullOrEmpty( message ) )
                    server.ClientPrint( "Kicked by {0}: {1}\n", who, message );
                else
                    server.ClientPrint( "Kicked by {0}\n", who );
                server.DropClient( false );
            }

            HostClient = save;
        }

        /// <summary>
        /// Host_Give_f
        /// </summary>
        private void Give_f( )
        {
            if ( Command.Source == CommandSource.src_command )
            {
                Command.ForwardToServer( );
                return;
            }

            if ( progs.GlobalStruct.deathmatch != 0 && !HostClient.privileged )
                return;

            var t = Command.Argv( 1 );
            var v = MathLib.atoi( Command.Argv( 2 ) );

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
                                server.Player.v.items = ( Int32 ) server.Player.v.items | QItemsDef.HIT_PROXIMITY_GUN;
                            else
                                server.Player.v.items = ( Int32 ) server.Player.v.items | QItemsDef.IT_GRENADE_LAUNCHER;
                        }
                        else if ( t[0] == '9' )
                            server.Player.v.items = ( Int32 ) server.Player.v.items | QItemsDef.HIT_LASER_CANNON;
                        else if ( t[0] == '0' )
                            server.Player.v.items = ( Int32 ) server.Player.v.items | QItemsDef.HIT_MJOLNIR;
                        else if ( t[0] >= '2' )
                            server.Player.v.items = ( Int32 ) server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    else
                    {
                        if ( t[0] >= '2' )
                            server.Player.v.items = ( Int32 ) server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    break;

                case 's':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        progs.SetEdictFieldFloat( server.Player, "ammo_shells1", v );

                    server.Player.v.ammo_shells = v;
                    break;

                case 'n':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_nails1", v ) )
                            if ( server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_nails = v;
                    }
                    else
                        server.Player.v.ammo_nails = v;
                    break;

                case 'l':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_lava_nails", v ) )
                            if ( server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_nails = v;
                    }
                    break;

                case 'r':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_rockets1", v ) )
                            if ( server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_rockets = v;
                    }
                    else
                    {
                        server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'm':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_multi_rockets", v ) )
                            if ( server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'h':
                    server.Player.v.health = v;
                    break;

                case 'c':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_cells1", v ) )
                            if ( server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_cells = v;
                    }
                    else
                    {
                        server.Player.v.ammo_cells = v;
                    }
                    break;

                case 'p':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if ( progs.SetEdictFieldFloat( server.Player, "ammo_plasma", v ) )
                            if ( server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                server.Player.v.ammo_cells = v;
                    }
                    break;
            }
        }

        private MemoryEdict FindViewthing( )
        {
            for ( var i = 0; i < server.sv.num_edicts; i++ )
            {
                MemoryEdict e = server.EdictNum( i );
                if ( progs.GetString( e.v.classname ) == "viewthing" )
                    return e;
            }
            Con.Print( "No viewthing on map\n" );
            return null;
        }

        // Host_Startdemos_f
        private void Startdemos_f( )
        {
            if ( client.cls.state == cactive_t.ca_dedicated )
            {
                if ( !server.sv.active )
                    CommandBuffer.AddText( "map start\n" );
                return;
            }

            var c = Command.Argc - 1;
            if ( c > client.MAX_DEMOS )
            {
                Con.Print( "Max {0} demos in demoloop\n", client.MAX_DEMOS );
                c = client.MAX_DEMOS;
            }
            Con.Print( "{0} demo(s) in loop\n", c );

            for ( var i = 1; i < c + 1; i++ )
                client.cls.demos[i - 1] = Utilities.Copy( Command.Argv( i ), client.MAX_DEMONAME );

            if ( !server.sv.active && client.cls.demonum != -1 && !client.cls.demoplayback )
            {
                client.cls.demonum = 0;
                client.NextDemo( );
            }
            else
                client.cls.demonum = -1;
        }

        /// <summary>
        /// Host_Demos_f
        /// Return to looping demos
        /// </summary>
        private void Demos_f( )
        {
            if ( client.cls.state == cactive_t.ca_dedicated )
                return;
            if ( client.cls.demonum == -1 )
                client.cls.demonum = 1;
            client.Disconnect_f( );
            client.NextDemo( );
        }

        /// <summary>
        /// Host_Stopdemo_f
        /// Return to looping demos
        /// </summary>
        private void Stopdemo_f( )
        {
            if ( client.cls.state == cactive_t.ca_dedicated )
                return;
            if ( !client.cls.demoplayback )
                return;
            client.StopPlayback( );
            client.Disconnect( );
        }
    }
}
