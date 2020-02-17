Home Builder v0.4 for Quest

Requirements:
- .NET Core 3.1 Runtime
- Java installed and available from the command-line
- GLTF project exported to 'exported_from_blender':
	- Folder 'textures'
	- File '<name>.gltf'
	- File 'scene.bin'

Usage:
- Run the HomeBuilder executable.

This tool will automatically build 4 versions of the GLTF environment:
- <name>_ClassicHome_NoAudio.apk
- <name>_ClassicHome.apk
- <name>_WinterLodge_NoAudio.apk
- <name>_WinterLodge.apk

<name> will be the name of the *.gltf file inside the 'exported_from_blender' folder.
If a *.ogg file doesn't exist on the current folder, only the '_NoAudio' versions will be built.

The source code of this tool is available at:
https://github.com/QuestMods/HomeBuilder
