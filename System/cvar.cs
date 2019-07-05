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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class CVar
    {
        public static CVar First
        {
            get
            {
                return _Vars;
            }
        }

        public String Name
        {
            get
            {
                return _Name;
            }
        }

        public String String
        {
            get
            {
                return _String;
            }
        }

        public Single Value
        {
            get
            {
                return _Value;
            }
        }

        public Boolean IsArchive
        {
            get
            {
                return _Flags[Flags.Archive];
            }
        }

        public Boolean IsServer
        {
            get
            {
                return _Flags[Flags.Server];
            }
        }

        public CVar Next
        {
            get
            {
                return _Next;
            }
        }

        // CHANGE
        private static Command CommandInstance
        {
            get;
            set;
        }

        public static void Initialise( Command command )
        {
            CommandInstance = command;
        }

        private static CVar _Vars;

        private String _Name;

        // char	*name;
        private String _String;

        // char	*string;
        private BitVector32 _Flags;

        // qboolean archive;		// set to true to cause it to be saved to vars.rc
        // qboolean server;		// notifies players when changed
        private Single _Value;

        // float	value;
        private CVar _Next;

        // Cvar_FindVar()
        public static CVar Find( String name )
        {
            var var = _Vars;
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

        public static Boolean Exists( String name )
        {
            return ( Find( name ) != null );
        }

        // Cvar_VariableValue()
        public static Single GetValue( String name )
        {
            Single result = 0;
            var var = Find( name );
            if( var != null )
            {
                result = MathLib.atof( var._String );
            }
            return result;
        }

        // Cvar_VariableString()
        public static String GetString( String name )
        {
            var var = Find( name );
            if( var != null )
            {
                return var._String;
            }
            return String.Empty;
        }

        // Cvar_CompleteVariable()
        public static String[] CompleteName( String partial )
        {
            if( String.IsNullOrEmpty( partial ) )
                return null;

            var result = new List<String>();
            var var = _Vars;
            while( var != null )
            {
                if( var._Name.StartsWith( partial ) )
                    result.Add( var._Name );

                var = var._Next;
            }
            return ( result.Count > 0 ? result.ToArray() : null );
        }

        // Cvar_Set()
        public static void Set( String name, String value )
        {
            var var = Find( name );
            if( var == null )
            {
                // there is an error in C code if this happens
                CommandInstance.Host.Console.Print( "Cvar.Set: variable {0} not found\n", name );
                return;
            }
            var.Set( value );
        }

        // Cvar_SetValue()
        public static void Set( String name, Single value )
        {
            Set( name, value.ToString( CultureInfo.InvariantCulture.NumberFormat ) );
        }

        // Cvar_Command()
        // Handles variable inspection and changing from the console
        public static Boolean Command()
        {
            // check variables
            var var = Find( CommandInstance.Argv( 0 ) );
            if( var == null )
                return false;

            // perform a variable print or set
            if( CommandInstance.Argc == 1 )
            {
                CommandInstance.Host.Console.Print( "\"{0}\" is \"{1}\"\n", var._Name, var._String );
            }
            else
            {
                var.Set( CommandInstance.Argv( 1 ) );
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
            var sb = new StringBuilder( 4096 );
            var var = _Vars;
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
            var buf = Encoding.ASCII.GetBytes( sb.ToString() );
            dest.Write( buf, 0, buf.Length );
        }

        public void Set( String value )
        {
            var changed = ( String.Compare( _String, value ) != 0 );
            if( !changed )
                return;

            _String = value;
            _Value = MathLib.atof( _String );

            if( IsServer && CommandInstance.Host.Server.sv.active )
            {
                CommandInstance.Host.Server.BroadcastPrint( "\"{0}\" changed to \"{1}\"\n", _Name, _String );
            }
        }

        private class Flags
        {
            public const Int32 Archive = 1;
            public const Int32 Server = 2;
        }

        public CVar( String name, String value )
                    : this( name, value, false )
        {
        }

        public CVar( String name, String value, Boolean archive )
                    : this( name, value, archive, false )
        {
        }

        public CVar( String name, String value, Boolean archive, Boolean server )
        {
            if( String.IsNullOrEmpty( name ) )
            {
                throw new ArgumentNullException( "name" );
            }
            var var = Find( name );
            if( var != null )
            {
                throw new ArgumentException( String.Format( "Can't register variable {0}, already defined!\n", name ) );
                //Con_Printf("Can't register variable %s, allready defined\n", variable->name);
                //return;
            }
            if( CommandInstance.Exists( name ) )
            {
                throw new ArgumentException( String.Format( "Can't register variable: {0} is a command!\n", name ) );
            }
            _Next = _Vars;
            _Vars = this;

            _Name = name;
            _String = value;
            _Flags[Flags.Archive] = archive;
            _Flags[Flags.Server] = server;
            _Value = MathLib.atof( _String );
        }

        //struct cvar_s *next;
        protected CVar()
        {
        }
    }
}
