﻿/// This program is free software; you can redistribute it and/or
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

using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Remnants of gl_mesh.c

namespace SharpQuake.Game.Data.Models
{
	public class AliasModelBuilder
	{
		private const Int32 MAX_COMMANDS = 8192;
		private const Int32 MAX_STRIP = 128;

		private ModelData _AliasModel; // AliasModelData
		private aliashdr_t _AliasHdr; // paliashdr

		private Byte[] _Used = new Byte[MAX_COMMANDS]; // qboolean used. changed to vyte because can have values 0, 1, 2...

		// the command list holds counts and s/t values that are valid for
		// every frame
		private Int32[] _Commands = new Int32[MAX_COMMANDS]; // commands

		private Int32 _NumCommands; // numcommands

		// all frames will have their vertexes rearranged and expanded
		// so they are in the order expected by the command list
		private Int32[] _VertexOrder = new Int32[MAX_COMMANDS]; // vertexorder

		private Int32 _NumOrder; // numorder

		private Int32 _AllVerts; // allverts
		private Int32 _AllTris; // alltris

		private Int32[] _StripVerts = new Int32[MAX_STRIP]; // stripverts
		private Int32[] _StripTris = new Int32[MAX_STRIP]; // striptris
		private Int32 _StripCount; // stripcount

		private void CheckForCachedAliasModelDisplayList( AliasModelData m )
		{
			var path = Path.ChangeExtension( "glquake/" + Path.GetFileNameWithoutExtension( m.Name ), ".ms2" );

			DisposableWrapper<BinaryReader> file;
			FileSystem.FOpenFile( path, out file );
			if ( file != null )
			{
				using ( file )
				{
					var reader = file.Object;
					_NumCommands = reader.ReadInt32();
					_NumOrder = reader.ReadInt32();
					for ( var i = 0; i < _NumCommands; i++ )
						_Commands[i] = reader.ReadInt32();
					for ( var i = 0; i < _NumOrder; i++ )
						_VertexOrder[i] = reader.ReadInt32();
				}
			}
			else
			{
				//
				// build it from scratch
				//
				ConsoleWrapper.Print( "meshing {0}...\n", m.Name );

				BuildTris( m );     // trifans or lists

				//
				// save out the cached version
				//
				var fullpath = Path.Combine( FileSystem.GameDir, path );
				Stream fs = FileSystem.OpenWrite( fullpath, true );
				if ( fs != null )
					using ( var writer = new BinaryWriter( fs, Encoding.ASCII ) )
					{
						writer.Write( _NumCommands );
						writer.Write( _NumOrder );
						for ( var i = 0; i < _NumCommands; i++ )
							writer.Write( _Commands[i] );
						for ( var i = 0; i < _NumOrder; i++ )
							writer.Write( _VertexOrder[i] );
					}
			}
		}

		private void SaveAliasModelDisplayList( AliasModelData m )
		{
			_AliasHdr.poseverts = _NumOrder;

			var cmds = new Int32[_NumCommands]; //Hunk_Alloc (numcommands * 4);
			_AliasHdr.commands = cmds; // in bytes??? // (byte*)cmds - (byte*)paliashdr;
			Buffer.BlockCopy( _Commands, 0, cmds, 0, _NumCommands * 4 ); //memcpy (cmds, commands, numcommands * 4);

			var poseverts = m.PoseVerts;
			var verts = new trivertx_t[_AliasHdr.numposes * _AliasHdr.poseverts]; // Hunk_Alloc (paliashdr->numposes * paliashdr->poseverts * sizeof(trivertx_t) );
			_AliasHdr.posedata = verts; // (byte*)verts - (byte*)paliashdr;
			var offset = 0;

			for ( var i = 0; i < _AliasHdr.numposes; i++ )
				for ( var j = 0; j < _NumOrder; j++ )
					verts[offset++] = poseverts[i][_VertexOrder[j]];  // *verts++ = poseverts[i][vertexorder[j]];
		}

		/// <summary>
		/// GL_MakeAliasModelDisplayLists
		/// </summary>
		public void MakeDisplayLists( AliasModelData m )
		{
			_AliasModel = m;
			_AliasHdr = m.Header;

			// Look for a cached version
			CheckForCachedAliasModelDisplayList( m );

			// Save the data out
			SaveAliasModelDisplayList( m );
		}

		/// <summary>
		/// BuildTris
		/// Generate a list of trifans or strips for the model, which holds for all frames
		/// </summary>
		private void BuildTris( AliasModelData m )
		{
			var bestverts = new Int32[1024];
			var besttris = new Int32[1024];

			// Uze
			// All references to pheader from model.c changed to _AliasHdr (former paliashdr)

			//
			// build tristrips
			//
			var stverts = m.STVerts;
			var triangles = m.Triangles;
			_NumOrder = 0;
			_NumCommands = 0;
			Array.Clear( _Used, 0, _Used.Length ); // memset (used, 0, sizeof(used));
			Int32 besttype = 0, len;
			for ( var i = 0; i < _AliasHdr.numtris; i++ )
			{
				// pick an unused triangle and start the trifan
				if ( _Used[i] != 0 )
					continue;

				var bestlen = 0;
				for ( var type = 0; type < 2; type++ )
				{
					for ( var startv = 0; startv < 3; startv++ )
					{
						if ( type == 1 )
							len = StripLength( m, i, startv );
						else
							len = FanLength( m, i, startv );
						if ( len > bestlen )
						{
							besttype = type;
							bestlen = len;
							for ( var j = 0; j < bestlen + 2; j++ )
								bestverts[j] = _StripVerts[j];
							for ( var j = 0; j < bestlen; j++ )
								besttris[j] = _StripTris[j];
						}
					}
				}

				// mark the tris on the best strip as used
				for ( var j = 0; j < bestlen; j++ )
					_Used[besttris[j]] = 1;

				if ( besttype == 1 )
					_Commands[_NumCommands++] = ( bestlen + 2 );
				else
					_Commands[_NumCommands++] = -( bestlen + 2 );

				var uval = Union4b.Empty;
				for ( var j = 0; j < bestlen + 2; j++ )
				{
					// emit a vertex into the reorder buffer
					var k = bestverts[j];
					_VertexOrder[_NumOrder++] = k;

					// emit s/t coords into the commands stream
					Single s = stverts[k].s;
					Single t = stverts[k].t;
					if ( triangles[besttris[0]].facesfront == 0 && stverts[k].onseam != 0 )
						s += _AliasHdr.skinwidth / 2;   // on back side
					s = ( s + 0.5f ) / _AliasHdr.skinwidth;
					t = ( t + 0.5f ) / _AliasHdr.skinheight;

					uval.f0 = s;
					_Commands[_NumCommands++] = uval.i0;
					uval.f0 = t;
					_Commands[_NumCommands++] = uval.i0;
				}
			}

			_Commands[_NumCommands++] = 0;      // end of list marker

			ConsoleWrapper.DPrint( "{0,3} tri {1,3} vert {2,3} cmd\n", _AliasHdr.numtris, _NumOrder, _NumCommands );

			_AllVerts += _NumOrder;
			_AllTris += _AliasHdr.numtris;
		}

		private Int32 StripLength( AliasModelData m, Int32 starttri, Int32 startv )
		{
			_Used[starttri] = 2;

			var triangles = m.Triangles;

			var vidx = triangles[starttri].vertindex; //last = &triangles[starttri];
			_StripVerts[0] = vidx[( startv ) % 3];
			_StripVerts[1] = vidx[( startv + 1 ) % 3];
			_StripVerts[2] = vidx[( startv + 2 ) % 3];

			_StripTris[0] = starttri;
			_StripCount = 1;

			var m1 = _StripVerts[2]; // last->vertindex[(startv + 2) % 3];
			var m2 = _StripVerts[1]; // last->vertindex[(startv + 1) % 3];
			var lastfacesfront = triangles[starttri].facesfront;

		// look for a matching triangle
		nexttri:
			for ( var j = starttri + 1; j < _AliasHdr.numtris; j++ )
			{
				if ( triangles[j].facesfront != lastfacesfront )
					continue;

				vidx = triangles[j].vertindex;

				for ( var k = 0; k < 3; k++ )
				{
					if ( vidx[k] != m1 )
						continue;
					if ( vidx[( k + 1 ) % 3] != m2 )
						continue;

					// this is the next part of the fan

					// if we can't use this triangle, this tristrip is done
					if ( _Used[j] != 0 )
						goto done;

					// the new edge
					if ( ( _StripCount & 1 ) != 0 )
						m2 = vidx[( k + 2 ) % 3];
					else
						m1 = vidx[( k + 2 ) % 3];

					_StripVerts[_StripCount + 2] = triangles[j].vertindex[( k + 2 ) % 3];
					_StripTris[_StripCount] = j;
					_StripCount++;

					_Used[j] = 2;
					goto nexttri;
				}
			}
		done:

			// clear the temp used flags
			for ( var j = starttri + 1; j < _AliasHdr.numtris; j++ )
				if ( _Used[j] == 2 )
					_Used[j] = 0;

			return _StripCount;
		}

		private Int32 FanLength( AliasModelData m, Int32 starttri, Int32 startv )
		{
			_Used[starttri] = 2;

			var triangles = m.Triangles;
			//last = &triangles[starttri];

			var vidx = triangles[starttri].vertindex;

			_StripVerts[0] = vidx[( startv ) % 3];
			_StripVerts[1] = vidx[( startv + 1 ) % 3];
			_StripVerts[2] = vidx[( startv + 2 ) % 3];

			_StripTris[0] = starttri;
			_StripCount = 1;

			var m1 = vidx[( startv + 0 ) % 3];
			var m2 = vidx[( startv + 2 ) % 3];
			var lastfacesfront = triangles[starttri].facesfront;

		// look for a matching triangle
		nexttri:
			for ( var j = starttri + 1; j < _AliasHdr.numtris; j++ )//, check++)
			{
				vidx = triangles[j].vertindex;
				if ( triangles[j].facesfront != lastfacesfront )
					continue;

				for ( var k = 0; k < 3; k++ )
				{
					if ( vidx[k] != m1 )
						continue;
					if ( vidx[( k + 1 ) % 3] != m2 )
						continue;

					// this is the next part of the fan

					// if we can't use this triangle, this tristrip is done
					if ( _Used[j] != 0 )
						goto done;

					// the new edge
					m2 = vidx[( k + 2 ) % 3];

					_StripVerts[_StripCount + 2] = m2;
					_StripTris[_StripCount] = j;
					_StripCount++;

					_Used[j] = 2;
					goto nexttri;
				}
			}
		done:

			// clear the temp used flags
			for ( var j = starttri + 1; j < _AliasHdr.numtris; j++ )
				if ( _Used[j] == 2 )
					_Used[j] = 0;

			return _StripCount;
		}
	}
}
