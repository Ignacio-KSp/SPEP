# SPEP - Space Exploration Program

Un simulador de exploración espacial 3D estilo Kerbal Space Program (KSP), hecho en Godot Engine con C#.

## ¿Qué es?

SPEP te deja armar cohetes por piezas (cabina, tanque, motor) en un taller de ensamblado (VAB), y lanzarlos a un planeta con física real: gravedad newtoniana, rozamiento atmosférico según altitud y densidad, combustible/oxidante, y control de vuelo tipo KSP (WASD para orientar, Q/E para rotar, SAS para mantener la orientación).

## Características

- **VAB (taller de ensamblado):** arrastrás piezas con el mouse y se pegan solas a los puntos de unión compatibles (como en KSP).
- **Física orbital real:** gravedad por ley del inverso del cuadrado, mapa orbital con predicción de trayectoria (elipses, escape hiperbólico).
- **Atmósfera con densidad variable:** el rozamiento depende de la altitud, generando una velocidad terminal realista en vuelo atmosférico.
- **Escala real:** planeta de 600 km de radio y 70 km de atmósfera, similar a Kerbin.
- **Sistema de origen flotante:** mantiene la precisión física aunque la nave esté lejos del punto de partida.

## Cómo jugar

1. Andá al VAB (arranca ahí por defecto).
2. Armá tu nave: Cabina → Tanque → Motor (en ese orden).
3. "Guardar y Lanzar" para pasar a la plataforma de lanzamiento.
4. Controles en vuelo:
   - `Espacio`: encender/apagar motor
   - `Shift` / `Ctrl`: subir/bajar throttle
   - `W A S D Q E`: orientar la nave
   - `T`: SAS (mantener orientación)
   - `M`: mapa orbital

## Tecnología

- Motor: Godot 4.7 (C# / Mono)
- Física: Jolt Physics
