using Godot;
using System.Collections.Generic;

// VAB con arrastre tipo KSP:
// - Click en una tarjeta de pieza -> la pieza "queda en la mano" y sigue al mouse.
// - Click izquierdo de nuevo -> la suelta (si hay un punto de unión compatible cerca).
// - Click derecho / Esc -> cancela y descarta la pieza que tenías en la mano.
// - Regla: la PRIMERA pieza siempre tiene que ser una Cabina (como en KSP).
//   A partir de ahí, cada pieza nueva se pega con SU punto "de arriba" al punto
//   "de abajo" abierto más cercano de la nave (se construye de arriba hacia abajo).
public partial class VABManager : Node3D
{
	[Export] public PackedScene PiezaTanque { get; set; }
	[Export] public PackedScene PiezaMotor { get; set; }
	[Export] public PackedScene PiezaCabina { get; set; }

	[Export] public NodePath RaizNavePath { get; set; } = new NodePath("RaizNave");
	[Export] public NodePath LabelInfoPath { get; set; } = new NodePath("../CanvasLayer/UI/Info");
	[Export] public NodePath LabelAvisoPath { get; set; } = new NodePath("../CanvasLayer/UI/Aviso");

	// Qué tan cerca (en píxeles de pantalla) tiene que estar el mouse de un
	// punto de unión para que la pieza "salte" (snapee) a él.
	[Export] public float UmbralSnapPixeles { get; set; } = 55.0f;
	// Altura (eje Y) del "aire" donde flota la pieza cuando no está pegada a nada.
	[Export] public float AlturaFlotando { get; set; } = 2.0f;

	private Cohete raizNave;
	private Label labelInfo;
	private Label labelAviso;

	private bool colocando = false;
	private Node3D piezaEnMano;
	private PackedScene escenaPiezaEnMano;
	private PuntoUnion puntoArribaPiezaEnMano;
	private PuntoUnion puntoObjetivoActual;

	public override void _Ready()
	{
		raizNave = GetNodeOrNull<Cohete>(RaizNavePath);
		labelInfo = GetNodeOrNull<Label>(LabelInfoPath);
		labelAviso = GetNodeOrNull<Label>(LabelAvisoPath);
		ActualizarInfo();
		MostrarAviso("");
	}

	public override void _Process(double delta)
	{
		if (colocando && piezaEnMano != null)
			ActualizarPosicionPiezaEnMano();
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (!colocando) return;

		if (evento is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.Left)
				IntentarConfirmar();
			else if (mb.ButtonIndex == MouseButton.Right)
				CancelarColocacion();
		}

		if (evento is InputEventKey k && k.Pressed && k.Keycode == Key.Escape)
			CancelarColocacion();
	}

	// === Botones de las tarjetas ===
	public void SeleccionarCabina() => IniciarColocacion(PiezaCabina, "Cabina");
	public void SeleccionarTanque() => IniciarColocacion(PiezaTanque, "Tanque");
	public void SeleccionarMotor() => IniciarColocacion(PiezaMotor, "Motor");

	private void IniciarColocacion(PackedScene piezaScene, string nombreDebug)
	{
		if (piezaScene == null || raizNave == null || colocando) return;

		bool esPrimeraPieza = raizNave.GetChildCount() == 0;

		if (esPrimeraPieza && piezaScene != PiezaCabina)
		{
			MostrarAviso("Poné primero una Cabina: toda nave necesita un módulo de mando.");
			return;
		}
		if (!esPrimeraPieza && piezaScene == PiezaCabina)
		{
			MostrarAviso("Ya tenés una cabina. Ahora agregá Tanques o Motores debajo.");
			return;
		}

		piezaEnMano = piezaScene.Instantiate<Node3D>();
		escenaPiezaEnMano = piezaScene;
		raizNave.AddChild(piezaEnMano);
		DesactivarColisionRecursivo(piezaEnMano, true);

		puntoArribaPiezaEnMano = BuscarUnionArribaDirecta(piezaEnMano);

		if (!esPrimeraPieza && puntoArribaPiezaEnMano == null)
		{
			// Esta pieza no tiene forma de pegarse por abajo de nada (ej. una
			// segunda cabina). No debería pasar con las 3 piezas de prueba,
			// pero por las dudas lo bloqueamos.
			piezaEnMano.QueueFree();
			piezaEnMano = null;
			MostrarAviso("Esa pieza no se puede pegar debajo de la nave actual.");
			return;
		}

		colocando = true;
		MostrarAviso(esPrimeraPieza
			? $"Moviendo {nombreDebug}: clic izquierdo para soltar en cualquier lugar."
			: $"Moviendo {nombreDebug}: acercala a un punto de unión abierto (se pinta verde) y clic izquierdo para pegarla. Clic derecho para cancelar.");
	}

	private void ActualizarPosicionPiezaEnMano()
	{
		Camera3D camara = GetViewport().GetCamera3D();
		if (camara == null) return;

		Vector2 mousePos = GetViewport().GetMousePosition();
		bool esPrimeraPieza = raizNave.GetChildCount() == 1; // sólo está piezaEnMano

		PuntoUnion mejorPunto = null;
		float mejorDistancia = UmbralSnapPixeles;

		if (!esPrimeraPieza && puntoArribaPiezaEnMano != null)
		{
			foreach (PuntoUnion punto in BuscarTodosLosPuntosAbajoAbiertos(raizNave))
			{
				if (!punto.EsCompatibleCon(puntoArribaPiezaEnMano)) continue;

				Vector2 pantalla = camara.UnprojectPosition(punto.GlobalPosition);
				float distancia = pantalla.DistanceTo(mousePos);
				if (distancia < mejorDistancia)
				{
					mejorDistancia = distancia;
					mejorPunto = punto;
				}
			}
		}

		puntoObjetivoActual = mejorPunto;

		if (mejorPunto != null)
		{
			// Snap exacto: el punto "de arriba" de la pieza en mano queda
			// pegado al punto "de abajo" abierto de la nave.
			Transform3D destino = mejorPunto.GlobalTransform;
			Transform3D offsetLocal = puntoArribaPiezaEnMano.Transform;
			piezaEnMano.GlobalTransform = destino * offsetLocal.AffineInverse();
			MarcarValidez(true);
		}
		else
		{
			piezaEnMano.GlobalPosition = ProyectarMouseAPlano(camara, mousePos, AlturaFlotando);
			MarcarValidez(esPrimeraPieza);
		}
	}

	private Vector3 ProyectarMouseAPlano(Camera3D camara, Vector2 mousePos, float alturaPlano)
	{
		Vector3 origen = camara.ProjectRayOrigin(mousePos);
		Vector3 direccion = camara.ProjectRayNormal(mousePos);
		Plane plano = new Plane(Vector3.Up, alturaPlano);

		if (plano.IntersectsRay(origen, direccion) is Vector3 interseccion)
			return interseccion;

		return origen + direccion * 5f;
	}

	private void IntentarConfirmar()
	{
		if (!colocando || piezaEnMano == null) return;

		bool esPrimeraPieza = raizNave.GetChildCount() == 1;
		bool esValido = esPrimeraPieza || puntoObjetivoActual != null;

		if (!esValido)
		{
			MostrarAviso("No hay ningún punto de unión compatible cerca. Acercala a la nave.");
			return;
		}

		if (!esPrimeraPieza)
		{
			puntoObjetivoActual.Ocupado = true;
			puntoArribaPiezaEnMano.Ocupado = true;
		}

		QuitarTinte(piezaEnMano);
		DesactivarColisionRecursivo(piezaEnMano, false);
		EstablecerOwnerRecursivo(piezaEnMano, raizNave);

		piezaEnMano = null;
		escenaPiezaEnMano = null;
		puntoArribaPiezaEnMano = null;
		puntoObjetivoActual = null;
		colocando = false;

		raizNave.RecolectarPartes();
		ActualizarInfo();
		MostrarAviso("");
	}

	private void CancelarColocacion()
	{
		if (piezaEnMano != null)
			piezaEnMano.QueueFree();

		piezaEnMano = null;
		escenaPiezaEnMano = null;
		puntoArribaPiezaEnMano = null;
		puntoObjetivoActual = null;
		colocando = false;
		MostrarAviso("");
	}

	// === Utilidades de árbol ===

	private PuntoUnion BuscarUnionArribaDirecta(Node pieza)
	{
		foreach (Node hijo in pieza.GetChildren())
			if (hijo is PuntoUnion pu && pu.Tipo == PuntoUnion.TipoUnion.Apilable_Arriba)
				return pu;
		return null;
	}

	private List<PuntoUnion> BuscarTodosLosPuntosAbajoAbiertos(Node nodo)
	{
		var resultado = new List<PuntoUnion>();
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo == piezaEnMano) continue; // no te pegues a la pieza que sostenés
			if (hijo is PuntoUnion pu && pu.Tipo == PuntoUnion.TipoUnion.Apilable_Abajo && !pu.Ocupado)
				resultado.Add(pu);
			resultado.AddRange(BuscarTodosLosPuntosAbajoAbiertos(hijo));
		}
		return resultado;
	}

	private void DesactivarColisionRecursivo(Node nodo, bool desactivar)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is CollisionShape3D cs) cs.Disabled = desactivar;
			DesactivarColisionRecursivo(hijo, desactivar);
		}
	}

	private void EstablecerOwnerRecursivo(Node nodo, Node propietario)
	{
		nodo.Owner = propietario;
		foreach (Node hijo in nodo.GetChildren())
			EstablecerOwnerRecursivo(hijo, propietario);
	}

	private void MarcarValidez(bool valido)
	{
		Color color = valido ? new Color(0.3f, 1.0f, 0.3f, 0.85f) : new Color(1.0f, 0.3f, 0.3f, 0.85f);
		AplicarTinte(piezaEnMano, color);
	}

	private void AplicarTinte(Node nodo, Color color)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is MeshInstance3D mi)
			{
				var mat = new StandardMaterial3D();
				mat.AlbedoColor = color;
				mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				mi.MaterialOverride = mat;
			}
			AplicarTinte(hijo, color);
		}
	}

	private void QuitarTinte(Node nodo)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is MeshInstance3D mi) mi.MaterialOverride = null;
			QuitarTinte(hijo);
		}
	}

	private void ActualizarInfo()
	{
		if (labelInfo == null || raizNave == null) return;
		labelInfo.Text = $"Masa total: {raizNave.GetMasaTotal():F1} kg";
	}

	private void MostrarAviso(string texto)
	{
		if (labelAviso != null) labelAviso.Text = texto;
	}

	public void Reiniciar()
	{
		GetTree().ReloadCurrentScene();
	}

	public void GuardarYLanzar()
	{
		if (raizNave == null || colocando) return;

		if (raizNave.GetChildCount() == 0)
		{
			MostrarAviso("No podés lanzar una nave vacía.");
			return;
		}

		DirAccess.MakeDirRecursiveAbsolute("user://Naves");

		var empaquetada = new PackedScene();
		Error err = empaquetada.Pack(raizNave);
		if (err != Error.Ok)
		{
			GD.PushError("No se pudo empaquetar la nave: " + err);
			return;
		}

		string ruta = "user://Naves/NaveVAB.tscn";
		ResourceSaver.Save(empaquetada, ruta);

		if (EstadoJuego.Instancia != null)
			EstadoJuego.Instancia.NaveActivaPath = ruta;

		GetTree().ChangeSceneToFile("res://mundo.tscn");
	}
}
