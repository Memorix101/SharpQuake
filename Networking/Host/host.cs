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
    public partial class Host : IDisposable
    {
        public QuakeParameters Parameters
        {
            get;
            private set;
        }

        public Boolean IsDedicated
        {
            get;
            private set;
        }

        public Boolean IsInitialised
        {
            get;
            private set;
        }

        public Double Time
        {
            get;
            private set;
        }

        public Int32 FrameCount
        {
            get;
            private set;
        }

        public Boolean IsDeveloper
        {
            get;
            private set;
        }

        public Byte[] ColorMap
        {
            get;
            private set;
        }

        public Single TeamPlay
        {
            get
            {
                return _Teamplay.Value;
            }
        }

        public Byte[] BasePal
        {
            get;
            private set;
        }

        public Boolean IsCoop
        {
            get
            {
                return _Coop.Value != 0;
            }
        }

        public Single Skill
        {
            get
            {
                return _Skill.Value;
            }
        }

        public Single Deathmatch
        {
            get
            {
                return _Deathmatch.Value;
            }
        }

        public Int32 ClientNum
        {
            get
            {
                return Array.IndexOf( server.svs.clients, HostClient );
            }
        }

        public Single FragLimit
        {
            get;
            private set;
        }

        public Single TimeLimit
        {
            get;
            private set;
        }

        public Double RealTime
        {
            get;
            private set;
        }

        public Double FrameTime
        {
            get;
            set;
        }

        public BinaryReader VcrReader
        {
            get;
            private set;
        }

        public BinaryWriter VcrWriter
        {
            get;
            private set;
        }

        public Int32 CurrentSkill
        {
            get;
            set;
        }

        public Boolean NoClipAngleHack
        {
            get;
            set;
        }

        // Instances
        private MainWindow Window
        {
            get;
            set;
        }

        public client_t HostClient
        {
            get;
            set;
        }

        public Cache Cache
        {
            get;
            private set;
        }

        public CommandBuffer CommandBuffer
        {
            get;
            private set;
        }

        public Command Command
        {
            get;
            private set;
        }

        //private Server Server
        //{
        //    get;
        //    set;
        //}

        private CVar _Sys_TickRate; // = {"sys_ticrate","0.05"};
        private CVar _Developer; // {"developer","0"};
        private CVar _FrameRate;// = {"host_framerate","0"};	// set for slow motion
        private CVar _Speeds;// = {"host_speeds","0"};			// set for running times
        private CVar _ServerProfile;// = {"serverprofile","0"};
        private CVar _FragLimit;// = {"fraglimit","0",false,true};
        private CVar _TimeLimit;// = {"timelimit","0",false,true};
        private CVar _Teamplay;// = {"teamplay","0",false,true};
        private CVar _SameLevel;// = {"samelevel","0"};
        private CVar _NoExit; // = {"noexit","0",false,true};
        private CVar _Skill;// = {"skill","1"};						// 0 - 3
        private CVar _Deathmatch;// = {"deathmatch","0"};			// 0, 1, or 2
        private CVar _Coop;// = {"coop","0"};			// 0 or 1
        private CVar _Pausable;// = {"pausable","1"};
        private CVar _Temp1;// = {"temp1","0"};

        private Double _TimeTotal; // static double timetotal from Host_Frame
        private Int32 _TimeCount; // static int timecount from Host_Frame
        private Double _OldRealTime; //double oldrealtime;	
        private Double _Time1 = 0; // static double time1 from _Host_Frame
        private Double _Time2 = 0; // static double time2 from _Host_Frame
        private Double _Time3 = 0; // static double time3 from _Host_Frame

        private static Int32 _ShutdownDepth;
        private static Int32 _ErrorDepth;

        public Host( MainWindow window )
        {
            Window = window;

            Cache = new Cache( );
            CommandBuffer = new CommandBuffer( );
            Command = new Command( );
        }

        /// <summary>
        /// Host_ServerFrame
        /// </summary>
        public void ServerFrame( )
        {
            // run the world state
            progs.GlobalStruct.frametime = ( Single ) FrameTime;

            // set the time and clear the general datagram
            server.ClearDatagram( );

            // check for new clients
            server.CheckForNewClients( );

            // read client messages
            server.RunClients( );

            // move things around and think
            // always pause in single player if in console or menus
            if ( !server.sv.paused && ( server.svs.maxclients > 1 || Key.Destination == keydest_t.key_game ) )
                server.Physics( );

            // send all messages to the clients
            server.SendClientMessages( );
        }

        /// <summary>
        /// host_old_ClearMemory
        /// </summary>
        public void ClearMemory( )
        {
            Con.DPrint( "Clearing memory\n" );

            Mod.ClearAll( );
            client.cls.signon = 0;
            server.sv.Clear( );
            client.cl.Clear( );
        }

        /// <summary>
        /// host_old_Error
        /// This shuts down both the client and server
        /// </summary>
        public void Error( String error, params Object[] args )
        {
            _ErrorDepth++;
            try
            {
                if ( _ErrorDepth > 1 )
                    Utilities.Error( "host_old_Error: recursively entered. " + error, args );

                Scr.EndLoadingPlaque( );		// reenable screen updates

                var message = ( args.Length > 0 ? String.Format( error, args ) : error );
                Con.Print( "host_old_Error: {0}\n", message );

                if ( server.sv.active )
                    ShutdownServer( false );

                if ( client.cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "host_old_Error: {0}\n", message );	// dedicated servers exit

                client.Disconnect( );
                client.cls.demonum = -1;

                throw new EndGameException( ); // longjmp (host_old_abortserver, 1);
            }
            finally
            {
                _ErrorDepth--;
            }
        }

        public void Initialise( QuakeParameters parms )
        {
            Parameters = parms;

            Command.SetupWrapper( ); // Temporary workaround - change soon!
            Cache.Initialise( 1024 * 1024 * 512 ); // debug
            CommandBuffer.Initialise( this );
            Command.Initialise( this );
            CVar.Init( Command );
            view.Init( this );
            chase.Init( );
            InitialiseVCR( parms );
            Common.Init( this, parms.basedir, parms.argv );
            InitialiseLocal( );
            Wad.LoadWadFile( "gfx.wad" );
            Key.Init( CommandBuffer );
            Con.Init( this );
            Menu.Init( this );
            progs.Init( this );
            Mod.Init( Cache );
            net.Init( this );
            server.Init( this );

            //Con.Print("Exe: "__TIME__" "__DATE__"\n");
            //Con.Print("%4.1f megabyte heap\n",parms->memsize/ (1024*1024.0));

            render.InitTextures( );		// needed even for dedicated servers

            if ( client.cls.state != cactive_t.ca_dedicated )
            {
                BasePal = FileSystem.LoadFile( "gfx/palette.lmp" );
                if ( BasePal == null )
                    Utilities.Error( "Couldn't load gfx/palette.lmp" );
                ColorMap = FileSystem.LoadFile( "gfx/colormap.lmp" );
                if ( ColorMap == null )
                    Utilities.Error( "Couldn't load gfx/colormap.lmp" );

                // on non win32, mouse comes before video for security reasons
                Input.Init( );
                vid.Init( this, BasePal );
                Drawer.Init( this );
                Scr.Init( this );
                render.Init( this );
                snd.Init( this );
                cd_audio.Init( this );
                sbar.Init( this );
                client.Init( this );
            }

            CommandBuffer.InsertText( "exec quake.rc\n" );

            IsInitialised = true;

            Con.DPrint( "========Quake Initialized=========\n" );
        }

        /// <summary>
        /// host_ClientCommands
        /// Send text over to the client to be executed
        /// </summary>
        public void ClientCommands( String fmt, params Object[] args )
        {
            var tmp = String.Format( fmt, args );
            HostClient.message.WriteByte( protocol.svc_stufftext );
            HostClient.message.WriteString( tmp );
        }

        // Host_InitLocal
        private void InitialiseLocal( )
        {
            InititaliseCommands( );

            if ( _Sys_TickRate == null )
            {
                _Sys_TickRate = new CVar( "sys_ticrate", "0.05" );
                _Developer = new CVar( "developer", "0" );
                _FrameRate = new CVar( "host_framerate", "0" ); // set for slow motion
                _Speeds = new CVar( "host_speeds", "0" );	// set for running times
                _ServerProfile = new CVar( "serverprofile", "0" );
                _FragLimit = new CVar( "fraglimit", "0", false, true );
                _TimeLimit = new CVar( "timelimit", "0", false, true );
                _Teamplay = new CVar( "teamplay", "0", false, true );
                _SameLevel = new CVar( "samelevel", "0" );
                _NoExit = new CVar( "noexit", "0", false, true );
                _Skill = new CVar( "skill", "1" ); // 0 - 3
                _Deathmatch = new CVar( "deathmatch", "0" ); // 0, 1, or 2
                _Coop = new CVar( "coop", "0" ); // 0 or 1
                _Pausable = new CVar( "pausable", "1" );
                _Temp1 = new CVar( "temp1", "0" );
            }

            FindMaxClients( );

            Time = 1.0;		// so a think at time 0 won't get called
        }

        private void InitialiseVCR( QuakeParameters parms )
        {
            if ( CommandLine.HasParam( "-playback" ) )
            {
                if ( CommandLine.Argc != 2 )
                    Utilities.Error( "No other parameters allowed with -playback\n" );

                Stream file = FileSystem.OpenRead( "quake.vcr" );
                if ( file == null )
                    Utilities.Error( "playback file not found\n" );

                VcrReader = new BinaryReader( file, Encoding.ASCII );
                var signature = VcrReader.ReadInt32( );  //Sys_FileRead(vcrFile, &i, sizeof(int));
                if ( signature != HostDef.VCR_SIGNATURE )
                    Utilities.Error( "Invalid signature in vcr file\n" );

                var argc = VcrReader.ReadInt32( ); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
                String[] argv = new String[argc + 1];
                argv[0] = parms.argv[0];

                for ( var i = 1; i < argv.Length; i++ )
                {
                    argv[i] = Utilities.ReadString( VcrReader );
                }
                CommandLine.Args = argv;
                parms.argv = argv;
            }

            var n = CommandLine.CheckParm( "-record" );
            if ( n != 0 )
            {
                Stream file = FileSystem.OpenWrite( "quake.vcr" ); // vcrFile = Sys_FileOpenWrite("quake.vcr");
                VcrWriter = new BinaryWriter( file, Encoding.ASCII );

                VcrWriter.Write( HostDef.VCR_SIGNATURE ); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
                VcrWriter.Write( CommandLine.Argc - 1 );
                for ( var i = 1; i < CommandLine.Argc; i++ )
                {
                    if ( i == n )
                    {
                        Utilities.WriteString( VcrWriter, "-playback" );
                        continue;
                    }
                    Utilities.WriteString( VcrWriter, CommandLine.Argv( i ) );
                }
            }
        }
        /// <summary>
        /// Host_FindMaxClients
        /// </summary>
        private void FindMaxClients( )
        {
            server_static_t svs = server.svs;
            client_static_t cls = client.cls;

            svs.maxclients = 1;

            var i = CommandLine.CheckParm( "-dedicated" );
            if ( i > 0 )
            {
                cls.state = cactive_t.ca_dedicated;
                if ( i != ( CommandLine.Argc - 1 ) )
                {
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                }
                else
                    svs.maxclients = 8;
            }
            else
                cls.state = cactive_t.ca_disconnected;

            i = CommandLine.CheckParm( "-listen" );
            if ( i > 0 )
            {
                if ( cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "Only one of -dedicated or -listen can be specified" );
                if ( i != ( CommandLine.Argc - 1 ) )
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                else
                    svs.maxclients = 8;
            }
            if ( svs.maxclients < 1 )
                svs.maxclients = 8;
            else if ( svs.maxclients > QDef.MAX_SCOREBOARD )
                svs.maxclients = QDef.MAX_SCOREBOARD;

            svs.maxclientslimit = svs.maxclients;
            if ( svs.maxclientslimit < 4 )
                svs.maxclientslimit = 4;
            svs.clients = new client_t[svs.maxclientslimit]; // Hunk_AllocName (svs.maxclientslimit*sizeof(client_t), "clients");
            for ( i = 0; i < svs.clients.Length; i++ )
                svs.clients[i] = new client_t( );

            if ( svs.maxclients > 1 )
                CVar.Set( "deathmatch", 1.0f );
            else
                CVar.Set( "deathmatch", 0.0f );
        }

        /// <summary>
        /// Host_FilterTime
        /// Returns false if the time is too short to run a frame
        /// </summary>
        private Boolean FilterTime( Double time )
        {
            RealTime += time;

            if ( !client.cls.timedemo && RealTime - _OldRealTime < 1.0 / 72.0 )
                return false;	// framerate is too high

            FrameTime = RealTime - _OldRealTime;
            _OldRealTime = RealTime;

            if ( _FrameRate.Value > 0 )
                FrameTime = _FrameRate.Value;
            else
            {	// don't allow really long or short frames
                if ( FrameTime > 0.1 )
                    FrameTime = 0.1;
                if ( FrameTime < 0.001 )
                    FrameTime = 0.001;
            }

            return true;
        }

        // _Host_Frame
        //
        //Runs all active servers
        private void InternalFrame( Double time )
        {
            // keep the random time dependent
            MathLib.Random( );

            // decide the simulation time
            if ( !FilterTime( time ) )
                return;			// don't run too fast, or packets will flood out

            // get new key events
            sys.SendKeyEvents( );

            // allow mice or other external controllers to add commands
            Input.Commands( );

            // process console commands
            CommandBuffer.Execute( );

            net.Poll( );

            // if running the server locally, make intentions now
            if ( server.sv.active )
                client.SendCmd( );

            //-------------------
            //
            // server operations
            //
            //-------------------

            // check for commands typed to the host
            GetConsoleCommands( );

            if ( server.sv.active )
                ServerFrame( );

            //-------------------
            //
            // client operations
            //
            //-------------------

            // if running the server remotely, send intentions now after
            // the incoming messages have been read
            if ( !server.sv.active )
                client.SendCmd( );

            Time += FrameTime;

            // fetch results from server
            if ( client.cls.state == cactive_t.ca_connected )
            {
                client.ReadFromServer( );
            }

            // update video
            if ( _Speeds.Value != 0 )
                _Time1 = Timer.GetFloatTime( );

            Scr.UpdateScreen( );

            if ( _Speeds.Value != 0 )
                _Time2 = Timer.GetFloatTime( );

            // update audio
            if ( client.cls.signon == client.SIGNONS )
            {
                snd.Update( ref render.Origin, ref render.ViewPn, ref render.ViewRight, ref render.ViewUp );
                client.DecayLights( );
            }
            else
                snd.Update( ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector );

            cd_audio.Update( );

            if ( _Speeds.Value != 0 )
            {
                var pass1 = ( Int32 ) ( ( _Time1 - _Time3 ) * 1000 );
                _Time3 = Timer.GetFloatTime( );
                var pass2 = ( Int32 ) ( ( _Time2 - _Time1 ) * 1000 );
                var pass3 = ( Int32 ) ( ( _Time3 - _Time2 ) * 1000 );
                Con.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            FrameCount++;
        }

        // Host_GetConsoleCommands
        //
        // Add them exactly as if they had been typed at the console
        private void GetConsoleCommands( )
        {
            while ( true )
            {
                var cmd = sys.ConsoleInput( );
                if ( String.IsNullOrEmpty( cmd ) )
                    break;

                CommandBuffer.AddText( cmd );
            }
        }
        
        /// <summary>
        /// host_EndGame
        /// </summary>
        public void EndGame( String message, params Object[] args )
        {
            var str = String.Format( message, args );
            Con.DPrint( "host_old_EndGame: {0}\n", str );

            if ( server.IsActive )
                ShutdownServer( false );

            if ( client.cls.state == cactive_t.ca_dedicated )
                Utilities.Error( "host_old_EndGame: {0}\n", str );	// dedicated servers exit

            if ( client.cls.demonum != -1 )
                client.NextDemo( );
            else
                client.Disconnect( );

            throw new EndGameException( );  //longjmp (host_old_abortserver, 1);
        }

        /// <summary>
        /// Host_ShutdownServer
        /// This only happens at the end of a game, not between levels
        /// </summary>
        public void ShutdownServer( Boolean crash )
        {
            if ( !server.IsActive )
                return;

            server.sv.active = false;

            // stop all client sounds immediately
            if ( client.cls.state == cactive_t.ca_connected )
                client.Disconnect( );

            // flush any pending messages - like the score!!!
            var start = Timer.GetFloatTime( );
            Int32 count;
            do
            {
                count = 0;
                for ( var i = 0; i < server.svs.maxclients; i++ )
                {
                    HostClient = server.svs.clients[i];
                    if ( HostClient.active && !HostClient.message.IsEmpty )
                    {
                        if ( net.CanSendMessage( HostClient.netconnection ) )
                        {
                            net.SendMessage( HostClient.netconnection, HostClient.message );
                            HostClient.message.Clear( );
                        }
                        else
                        {
                            net.GetMessage( HostClient.netconnection );
                            count++;
                        }
                    }
                }
                if ( ( Timer.GetFloatTime( ) - start ) > 3.0 )
                    break;
            }
            while ( count > 0 );

            // make sure all the clients know we're disconnecting
            MessageWriter writer = new MessageWriter( 4 );
            writer.WriteByte( protocol.svc_disconnect );
            count = net.SendToAll( writer, 5 );
            if ( count != 0 )
                Con.Print( "Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count );

            for ( var i = 0; i < server.svs.maxclients; i++ )
            {
                HostClient = server.svs.clients[i];
                if ( HostClient.active )
                    server.DropClient( crash );
            }

            //
            // clear structures
            //
            server.sv.Clear( );
            for ( var i = 0; i < server.svs.clients.Length; i++ )
                server.svs.clients[i].Clear( );
        }

        // Host_Frame
        public void Frame( Double time )
        {
            if ( _ServerProfile.Value == 0 )
            {
                InternalFrame( time );
                return;
            }

            var time1 = Timer.GetFloatTime( );
            InternalFrame( time );
            var time2 = Timer.GetFloatTime( );

            _TimeTotal += time2 - time1;
            _TimeCount++;

            if ( _TimeCount < 1000 )
                return;

            var m = ( Int32 ) ( _TimeTotal * 1000 / _TimeCount );
            _TimeCount = 0;
            _TimeTotal = 0;
            var c = 0;
            foreach ( client_t cl in server.svs.clients )
            {
                if ( cl.active )
                    c++;
            }

            Con.Print( "serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m );
        }

        /// <summary>
        /// Host_WriteConfiguration
        /// Writes key bindings and archived cvars to config.cfg
        /// </summary>
        private void WriteConfiguration( )
        {
            // dedicated servers initialize the host but don't parse and set the
            // config.cfg cvars
            if ( IsInitialised & !IsDedicated )
            {
                var path = Path.Combine( FileSystem.GameDir, "config.cfg" );

                using ( var fs = FileSystem.OpenWrite( path, true ) )
                {
                    if ( fs != null )
                    {
                        Key.WriteBindings( fs );
                        CVar.WriteVariables( fs );
                    }
                }
            }
        }

        /// <summary>
        /// Host_Shutdown
        /// </summary>
        public void Shutdown( )
        {
            _ShutdownDepth++;
            try
            {
                if ( _ShutdownDepth > 1 )
                    return;

                // keep Con_Printf from trying to update the screen
                Scr.IsDisabledForLoading = true;

                WriteConfiguration( );

                cd_audio.Shutdown( );
                net.Shutdown( );
                snd.Shutdown( );
                Input.Shutdown( );

                if ( VcrWriter != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    VcrWriter.Close( );
                    VcrWriter = null;
                }
                if ( VcrReader != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    VcrReader.Close( );
                    VcrReader = null;
                }

                if ( client.cls.state != cactive_t.ca_dedicated )
                {
                    vid.Shutdown( );
                }

                Con.Shutdown( );
            }
            finally
            {
                _ShutdownDepth--;

                // Hack to close process property
                Environment.Exit( 0 );
            }
        }

        public void Dispose( )
        {
            ShutdownServer( false );
        }
    }
}
