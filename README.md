# UnityGGPO

A interop dll warpper to ggpo library that works with unity. Uses unities native collections library.

Build:

run build_windows.cmd to build the solution.
the INSTALL project copies into the Unity Plugin folder.

Use:

Call direct access to the ggpo library calls using unsafe code and IntPtr function pointers or use the GGPO.Session helper class to have safe access.
