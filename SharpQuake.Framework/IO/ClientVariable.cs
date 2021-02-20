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
using System.Drawing;
using System.Linq;
using SharpQuake.Framework.Mathematics;

namespace SharpQuake.Framework.IO
{
	public class ClientVariable
    {
        public String Name
        {
            get;
            private set;
        }

        public Boolean IsArchive
        {
            get
            {
                return Flags.HasFlag( ClientVariableFlags.Archive );
            }
        }

        public Boolean IsServer
        {
            get
            {
                return Flags.HasFlag( ClientVariableFlags.Server );
            }
        }

        private Object Value
        {
            get;
            set;
        }

        private Object DefaultValue
        {
            get;
            set;
        }

        public Type ValueType
        {
            get;
            private set;
        }

        public ClientVariableFlags Flags
        {
            get;
            private set;
        }

        public ClientVariable( String name, Object defaultValue, Type valueType, ClientVariableFlags flags = ClientVariableFlags.None )
        {
            if ( String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name" );

            Name = name;
            DefaultValue = defaultValue;
            Value = DefaultValue;
            ValueType = valueType;
            Flags = flags;

            //var var = Find( name );
            //if ( var != null )
            //{
            //    throw new ArgumentException( String.Format( "Can't register variable {0}, already defined!\n", name ) );
            //    //Con_Printf("Can't register variable %s, allready defined\n", variable->name);
            //    //return;
            //}
            //if ( CommandInstance.Exists( name ) )
            //{
            //    throw new ArgumentException( String.Format( "Can't register variable: {0} is a command!\n", name ) );
            //}
        }
        
        public ClientVariable( String name, String defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( String ), flags )
        {
        }

        public ClientVariable( String name, Int16 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Int16 ), flags )
        {
        }

        public ClientVariable( String name, Int32 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Int32 ), flags )
        {
        }

        public ClientVariable( String name, Int64 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Int64 ), flags )
        {
        }

        public ClientVariable( String name, UInt16 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( UInt16 ), flags )
        {
        }

        public ClientVariable( String name, UInt32 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( UInt32 ), flags )
        {
        }

        public ClientVariable( String name, UInt64 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( UInt64 ), flags )
        {
        }

        public ClientVariable( String name, Single defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Single ), flags )
        {
        }

        public ClientVariable( String name, Double defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Double ), flags )
        {
        }

        public ClientVariable( String name, Vector2 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector2 ), flags )
        {
        }

        public ClientVariable( String name, Vector3 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector3 ), flags )
        {
        }

        public ClientVariable( String name, Vector4 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector4 ), flags )
        {
        }

        public ClientVariable( String name, Boolean defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Boolean ), flags )
        {
        }

        public ClientVariable( String name, Rectangle defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Rectangle ), flags )
        {
        }

        public ClientVariable( String name, Point defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Point ), flags )
        {
        }

        public ClientVariable( String name, Size defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Size ), flags )
        {
        }

        public ClientVariable( String name, Color defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Color ), flags )
        {
        }

        public T Get<T>( )
        {
            var type = typeof( T );

            return ( T ) Value;
        }

        public Object Get( )
        {
            return Value;
        }

        public void Set<T>( T value )
        {
            var newValueType = typeof( T );

            if ( newValueType != ValueType )
            {
                if ( value is String stringValue )
                {
                    switch ( ValueType.Name )
                    {
                        case "Single":
                            if ( Single.TryParse( stringValue, out var singleResult ) )
                                Value = singleResult;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Double":
                            if ( Double.TryParse( stringValue, out var doubleResult ) )
                                Value = doubleResult;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Int16":
                            if ( Int16.TryParse( stringValue, out var int16Result ) )
                                Value = int16Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Boolean":
                            if ( Int16.TryParse( stringValue, out var booleanResult ) )
                                Value = booleanResult == 1;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Int32":
                            if ( Int32.TryParse( stringValue, out var int32Result ) )
                                Value = int32Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Int64":
                            if ( Int64.TryParse( stringValue, out var int64Result ) )
                                Value = int64Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "UInt16":
                            if ( UInt16.TryParse( stringValue, out var uint16Result ) )
                                Value = uint16Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "UInt32":
                            if ( UInt32.TryParse( stringValue, out var uint32Result ) )
                                Value = uint32Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "UInt64":
                            if ( UInt64.TryParse( stringValue, out var uint64Result ) )
                                Value = uint64Result;
                            else
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            break;

                        case "Vector2":
                            var vector2Values = stringValue.Split( ' ' )?.Select( s => Single.Parse( s ) ).ToArray();

                            if ( vector2Values == null || vector2Values.Length < 2 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Vector2( vector2Values[0], vector2Values[1] );
                            break;

                        case "Vector3":
                            var vector3Values = stringValue.Split( ' ' )?.Select( s => Single.Parse( s ) ).ToArray( );

                            if ( vector3Values == null || vector3Values.Length < 3 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Vector3( vector3Values[0], vector3Values[1], vector3Values[2] );
                            break;

                        case "Vector4":
                            var vector4Values = stringValue.Split( ' ' )?.Select( s => Single.Parse( s ) ).ToArray( );

                            if ( vector4Values == null || vector4Values.Length < 4 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Vector4( vector4Values[0], vector4Values[1], vector4Values[2], vector4Values[3] );
                            break;

                        case "Rectangle":
                            var rectValues = stringValue.Split( ' ' )?.Select( s => Int32.Parse( s ) ).ToArray( );

                            if ( rectValues == null || rectValues.Length < 4 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Rectangle( rectValues[0], rectValues[1], rectValues[2], rectValues[3] );
                            break;

                        case "Point":
                            var pointValues = stringValue.Split( ' ' )?.Select( s => Int32.Parse( s ) ).ToArray( );

                            if ( pointValues == null || pointValues.Length < 2 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Point( pointValues[0], pointValues[1] );
                            break;

                        case "Size":
                            var sizeValues = stringValue.Split( ' ' )?.Select( s => Int32.Parse( s ) ).ToArray( );

                            if ( sizeValues == null || sizeValues.Length < 2 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = new Size( sizeValues[0], sizeValues[1] );
                            break;

                        case "Color":
                            var colourValues = stringValue.Split( ' ' )?
                                .Select( s => ( Int32 ) ( Single.Parse( s ) * 255f ) )
                                .ToArray( );

                            if ( colourValues == null || colourValues.Length < 3 )
                                Utilities.Error( $"Failed to set value for {Name}, invalid format" );
                            else
                                Value = Color.FromArgb( colourValues[0], colourValues[1], colourValues[2], colourValues.Length < 4 ? 255 : colourValues[3] );
                            break;
                    }
                }
                else
                    Utilities.Error( $"Failed to set value for {Name}, expected {ValueType.Name} got {newValueType.Name}" );
            }
            else
                Value = value;
            //var changed = ( String.Compare( _String, value ) != 0 );
            //if ( !changed )
            //    return;

            //_String = value;
            //_Value = MathLib.atof( _String );

            //if ( IsServer && CommandInstance.Host.Server.sv.active )
            //{
            //    CommandInstance.Host.Server.BroadcastPrint( "\"{0}\" changed to \"{1}\"\n", _Name, _String );
            //}
        }
    }
}
