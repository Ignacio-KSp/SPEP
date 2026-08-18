# SPEP - Space Exploration Program

A 3D space exploration simulator inspired by Kerbal Space Program (KSP), built in Godot Engine with C#.

## What is it?

SPEP lets you build rockets piece by piece (capsule, fuel tank, engine) in an assembly building (VAB), then launch them at a planet simulated with real physics: Newtonian gravity, atmospheric drag based on altitude and air density, fuel/oxidizer consumption, and KSP-style flight controls (WASD to steer, Q/E to roll, SAS to hold attitude).

## Features

- **VAB (Vehicle Assembly Building):** drag parts with the mouse and they snap onto compatible attachment points automatically, just like KSP.
- **Real orbital physics:** inverse-square gravity, an orbital map that predicts your trajectory (ellipses, hyperbolic escape paths).
- **Atmosphere with variable density:** drag depends on altitude, producing a realistic terminal velocity during atmospheric flight.
- **Real-world scale:** a 600 km radius planet with a 70 km atmosphere, similar to Kerbin.
- **Floating origin system:** keeps physics precision stable even when the ship is far from the starting point.

## How to play

1. You start in the VAB by default.
2. Build your rocket in order: Capsule → Fuel Tank → Engine.
3. Hit "Save and Launch" to move to the launch pad.
4. Flight controls:
   - `Space`: toggle engine on/off
   - `Shift` / `Ctrl`: increase/decrease throttle
   - `W A S D Q E`: steer/rotate the ship
   - `T`: SAS (hold current attitude)
   - `M`: orbital map

## Tech stack

- Engine: Godot 4.7 (C# / Mono)
- Physics: Jolt Physics
