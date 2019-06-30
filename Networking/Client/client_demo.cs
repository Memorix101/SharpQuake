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

namespace SharpQuake
{
    partial class client
    {
        /// <summary>
        /// CL_StopPlayback
        ///
        /// Called when a demo file runs out, or the user starts a game
        /// </summary>
        public static void StopPlayback()
        {
            if( !cls.demoplayback )
                return;

            if( cls.demofile != null )
            {
                cls.demofile.Dispose();
                cls.demofile = null;
            }
            cls.demoplayback = false;
            cls.state = cactive_t.ca_disconnected;

            if( cls.timedemo )
                FinishTimeDemo();
        }

        /// <summary>
        /// CL_Record_f
        /// record <demoname> <map> [cd track]
        /// </summary>
        private static void Record_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            int c = cmd.Argc;
            if( c != 2 && c != 3 && c != 4 )
            {
                Con.Print( "record <demoname> [<map> [cd track]]\n" );
                return;
            }

            if( cmd.Argv( 1 ).Contains( ".." ) )
            {
                Con.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            if( c == 2 && cls.state == cactive_t.ca_connected )
            {
                Con.Print( "Can not record - already connected to server\nClient demo recording must be started before connecting\n" );
                return;
            }

            // write the forced cd track number, or -1
            int track;
            if( c == 4 )
            {
                track = common.atoi( cmd.Argv( 3 ) );
                Con.Print( "Forcing CD track to {0}\n", track );
            }
            else
                track = -1;

            string name = Path.Combine( common.GameDir, cmd.Argv( 1 ) );

            //
            // start the map up
            //
            if( c > 2 )
                cmd.ExecuteString( String.Format( "map {0}", cmd.Argv( 2 ) ), cmd_source_t.src_command );

            //
            // open the demo file
            //
            name = Path.ChangeExtension( name, ".dem" );

            Con.Print( "recording to {0}.\n", name );
            FileStream fs = sys.FileOpenWrite( name, true );
            if( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }
            BinaryWriter writer = new BinaryWriter( fs, Encoding.ASCII );
            cls.demofile = new DisposableWrapper<BinaryWriter>( writer, true );
            cls.forcetrack = track;
            byte[] tmp = Encoding.ASCII.GetBytes( cls.forcetrack.ToString() );
            writer.Write( tmp );
            writer.Write( '\n' );
            cls.demorecording = true;
        }

        /// <summary>
        /// CL_Stop_f
        /// stop recording a demo
        /// </summary>
        private static void Stop_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            if( !cls.demorecording )
            {
                Con.Print( "Not recording a demo.\n" );
                return;
            }

            // write a disconnect message to the demo file
            net.Message.Clear();
            net.Message.WriteByte( protocol.svc_disconnect );
            WriteDemoMessage();

            // finish up
            if( cls.demofile != null )
            {
                cls.demofile.Dispose();
                cls.demofile = null;
            }
            cls.demorecording = false;
            Con.Print( "Completed demo\n" );
        }

        // CL_PlayDemo_f
        //
        // play [demoname]
        private static void PlayDemo_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            if( cmd.Argc != 2 )
            {
                Con.Print( "play <demoname> : plays a demo\n" );
                return;
            }

            //
            // disconnect from server
            //
            client.Disconnect();

            //
            // open the demo file
            //
            string name = Path.ChangeExtension( cmd.Argv( 1 ), ".dem" );

            Con.Print( "Playing demo from {0}.\n", name );
            if( cls.demofile != null )
            {
                cls.demofile.Dispose();
            }
            DisposableWrapper<BinaryReader> reader;
            FileSystem.FOpenFile( name, out reader );
            cls.demofile = reader;
            if( cls.demofile == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                cls.demonum = -1;		// stop demo loop
                return;
            }

            cls.demoplayback = true;
            cls.state = cactive_t.ca_connected;
            cls.forcetrack = 0;

            BinaryReader s = reader.Object;
            int c;
            bool neg = false;
            while( true )
            {
                c = s.ReadByte();
                if( c == '\n' )
                    break;

                if( c == '-' )
                    neg = true;
                else
                    cls.forcetrack = cls.forcetrack * 10 + ( c - '0' );
            }

            if( neg )
                cls.forcetrack = -cls.forcetrack;
            // ZOID, fscanf is evil
            //	fscanf (cls.demofile, "%i\n", &cls.forcetrack);
        }

        /// <summary>
        /// CL_TimeDemo_f
        /// timedemo [demoname]
        /// </summary>
        private static void TimeDemo_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            if( cmd.Argc != 2 )
            {
                Con.Print( "timedemo <demoname> : gets demo speeds\n" );
                return;
            }

            PlayDemo_f();

            // cls.td_starttime will be grabbed at the second frame of the demo, so
            // all the loading time doesn't get counted
            _Static.timedemo = true;
            _Static.td_startframe = host.FrameCount;
            _Static.td_lastframe = -1;		// get a new message this frame
        }

        /// <summary>
        /// CL_GetMessage
        /// Handles recording and playback of demos, on top of NET_ code
        /// </summary>
        /// <returns></returns>
        private static int GetMessage()
        {
            if( cls.demoplayback )
            {
                // decide if it is time to grab the next message
                if( cls.signon == SIGNONS )	// allways grab until fully connected
                {
                    if( cls.timedemo )
                    {
                        if( host.FrameCount == cls.td_lastframe )
                            return 0;		// allready read this frame's message
                        cls.td_lastframe = host.FrameCount;
                        // if this is the second frame, grab the real td_starttime
                        // so the bogus time on the first frame doesn't count
                        if( host.FrameCount == cls.td_startframe + 1 )
                            cls.td_starttime = (float)host.RealTime;
                    }
                    else if( cl.time <= cl.mtime[0] )
                    {
                        return 0;	// don't need another message yet
                    }
                }

                // get the next message
                BinaryReader reader = ( (DisposableWrapper<BinaryReader>)cls.demofile ).Object;
                int size = common.LittleLong( reader.ReadInt32() );
                if( size > QDef.MAX_MSGLEN )
                    sys.Error( "Demo message > MAX_MSGLEN" );

                cl.mviewangles[1] = cl.mviewangles[0];
                cl.mviewangles[0].X = common.LittleFloat( reader.ReadSingle() );
                cl.mviewangles[0].Y = common.LittleFloat( reader.ReadSingle() );
                cl.mviewangles[0].Z = common.LittleFloat( reader.ReadSingle() );

                net.Message.FillFrom( reader.BaseStream, size );
                if( net.Message.Length < size )
                {
                    StopPlayback();
                    return 0;
                }
                return 1;
            }

            int r;
            while( true )
            {
                r = net.GetMessage( cls.netcon );

                if( r != 1 && r != 2 )
                    return r;

                // discard nop keepalive message
                if( net.Message.Length == 1 && net.Message.Data[0] == protocol.svc_nop )
                    Con.Print( "<-- server to client keepalive\n" );
                else
                    break;
            }

            if( cls.demorecording )
                WriteDemoMessage();

            return r;
        }

        /// <summary>
        /// CL_FinishTimeDemo
        /// </summary>
        private static void FinishTimeDemo()
        {
            cls.timedemo = false;

            // the first frame didn't count
            int frames = ( host.FrameCount - cls.td_startframe ) - 1;
            float time = (float)host.RealTime - cls.td_starttime;
            if( time == 0 )
                time = 1;
            Con.Print( "{0} frames {1:F5} seconds {2:F2} fps\n", frames, time, frames / time );
        }

        /// <summary>
        /// CL_WriteDemoMessage
        /// Dumps the current net message, prefixed by the length and view angles
        /// </summary>
        private static void WriteDemoMessage()
        {
            int len = common.LittleLong( net.Message.Length );
            BinaryWriter writer = ( (DisposableWrapper<BinaryWriter>)cls.demofile ).Object;
            writer.Write( len );
            writer.Write( common.LittleFloat( cl.viewangles.X ) );
            writer.Write( common.LittleFloat( cl.viewangles.Y ) );
            writer.Write( common.LittleFloat( cl.viewangles.Z ) );
            writer.Write( net.Message.Data, 0, net.Message.Length );
            writer.Flush();
        }
    }
}
