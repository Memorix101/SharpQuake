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
/// 

using SharpQuake.Framework.Factories;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Renderer.Textures;
using SharpQuake.Rendering.UI;
using SharpQuake.Rendering.UI.Elements;
using SharpQuake.Rendering.UI.Elements.Text;
using SharpQuake.Rendering.UI.Elements.HUD;
using System;
using SharpQuake.Framework.Rendering.UI;
using SharpQuake.Rendering.UI.Elements.Warnings;

namespace SharpQuake.Factories.Rendering.UI
{
	/// <summary>
	/// Factory to manage rendering UI elements
	/// </summary>
	public class ElementFactory : BaseFactory<String, IElementRenderer>
	{
		// HUD elements
		public const String HUD = "Hud";
		public const String INTERMISSION = "IntermissionOverlay";
		public const String FINALE = "FinaleOverlay";
		public const String SP_SCOREBOARD = "SPScoreboard";
		public const String MP_SCOREBOARD = "MPScoreboard";
		public const String MP_MINI_SCOREBOARD = "MPMiniScoreboard";
		public const String FRAGS = "Frags";
		public const String CONSOLE = "VisualConsole";

		// Dynamic text elements
		public const String CENTRE_PRINT = "CentrePrint";
		public const String FPS = "FPSCounter";
		public const String MODAL = "ModalMessage";

		// Static elements
		public const String LOADING = "Loading";
		public const String RAM = "RAMWarning";
		public const String TURTLE = "LagWarning";
		public const String NET = "NetLagWarning";
		public const String PAUSE = "Pause";

		public Host Host
		{
			get;
			private set;
		}

		public ElementFactory( Host host )
        {
			Host = host;

			Configure( );
		}

		/// <summary>
		/// Configure common UI elements
		/// </summary>
		private void Configure()
		{
			// HUD elements
			Add( new Hud( Host ) );
			Add( new SPScoreboard( Host ) );
			Add( new MPScoreboard( Host ) );
			Add( new MPMiniScoreboard( Host ) );
			Add( new Frags( Host ) );
			Add( new IntermissionOverlay( Host ) );
			Add( new FinaleOverlay( Host ) );
			Add( new VisualConsole( Host ) );

			// Dynamic text elements
			Add( new CentrePrint( Host ) );
			Add( new ModalMessage( Host ) );
			Add( new FPSCounter( Host ) );

			// Static elements
			Add( new Loading( Host ) );
			Add( new RAMWarning( Host ) );
			Add( new LagWarning( Host ) );
			Add( new NetLagWarning( Host ) );
			Add( new Pause( Host ) );
		}

		/// <summary>
		/// Initialise all UI elements
		/// </summary>
		public void Initialise()
        {
			foreach ( var element in DictionaryItems.Values )
            {
				if ( !element.ManualInitialisation )
					element.Initialise();
            }
        }

		/// <summary>
		/// Manually initialise an element
		/// </summary>
		/// <param name="name"></param>
		public void Initialise( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			DictionaryItems[name].Initialise( );
		}

		/// <summary>
		/// Add a UI element
		/// </summary>
		/// <remarks>
		/// (Must be called before Initialise)
		/// </remarks>
		/// <param name="textRenderer"></param>
		public void Add( IElementRenderer textRenderer )
		{
			Add( textRenderer.GetType( ).Name, textRenderer );
		}

		/// <summary>
		/// Get a UI element
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T Get<T>( String name )
			where T : BaseUIElement
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return null;

			return ( T ) DictionaryItems[name];
		}

		/// <summary>
		/// Draw a UI element
		/// </summary>
		/// <param name="name"></param>
		public void Draw( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			DictionaryItems[name].Draw( );
		}

		/// <summary>
		/// Check if a UI element is visible
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Boolean IsVisible( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return false;

			return DictionaryItems[name].IsVisible;
		}

		/// <summary>
		/// Set the visibility for a UI element
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isVisible"></param>
		public void SetVisibility( String name, Boolean isVisible )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			DictionaryItems[name].IsVisible = isVisible;
		}

		/// <summary>
		/// Check if a UI element needs re-drawing
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Boolean IsDirty( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return false;

			return DictionaryItems[name].IsDirty;
		}

		/// <summary>
		/// Make a UI element dirty
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isVisible"></param>
		public void SetDirty( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			DictionaryItems[name].IsDirty = true;
		}

		/// <summary>
		/// Show a UI element
		/// </summary>
		/// <param name="name"></param>
		public void Show( String name )
		{
			SetVisibility( name, true );
		}

		/// <summary>
		/// Hide a UI element
		/// </summary>
		/// <param name="name"></param>
		public void Hide( String name )
		{
			SetVisibility( name, false );
		}

		/// <summary>
		/// Enqueue text for a text element
		/// </summary>
		/// <param name="name"></param>
		/// <param name="text"></param>
		public void Enqueue( String name, String text )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			if ( DictionaryItems[name] is ITextRenderer textRenderer )
				textRenderer.Enqueue( text );
		}

		/// <summary>
		/// Reset the state of an element
		/// </summary>
		/// <param name="name"></param>
		public void Reset( String name )
		{
			if ( !DictionaryItems.ContainsKey( name ) )
				return;

			if ( DictionaryItems[name] is IResetableRenderer renderer )
				renderer.Reset( );
		}
	}
}
