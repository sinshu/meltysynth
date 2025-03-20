cd CSCore
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 CSCore\bin\Release\net8.0\TimGM6mb.sf2

cd DotFeather
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 DotFeather\bin\Release\net8.0\TimGM6mb.sf2

cd DrippyAL
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 DrippyAL\bin\Release\net8.0\TimGM6mb.sf2

cd FNA.NET
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 FNA.NET\bin\Release\net8.0\TimGM6mb.sf2

cd MonoGame
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 MonoGame\bin\Release\net8.0\TimGM6mb.sf2

cd NAudio
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 NAudio\bin\Release\net8.0-windows\TimGM6mb.sf2

cd OpenTK
dotnet build -c Release
cd ..
copy openal32.dll OpenTK\bin\Release\net8.0\openal32.dll
copy TimGM6mb.sf2 OpenTK\bin\Release\net8.0\TimGM6mb.sf2

cd Raylib_cs
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 Raylib_cs\bin\Release\net8.0\TimGM6mb.sf2

cd Raylib_CsLo
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 Raylib_CsLo\bin\Release\net8.0\TimGM6mb.sf2

cd SDL2
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 SDL2\bin\Release\net8.0\TimGM6mb.sf2

cd SFML.Net
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 SFML.Net\bin\Release\net8.0\TimGM6mb.sf2

cd Silk.NET.OpenAL
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 Silk.NET.OpenAL\bin\Release\net8.0\TimGM6mb.sf2

cd Silk.NET.SDL
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 Silk.NET.SDL\bin\Release\net8.0\TimGM6mb.sf2

cd Sokol
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 Sokol\bin\Release\net8.0\TimGM6mb.sf2

cd SoundFlow
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 SoundFlow\bin\Release\net8.0\TimGM6mb.sf2

cd TinyAudio
dotnet build -c Release
cd ..
copy TimGM6mb.sf2 TinyAudio\bin\Release\net8.0\TimGM6mb.sf2

pause
