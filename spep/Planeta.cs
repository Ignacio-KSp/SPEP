using Godot;

public partial class Planeta : StaticBody3D
{
	[ExportGroup("Propiedades del Planeta")]
	[Export] public float Radio { get; set; } = 1000.0f;
	[Export] public float Masa { get; set; } = 9800000.0f;

	[ExportGroup("Atmósfera")]
	[Export] public float AlturaAtmosfera { get; set; } = 5500.0f;
	[Export] public float ScaleHeight { get; set; } = 700.0f;
	[Export] public float DensidadSuperficie { get; set; } = 1.0f;

	[ExportGroup("Gravedad")]
	[Export] public float FactorGravedad { get; set; } = 0.22f;

	// ======================
	// API pública
	// ======================

	public float GetRadio()
	{
		return Radio;
	}

	public float GetMasa()
	{
		return Masa;
	}

	public float GetAltitud(Vector3 posicionGlobal)
	{
		return posicionGlobal.DistanceTo(GlobalPosition) - Radio;
	}

	public float GetDensidadAtmosfera(Vector3 posicionGlobal)
	{
		float altitud = GetAltitud(posicionGlobal);

		if (altitud >= AlturaAtmosfera)
			return 0.0f;

		if (altitud < 0.0f)
			return DensidadSuperficie;

		return DensidadSuperficie * Mathf.Exp(-altitud / ScaleHeight);
	}

	public Vector3 GetGravedadEn(Vector3 posicionGlobal, float masaObjeto)
	{
		Vector3 direccion = GlobalPosition - posicionGlobal;
		float distancia = direccion.Length();

		if (distancia < 1.0f)
			return Vector3.Zero;

		direccion = direccion.Normalized();

		float fuerza = (Masa * masaObjeto) / (distancia * distancia) * FactorGravedad;
		return direccion * fuerza;
	}
}
