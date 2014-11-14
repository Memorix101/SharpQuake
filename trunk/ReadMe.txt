SharpQuake 1.2 release notes

Building and running

To build project successfuly, provide correct reference to OpenTK library (see References).
To build with Windows-related stuff add _WINDOWS symbol to "Conditional Compilation Symbols" (there must be already defined GLQUAKE and QUAKE_GAME symbols).

On x64 platforms select x86 as a Platform.

To run from IDE go to the Project Properties -> Debug tab -> Start Options. In "Command line arguments" edit box add -basedir switch 
and correct path to your directory of Quake (for example, if Quake is installed in C:\Games\Quake):
-basedir C:\Games\Quake

To run in windowed mode add -window comand line switch.

If you have original mod packs (from Rogue or/and from Hipnotic) run them with appropriate switch: -rogue or -hipnotic

All mentioned switches are native Quake switches, so they documented in original Q-docs.


Fixed bugs

1) Audio initialization bug: sometimes programm starts without sound and (with developer=1) there is a 3 messages in console:
UnlockBuffer: No free buffers!
UnlockBuffer: No free buffers!
UnlockBuffer: No free buffers!

2) Wrong index returned by String.IndexOf () in ProgsEdict.cs in Linux (reported by Miguel G L):
For example, in linux "String.IndexOf()" method returns an index "offset = 6633" in the case of Windows "offset = 6632". The correct value is 6632


Enjoy!