using Godot;

// VAB simple: armás la nave APILANDO piezas, en orden de abajo hacia arriba.
// Orden de uso: 1) Agregar Motor  2) Agregar Tanque  3) Agregar Cabina
// (cada pieza nueva se pega en el punto de unión "de arriba" libre más
// cercano a la raíz; por eso el motor va primero, es la base).
public partial class VABManager : Node3D
{
	[Export] public PackedScene PiezaTanque { get; set; }
	[Export] public PackedScene PiezaMotor { get; set; }
	[Export] public PackedScene PiezaCabina { get; set; }

	[Export] public NodePath RaizNavePath { get; set; } = new NodePath("RaizNave");
	[Export] public NodePath LabelInfoPath { get; set; } = new NodePath("../CanvasLayer/UI/Info");

	private Cohete raizNave;
	private Label labelInfo;

	public override void _Ready()
	{
		raizNave = GetNodeOrNull<Cohete>(RaizNavePath);
		labelInfo = GetNodeOrNull<Label>(LabelInfoPath);
		ActualizarInfo();
	}

	public void AgregarTanque() => AgregarPieza(PiezaTanque);
	public void AgregarMotor() => AgregarPieza(PiezaMotor);
	public void AgregarCabina() => AgregarPieza(PiezaCabina);

	private void AgregarPieza(PackedScene piezaScene)
	{
		if (piezaScene == null || raizNave == null) return;

		Node3D pieza = piezaScene.Instantiate<Node3D>();
		raizNave.AddChild(pieza);

		PuntoUnion puntoAbierto = BuscarPuntoArribaAbierto(raizNave);
		PuntoUnion puntoNuevoAbajo = BuscarUnionAbajoDirecta(pieza);

		if (puntoAbierto != null && puntoNuevoAbajo != null)
		{
			// Alineamos la pieza nueva para que su punto "de abajo" quede
			// exactamente pegado al punto "de arriba" libre de la nave.
			Transform3D destino = puntoAbierto.GlobalTransform;
			Transform3D offsetLocal = puntoNuevoAbajo.Transform;

			pieza.GlobalTransform = destino * offsetLocal.AffineInverse();

			puntoAbierto.Ocupado = true;
			puntoNuevoAbajo.Ocupado = true;
		}
		else
		{
			// Primera pieza del stack (o pieza sin punto de abajo, ej. un motor
			// suelto): va justo en el origen de la nave.
			pieza.Transform = Transform3D.Identity;
		}

		// Pack() sólo guarda los nodos cuyo Owner es la raíz de la escena.
		EstablecerOwnerRecursivo(pieza, raizNave);

		raizNave.RecolectarPartes();
		ActualizarInfo();
	}

	private void EstablecerOwnerRecursivo(Node nodo, Node propietario)
	{
		nodo.Owner = propietario;
		foreach (Node hijo in nodo.GetChildren())
			EstablecerOwnerRecursivo(hijo, propietario);
	}

	private PuntoUnion BuscarPuntoArribaAbierto(Node nodo)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is PuntoUnion pu && pu.Tipo == PuntoUnion.TipoUnion.Apilable_Arriba && !pu.Ocupado)
				return pu;

			PuntoUnion enHijos = BuscarPuntoArribaAbierto(hijo);
			if (enHijos != null)
				return enHijos;
		}
		return null;
	}

	private PuntoUnion BuscarUnionAbajoDirecta(Node pieza)
	{
		foreach (Node hijo in pieza.GetChildren())
		{
			if (hijo is PuntoUnion pu && pu.Tipo == PuntoUnion.TipoUnion.Apilable_Abajo)
				return pu;
		}
		return null;
	}

	private void ActualizarInfo()
	{
		if (labelInfo == null || raizNave == null) return;
		labelInfo.Text = $"Masa total: {raizNave.GetMasaTotal():F1} kg";
	}

	public void Reiniciar()
	{
		GetTree().ReloadCurrentScene();
	}

	public void GuardarYLanzar()
	{
		if (raizNave == null) return;

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
