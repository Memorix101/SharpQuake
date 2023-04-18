using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Renderer.Textures;

namespace SharpQuake.Renderer.Models
{
	public class BaseAliasModel : BaseModel
	{
		public BaseAliasModelDesc AliasDesc
		{
			get;
			private set;
		}

		public BaseAliasModel( BaseDevice device, BaseAliasModelDesc desc )
			: base( device, desc )
		{
			AliasDesc = desc;
		}

		/// <summary>
		/// R_DrawAliasModel
		/// </summary>
		public virtual void DrawAliasModel( Single shadeLight, Vector3 shadeVector, Single[] shadeDots, Single lightSpotZ, Double realTime, Double time, ref Int32 poseNum, ref Int32 poseNum2, ref Single frameStartTime, ref Single frameInterval, ref Vector3 origin1, ref Vector3 origin2, ref Single translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref Single rotateStartTime, Boolean shadows = true, Boolean smoothModels = true, Boolean affineModels = false, Boolean noColours = false, Boolean isEyes = false, Boolean useInterpolation = true )
		{
			throw new NotImplementedException( );
		}

		/// <summary>
		/// GL_DrawAliasShadow
		/// </summary>
		protected virtual void DrawAliasShadow( Int32 posenum, Single lightSpotZ, Vector3 shadeVector )
		{
			throw new NotImplementedException( );
		}

		protected virtual void DrawAliasBlendedFrame( Single shadeLight, Single[] shadeDots, Int32 posenum, Int32 posenum2, Single blend )
		{
			throw new NotImplementedException( );
		}
		/*
		=================
		R_SetupAliasBlendedFrame

		fenix@io.com: model animation interpolation
		=================
		*/
		protected virtual void SetupAliasBlendedFrame( Single shadeLight, Int32 frame, Double realTime, Double time, Single[] shadeDots, ref Int32 poseNum, ref Int32 poseNum2, ref Single frameStartTime, ref Single frameInterval )
		{
			if ( ( frame >= AliasDesc.AliasHeader.numframes ) || ( frame < 0 ) )
			{
				ConsoleWrapper.Print( "R_AliasSetupFrame: no such frame {0}\n", frame );
				frame = 0;
			}

			var pose = AliasDesc.AliasHeader.frames[frame].firstpose;
			var numposes = AliasDesc.AliasHeader.frames[frame].numposes;

			if ( numposes > 1 )
			{
				var interval = AliasDesc.AliasHeader.frames[frame].interval;
				pose += ( Int32 ) ( time / interval ) % numposes;
				frameInterval = interval;
			}
			else
			{
				/* One tenth of a second is a good for most Quake animations.
				If the nextthink is longer then the animation is usually meant to pause
				( e.g.check out the shambler magic animation in shambler.qc).  If its
				shorter then things will still be smoothed partly, and the jumps will be
				less noticable because of the shorter time.So, this is probably a good
				assumption. */
				frameInterval = 0.1f;
			}

			var blend = 0f;

			var e = AliasDesc.AliasHeader.frames[frame];

			if ( poseNum2 != pose )
			{
				frameStartTime = ( Single ) realTime;
				poseNum = poseNum2;
				poseNum2 = pose;
				blend = 0;
			}
			else
			{
				blend = ( Single ) ( ( realTime - frameStartTime ) / frameInterval );
			}

			// wierd things start happening if blend passes 1
			if ( /*cl.paused || */ blend > 1 )
				blend = 1;

			DrawAliasBlendedFrame( shadeLight, shadeDots, poseNum, poseNum2, blend );
		}

		/// <summary>
		/// R_SetupAliasFrame
		/// </summary>
		protected virtual void SetupAliasFrame( Single shadeLight, Int32 frame, Double time, Single[] shadeDots )
		{
			if ( ( frame >= AliasDesc.AliasHeader.numframes ) || ( frame < 0 ) )
			{
				ConsoleWrapper.Print( "R_AliasSetupFrame: no such frame {0}\n", frame );
				frame = 0;
			}

			var pose = AliasDesc.AliasHeader.frames[frame].firstpose;
			var numposes = AliasDesc.AliasHeader.frames[frame].numposes;

			if ( numposes > 1 )
			{
				var interval = AliasDesc.AliasHeader.frames[frame].interval;
				pose += ( Int32 ) ( time / interval ) % numposes;
			}

			DrawAliasFrame( shadeLight, shadeDots, pose );
		}

		/// <summary>
		/// GL_DrawAliasFrame
		/// </summary>
		protected virtual void DrawAliasFrame( Single shadeLight, Single[] shadeDots, Int32 posenum )
		{
			throw new NotImplementedException( );
		}

		public static BaseAliasModel Create( BaseDevice device, String identifier, BaseTexture texture, aliashdr_t aliasHeader )
		{
			return Create( device, identifier, texture, device.AliasModelType, device.AliasModelDescType, aliasHeader );
		}

		public static BaseAliasModel Create( BaseDevice device, String identifier, BaseTexture texture, Type modelType, Type descType, aliashdr_t aliasHeader )
		{
			if ( ModelPool.ContainsKey( identifier ) )
				return ( BaseAliasModel ) ModelPool[identifier];

			var desc = ( BaseAliasModelDesc ) Activator.CreateInstance( descType );
			desc.Name = identifier;
			desc.Texture = texture;
			desc.AliasHeader = aliasHeader;

			var model = ( BaseAliasModel ) Activator.CreateInstance( modelType, device, desc );
			model.Initialise();

			return model;
		}
	}
}
