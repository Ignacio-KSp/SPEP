using Godot;

public partial class Planeta : Node3D
{
	[ExportGroup("Propiedades Físicas")]
	// Constante de Gravitación Universal (puedes subirla si quieres planetas pequeños pero pesados)
	[Export] public float G { get; set; } = 6.674e-11f; 
	[Export] public float MasaPlaneta { get; set; } = 5.972e24f; // Masa en kg (ejemplo: Tierra)
	[Export] public float RadioPlaneta { get; set; } = 600000.0f; // Radio en metros (ej. 600 km escala KSP)

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
