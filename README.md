# Mouse Mover

A tiny Windows app that keeps your PC awake: while it is running, Windows does not go to sleep
or turn off the screen, and your status in chat apps stays "active".
A single `.exe` of about 20 KB: no installation and no administrator rights required.

## Usage

1. Download `MouseMover.exe` from the [Releases](../../releases) page and double-click it.
2. A green icon appears in the system tray, next to the clock. No window opens.
3. Double-click the icon to pause (grey) or resume (green).
4. Right-click the icon for the menu: **Metti in pausa** (pause), **Avvia con Windows** (start with Windows), **Esci** (exit).

The exe is not signed, so on first launch Windows may show "Windows protected your PC".
Click "More info" and then "Run anyway".

## How it works

- Every 30 seconds it checks how long mouse and keyboard have been idle.
- If you have been idle for at least 60 seconds, it presses F15 (a key no program uses)
  and moves the mouse 1 pixel back and forth. While you are working it does nothing.
- While it is running, Windows does not go to sleep and does not turn off the screen.
- "Start with Windows" only writes to the per-user `HKCU\...\Run` key, no administrator rights needed.
- It runs on the .NET Framework 4 that ships with Windows 10 and 11.

## Building

On Windows, with nothing to install:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /win32icon:src\MouseMover.ico /out:MouseMover.exe src\MouseMover.cs
```

On Linux or macOS with Mono:

```sh
mcs -target:winexe -sdk:4.5 -optimize+ -win32icon:src/MouseMover.ico -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:MouseMover.exe src/MouseMover.cs
```

Every push to `main` builds the exe with GitHub Actions. To publish a release with the exe attached,
go to Actions → Build → Run workflow and enter the version (e.g. `v1.1.0`). A `v*` tag also publishes a release.
