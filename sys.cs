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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake
{
    internal static class sys
    {
        public static bool IsWindows
        {
            get
            {
                PlatformID platform = Environment.OSVersion.Platform;
                return ( platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT || platform == PlatformID.WinCE || platform == PlatformID.Xbox );
            }
        }

        private static Stopwatch _StopWatch;
        private static Random _Random = new Random();

        /// <summary>
        /// Sys_Error
        /// an error will cause the entire program to exit
        /// </summary>
        public static void Error( string fmt, params object[] args )
        {
            throw new QuakeSystemError( args.Length > 0 ? String.Format( fmt, args ) : fmt );
        }

        // Sys_FileOpenRead
        public static FileStream FileOpenRead( string path )
        {
            try
            {
                return new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read );
            }
            catch( Exception )
            {
                return null;
            }
        }

        /// <summary>
        /// Sys_FileOpenWrite
        /// </summary>
        public static FileStream FileOpenWrite( string path, bool allowFail = false )
        {
            try
            {
                return new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.Read );
            }
            catch( Exception ex )
            {
                if( !allowFail )
                {
                    Error( "Error opening {0}: {1}", path, ex.Message );
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Sys_FloatTime
        /// </summary>
        public static double GetFloatTime()
        {
            if( _StopWatch == null )
            {
                _StopWatch = new Stopwatch();
                _StopWatch.Start();
            }
            return _StopWatch.Elapsed.TotalSeconds;
        }

        public static void WriteString( BinaryWriter dest, string value )
        {
            byte[] buf = Encoding.ASCII.GetBytes( value );
            dest.Write( buf.Length );
            dest.Write( buf );
        }

        public static string ReadString( BinaryReader src )
        {
            int length = src.ReadInt32();
            if( length <= 0 )
            {
                throw new Exception( "Invalid string length: " + length.ToString() );
            }
            byte[] buf = new byte[length];
            src.Read( buf, 0, length );
            return Encoding.ASCII.GetString( buf );
        }

        // Sys_FileTime()
        public static DateTime GetFileTime( string path )
        {
            if( String.IsNullOrEmpty( path ) || path.LastIndexOf( '*' ) != -1 )
                return DateTime.MinValue;
            try
            {
                DateTime result = File.GetLastWriteTimeUtc( path );
                if( result.Year == 1601 )
                    return DateTime.MinValue; // file does not exists

                return result.ToLocalTime();
            }
            catch( IOException )
            {
                return DateTime.MinValue;
            }
        }

        public static T ReadStructure<T>( Stream stream )
        {
            int count = Marshal.SizeOf( typeof( T ) );
            byte[] buf = new byte[count];
            if( stream.Read( buf, 0, count ) < count )
            {
                throw new IOException( "Stream reading error!" );
            }
            return BytesToStructure<T>( buf, 0 );
        }

        public static T BytesToStructure<T>( byte[] src, int startIndex )
        {
            GCHandle handle = GCHandle.Alloc( src, GCHandleType.Pinned );
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                if( startIndex != 0 )
                {
                    long ptr2 = ptr.ToInt64() + startIndex;
                    ptr = new IntPtr( ptr2 );
                }
                return (T)Marshal.PtrToStructure( ptr, typeof( T ) );
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToBytes<T>( ref T src )
        {
            byte[] buf = new byte[Marshal.SizeOf( typeof( T ) )];
            GCHandle handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            try
            {
                Marshal.StructureToPtr( src, handle.AddrOfPinnedObject(), true );
            }
            finally
            {
                handle.Free();
            }
            return buf;
        }

        public static void StructureToBytes<T>( ref T src, byte[] dest, int offset )
        {
            GCHandle handle = GCHandle.Alloc( dest, GCHandleType.Pinned );
            try
            {
                long addr = handle.AddrOfPinnedObject().ToInt64() + offset;
                Marshal.StructureToPtr( src, new IntPtr( addr ), true );
            }
            finally
            {
                handle.Free();
            }
        }

        public static int Random()
        {
            return _Random.Next();
        }

        public static int Random( int maxValue )
        {
            return _Random.Next( maxValue );
        }

        /// <summary>
        /// Sys_SendKeyEvents
        /// </summary>
        public static void SendKeyEvents()
        {
            Scr.SkipUpdate = false;
            mainwindow.Instance.ProcessEvents();
        }

        /// <summary>
        /// Sys_ConsoleInput
        /// </summary>
        public static string ConsoleInput()
        {
            return null; // this is needed only for dedicated servers
        }

        /// <summary>
        /// Sys_Quit
        /// </summary>
        public static void Quit()
        {
            if( mainwindow.Instance != null )
            {
                mainwindow.Instance.ConfirmExit = false;
                mainwindow.Instance.Exit();
            }
        }
    }
}
