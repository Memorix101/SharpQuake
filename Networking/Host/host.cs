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
using System.IO;
using System.Text;
using SharpQuake.Framework;

// host.c

namespace SharpQuake
{
    /// <summary>
    /// Host_functions
    /// </summary>
    static partial class host
    {
        public static quakeparms_t Params
        {
            get
            {
                return _Params;
            }
        }

        public static Boolean IsDedicated
        {
            get
            {
                return false;
            }
        }

        public static Boolean IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static Double Time
        {
            get
            {
                return _Time;
            }
        }

        public static Int32 FrameCount
        {
            get
            {
                return _FrameCount;
            }
        }

        public static Boolean IsDeveloper
        {
            get
            {
                return ( _Developer != null ? _Developer.Value != 0 : false );
            }
        }

        public static Byte[] ColorMap
        {
            get
            {
                return _ColorMap;
            }
        }

        public static Single TeamPlay
        {
            get
            {
                return _Teamplay.Value;
            }
        }

        public static Byte[] BasePal
        {
            get
            {
                return _BasePal;
            }
        }

        public static Boolean IsCoop
        {
            get
            {
                return _Coop.Value != 0;
            }
        }

        public static Single Skill
        {
            get
            {
                return _Skill.Value;
            }
        }

        public static Single Deathmatch
        {
            get
            {
                return _Deathmatch.Value;
            }
        }

        public static Int32 ClientNum
        {
            get
            {
                return Array.IndexOf( server.svs.clients, host.HostClient );
            }
        }

        public static Single FragLimit
        {
            get
            {
                return _FragLimit.Value;
            }
        }

        public static Single TimeLimit
        {
            get
            {
                return _TimeLimit.Value;
            }
        }

        public static BinaryReader VcrReader
        {
            get
            {
                return _VcrReader;
            }
        }

        public static BinaryWriter VcrWriter
        {
            get
            {
                return _VcrWriter;
            }
        }

        public static Double FrameTime;
        public static Double RealTime;
        public static Int32 CurrentSkill;
        public static Boolean NoClipAngleHack;
        public static client_t HostClient;
        private const Int32 VCR_SIGNATURE = 0x56435231;
        private const Int32 SAVEGAME_VERSION = 5;

        private static quakeparms_t _Params; // quakeparms_t host_parms;

        private static CVar _Sys_TickRate; // = {"sys_ticrate","0.05"};
        private static CVar _Developer; // {"developer","0"};
        private static CVar _FrameRate;// = {"host_framerate","0"};	// set for slow motion
        private static CVar _Speeds;// = {"host_speeds","0"};			// set for running times
        private static CVar _ServerProfile;// = {"serverprofile","0"};
        private static CVar _FragLimit;// = {"fraglimit","0",false,true};
        private static CVar _TimeLimit;// = {"timelimit","0",false,true};
        private static CVar _Teamplay;// = {"teamplay","0",false,true};
        private static CVar _SameLevel;// = {"samelevel","0"};
        private static CVar _NoExit; // = {"noexit","0",false,true};
        private static CVar _Skill;// = {"skill","1"};						// 0 - 3
        private static CVar _Deathmatch;// = {"deathmatch","0"};			// 0, 1, or 2
        private static CVar _Coop;// = {"coop","0"};			// 0 or 1
        private static CVar _Pausable;// = {"pausable","1"};
        private static CVar _Temp1;// = {"temp1","0"};

        private static Boolean _IsInitialized; //extern	qboolean	host_initialized;		// true if into command execution
        private static Int32 _FrameCount; //extern	int			host_framecount;	// incremented every frame, never reset
        private static Byte[] _BasePal; // host_basepal
        private static Byte[] _ColorMap; // host_colormap

        // host_framtime
        private static Double _Time; // host_time

        // realtime;		// not bounded in any way, changed at
        // start of every frame, never reset
        private static Double _OldRealTime; //double oldrealtime;			// last frame run

        // current_skill;		// skill level for currently loaded level (in case
        //  the user changes the cvar while the level is
        //  running, this reflects the level actually in use)

        // qboolean noclip_anglehack;

        private static BinaryReader _VcrReader; // vcrFile
        private static BinaryWriter _VcrWriter; // vcrFile

        // client_t* host_client;			// current client

        private static Double _TimeTotal; // static double timetotal from Host_Frame
        private static Int32 _TimeCount; // static int timecount from Host_Frame
        private static Double _Time1 = 0; // static double time1 from _Host_Frame
        private static Double _Time2 = 0; // static double time2 from _Host_Frame
        private static Double _Time3 = 0; // static double time3 from _Host_Frame

        private static Int32 _ShutdownDepth;
        private static Int32 _ErrorDepth;

        /// <summary>
        /// Host_ClearMemory
        /// </summary>
        public static void ClearMemory()
        {
            Con.DPrint( "Clearing memory\n" );

            Mod.ClearAll();
            client.cls.signon = 0;
            server.sv.Clear();
            client.cl.Clear();
        }

        /// <summary>
        /// Host_ServerFrame
        /// </summary>
        public static void ServerFrame()
        {
            // run the world state
            progs.GlobalStruct.frametime = ( Single ) host.FrameTime;

            // set the time and clear the general datagram
            server.ClearDatagram();

            // check for new clients
            server.CheckForNewClients();

            // read client messages
            server.RunClients();

            // move things around and think
            // always pause in single player if in console or menus
            if( !server.sv.paused && ( server.svs.maxclients > 1 || Key.Destination == keydest_t.key_game ) )
                server.Physics();

            // send all messages to the clients
            server.SendClientMessages();
        }

        public static void Init( quakeparms_t parms )
        {
            _Params = parms;
            Command.SetupWrapper( ); // Temporary workaround
            Cache.Init( 1024 * 1024 * 512 ); // debug
            CommandBuffer.Init();
            Command.Init();
            view.Init();
            chase.Init();
            InitVCR( parms );
            Common.Init( parms.basedir, parms.argv );
            InitLocal();
            Wad.LoadWadFile( "gfx.wad" );
            Key.Init();
            Con.Init();
            Menu.Init();
            progs.Init();
            Mod.Init();
            net.Init();
            server.Init();

            //Con.Print("Exe: "__TIME__" "__DATE__"\n");
            //Con.Print("%4.1f megabyte heap\n",parms->memsize/ (1024*1024.0));

            render.InitTextures();		// needed even for dedicated servers

            if( client.cls.state != cactive_t.ca_dedicated )
            {
                _BasePal = FileSystem.LoadFile( "gfx/palette.lmp" );
                if( _BasePal == null )
                    Utilities.Error( "Couldn't load gfx/palette.lmp" );
                _ColorMap = FileSystem.LoadFile( "gfx/colormap.lmp" );
                if( _ColorMap == null )
                    Utilities.Error( "Couldn't load gfx/colormap.lmp" );

                // on non win32, mouse comes before video for security reasons
                Input.Init();
                vid.Init( _BasePal );
                Drawer.Init();
                Scr.Init();
                render.Init();
                snd.Init();
                cd_audio.Init();
                sbar.Init();
                client.Init();
            }

            CommandBuffer.InsertText( "exec quake.rc\n" );

            _IsInitialized = true;

            Con.DPrint( "========Quake Initialized=========\n" );
        }

        /// <summary>
        /// Host_Shutdown
        /// </summary>
        public static void Shutdown()
        {
            _ShutdownDepth++;
            try
            {
                if( _ShutdownDepth > 1 )
                    return;

                // keep Con_Printf from trying to update the screen
                Scr.IsDisabledForLoading = true;

                WriteConfiguration();

                cd_audio.Shutdown();
                net.Shutdown();
                snd.Shutdown();
                Input.Shutdown();

                if( _VcrWriter != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    _VcrWriter.Close();
                    _VcrWriter = null;
                }
                if( _VcrReader != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    _VcrReader.Close();
                    _VcrReader = null;
                }

                if( client.cls.state != cactive_t.ca_dedicated )
                {
                    vid.Shutdown();
                }

                Con.Shutdown();
            }
            finally
            {
                _ShutdownDepth--;

                // Hack to close process property
                Environment.Exit( 0 );
            }
        }

        /// <summary>
        /// Host_Error
        /// This shuts down both the client and server
        /// </summary>
        public static void Error( String error, params Object[] args )
        {
            _ErrorDepth++;
            try
            {
                if( _ErrorDepth > 1 )
                    Utilities.Error( "Host_Error: recursively entered. " + error, args );

                Scr.EndLoadingPlaque();		// reenable screen updates

                var message = ( args.Length > 0 ? String.Format( error, args ) : error );
                Con.Print( "Host_Error: {0}\n", message );

                if( server.sv.active )
                    ShutdownServer( false );

                if( client.cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "Host_Error: {0}\n", message );	// dedicated servers exit

                client.Disconnect();
                client.cls.demonum = -1;

                throw new EndGameException(); // longjmp (host_abortserver, 1);
            }
            finally
            {
                _ErrorDepth--;
            }
        }

        /// <summary>
        /// Host_EndGame
        /// </summary>
        public static void EndGame( String message, params Object[] args )
        {
            var str = String.Format( message, args );
            Con.DPrint( "Host_EndGame: {0}\n", str );

            if( server.IsActive )
                host.ShutdownServer( false );

            if( client.cls.state == cactive_t.ca_dedicated )
                Utilities.Error( "Host_EndGame: {0}\n", str );	// dedicated servers exit

            if( client.cls.demonum != -1 )
                client.NextDemo();
            else
                client.Disconnect();

            throw new EndGameException();  //longjmp (host_abortserver, 1);
        }

        // Host_Frame
        public static void Frame( Double time )
        {
            if( _ServerProfile.Value == 0 )
            {
                InternalFrame( time );
                return;
            }

            var time1 = Timer.GetFloatTime();
            InternalFrame( time );
            var time2 = Timer.GetFloatTime();

            _TimeTotal += time2 - time1;
            _TimeCount++;

            if( _TimeCount < 1000 )
                return;

            var m = ( Int32 ) ( _TimeTotal * 1000 / _TimeCount );
            _TimeCount = 0;
            _TimeTotal = 0;
            var c = 0;
            foreach( client_t cl in server.svs.clients )
            {
                if( cl.active )
                    c++;
            }

            Con.Print( "serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m );
        }

        /// <summary>
        /// Host_ClientCommands
        /// Send text over to the client to be executed
        /// </summary>
        public static void ClientCommands( String fmt, params Object[] args )
        {
            var tmp = String.Format( fmt, args );
            HostClient.message.WriteByte( protocol.svc_stufftext );
            HostClient.message.WriteString( tmp );
        }

        /// <summary>
        /// Host_ShutdownServer
        /// This only happens at the end of a game, not between levels
        /// </summary>
        public static void ShutdownServer( Boolean crash )
        {
            if( !server.IsActive )
                return;

            server.sv.active = false;

            // stop all client sounds immediately
            if( client.cls.state == cactive_t.ca_connected )
                client.Disconnect();

            // flush any pending messages - like the score!!!
            var start = Timer.GetFloatTime();
            Int32 count;
            do
            {
                count = 0;
                for( var i = 0; i < server.svs.maxclients; i++ )
                {
                    HostClient = server.svs.clients[i];
                    if( HostClient.active && !HostClient.message.IsEmpty )
                    {
                        if( net.CanSendMessage( HostClient.netconnection ) )
                        {
                            net.SendMessage( HostClient.netconnection, HostClient.message );
                            HostClient.message.Clear();
                        }
                        else
                        {
                            net.GetMessage( HostClient.netconnection );
                            count++;
                        }
                    }
                }
                if( ( Timer.GetFloatTime() - start ) > 3.0 )
                    break;
            }
            while( count > 0 );

            // make sure all the clients know we're disconnecting
            MessageWriter writer = new MessageWriter( 4 );
            writer.WriteByte( protocol.svc_disconnect );
            count = net.SendToAll( writer, 5 );
            if( count != 0 )
                Con.Print( "Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count );

            for( var i = 0; i < server.svs.maxclients; i++ )
            {
                HostClient = server.svs.clients[i];
                if( HostClient.active )
                    server.DropClient( crash );
            }

            //
            // clear structures
            //
            server.sv.Clear();
            for( var i = 0; i < server.svs.clients.Length; i++ )
                server.svs.clients[i].Clear();
        }

        /// <summary>
        /// Host_WriteConfiguration
        /// Writes key bindings and archived cvars to config.cfg
        /// </summary>
        private static void WriteConfiguration()
        {
            // dedicated servers initialize the host but don't parse and set the
            // config.cfg cvars
            if( _IsInitialized & !host.IsDedicated )
            {
                var path = Path.Combine( FileSystem.GameDir, "config.cfg" );
                using( FileStream fs = FileSystem.OpenWrite( path, true ) )
                {
                    if( fs != null )
                    {
                        Key.WriteBindings( fs );
                        CVar.WriteVariables( fs );
                    }
                }
            }
        }

        // Host_InitLocal
        private static void InitLocal()
        {
            InitCommands();

            if( _Sys_TickRate == null )
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

            FindMaxClients();

            _Time = 1.0;		// so a think at time 0 won't get called
        }

        /// <summary>
        /// Host_FindMaxClients
        /// </summary>
        private static void FindMaxClients()
        {
            server_static_t svs = server.svs;
            client_static_t cls = client.cls;

            svs.maxclients = 1;

            var i = CommandLine.CheckParm( "-dedicated" );
            if( i > 0 )
            {
                cls.state = cactive_t.ca_dedicated;
                if( i != ( CommandLine.Argc - 1 ) )
                {
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                }
                else
                    svs.maxclients = 8;
            }
            else
                cls.state = cactive_t.ca_disconnected;

            i = CommandLine.CheckParm( "-listen" );
            if( i > 0 )
            {
                if( cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "Only one of -dedicated or -listen can be specified" );
                if( i != ( CommandLine.Argc - 1 ) )
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                else
                    svs.maxclients = 8;
            }
            if( svs.maxclients < 1 )
                svs.maxclients = 8;
            else if( svs.maxclients > QDef.MAX_SCOREBOARD )
                svs.maxclients = QDef.MAX_SCOREBOARD;

            svs.maxclientslimit = svs.maxclients;
            if( svs.maxclientslimit < 4 )
                svs.maxclientslimit = 4;
            svs.clients = new client_t[svs.maxclientslimit]; // Hunk_AllocName (svs.maxclientslimit*sizeof(client_t), "clients");
            for( i = 0; i < svs.clients.Length; i++ )
                svs.clients[i] = new client_t();

            if( svs.maxclients > 1 )
                CVar.Set( "deathmatch", 1.0f );
            else
                CVar.Set( "deathmatch", 0.0f );
        }

        private static void InitVCR( quakeparms_t parms )
        {
            if( CommandLine.HasParam( "-playback" ) )
            {
                if( CommandLine.Argc != 2 )
                    Utilities.Error( "No other parameters allowed with -playback\n" );

                Stream file = FileSystem.OpenRead( "quake.vcr" );
                if( file == null )
                    Utilities.Error( "playback file not found\n" );

                _VcrReader = new BinaryReader( file, Encoding.ASCII );
                var signature = _VcrReader.ReadInt32();  //Sys_FileRead(vcrFile, &i, sizeof(int));
                if( signature != host.VCR_SIGNATURE )
                    Utilities.Error( "Invalid signature in vcr file\n" );

                var argc = _VcrReader.ReadInt32(); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
                String[] argv = new String[argc + 1];
                argv[0] = parms.argv[0];

                for( var i = 1; i < argv.Length; i++ )
                {
                    argv[i] = Utilities.ReadString( _VcrReader );
                }
                CommandLine.Args = argv;
                parms.argv = argv;
            }

            var n = CommandLine.CheckParm( "-record" );
            if( n != 0 )
            {
                Stream file = FileSystem.OpenWrite( "quake.vcr" ); // vcrFile = Sys_FileOpenWrite("quake.vcr");
                _VcrWriter = new BinaryWriter( file, Encoding.ASCII );

                _VcrWriter.Write( VCR_SIGNATURE ); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
                _VcrWriter.Write( CommandLine.Argc - 1 );
                for( var i = 1; i < CommandLine.Argc; i++ )
                {
                    if( i == n )
                    {
                        Utilities.WriteString( _VcrWriter, "-playback" );
                        continue;
                    }
                    Utilities.WriteString( _VcrWriter, CommandLine.Argv( i ) );
                }
            }
        }

        // _Host_Frame
        //
        //Runs all active servers
        private static void InternalFrame( Double time )
        {
            // keep the random time dependent
            MathLib.Random();

            // decide the simulation time
            if( !FilterTime( time ) )
                return;			// don't run too fast, or packets will flood out

            // get new key events
            sys.SendKeyEvents();

            // allow mice or other external controllers to add commands
            Input.Commands();

            // process console commands
            CommandBuffer.Execute();

            net.Poll();

            // if running the server locally, make intentions now
            if( server.sv.active )
                client.SendCmd();

            //-------------------
            //
            // server operations
            //
            //-------------------

            // check for commands typed to the host
            GetConsoleCommands();

            if( server.sv.active )
                ServerFrame();

            //-------------------
            //
            // client operations
            //
            //-------------------

            // if running the server remotely, send intentions now after
            // the incoming messages have been read
            if( !server.sv.active )
                client.SendCmd();

            _Time += FrameTime;

            // fetch results from server
            if( client.cls.state == cactive_t.ca_connected )
            {
                client.ReadFromServer();
            }

            // update video
            if( _Speeds.Value != 0 )
                _Time1 = Timer.GetFloatTime();

            Scr.UpdateScreen();

            if( _Speeds.Value != 0 )
                _Time2 = Timer.GetFloatTime();

            // update audio
            if( client.cls.signon == client.SIGNONS )
            {
                snd.Update( ref render.Origin, ref render.ViewPn, ref render.ViewRight, ref render.ViewUp );
                client.DecayLights();
            }
            else
                snd.Update( ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector );

            cd_audio.Update();

            if( _Speeds.Value != 0 )
            {
                var pass1 = ( Int32 ) ( ( _Time1 - _Time3 ) * 1000 );
                _Time3 = Timer.GetFloatTime();
                var pass2 = ( Int32 ) ( ( _Time2 - _Time1 ) * 1000 );
                var pass3 = ( Int32 ) ( ( _Time3 - _Time2 ) * 1000 );
                Con.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            _FrameCount++;
        }

        /// <summary>
        /// Host_FilterTime
        /// Returns false if the time is too short to run a frame
        /// </summary>
        private static Boolean FilterTime( Double time )
        {
            host.RealTime += time;

            if( !client.cls.timedemo && RealTime - _OldRealTime < 1.0 / 72.0 )
                return false;	// framerate is too high

            FrameTime = RealTime - _OldRealTime;
            _OldRealTime = RealTime;

            if( _FrameRate.Value > 0 )
                FrameTime = _FrameRate.Value;
            else
            {	// don't allow really long or short frames
                if( FrameTime > 0.1 )
                    FrameTime = 0.1;
                if( FrameTime < 0.001 )
                    FrameTime = 0.001;
            }

            return true;
        }

        // Host_GetConsoleCommands
        //
        // Add them exactly as if they had been typed at the console
        private static void GetConsoleCommands()
        {
            while( true )
            {
                var cmd = sys.ConsoleInput();
                if( String.IsNullOrEmpty( cmd ) )
                    break;

                CommandBuffer.AddText( cmd );
            }
        }
    }
}
