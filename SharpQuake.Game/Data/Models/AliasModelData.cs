using System;
using System.Linq;
using SharpQuake.Framework;
using SharpQuake.Framework.World;
using SharpQuake.Framework.IO.Alias;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework.Rendering;
using SharpQuake.Game.Rendering.Textures;

namespace SharpQuake.Game.Data.Models
{
	public class AliasModelData : ModelData
    {
        public aliashdr_t Header
        {
            get;
            private set;
        }

        private Int32 PoseNum
        {
            get;
            set;
        }

        public trivertx_t[][] PoseVerts
        {
            get
            {
                return _PoseVerts;
            }
        }

        public stvert_t[] STVerts
        {
            get
            {
                return _STVerts;
            }
        }

        public dtriangle_t[] Triangles
        {
            get
            {
                return _Triangles;
            }
        }

        private stvert_t[] _STVerts = new stvert_t[ModelDef.MAXALIASVERTS]; // stverts
        private dtriangle_t[] _Triangles = new dtriangle_t[ModelDef.MAXALIASTRIS]; // triangles
        private trivertx_t[][] _PoseVerts = new trivertx_t[ModelDef.MAXALIASFRAMES][]; // poseverts

        public AliasModelData( ModelTexture noTexture ) : base( noTexture )
        {

        }

        public void Load( UInt32[] table8to24, String name, Byte[] buffer, Func<String, ByteArraySegment, aliashdr_t, Int32> onLoadSkinTexture, Action<AliasModelData, aliashdr_t> onMakeAliasModelDisplayList )
        {
            Name = name;
            Buffer = buffer;

            var pinmodel = Utilities.BytesToStructure<mdl_t>( Buffer, 0 );

            var version = EndianHelper.LittleLong( pinmodel.version );

            if ( version != ModelDef.ALIAS_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})",
                    Name, version, ModelDef.ALIAS_VERSION );

            //
            // allocate space for a working header, plus all the data except the frames,
            // skin and group info
            //
            Header = new aliashdr_t( );

            Flags = ( EntityFlags ) EndianHelper.LittleLong( pinmodel.flags );

            //
            // endian-adjust and copy the data, starting with the alias model header
            //
            Header.boundingradius = EndianHelper.LittleFloat( pinmodel.boundingradius );
            Header.numskins = EndianHelper.LittleLong( pinmodel.numskins );
            Header.skinwidth = EndianHelper.LittleLong( pinmodel.skinwidth );
            Header.skinheight = EndianHelper.LittleLong( pinmodel.skinheight );

            if ( Header.skinheight > ModelDef.MAX_LBM_HEIGHT )
                Utilities.Error( "model {0} has a skin taller than {1}", Name, ModelDef.MAX_LBM_HEIGHT );

            Header.numverts = EndianHelper.LittleLong( pinmodel.numverts );

            if ( Header.numverts <= 0 )
                Utilities.Error( "model {0} has no vertices", Name );

            if ( Header.numverts > ModelDef.MAXALIASVERTS )
                Utilities.Error( "model {0} has too many vertices", Name );

            Header.numtris = EndianHelper.LittleLong( pinmodel.numtris );

            if ( Header.numtris <= 0 )
                Utilities.Error( "model {0} has no triangles", Name );

            Header.numframes = EndianHelper.LittleLong( pinmodel.numframes );
            var numframes = Header.numframes;
            if ( numframes < 1 )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes );

            Header.size = EndianHelper.LittleFloat( pinmodel.size ) * ModelDef.ALIAS_BASE_SIZE_RATIO;
            SyncType = ( SyncType ) EndianHelper.LittleLong( ( Int32 ) pinmodel.synctype );
            FrameCount = Header.numframes;

            Header.scale = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale ) );
            Header.scale_origin = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale_origin ) );
            Header.eyeposition = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.eyeposition ) );

            //
            // load the skins
            //
            var offset = LoadAllSkins( table8to24, Header.numskins, new ByteArraySegment( buffer, mdl_t.SizeInBytes ), onLoadSkinTexture );

            //
            // load base s and t vertices
            //
            var stvOffset = offset; // in bytes
            for ( var i = 0; i < Header.numverts; i++, offset += stvert_t.SizeInBytes )
            {
                _STVerts[i] = Utilities.BytesToStructure<stvert_t>( buffer, offset );

                _STVerts[i].onseam = EndianHelper.LittleLong( _STVerts[i].onseam );
                _STVerts[i].s = EndianHelper.LittleLong( _STVerts[i].s );
                _STVerts[i].t = EndianHelper.LittleLong( _STVerts[i].t );
            }

            //
            // load triangle lists
            //
            var triOffset = stvOffset + Header.numverts * stvert_t.SizeInBytes;
            offset = triOffset;
            for ( var i = 0; i < Header.numtris; i++, offset += dtriangle_t.SizeInBytes )
            {
                _Triangles[i] = Utilities.BytesToStructure<dtriangle_t>( buffer, offset );
                _Triangles[i].facesfront = EndianHelper.LittleLong( _Triangles[i].facesfront );

                for ( var j = 0; j < 3; j++ )
                    _Triangles[i].vertindex[j] = EndianHelper.LittleLong( _Triangles[i].vertindex[j] );
            }

            //
            // load the frames
            //
            PoseNum = 0;
            var framesOffset = triOffset + Header.numtris * dtriangle_t.SizeInBytes;

            Header.frames = new maliasframedesc_t[Header.numframes];

            for ( var i = 0; i < numframes; i++ )
            {
                var frametype = ( aliasframetype_t ) BitConverter.ToInt32( buffer, framesOffset );
                framesOffset += 4;

                if ( frametype == aliasframetype_t.ALIAS_SINGLE )
                {
                    framesOffset = LoadAliasFrame( new ByteArraySegment( buffer, framesOffset ), ref Header.frames[i] );
                }
                else
                {
                    framesOffset = LoadAliasGroup( new ByteArraySegment( buffer, framesOffset ), ref Header.frames[i] );
                }
            }

            Header.numposes = PoseNum;

            Type = ModelType.mod_alias;

            // FIXME: do this right
            BoundsMin = -Vector3.One * 16.0f;
            BoundsMax = -BoundsMin;

            //
            // build the draw lists
            //
            onMakeAliasModelDisplayList( this, Header );
            //mesh.MakeAliasModelDisplayLists( mod, Header );

            //
            // move the complete, relocatable alias model to the cache
            //
            //cache = Host.Cache.Alloc( aliashdr_t.SizeInBytes * Header.frames.Length * maliasframedesc_t.SizeInBytes, null );

            //if ( cache == null )
            //    return;

            //cache.data = Header;
        }

        /// <summary>
        /// Mod_LoadAllSkins
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAllSkins( UInt32[] table8to24, Int32 numskins, ByteArraySegment data, Func<String, ByteArraySegment, aliashdr_t, Int32> onLoadSkinTexture )
        {
            if ( numskins < 1 || numskins > ModelDef.MAX_SKINS )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins );

            var offset = data.StartIndex;
            var skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
            var s = Header.skinwidth * Header.skinheight;

            var pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );

            for ( var i = 0; i < numskins; i++ )
            {
                if ( pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE )
                {
                    FloodFillSkin( table8to24, new ByteArraySegment( data.Data, skinOffset ), Header.skinwidth, Header.skinheight );

                    // save 8 bit texels for the player model to remap
                    var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                    Header.texels[i] = texels;// -(byte*)pheader;
                    System.Buffer.BlockCopy( data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s );

                    // set offset to pixel data after daliasskintype_t block...
                    offset += daliasskintype_t.SizeInBytes;

                    var name = Name + "_" + i.ToString( );

                    var index = onLoadSkinTexture( name, new ByteArraySegment( data.Data, offset ), Header );
                    
                    Header.gl_texturenum[i, 0] =
                    Header.gl_texturenum[i, 1] =
                    Header.gl_texturenum[i, 2] =
                    Header.gl_texturenum[i, 3] = index;
                    // Host.DrawingContext.LoadTexture( name, Header.skinwidth,
                    //Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); // (byte*)(pskintype + 1)

                    // set offset to next daliasskintype_t block...
                    offset += s;
                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                }
                else
                {
                    // animating skin group.  yuck.
                    offset += daliasskintype_t.SizeInBytes;
                    var pinskingroup = Utilities.BytesToStructure<daliasskingroup_t>( data.Data, offset );
                    var groupskins = EndianHelper.LittleLong( pinskingroup.numskins );
                    offset += daliasskingroup_t.SizeInBytes;
                    var pinskinintervals = Utilities.BytesToStructure<daliasskininterval_t>( data.Data, offset );

                    offset += daliasskininterval_t.SizeInBytes * groupskins;

                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    Int32 j;
                    for ( j = 0; j < groupskins; j++ )
                    {
                        FloodFillSkin( table8to24, new ByteArraySegment( data.Data, skinOffset ), Header.skinwidth, Header.skinheight );
                        if ( j == 0 )
                        {
                            var texels = new Byte[s]; // Hunk_AllocName(s, loadname);
                            Header.texels[i] = texels;// -(byte*)pheader;
                            System.Buffer.BlockCopy( data.Data, offset, texels, 0, s );
                        }

                        var name = String.Format( "{0}_{1}_{2}", Name, i, j );

                        var index = onLoadSkinTexture( name, new ByteArraySegment( data.Data, offset ), Header );
                                               
                        Header.gl_texturenum[i, j & 3] = index;// //  (byte*)(pskintype)

                        offset += s;

                        pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    }
                    var k = j;
                    for ( ; j < 4; j++ )
                        Header.gl_texturenum[i, j & 3] = Header.gl_texturenum[i, j - k];
                }
            }

            return offset;// (void*)pskintype;
        }

        /// <summary>
        /// Mod_LoadAliasFrame
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAliasFrame( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var pdaliasframe = Utilities.BytesToStructure<daliasframe_t>( pin.Data, pin.StartIndex );

            frame.name = Utilities.GetString( pdaliasframe.name );
            frame.firstpose = PoseNum;
            frame.numposes = 1;
            frame.bboxmin.Init( );
            frame.bboxmax.Init( );

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about
                // endianness
                frame.bboxmin.v[i] = pdaliasframe.bboxmin.v[i];
                frame.bboxmax.v[i] = pdaliasframe.bboxmax.v[i];
            }

            var verts = new trivertx_t[Header.numverts];
            var offset = pin.StartIndex + daliasframe_t.SizeInBytes; //pinframe = (trivertx_t*)(pdaliasframe + 1);
            for ( var i = 0; i < verts.Length; i++, offset += trivertx_t.SizeInBytes )
            {
                verts[i] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset );
            }
            _PoseVerts[PoseNum] = verts;
            PoseNum++;

            return offset;
        }

        /// <summary>
        /// Mod_LoadAliasGroup
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private Int32 LoadAliasGroup( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var offset = pin.StartIndex;
            var pingroup = Utilities.BytesToStructure<daliasgroup_t>( pin.Data, offset );
            var numframes = EndianHelper.LittleLong( pingroup.numframes );

            frame.Init( );
            frame.firstpose = PoseNum;
            frame.numposes = numframes;

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about endianness
                frame.bboxmin.v[i] = pingroup.bboxmin.v[i];
                frame.bboxmin.v[i] = pingroup.bboxmax.v[i];
            }

            offset += daliasgroup_t.SizeInBytes;
            var pin_intervals = Utilities.BytesToStructure<daliasinterval_t>( pin.Data, offset ); // (daliasinterval_t*)(pingroup + 1);

            frame.interval = EndianHelper.LittleFloat( pin_intervals.interval );

            offset += numframes * daliasinterval_t.SizeInBytes;

            for ( var i = 0; i < numframes; i++ )
            {
                var tris = new trivertx_t[Header.numverts];
                var offset1 = offset + daliasframe_t.SizeInBytes;
                for ( var j = 0; j < Header.numverts; j++, offset1 += trivertx_t.SizeInBytes )
                {
                    tris[j] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset1 );
                }
                _PoseVerts[PoseNum] = tris;
                PoseNum++;

                offset += daliasframe_t.SizeInBytes + Header.numverts * trivertx_t.SizeInBytes;
            }

            return offset;
        }

        /// <summary>
        /// Mod_FloodFillSkin
        /// Fill background pixels so mipmapping doesn't have haloes - Ed
        /// </summary>
        private void FloodFillSkin( UInt32[] table8To24, ByteArraySegment skin, Int32 skinwidth, Int32 skinheight )
        {
            var filler = new FloodFiller( skin, skinwidth, skinheight );
            filler.Perform( table8To24 );
        }

        public override void Clear( )
        {
            base.Clear( );

            Header = null;
            PoseNum = 0;
            _PoseVerts = null;
            _STVerts = null;
            _Triangles = null;
        }

        public override void CopyFrom( ModelData src )
        {
            base.CopyFrom( src );

            Type = ModelType.mod_alias;

            if ( ! ( src is AliasModelData ) )
                return;
            
            var aliasSrc = ( AliasModelData ) src;

            Header = aliasSrc.Header;
            PoseNum = aliasSrc.PoseNum;
            _PoseVerts = aliasSrc.PoseVerts.ToArray( );
            _STVerts = aliasSrc.STVerts.ToArray( );
            _Triangles = aliasSrc.Triangles.ToArray( );
        }
    }
}
