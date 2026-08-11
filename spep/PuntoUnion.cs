using Godot;

// Punto de unión estilo KSP: un Marker3D que marca dónde se puede acoplar otra pieza.
// Poné uno "Apilable_Arriba" en la punta de arriba de una pieza y uno
// "Apilable_Abajo" en la base. En el VAB, sólo se acoplan dos puntos si:
//  - Uno es "Arriba" y el otro "Abajo" (o ambos "Radial")
//  - Tienen el mismo Diametro (con una tolerancia)
//  - Ninguno de los dos está Ocupado
[Tool]
public partial class PuntoUnion : Marker3D
{
	public enum TipoUnion { Apilable_Arriba, Apilable_Abajo, Radial }

	[Export] public TipoUnion Tipo { get; set; } = TipoUnion.Apilable_Arriba;
	[Export] public float Diametro { get; set; } = 1.25f;

	[Export]
	public bool Ocupado
	{
		get => _ocupado;
		set { _ocupado = value; ActualizarColorEditor(); }
	}
	private bool _ocupado = false;

	private CsgSphere3D _bolita;

	public override void _Ready()
	{
		// Sólo se ve en el editor del VAB, como referencia visual (bolita
		// verde/negra tipo KSP). En el vuelo real no hace falta mostrarla.
		if (Engine.IsEditorHint())
		{
			_bolita = new CsgSphere3D();
			_bolita.Radius = 0.06f;
			_bolita.RadialSegments = 8;
			_bolita.Rings = 6;
			AddChild(_bolita);
			ActualizarColorEditor();
		}
	}

	private void ActualizarColorEditor()
	{
		if (_bolita == null) return;
		var mat = new StandardMaterial3D();
		mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		mat.AlbedoColor = Ocupado ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.15f, 1.0f, 0.15f);
		_bolita.MaterialOverride = mat;
	}

	// Compatibilidad entre dos puntos: opuestos en tipo apilable, o ambos radiales,
	// mismo diámetro (tolerancia 5%), y ninguno ocupado.
	public bool EsCompatibleCon(PuntoUnion otro)
	{
		if (otro == null || Ocupado || otro.Ocupado) return false;

		bool tiposCompatibles =
			(Tipo == TipoUnion.Apilable_Arriba && otro.Tipo == TipoUnion.Apilable_Abajo) ||
			(Tipo == TipoUnion.Apilable_Abajo && otro.Tipo == TipoUnion.Apilable_Arriba) ||
			(Tipo == TipoUnion.Radial && otro.Tipo == TipoUnion.Radial);

		bool diametroCompatible = Mathf.Abs(Diametro - otro.Diametro) <= Diametro * 0.05f;

		return tiposCompatibles && diametroCompatible;
	}
}
