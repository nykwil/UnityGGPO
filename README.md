# UnityGGPO

A interop DLL of the ggpo library that works with Unity 3D. 

Build:
- run build_windows.cmd to build the solution.
- build and run the INSTALL project to copy the built DLL into the Unity Plugin folder.

Two ways to use:
- Direct access to the ggpo library calls using unsafe calls and IntPtr function pointers.
- Use the GGPO.Session helper class to have safe access using Unity's native collections library.

