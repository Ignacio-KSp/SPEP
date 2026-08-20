using Godot;

// Base de TODAS las piezas de cohete (tanques, motores, cabinas, etc).
// Ponés este script (o uno que herede de él) en la raíz de cada escena de pieza.
// El Cohete NO tiene stats fijos: recorre sus hijos, encuentra los nodos
// "Parte" y suma lo que corresponda. Así una pieza nueva funciona sola,
// sin tocar Cohete.cs.
public partial class Parte : Node3D
{
	public enum TipoParte { Estructural, Tanque, Motor, Cabina, Control, Aerodinamica }

	[ExportGroup("Datos generales")]
	[Export] public string NombreParte { get; set; } = "Parte";
	[Export] public TipoParte Tipo { get; set; } = TipoParte.Estructural;
	[Export] public float MasaSeca { get; set; } = 0.1f; // kg, SIN combustible
	[Export] public float Costo { get; set; } = 0.0f;

	// Diámetro de acople (metros). Dos PuntoUnion sólo se pegan si el diámetro coincide
	// (o está dentro de una tolerancia), igual que los distintos tamaños de KSP (0.625, 1.25, 2.5...).
	[Export] public float Diametro { get; set; } = 1.25f;

	[ExportGroup("Aerodinámica")]
	[Export] public float CoeficienteDrag { get; set; } = 0.2f;
	[Export] public float AreaFrontal { get; set; } = 1.0f;

	[ExportGroup("Si es Tanque (dejar en 0 si no aplica)")]
	[Export] public float CapacidadCombustible { get; set; } = 0.0f;
	[Export] public float CapacidadOxidante { get; set; } = 0.0f;

	[ExportGroup("Si es Motor (dejar en 0 / false si no aplica)")]
	[Export] public bool EsMotor { get; set; } = false;
	[Export] public float EmpujeMax { get; set; } = 0.0f;
	[Export] public float IspVacio { get; set; } = 0.0f;
	[Export] public float IspAtmosfera { get; set; } = 0.0f;
	// Marker3D (hijo de esta pieza) desde donde sale el chorro. Su eje +Y local
	// marca la dirección del empuje, igual que hace el Motor del cohete de prueba.
	[Export] public NodePath PuntoEmpujePath { get; set; }

	[ExportGroup("Si es Módulo de Control (Cabina/Sonda, dejar EsControlador=false si no aplica)")]
	// Marcá esto en tu Cabina/sonda de mando. El cohete usa los valores del
	// PRIMER módulo controlador que encuentre entre sus piezas, así distintas
	// cabinas/sondas pueden tener SAS más fuerte o más débil (como en KSP:
	// un probe core básico gira más lento que una cabina tripulada avanzada).
	[Export] public bool EsControlador { get; set; } = false;
	[Export] public float PotenciaRotacion { get; set; } = 60.0f;
	[Export] public float SasRigidez { get; set; } = 25.0f;
	[Export] public float SasAmortiguacion { get; set; } = 12.0f;
}
