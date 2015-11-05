SharpQuake 

SharpQuake is a GLQuake rewritten in C# (.Net Framework 2.0) and using OpenTK library.

Building and running

To build project successfuly, provide correct reference to OpenTK library (see References).
To build with Windows-related stuff add _WINDOWS symbol to "Conditional Compilation Symbols" (there must be already defined GLQUAKE and QUAKE_GAME symbols).

On x64 platforms select x86 as a Platform.

To run from any folder except Quake's one add -basedir switch and correct path to your directory of Quake (for example, if Quake is installed in C:\Games\Quake):
SharpQuake.exe -basedir C:\Games\Quake

To run from IDE go to the Project Properties -> Debug tab -> Start Options. In "Command line arguments" edit box add -basedir switch 
and correct path to your directory of Quake (for example, if Quake is installed in C:\Games\Quake):
-basedir C:\Games\Quake

To run in windowed mode add -window comand line switch.

If you have original mod packs (from Rogue or/and from Hipnotic) run them with appropriate switch: -rogue or -hipnotic

All mentioned switches are native Quake switches, so they documented in original Q-docs.

Enjoy!
