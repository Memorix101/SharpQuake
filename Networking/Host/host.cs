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

        public static bool IsDedicated
        {
            get
            {
                return false;
            }
        }

        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static double Time
        {
            get
            {
                return _Time;
            }
        }

        public static int FrameCount
        {
            get
            {
                return _FrameCount;
            }
        }

        public static bool IsDeveloper
        {
            get
            {
                return ( _Developer != null ? _Developer.Value != 0 : false );
            }
        }

        public static byte[] ColorMap
        {
            get
            {
                return _ColorMap;
            }
        }

        public static float TeamPlay
        {
            get
            {
                return _Teamplay.Value;
            }
        }

        public static byte[] BasePal
        {
            get
            {
                return _BasePal;
            }
        }

        public static bool IsCoop
        {
            get
            {
                return _Coop.Value != 0;
            }
        }

        public static float Skill
        {
            get
            {
                return _Skill.Value;
            }
        }

        public static float Deathmatch
        {
            get
            {
                return _Deathmatch.Value;
            }
        }

        public static int ClientNum
        {
            get
            {
                return Array.IndexOf( server.svs.clients, host.HostClient );
            }
        }

        public static float FragLimit
        {
            get
            {
                return _FragLimit.Value;
            }
        }

        public static float TimeLimit
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

        public static double FrameTime;
        public static double RealTime;
        public static int CurrentSkill;
        public static bool NoClipAngleHack;
        public static client_t HostClient;
        private const int VCR_SIGNATURE = 0x56435231;
        private const int SAVEGAME_VERSION = 5;

        private static quakeparms_t _Params; // quakeparms_t host_parms;

        private static cvar _Sys_TickRate; // = {"sys_ticrate","0.05"};
        private static cvar _Developer; // {"developer","0"};
        private static cvar _FrameRate;// = {"host_framerate","0"};	// set for slow motion
        private static cvar _Speeds;// = {"host_speeds","0"};			// set for running times
        private static cvar _ServerProfile;// = {"serverprofile","0"};
        private static cvar _FragLimit;// = {"fraglimit","0",false,true};
        private static cvar _TimeLimit;// = {"timelimit","0",false,true};
        private static cvar _Teamplay;// = {"teamplay","0",false,true};
        private static cvar _SameLevel;// = {"samelevel","0"};
        private static cvar _NoExit; // = {"noexit","0",false,true};
        private static cvar _Skill;// = {"skill","1"};						// 0 - 3
        private static cvar _Deathmatch;// = {"deathmatch","0"};			// 0, 1, or 2
        private static cvar _Coop;// = {"coop","0"};			// 0 or 1
        private static cvar _Pausable;// = {"pausable","1"};
        private static cvar _Temp1;// = {"temp1","0"};

        private static bool _IsInitialized; //extern	qboolean	host_initialized;		// true if into command execution
        private static int _FrameCount; //extern	int			host_framecount;	// incremented every frame, never reset
        private static byte[] _BasePal; // host_basepal
        private static byte[] _ColorMap; // host_colormap

        // host_framtime
        private static double _Time; // host_time

        // realtime;		// not bounded in any way, changed at
        // start of every frame, never reset
        private static double _OldRealTime; //double oldrealtime;			// last frame run

        // current_skill;		// skill level for currently loaded level (in case
        //  the user changes the cvar while the level is
        //  running, this reflects the level actually in use)

        // qboolean noclip_anglehack;

        private static BinaryReader _VcrReader; // vcrFile
        private static BinaryWriter _VcrWriter; // vcrFile

        // client_t* host_client;			// current client

        private static double _TimeTotal; // static double timetotal from Host_Frame
        private static int _TimeCount; // static int timecount from Host_Frame
        private static double _Time1 = 0; // static double time1 from _Host_Frame
        private static double _Time2 = 0; // static double time2 from _Host_Frame
        private static double _Time3 = 0; // static double time3 from _Host_Frame

        private static int _ShutdownDepth;
        private static int _ErrorDepth;

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
            progs.GlobalStruct.frametime = (float)host.FrameTime;

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

            Cache.Init( 1024 * 1024 * 512 ); // debug
            Cbuf.Init();
            cmd.Init();
            view.Init();
            chase.Init();
            InitVCR( parms );
            common.Init( parms.basedir, parms.argv );
            InitLocal();
            wad.LoadWadFile( "gfx.wad" );
            Key.Init();
            Con.Init();
            menu.Init();
            progs.Init();
            Mod.Init();
            net.Init();
            server.Init();

            //Con.Print("Exe: "__TIME__" "__DATE__"\n");
            //Con.Print("%4.1f megabyte heap\n",parms->memsize/ (1024*1024.0));

            render.InitTextures();		// needed even for dedicated servers

            if( client.cls.state != cactive_t.ca_dedicated )
            {
                _BasePal = common.LoadFile( "gfx/palette.lmp" );
                if( _BasePal == null )
                    sys.Error( "Couldn't load gfx/palette.lmp" );
                _ColorMap = common.LoadFile( "gfx/colormap.lmp" );
                if( _ColorMap == null )
                    sys.Error( "Couldn't load gfx/colormap.lmp" );

                // on non win32, mouse comes before video for security reasons
                input.Init();
                vid.Init( _BasePal );
                Drawer.Init();
                Scr.Init();
                render.Init();
                snd.Init();
                cd_audio.Init();
                sbar.Init();
                client.Init();
            }

            Cbuf.InsertText( "exec quake.rc\n" );

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
                input.Shutdown();

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
        public static void Error( string error, params object[] args )
        {
            _ErrorDepth++;
            try
            {
                if( _ErrorDepth > 1 )
                    sys.Error( "Host_Error: recursively entered. " + error, args );

                Scr.EndLoadingPlaque();		// reenable screen updates

                string message = ( args.Length > 0 ? String.Format( error, args ) : error );
                Con.Print( "Host_Error: {0}\n", message );

                if( server.sv.active )
                    ShutdownServer( false );

                if( client.cls.state == cactive_t.ca_dedicated )
                    sys.Error( "Host_Error: {0}\n", message );	// dedicated servers exit

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
        public static void EndGame( string message, params object[] args )
        {
            string str = String.Format( message, args );
            Con.DPrint( "Host_EndGame: {0}\n", str );

            if( server.IsActive )
                host.ShutdownServer( false );

            if( client.cls.state == cactive_t.ca_dedicated )
                sys.Error( "Host_EndGame: {0}\n", str );	// dedicated servers exit

            if( client.cls.demonum != -1 )
                client.NextDemo();
            else
                client.Disconnect();

            throw new EndGameException();  //longjmp (host_abortserver, 1);
        }

        // Host_Frame
        public static void Frame( double time )
        {
            if( _ServerProfile.Value == 0 )
            {
                InternalFrame( time );
                return;
            }

            double time1 = sys.GetFloatTime();
            InternalFrame( time );
            double time2 = sys.GetFloatTime();

            _TimeTotal += time2 - time1;
            _TimeCount++;

            if( _TimeCount < 1000 )
                return;

            int m = (int)( _TimeTotal * 1000 / _TimeCount );
            _TimeCount = 0;
            _TimeTotal = 0;
            int c = 0;
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
        public static void ClientCommands( string fmt, params object[] args )
        {
            string tmp = String.Format( fmt, args );
            HostClient.message.WriteByte( protocol.svc_stufftext );
            HostClient.message.WriteString( tmp );
        }

        /// <summary>
        /// Host_ShutdownServer
        /// This only happens at the end of a game, not between levels
        /// </summary>
        public static void ShutdownServer( bool crash )
        {
            if( !server.IsActive )
                return;

            server.sv.active = false;

            // stop all client sounds immediately
            if( client.cls.state == cactive_t.ca_connected )
                client.Disconnect();

            // flush any pending messages - like the score!!!
            double start = sys.GetFloatTime();
            int count;
            do
            {
                count = 0;
                for( int i = 0; i < server.svs.maxclients; i++ )
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
                if( ( sys.GetFloatTime() - start ) > 3.0 )
                    break;
            }
            while( count > 0 );

            // make sure all the clients know we're disconnecting
            MsgWriter writer = new MsgWriter( 4 );
            writer.WriteByte( protocol.svc_disconnect );
            count = net.SendToAll( writer, 5 );
            if( count != 0 )
                Con.Print( "Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count );

            for( int i = 0; i < server.svs.maxclients; i++ )
            {
                HostClient = server.svs.clients[i];
                if( HostClient.active )
                    server.DropClient( crash );
            }

            //
            // clear structures
            //
            server.sv.Clear();
            for( int i = 0; i < server.svs.clients.Length; i++ )
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
                string path = Path.Combine( common.GameDir, "config.cfg" );
                using( FileStream fs = sys.FileOpenWrite( path, true ) )
                {
                    if( fs != null )
                    {
                        Key.WriteBindings( fs );
                        cvar.WriteVariables( fs );
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
                _Sys_TickRate = new cvar( "sys_ticrate", "0.05" );
                _Developer = new cvar( "developer", "0" );
                _FrameRate = new cvar( "host_framerate", "0" ); // set for slow motion
                _Speeds = new cvar( "host_speeds", "0" );	// set for running times
                _ServerProfile = new cvar( "serverprofile", "0" );
                _FragLimit = new cvar( "fraglimit", "0", false, true );
                _TimeLimit = new cvar( "timelimit", "0", false, true );
                _Teamplay = new cvar( "teamplay", "0", false, true );
                _SameLevel = new cvar( "samelevel", "0" );
                _NoExit = new cvar( "noexit", "0", false, true );
                _Skill = new cvar( "skill", "1" ); // 0 - 3
                _Deathmatch = new cvar( "deathmatch", "0" ); // 0, 1, or 2
                _Coop = new cvar( "coop", "0" ); // 0 or 1
                _Pausable = new cvar( "pausable", "1" );
                _Temp1 = new cvar( "temp1", "0" );
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

            int i = common.CheckParm( "-dedicated" );
            if( i > 0 )
            {
                cls.state = cactive_t.ca_dedicated;
                if( i != ( common.Argc - 1 ) )
                {
                    svs.maxclients = common.atoi( common.Argv( i + 1 ) );
                }
                else
                    svs.maxclients = 8;
            }
            else
                cls.state = cactive_t.ca_disconnected;

            i = common.CheckParm( "-listen" );
            if( i > 0 )
            {
                if( cls.state == cactive_t.ca_dedicated )
                    sys.Error( "Only one of -dedicated or -listen can be specified" );
                if( i != ( common.Argc - 1 ) )
                    svs.maxclients = common.atoi( common.Argv( i + 1 ) );
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
                cvar.Set( "deathmatch", 1.0f );
            else
                cvar.Set( "deathmatch", 0.0f );
        }

        private static void InitVCR( quakeparms_t parms )
        {
            if( common.HasParam( "-playback" ) )
            {
                if( common.Argc != 2 )
                    sys.Error( "No other parameters allowed with -playback\n" );

                Stream file = sys.FileOpenRead( "quake.vcr" );
                if( file == null )
                    sys.Error( "playback file not found\n" );

                _VcrReader = new BinaryReader( file, Encoding.ASCII );
                int signature = _VcrReader.ReadInt32();  //Sys_FileRead(vcrFile, &i, sizeof(int));
                if( signature != host.VCR_SIGNATURE )
                    sys.Error( "Invalid signature in vcr file\n" );

                int argc = _VcrReader.ReadInt32(); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
                string[] argv = new string[argc + 1];
                argv[0] = parms.argv[0];

                for( int i = 1; i < argv.Length; i++ )
                {
                    argv[i] = sys.ReadString( _VcrReader );
                }
                common.Args = argv;
                parms.argv = argv;
            }

            int n = common.CheckParm( "-record" );
            if( n != 0 )
            {
                Stream file = sys.FileOpenWrite( "quake.vcr" ); // vcrFile = Sys_FileOpenWrite("quake.vcr");
                _VcrWriter = new BinaryWriter( file, Encoding.ASCII );

                _VcrWriter.Write( VCR_SIGNATURE ); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
                _VcrWriter.Write( common.Argc - 1 );
                for( int i = 1; i < common.Argc; i++ )
                {
                    if( i == n )
                    {
                        sys.WriteString( _VcrWriter, "-playback" );
                        continue;
                    }
                    sys.WriteString( _VcrWriter, common.Argv( i ) );
                }
            }
        }

        // _Host_Frame
        //
        //Runs all active servers
        private static void InternalFrame( double time )
        {
            // keep the random time dependent
            sys.Random();

            // decide the simulation time
            if( !FilterTime( time ) )
                return;			// don't run too fast, or packets will flood out

            // get new key events
            sys.SendKeyEvents();

            // allow mice or other external controllers to add commands
            input.Commands();

            // process console commands
            Cbuf.Execute();

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
                _Time1 = sys.GetFloatTime();

            Scr.UpdateScreen();

            if( _Speeds.Value != 0 )
                _Time2 = sys.GetFloatTime();

            // update audio
            if( client.cls.signon == client.SIGNONS )
            {
                snd.Update( ref render.Origin, ref render.ViewPn, ref render.ViewRight, ref render.ViewUp );
                client.DecayLights();
            }
            else
                snd.Update( ref common.ZeroVector, ref common.ZeroVector, ref common.ZeroVector, ref common.ZeroVector );

            cd_audio.Update();

            if( _Speeds.Value != 0 )
            {
                int pass1 = (int)( ( _Time1 - _Time3 ) * 1000 );
                _Time3 = sys.GetFloatTime();
                int pass2 = (int)( ( _Time2 - _Time1 ) * 1000 );
                int pass3 = (int)( ( _Time3 - _Time2 ) * 1000 );
                Con.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            _FrameCount++;
        }

        /// <summary>
        /// Host_FilterTime
        /// Returns false if the time is too short to run a frame
        /// </summary>
        private static bool FilterTime( double time )
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
                string cmd = sys.ConsoleInput();
                if( String.IsNullOrEmpty( cmd ) )
                    break;

                Cbuf.AddText( cmd );
            }
        }
    }
}
