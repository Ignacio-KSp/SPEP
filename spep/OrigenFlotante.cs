using Godot;

// "Origen flotante" (floating origin): Godot usa precisión simple
// (float32) para las posiciones. A cientos de miles de metros del origen
// del mundo (como pasa con un planeta a escala real, radio 600 km), el
// error de redondeo empieza a notarse como temblor/jitter en física y
// renderizado.
//
// Solución estándar en juegos de escala planetaria: en vez de dejar que
// la nave se aleje del origen, la nave se mantiene siempre CERCA de
// (0,0,0), y en su lugar movemos el resto del mundo (el planeta) en la
// dirección contraria. Las posiciones relativas entre nave y planeta no
// cambian, así que no se nota nada en el juego (ni la cámara, ni la
// física, ni el mapa orbital, que ya calculan todo en base a posiciones
// relativas), pero los números vuelven a ser chicos y precisos.
public partial class OrigenFlotante : Node
{
	[Export] public NodePath PlanetaPath { get; set; } = new NodePath("../Planeta");

	// Distancia del origen del mundo a partir de la cual se re-centra todo.
	// Ni muy chica (recentraría todo el tiempo, sin necesidad) ni muy
	// grande (dejaría que la precisión se degrade antes de corregir).
	[Export] public float UmbralRecentrado { get; set; } = 50000.0f;

	private Node3D planeta;
	private RigidBody3D nave;

	public override void _Ready()
	{
		planeta = GetNodeOrNull<Node3D>(PlanetaPath);
	}

	public override void _PhysicsProcess(double delta)
	{
		// La nave activa puede no existir todavía (antes del lanzamiento)
		// o puede cambiar (si más adelante hay más de una). La buscamos
		// por grupo en vez de depender de una referencia fija.
		if (nave == null || !GodotObject.IsInstanceValid(nave))
		{
			var nodos = GetTree().GetNodesInGroup("cohete_activo");
			nave = nodos.Count > 0 ? nodos[0] as RigidBody3D : null;
			if (nave == null) return;
		}

		if (planeta == null) return;

		Vector3 posicionNave = nave.GlobalPosition;
		if (posicionNave.Length() < UmbralRecentrado) return;

		// Desplazamos TODO el mundo (menos la nave) en sentido contrario a
		// donde está la nave, así la nave vuelve a quedar cerca de (0,0,0).
		Vector3 desplazamiento = -posicionNave;

		nave.GlobalPosition += desplazamiento;
		planeta.GlobalPosition += desplazamiento;
	}
}
