using Godot;

public partial class Planeta : Node3D
{
	[ExportGroup("Propiedades Físicas")]
	// Constante de Gravitación Universal real. No la toques para "ajustar" la gravedad:
	// usa MasaPlaneta, que es la variable pensada para eso.
	[Export] public float G { get; set; } = 6.674e-11f;

	// IMPORTANTE: MasaPlaneta y RadioPlaneta están acoplados a través de la fórmula
	// g = G * MasaPlaneta / RadioPlaneta^2 (gravedad en la superficie).
	// Los valores de abajo (radio 10 km) dan g ≈ 9.81 m/s², similar a Kerbin.
	// Si cambiás RadioPlaneta, recalculá MasaPlaneta con:
	//     MasaPlaneta = g_deseada * RadioPlaneta^2 / G
	// Ejemplos ya calculados para g = 9.81 m/s²:
	//   Radio 10.000 m (10 km)  -> MasaPlaneta ≈ 1.47e19 kg
	//   Radio 600.000 m (600 km, Kerbin real) -> MasaPlaneta ≈ 5.29e22 kg
	[Export] public float MasaPlaneta { get; set; } = 1.47e19f; // kg
	[Export] public float RadioPlaneta { get; set; } = 10000.0f; // metros (10 km)

	[ExportGroup("Atmósfera")]
	[Export] public float AlturaAtmosfera { get; set; } = 70000.0f; // 70 km de atmósfera
	[Export] public float DensidadSuperficie { get; set; } = 1.225f; // kg/m³ a nivel del mar
	[Export] public float EscalaAltura { get; set; } = 5000.0f; // Controla qué tan rápido "cae" la densidad

	// ==========================================
	// 1. CÁLCULO DE ALTITUD
	// ==========================================
	public float GetAltitud(Vector3 posicionObjeto)
	{
		float distanciaAlCentro = GlobalPosition.DistanceTo(posicionObjeto);
		return distanciaAlCentro - RadioPlaneta;
	}

	// ==========================================
	// 2. GRAVEDAD REALISTA (Ley de Newton)
	// ==========================================
	public Vector3 GetGravedadEn(Vector3 posicionObjeto, float masaObjeto)
	{
		Vector3 direccionHaciaCentro = (GlobalPosition - posicionObjeto).Normalized();
		float distancia = GlobalPosition.DistanceTo(posicionObjeto);

		if (distancia <= 0.001f) return Vector3.Zero;

		// Magnitud de la fuerza: F = G * (M1 * M2) / r^2
		float fuerzaMagnitud = (G * MasaPlaneta * masaObjeto) / (distancia * distancia);

		return direccionHaciaCentro * fuerzaMagnitud;
	}

	// ==========================================
	// 3. DENSIDAD DE LA ATMÓSFERA (Exponencial)
	// ==========================================
	public float GetDensidadAtmosfera(Vector3 posicionObjeto)
	{
		float altitud = GetAltitud(posicionObjeto);

		// Si estamos fuera de la atmósfera, la densidad es 0
		if (altitud >= AlturaAtmosfera || altitud < 0)
			return 0.0f;

		// Fórmula de densidad barométrica: p = p0 * e^(-altitud / escala)
		return DensidadSuperficie * Mathf.Exp(-altitud / EscalaAltura);
	}
}
