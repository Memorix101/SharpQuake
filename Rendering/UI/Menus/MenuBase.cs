using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public abstract class MenuBase
    {
        public static MenuBase CurrentMenu
        {
            get
            {
                return _CurrentMenu;
            }
        }

        public Int32 Cursor
        {
            get
            {
                return _Cursor;
            }
        }

        // Top level menu items
        public static readonly MenuBase MainMenu = new MainMenu( );

        public static readonly MenuBase SinglePlayerMenu = new SinglePlayerMenu( );
        public static readonly MenuBase MultiPlayerMenu = new MultiplayerMenu( );
        public static readonly MenuBase OptionsMenu = new OptionsMenu( );
        public static readonly MenuBase HelpMenu = new HelpMenu( );
        public static readonly MenuBase QuitMenu = new QuitMenu( );
        public static readonly MenuBase LoadMenu = new LoadMenu( );
        public static readonly MenuBase SaveMenu = new SaveMenu( );

        // Submenus
        public static readonly MenuBase KeysMenu = new KeysMenu( );

        public static readonly MenuBase LanConfigMenu = new LanConfigMenu( );
        public static readonly MenuBase SetupMenu = new SetupMenu( );
        public static readonly MenuBase GameOptionsMenu = new GameOptionsMenu( );
        public static readonly MenuBase SearchMenu = new SearchMenu( );
        public static readonly MenuBase ServerListMenu = new ServerListMenu( );
        public static readonly MenuBase VideoMenu = new VideoMenu( );
        protected Int32 _Cursor;
        private static MenuBase _CurrentMenu;

        public static void Hide( )
        {
            Key.Destination = keydest_t.key_game;
            _CurrentMenu = null;
        }

        public virtual void Show( )
        {
            Menu.EnterSound = true;
            Key.Destination = keydest_t.key_menu;
            _CurrentMenu = this;
        }

        public abstract void KeyEvent( Int32 key );

        public abstract void Draw( );
    }
}
