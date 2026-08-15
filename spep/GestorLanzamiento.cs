using Godot;

// Reemplaza al viejo nodo "Cohete" fijo que estaba en la escena para pruebas.
// Al arrancar, instancia la nave (la guardada desde el VAB, o una de prueba
// asignada a mano) sobre la plataforma de lanzamiento, y le avisa a la
// cámara / HUD / mapa orbital cuál es la nave activa.
public partial class GestorLanzamiento : Node3D
{
	// Nave de prueba/respaldo. Se usa si todavía no armaste ninguna en el VAB
	// (o sea, si EstadoJuego.NaveActivaPath está vacío).
	[Export] public PackedScene NaveAEmplazar { get; set; }

	[Export] public NodePath BasePath { get; set; } = new NodePath("../Planeta/Base");
	[Export] public NodePath PlanetaPath { get; set; } = new NodePath("../Planeta");
	[Export] public float AlturaSobrePlataforma { get; set; } = 1.5f;
	[Export] public NodePath CamaraPath { get; set; } = new NodePath("../Camera3D");
	[Export] public NodePath HudPath { get; set; } = new NodePath("../CanvasLayer/HUD");
	[Export] public NodePath MapaOrbitalPath { get; set; } = new NodePath("../MapaOrbital");

	public override void _Ready()
	{
		// CallDeferred para no depender del orden en que otros nodos hacen
		// su propio _Ready (se ejecuta cuando el árbol ya está completo).
		CallDeferred(nameof(EmplazarNave));
	}

	private void EmplazarNave()
	{
		PackedScene naveAUsar = NaveAEmplazar;

		// Si venimos del VAB, EstadoJuego tiene la ruta de la nave guardada
		// por el jugador (en user://). Esa tiene prioridad sobre la de prueba.
		string rutaGuardada = EstadoJuego.Instancia?.NaveActivaPath;
		if (!string.IsNullOrEmpty(rutaGuardada) && ResourceLoader.Exists(rutaGuardada))
			naveAUsar = ResourceLoader.Load<PackedScene>(rutaGuardada);

		if (naveAUsar == null)
		{
			GD.PushWarning("GestorLanzamiento: no hay ninguna nave para emplazar (ni del VAB ni de respaldo).");
			return;
		}

		Node3D basePlataforma = GetNodeOrNull<Node3D>(BasePath);
		Node3D planetaNodo = GetNodeOrNull<Node3D>(PlanetaPath);
		Cohete nave = naveAUsar.Instantiate<Cohete>();

		GetParent().AddChild(nave);
		nave.Name = "Cohete";
		nave.AddToGroup("cohete_activo");

		// La nave se guardó desde el VAB con freeze=true (ahí la queremos
		// congelada para que no se caiga mientras la armás). Acá, en pleno
		// vuelo, tiene que estar descongelada o ni la gravedad ni el empuje
		// del motor la van a mover.
		nave.Freeze = false;
		nave.LinearVelocity = Vector3.Zero;
		nave.AngularVelocity = Vector3.Zero;

		if (basePlataforma != null && planetaNodo != null)
		{
			// La plataforma (Base) tiene rotación identidad, NO la orientación
			// real de ese punto sobre la esfera. Copiar su transform tal cual
			// dejaba al cohete acostado. Acá calculamos la dirección real
			// "hacia arriba" (desde el centro del planeta hasta la
			// plataforma) y armamos una base ortonormal para pararlo derecho,
			// sin importar en qué parte del planeta esté la plataforma.
			Vector3 arriba = (basePlataforma.GlobalPosition - planetaNodo.GlobalPosition).Normalized();

			Vector3 referencia = Mathf.Abs(arriba.Dot(Vector3.Forward)) > 0.99f ? Vector3.Right : Vector3.Forward;
			Vector3 derecha = arriba.Cross(referencia).Normalized();
			Vector3 adelante = derecha.Cross(arriba).Normalized();

			Basis baseOrientacion = new Basis(derecha, arriba, adelante);
			Vector3 posicion = basePlataforma.GlobalPosition + arriba * AlturaSobrePlataforma;

			nave.GlobalTransform = new Transform3D(baseOrientacion, posicion);
		}
		else if (basePlataforma != null)
		{
			nave.GlobalTransform = basePlataforma.GlobalTransform;
		}

		var camara = GetNodeOrNull(CamaraPath);
		camara?.Set("objetivo", nave);

		var hud = GetNodeOrNull<HUD>(HudPath);
		hud?.AsignarCohete(nave);

		var mapa = GetNodeOrNull(MapaOrbitalPath);
		mapa?.Set("cohete", nave);

		GD.Print("Nave emplazada en la plataforma de lanzamiento.");
	}
}
