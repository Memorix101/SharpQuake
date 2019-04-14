# Description 

SharpQuake is [GLQuake](https://github.com/dpteam/GLQuake3D) rewritten in C# using the [OpenTK](https://github.com/opentk/opentk) library.

# Dependencies
* OpenTK 3.0.1
* [x86 OpenTK Dependencies](https://github.com/opentk/opentk-dependencies/tree/master/x86) (SDL2.dll & libEGL.dll)
  
# Building

**Project is built against and tested for Visual Studio 2017 on .NET 4.7.1.**

1) **Add the OpenTK nuget package with the package manager console in visual studio.**
    - `Install-Package OpenTK -Version 3.0.1`

2) **Add SDL2.dll and libEGL.dll to your output directory (defaults to `<project root>/Quake`).**
    - See link "x86 OpenTK Dependencies" under Dependencies header above
3) **Add ld1, hipnotic, and rogue (minimum ld1) data directories to `<project root>/Quake`.**
    - You only need the `<mod>\pak0.pak`, etc. and `<mod>\config.cfg` files of each directory if copying from a steam install.

The project is set up with windows (x86) as the default platform. (untested) removing the `_WINDOWS` symbol from `project properties > Build > Conditional compilation symbols` should remove this dependency. `GLQUAKE` and `QUAKE_GAME` must be defined.

4) **Build solution.**

# Running

The `-basedir <directory>` switch can be used to change the default data directory. e.g. `SharpQuake.exe -basedir C:\Quake\`

`-window` switch is pre-defined in project settings, but if you change the default directory of where you want your data, you may need to add `-basedir <directory>` to `project properties > Debug > Command line arguments` 

The original expansion packs can be run using `-rogue` or `-hipnotic` switches if the data is present for it.

Original quake switches apply and can be used.

Enjoy!

![sq](https://cloud.githubusercontent.com/assets/1466920/10977605/5d97bd68-83f2-11e5-8d72-26691129cbff.jpg)


# Credits
* Made by **[yurykiselev](https://sourceforge.net/u/yurykiselev/profile/)** and **Uze** and brought to Github by **[Memorix101](https://github.com/Memorix101)** 

* Updated to .NET 4.7.1 and OpenTK 3.0.1 by Daniel Cornelius (Kerfuffles/NukeAndBeans)

* Mod support additions and fixes by **[multiguy18](https://github.com/multiguy18)** 
