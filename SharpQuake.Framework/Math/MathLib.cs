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
using OpenTK;

// mathlib.h
// mathlib.c

namespace SharpQuake.Framework
{
    /// <summary>
    /// Quake math functions
    /// </summary>
    public static class MathLib
    {
        /// <summary>
        /// AngleVectors
        /// </summary>
        public static void AngleVectors( ref Vector3 angles, out Vector3 forward, out Vector3 right, out Vector3 up )
        {
            Double angle, sr, sp, sy, cr, cp, cy;

            angle = angles.Y * ( Math.PI * 2 / 360 );
            sy = Math.Sin( angle );
            cy = Math.Cos( angle );
            angle = angles.X * ( Math.PI * 2 / 360 );
            sp = Math.Sin( angle );
            cp = Math.Cos( angle );
            angle = angles.Z * ( Math.PI * 2 / 360 );
            sr = Math.Sin( angle );
            cr = Math.Cos( angle );

            forward.X = ( Single ) ( cp * cy );
            forward.Y = ( Single ) ( cp * sy );
            forward.Z = -( ( System.Single ) sp );
            right.X = ( Single ) ( -1 * sr * sp * cy + -1 * cr * -sy );
            right.Y = ( Single ) ( -1 * sr * sp * sy + -1 * cr * cy );
            right.Z = ( Single ) ( -1 * sr * cp );
            up.X = ( Single ) ( cr * sp * cy + -sr * -sy );
            up.Y = ( Single ) ( cr * sp * sy + -sr * cy );
            up.Z = ( Single ) ( cr * cp );
        }

        public static Single Normalize( ref Vector3 v )
        {
            var length = v.Length;
            if( length != 0 )
            {
                var ool = 1 / length;
                v.X *= ool;
                v.Y *= ool;
                v.Z *= ool;
            }
            return length;
        }

        public static Single Length( ref Vector3f v )
        {
            return ( Single ) Math.Sqrt( v.x * v.x + v.y * v.y + v.z * v.z );
        }

        public static Single LengthXY( ref Vector3f v )
        {
            return ( Single ) Math.Sqrt( v.x * v.x + v.y * v.y );
        }

        public static Single Normalize( ref Vector3f v )
        {
            var length = ( Single ) Math.Sqrt( v.x * v.x + v.y * v.y + v.z * v.z );
            if( length != 0 )
            {
                var ool = 1 / length;
                v.x *= ool;
                v.y *= ool;
                v.z *= ool;
            }
            return length;
        }

        /// <summary>
        /// c = a + b * scale;
        /// </summary>
        public static void VectorMA( ref Vector3f a, Single scale, ref Vector3f b, out Vector3f c )
        {
            c.x = a.x + b.x * scale;
            c.y = a.y + b.y * scale;
            c.z = a.z + b.z * scale;
        }

        public static void VectorScale( ref Vector3f a, Single scale, out Vector3f b )
        {
            b.x = a.x * scale;
            b.y = a.y * scale;
            b.z = a.z * scale;
        }

        public static void VectorAdd( ref Vector3f a, ref Vector3f b, out Vector3f c )
        {
            c.x = a.x + b.x;
            c.y = a.y + b.y;
            c.z = a.z + b.z;
        }

        public static void VectorSubtract( ref Vector3f a, ref Vector3f b, out Vector3f c )
        {
            c.x = a.x - b.x;
            c.y = a.y - b.y;
            c.z = a.z - b.z;
        }

        public static void Clamp( ref Vector3f src, ref Vector3 min, ref Vector3 max, out Vector3f dest )
        {
            dest.x = Math.Max( Math.Min( src.x, max.X ), min.X );
            dest.y = Math.Max( Math.Min( src.y, max.Y ), min.Y );
            dest.z = Math.Max( Math.Min( src.z, max.Z ), min.Z );
        }

        public static Single DotProduct( ref Vector3f a, ref Vector3f b )
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static Boolean CheckNaN( ref Vector3f v, Single defValue )
        {
            var flag = false;
            if( Single.IsNaN( v.x ) )
            {
                flag = true;
                v.x = defValue;
            }
            if( Single.IsNaN( v.y ) )
            {
                flag = true;
                v.y = defValue;
            }
            if( Single.IsNaN( v.z ) )
            {
                flag = true;
                v.z = defValue;
            }
            return flag;
        }

        public static Single Comp( ref Vector3f a, Int32 index )
        {
            if( index < 0 || index > 2 )
                throw new ArgumentOutOfRangeException( "index" );
            return ( index == 0 ? a.x : ( index == 1 ? a.y : a.z ) );
        }

        public static Single Comp( ref Vector3 a, Int32 index )
        {
            if( index < 0 || index > 2 )
                throw new ArgumentOutOfRangeException( "index" );
            return ( index == 0 ? a.X : ( index == 1 ? a.Y : a.Z ) );
        }

        /// <summary>
        /// anglemod()
        /// </summary>
        public static Single AngleMod( Double a )
        {
            return ( Single ) ( ( 360.0 / 65536 ) * ( ( Int32 ) ( a * ( 65536 / 360.0 ) ) & 65535 ) );
        }

        public static Single DotProduct( ref Vector3 a, ref Vector4 b )
        {
            return ( a.X * b.X + a.Y * b.Y + a.Z * b.Z );
        }

        public static Int32 BoxOnPlaneSide( ref Vector3f emins, ref Vector3f emaxs, mplane_t p )
        {
            Single mindist, maxdist;
            switch( p.type )
            {
                case 0:
                    mindist = emins.x;
                    maxdist = emaxs.x;
                    break;

                case 1:
                    mindist = emins.y;
                    maxdist = emaxs.y;
                    break;

                case 2:
                    mindist = emins.z;
                    maxdist = emaxs.z;
                    break;

                default:
                    Vector3 mins, maxs;
                    Copy( ref emins, out mins );
                    Copy( ref emaxs, out maxs );
                    return _BoxOnPlaneSide( ref mins, ref maxs, p );
            }
            return ( p.dist <= mindist ? 1 : ( p.dist >= maxdist ? 2 : 3 ) );
        }

        public static Int32 BoxOnPlaneSide( ref Vector3 emins, ref Vector3 emaxs, mplane_t p )
        {
            Single mindist, maxdist;
            switch( p.type )
            {
                case 0:
                    mindist = emins.X;
                    maxdist = emaxs.X;
                    break;

                case 1:
                    mindist = emins.Y;
                    maxdist = emaxs.Y;
                    break;

                case 2:
                    mindist = emins.Z;
                    maxdist = emaxs.Z;
                    break;

                default:
                    return _BoxOnPlaneSide( ref emins, ref emaxs, p );
            }
            return ( p.dist <= mindist ? 1 : ( p.dist >= maxdist ? 2 : 3 ) );
        }

        public static void SetComp( ref Vector3 dest, Int32 index, Single value )
        {
            if( index == 0 )
                dest.X = value;
            else if( index == 1 )
                dest.Y = value;
            else if( index == 2 )
                dest.Z = value;
            else
                throw new ArgumentException( "Index must be in range 0-2!" );
        }

        public static void CorrectAngles180( ref Vector3 a )
        {
            if( a.X > 180 )
                a.X -= 360;
            else if( a.X < -180 )
                a.X += 360;
            if( a.Y > 180 )
                a.Y -= 360;
            else if( a.Y < -180 )
                a.Y += 360;
            if( a.Z > 180 )
                a.Z -= 360;
            else if( a.Z < -180 )
                a.Z += 360;
        }

        public static void RotatePointAroundVector( out Vector3 dst, ref Vector3 dir, ref Vector3 point, Single degrees )
        {
            Matrix3 m = Matrix3.CreateFromAxisAngle( dir, ( Single ) ( degrees * Math.PI / 180.0 ) );
            Vector3.Transform( ref point, ref m, out dst );
        }

        public static void Copy( ref Vector3f src, out Vector3 dest )
        {
            dest.X = src.x;
            dest.Y = src.y;
            dest.Z = src.z;
        }

        //    return p - d * n;
        //}
        public static void Copy( ref Vector3 src, out Vector3f dest )
        {
            dest.x = src.X;
            dest.y = src.Y;
            dest.z = src.Z;
        }

        //Returns 1, 2, or 1 + 2
        private static Int32 _BoxOnPlaneSide( ref Vector3 emins, ref Vector3 emaxs, mplane_t p )
        {
            // general case
            Single dist1, dist2;

            switch( p.signbits )
            {
                case 0:
                    dist1 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                    dist2 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                    break;

                case 1:
                    dist1 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                    dist2 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                    break;

                case 2:
                    dist1 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                    dist2 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                    break;

                case 3:
                    dist1 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                    dist2 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                    break;

                case 4:
                    dist1 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                    dist2 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                    break;

                case 5:
                    dist1 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                    dist2 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                    break;

                case 6:
                    dist1 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                    dist2 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                    break;

                case 7:
                    dist1 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                    dist2 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                    break;

                default:
                    dist1 = dist2 = 0;		// shut up compiler
                    Utilities.Error( "BoxOnPlaneSide:  Bad signbits" );
                    break;
            }

            var sides = 0;
            if( dist1 >= p.dist )
                sides = 1;
            if( dist2 < p.dist )
                sides |= 2;

#if PARANOID
            if (sides == 0)
                Utilities.Error("BoxOnPlaneSide: sides==0");
#endif

            return sides;
        }

        //static Vector3 PerpendicularVector(ref Vector3 src)
        //{
        //    float minelem = 1.0f;

        //    // find the smallest magnitude axially aligned vector
        //    Vector3 tempvec = Vector3.Zero;
        //    if (Math.Abs(src.X) < minelem)
        //    {
        //        minelem = Math.Abs(src.X);
        //        tempvec.X = 1;
        //    }
        //    if (Math.Abs(src.Y) < minelem)
        //    {
        //        minelem = Math.Abs(src.Y);
        //        tempvec = new Vector3(0, 1, 0);
        //    }
        //    else if (Math.Abs(src.Z) < minelem)
        //    {
        //        tempvec = new Vector3(0, 0, 1);
        //    }

        //    // project the point onto the plane defined by src
        //    Vector3 dst = ProjectPointOnPlane(ref tempvec, ref src);

        //    Normalize(ref dst);

        //    return dst;
        //}

        //static Vector3 ProjectPointOnPlane(ref Vector3 p, ref Vector3 normal)
        //{
        //    float inv_denom = 1.0f / normal.LengthSquared;
        //    float d = Vector3.Dot(normal, p) * inv_denom;
        //    Vector3 n = normal * inv_denom;
    }
}
