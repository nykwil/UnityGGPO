# UnityGGPO

This is home of UnityGGPO an interop DLL of the ggpo library that works with Unity 3D. This repository is and an amalgam of a few things if you are looking for just the Plugin or the Shared Game Library see Packages section below UPM git url is https://github.com/nykwil/UnityGGPO.git?path=/Unity/Packages/UnityGGPO

## Contents
- A Unity project (/Unity) containing.
  - Package of the plugin wrapper (/Unity/Packages/UnityGGPO)
  - Package for a Shared Game Library (/Unity/Packages/SharedGame)
  - Tests (/Unity/Assets/Tests)
  - VectorWar example with a view like implementation using the Shared Game Library (/Unity/Assets/VectorWar)
  - EcsWar example made with DOTS using the Shared Game Library (/Unity/Assets/EcsWar)
- The CMake project to build the UnityGGPO DLL (/UnityGGPO)
- A submodule of https://github.com/pond3r/ggpo for building the UnityGGPO.dll

## Packages
### Plugin
This is the bare bones wrapper with special Session abstraction layer to make it easier to use then direct DLL calls. Included is only the windows DLLs built from the CMake project found at /UnityGGPO/UnityGGPO

Two ways to use:
- Direct access to the ggpo library using the GGPO class requires unsafe calls and IntPtr function pointers.
- Use the GGPO.Session helper class to have safe access using Unity's native collections library and delegates.

Add to your project in the Package Manager using "Add package from git URL..." with this URL:
https://github.com/nykwil/UnityGGPO.git?path=/Unity/Packages/UnityGGPO

### Share Game Library
This is some boiler plate game layer so that you can create local and multiplayer enabled games with just some simple interfaces implementations. Includes very basic UI and Dialogs for the game. Also has an implementation of the Performance Dialog which you need in some capapcity to run. See the EcsWar of VectorWar examples at https://github.com/nykwil/UnityGGPO. For more information. 

Add to your project in the Package Manager using "Add package from git URL..." with this URL:
https://github.com/nykwil/UnityGGPO.git?path=/Unity/Packages/SharedGame

## Examples and Tests
### VectorWar

VectorWar example using a view like implementation. Found at /Unity/Assets/VectorWar

How to play
- Run the VectorWar.scene.
- Left UI panel is your player index and the connection list.
- Click Start Session

### EcsWar
An example made with DOTS using the Shared Game Library. This is a work in progress. Rollback eventually fails if you are using SharedComponents. Found at /Unity/Assets/EcsWar

## DLL CMake Project

This is the CMake Project found at /UnityGGPO. Windows version only.

Build:
- run build_windows.cmd to build the solution.
- build and run the INSTALL project to copy the built DLL into the Unity Plugin folder for the UnityGGPO package.







Feedback always welcome.

TODO
-Shared Game Library needs better documentation.
-Better tests, unit tests, and a way to run sync test.
-ECS rollback needs to be fixed so that it works with SharedComponents
