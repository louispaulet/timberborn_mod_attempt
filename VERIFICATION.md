# Verification

Verified on 2026-06-21 22:45:22 CEST with Timberborn `v1.1.0.1-652531f-xsm` running from the Steam install.

## Commands

```sh
make verify-env
make bootstrap
make build
make package
make install
```

`make install` copied the packaged mod to:

```text
/Users/louispaulet/Documents/Timberborn/Mods/HelloWorld
```

## Game Check

- Steam was opened before launching Timberborn.
- Timberborn's Mod Manager listed `Hello World v0.1.0` and it was enabled.
- Timberborn was loaded into the existing experimental save with:

```sh
Timberborn -skipModManager -settlementName "DaSauce" -saveName "DaSauce - 67"
```

## Results

`Player.log` contained:

```text
- Hello World (v0.1.0)
[LouisPaulet.HelloWorld] Hello World mod started from: /Users/louispaulet/Documents/Timberborn/Mods/HelloWorld
[LouisPaulet.HelloWorld] Showing Hello World popup.
```

The loaded settlement displayed an in-game popup with the exact message:

```text
Hello World
```

A local screenshot was saved to `verification/hello-world-popup.png` for inspection. It is intentionally ignored because it is a generated 9.5 MB image.
