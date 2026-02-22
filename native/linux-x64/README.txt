Place the Linux PicoGK native library here so the app can load it at runtime.

Required file:
  libpicogk.1.7.so   (Linux x64, PicoGK 1.7)

After placing the file, run from the repo root:
  dotnet build
  dotnet run

The .csproj copies this file to the build output (bin/Debug/net10.0/) automatically.
If you built the Raccoon Edition native runtime yourself, copy the resulting .so into this folder.
