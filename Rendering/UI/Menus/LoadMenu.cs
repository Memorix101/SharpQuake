using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    public class LoadMenu : MenuBase
    {
        public const Int32 MAX_SAVEGAMES = 12;
        protected String[] _FileNames; //[MAX_SAVEGAMES]; // filenames
        protected Boolean[] _Loadable; //[MAX_SAVEGAMES]; // loadable

        public override void Show( Host host )
        {
            base.Show( host );
            ScanSaves( );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.SinglePlayerMenu.Show( Host );
                    break;

                case KeysDef.K_ENTER:
                    snd.LocalSound( "misc/menu2.wav" );
                    if ( !_Loadable[_Cursor] )
                        return;
                    MenuBase.Hide( );

                    // Host_Loadgame_f can't bring up the loading plaque because too much
                    // stack space has been used, so do it now
                    Scr.BeginLoadingPlaque( );

                    // issue the load command
                    Host.CommandBuffer.AddText( String.Format( "load s{0}\n", _Cursor ) );
                    return;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor--;
                    if ( _Cursor < 0 )
                        _Cursor = MAX_SAVEGAMES - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    _Cursor++;
                    if ( _Cursor >= MAX_SAVEGAMES )
                        _Cursor = 0;
                    break;
            }
        }

        public override void Draw( )
        {
            GLPic p = Drawer.CachePic( "gfx/p_load.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );

            for ( var i = 0; i < MAX_SAVEGAMES; i++ )
                Menu.Print( 16, 32 + 8 * i, _FileNames[i] );

            // line cursor
            Menu.DrawCharacter( 8, 32 + _Cursor * 8, 12 + ( ( Int32 ) ( Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_ScanSaves
        /// </summary>
        protected void ScanSaves( )
        {
            for ( var i = 0; i < MAX_SAVEGAMES; i++ )
            {
                _FileNames[i] = "--- UNUSED SLOT ---";
                _Loadable[i] = false;
                var name = String.Format( "{0}/s{1}.sav", FileSystem.GameDir, i );
                FileStream fs = FileSystem.OpenRead( name );
                if ( fs == null )
                    continue;

                using ( StreamReader reader = new StreamReader( fs, Encoding.ASCII ) )
                {
                    var version = reader.ReadLine( );
                    if ( version == null )
                        continue;
                    var info = reader.ReadLine( );
                    if ( info == null )
                        continue;
                    info = info.TrimEnd( '\0', '_' ).Replace( '_', ' ' );
                    if ( !String.IsNullOrEmpty( info ) )
                    {
                        _FileNames[i] = info;
                        _Loadable[i] = true;
                    }
                }
            }
        }

        public LoadMenu( )
        {
            _FileNames = new String[MAX_SAVEGAMES];
            _Loadable = new Boolean[MAX_SAVEGAMES];
        }
    }
}
