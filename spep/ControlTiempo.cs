using Godot;
using System.Collections.Generic;

// Warp de tiempo estilo KSP.
// - En atmósfera: sólo niveles bajos (x1 a x5), para no arriesgar que la
//   física de colisión/atmósfera se comporte raro a mucha velocidad de
//   simulación cerca del suelo.
// - Fuera de la atmósfera (en el espacio): niveles más altos disponibles.
// - Si el motor está encendido, se corta el warp a x1 automáticamente
//   (no tiene sentido "acelerar el tiempo" mientras estás maniobrando).
//
// Teclas: "." (punto) sube un nivel, "," (coma) baja un nivel — igual que
// en KSP.

public partial class ControlTiempo : Node
{
	[Export] public NodePath PlanetaPath { get; set; } = new NodePath("../Planeta");

	private static readonly float[] NivelesAtmosfera = { 1f, 2f, 3f, 4f, 5f };
	private static readonly float[] NivelesEspacio   = { 1f, 2f, 3f, 4f, 5f, 10f, 25f, 50f, 100f };

	private int indiceActual = 0;
	private Planeta planeta;
	private Cohete nave;
	private Dictionary<Key, bool> teclasAnteriores = new Dictionary<Key, bool>();

	public override void _Ready()
	{
		planeta = GetNodeOrNull<Planeta>(PlanetaPath);
	}

	public override void _Process(double delta)
	{
		// Buscar la nave activa si aún no la tenemos o se destruyó
		if (nave == null || !GodotObject.IsInstanceValid(nave))
		{
			var nodos = GetTree().GetNodesInGroup("cohete_activo");
			nave = nodos.Count > 0 ? nodos[0] as Cohete : null;
		}

		float[] niveles = EstaEnAtmosfera() ? NivelesAtmosfera : NivelesEspacio;

		// Asegurar que el índice no se salga de rango
		if (indiceActual >= niveles.Length)
			indiceActual = niveles.Length - 1;

		// Subir / bajar nivel de warp
		if (JustPressed(Key.Period) && indiceActual < niveles.Length - 1)
			indiceActual++;

		if (JustPressed(Key.Comma) && indiceActual > 0)
			indiceActual--;

		// No permitir warp con el motor encendido
		if (nave != null && nave.EstaMotorEncendido() && indiceActual != 0)
			indiceActual = 0;

		Engine.TimeScale = niveles[indiceActual];
	}

	private bool EstaEnAtmosfera()
	{
		if (planeta == null || nave == null)
			return true;

		return planeta.GetDensidadAtmosfera(nave.GlobalPosition) > 0.001f;
	}

	public float ObtenerWarpActual() => (float)Engine.TimeScale;

	private bool JustPressed(Key key)
	{
		bool esta = Input.IsPhysicalKeyPressed(key);
		bool estaba = teclasAnteriores.GetValueOrDefault(key, false);
		teclasAnteriores[key] = esta;
		return esta && !estaba;
	}
}
