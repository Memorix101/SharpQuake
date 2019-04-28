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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpQuake
{
    internal class cvar
    {
        public static cvar First
        {
            get
            {
                return _Vars;
            }
        }

        public string Name
        {
            get
            {
                return _Name;
            }
        }

        public string String
        {
            get
            {
                return _String;
            }
        }

        public float Value
        {
            get
            {
                return _Value;
            }
        }

        public bool IsArchive
        {
            get
            {
                return _Flags[Flags.Archive];
            }
        }

        public bool IsServer
        {
            get
            {
                return _Flags[Flags.Server];
            }
        }

        public cvar Next
        {
            get
            {
                return _Next;
            }
        }

        private static cvar _Vars;

        private string _Name;

        // char	*name;
        private string _String;

        // char	*string;
        private BitVector32 _Flags;

        // qboolean archive;		// set to true to cause it to be saved to vars.rc
        // qboolean server;		// notifies players when changed
        private float _Value;

        // float	value;
        private cvar _Next;

        // Cvar_FindVar()
        public static cvar Find( string name )
        {
            cvar var = _Vars;
            while( var != null )
            {
                if( var._Name.Equals( name ) )
                {
                    return var;
                }
                var = var._Next;
            }
            return null;
        }

        public static bool Exists( string name )
        {
            return ( Find( name ) != null );
        }

        // Cvar_VariableValue()
        public static float GetValue( string name )
        {
            float result = 0;
            cvar var = Find( name );
            if( var != null )
            {
                result = common.atof( var._String );
            }
            return result;
        }

        // Cvar_VariableString()
        public static string GetString( string name )
        {
            cvar var = Find( name );
            if( var != null )
            {
                return var._String;
            }
            return String.Empty;
        }

        // Cvar_CompleteVariable()
        public static string[] CompleteName( string partial )
        {
            if( String.IsNullOrEmpty( partial ) )
                return null;

            List<string> result = new List<string>();
            cvar var = _Vars;
            while( var != null )
            {
                if( var._Name.StartsWith( partial ) )
                    result.Add( var._Name );

                var = var._Next;
            }
            return ( result.Count > 0 ? result.ToArray() : null );
        }

        // Cvar_Set()
        public static void Set( string name, string value )
        {
            cvar var = Find( name );
            if( var == null )
            {
                // there is an error in C code if this happens
                Con.Print( "Cvar.Set: variable {0} not found\n", name );
                return;
            }
            var.Set( value );
        }

        // Cvar_SetValue()
        public static void Set( string name, float value )
        {
            Set( name, value.ToString( CultureInfo.InvariantCulture.NumberFormat ) );
        }

        // Cvar_Command()
        // Handles variable inspection and changing from the console
        public static bool Command()
        {
            // check variables
            cvar var = Find( cmd.Argv( 0 ) );
            if( var == null )
                return false;

            // perform a variable print or set
            if( cmd.Argc == 1 )
            {
                Con.Print( "\"{0}\" is \"{1}\"\n", var._Name, var._String );
            }
            else
            {
                var.Set( cmd.Argv( 1 ) );
            }
            return true;
        }

        /// <summary>
        /// Cvar_WriteVariables
        /// Writes lines containing "set variable value" for all variables
        /// with the archive flag set to true.
        /// </summary>
        public static void WriteVariables( Stream dest )
        {
            StringBuilder sb = new StringBuilder( 4096 );
            cvar var = _Vars;
            while( var != null )
            {
                if( var.IsArchive )
                {
                    sb.Append( var._Name );
                    sb.Append( " \"" );
                    sb.Append( var._String );
                    sb.AppendLine( "\"" );
                }
                var = var._Next;
            }
            byte[] buf = Encoding.ASCII.GetBytes( sb.ToString() );
            dest.Write( buf, 0, buf.Length );
        }

        public void Set( string value )
        {
            bool changed = ( String.Compare( _String, value ) != 0 );
            if( !changed )
                return;

            _String = value;
            _Value = common.atof( _String );

            if( IsServer && server.sv.active )
            {
                server.BroadcastPrint( "\"{0}\" changed to \"{1}\"\n", _Name, _String );
            }
        }

        private class Flags
        {
            public const int Archive = 1;
            public const int Server = 2;
        }

        public cvar( string name, string value )
                    : this( name, value, false )
        {
        }

        public cvar( string name, string value, bool archive )
                    : this( name, value, archive, false )
        {
        }

        public cvar( string name, string value, bool archive, bool server )
        {
            if( String.IsNullOrEmpty( name ) )
            {
                throw new ArgumentNullException( "name" );
            }
            cvar var = Find( name );
            if( var != null )
            {
                throw new ArgumentException( String.Format( "Can't register variable {0}, already defined!\n", name ) );
                //Con_Printf("Can't register variable %s, allready defined\n", variable->name);
                //return;
            }
            if( cmd.Exists( name ) )
            {
                throw new ArgumentException( String.Format( "Can't register variable: {0} is a command!\n", name ) );
            }
            _Next = _Vars;
            _Vars = this;

            _Name = name;
            _String = value;
            _Flags[Flags.Archive] = archive;
            _Flags[Flags.Server] = server;
            _Value = common.atof( _String );
        }

        //struct cvar_s *next;
        protected cvar()
        {
        }
    }
}
