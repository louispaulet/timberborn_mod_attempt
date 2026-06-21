# Timberborn Hello World Mod

A small source-first Timberborn mod that shows a `Hello World` popup after the mod is enabled and a settlement loads.

This project follows the official Timberborn 1.0 modding pipeline. It builds a C# DLL and installs it into Timberborn's local mods directory instead of using BepInEx.

## Requirements

- macOS with Timberborn installed through Steam.
- Timberborn app path:
  `/Users/louispaulet/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app`
- Local mods path:
  `/Users/louispaulet/Documents/Timberborn/Mods`
- .NET SDK. If `dotnet` is not installed globally, `make bootstrap` installs a local SDK under `.tools/dotnet`.

## Useful Commands

```sh
make verify-env
make bootstrap
make build
make package
make install
make launch
make logs
make clean
```

## Build And Install

```sh
make bootstrap
make build
make package
make install
```

`make package` creates `dist/HelloWorld` with the mod manifest and DLL. `make install` copies that folder to `/Users/louispaulet/Documents/Timberborn/Mods/HelloWorld`.

## Verify In Timberborn

1. Run `make install`.
2. Run `make launch`, or open Steam and start Timberborn manually.
3. In the Timberborn Mod Manager, enable `Hello World`.
4. Continue into the game and load or start a settlement.
5. Confirm a popup says `Hello World`.
6. Check logs with `make logs`; the mod should log its startup message in `Player.log`.

## References

- [Mechanistry timberborn-modding repository](https://github.com/mechanistry/timberborn-modding)
- [Official Timberborn modding wiki](https://github.com/mechanistry/timberborn-modding/wiki)
- [Quick start](https://github.com/mechanistry/timberborn-modding/wiki/Quick-start)
- [Coding basics](https://github.com/mechanistry/timberborn-modding/wiki/Coding-basics)
- [User interface](https://github.com/mechanistry/timberborn-modding/wiki/User-interface)
- [Mod directory structure](https://github.com/mechanistry/timberborn-modding/wiki/Mod-directory-structure)
- [Mod management](https://github.com/mechanistry/timberborn-modding/wiki/Mod-management)
