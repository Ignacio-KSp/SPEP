# SPEP - Space Exploration Program

A 3D space exploration simulator inspired by Kerbal Space Program (KSP), built in Godot Engine with C#.

## What is it?

SPEP lets you build rockets piece by piece (capsule, fuel tank, engine) in an assembly building (VAB), then launch them at a planet simulated with real physics: Newtonian gravity, atmospheric drag based on altitude and air density, fuel/oxidizer consumption, and KSP-style flight controls.

## Features

- **VAB (Vehicle Assembly Building):** drag parts with the mouse and they snap onto compatible attachment points automatically, just like KSP.
- **Real orbital physics:** inverse-square gravity, an orbital map that predicts your trajectory (ellipses, hyperbolic escape paths).
- **Atmosphere with variable density:** drag depends on altitude, producing a realistic terminal velocity during atmospheric flight.
- **Real-world scale:** a 600 km radius planet with a 70 km atmosphere, similar to Kerbin.
- **Floating origin system:** keeps physics precision stable even when the ship is far from the starting point.


## How to play

### 1. Building in the VAB

You start in the **Vehicle Assembly Building (VAB)** by default.

- Build your rocket in this order: **Capsule → Fuel Tank → Engine**.
- **Important:** The capsule can **only** be placed on the central **Y-axis**. It cannot be moved sideways (X or Z).
- Drag parts with the left mouse button. Compatible attachment points will snap automatically.

#### Camera controls in the VAB
| Action                      | Control                          |
|----------------------------|----------------------------------|
| Zoom in / out              | Mouse scroll wheel               |
| Raise / lower camera       | Shift + Mouse scroll wheel**   |
| Rotate view                | Right-click + drag               |

### 2. Launch

Once your rocket is ready, press **"Save and Launch"** to go to the launch pad (Mundo scene).

### 3. Flight controls

| Key              | Action                                      |
|------------------|---------------------------------------------|
| `Space`          | Toggle engine on/off                        |
| `Shift` / `Ctrl` | Increase / decrease throttle                |
| `T`              | Toggle SAS (holds current attitude)         |
| `M`              | Open / close orbital map                    |

#### Orientation controls (Q W E A S D)

| Keys     | Axis / Action                                      |
|----------|----------------------------------------------------|
| **A / D** | Rotate around the **central axis** (yaw)          |
| **W / S** | Tilt in the **equatorial** plane (pitch)          |
| **Q / E** | Tilt in the **polar** plane (roll)                |

These controls are relative to the rocket’s current orientation.

### 4. Camera controls (Mundo & Orbital Map)

| Action              | Control                    |
|---------------------|----------------------------|
| Zoom in / out       | Mouse scroll wheel         |
| Rotate view         | Right-click + drag         |

> **Note:** Raising/lowering the camera with **Shift + scroll** is only available in the VAB.

## Tech stack

- Engine: Godot 4.7 (C# / Mono)
- Physics: Jolt Physics
