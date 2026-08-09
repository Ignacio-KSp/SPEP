using Godot;
using System.Collections.Generic;

public partial class Cohete : RigidBody3D
{
	// ======================
	// REFERENCIAS
	// ======================
	[Export] public NodePath PlanetaPath { get; set; } = new NodePath("../Planeta");
	private Planeta planeta;

	[Export] public Node3D ThrustPoint { get; set; }

	// ======================
	// TANQUE
	// ======================
	[ExportGroup("Tanque")]
	[Export] public float CombustibleMax { get; set; } = 700.0f;
	[Export] public float OxidanteMax { get; set; } = 700.0f;
	[Export] public float MasaSeca { get; set; } = 2.0f;

	private float combustible;
	private float oxidante;

	// ======================
	// MOTOR
	// ======================
	[ExportGroup("Motor")]
	[Export] public float EmpujeMax { get; set; } = 50000.0f;
	[Export] public float IspVacio { get; set; } = 320.0f;
	[Export] public float IspAtmosfera { get; set; } = 260.0f;
	[Export] public float RatioOxidante { get; set; } = 1.2f;
	// Multiplicador de empuje: escala EmpujeMax a las unidades del mundo de Godot.
	// Antes era un "15.0f" fijo escondido en el código; ahora es ajustable desde el Inspector.
	[Export] public float EscalaEmpuje { get; set; } = 15.0f;

	// ======================
	// CONTROL Y SAS
	// ======================
	[ExportGroup("Control")]
	[Export] public float PotenciaRotacion { get; set; } = 60.0f;
	[Export] public float PotenciaSas { get; set; } = 45.0f;
	[Export] public float ThrottleSpeed { get; set; } = 1.5f;

	private float throttle = 0.0f;
	private bool motorEncendido = false;
	private bool sasActivado = false;

	private Dictionary<Key, bool> teclasAnteriores = new Dictionary<Key, bool>();

	// ======================
	// FÍSICA DEL VEHÍCULO
	// ======================
	[ExportGroup("Física del Vehículo")]
	[Export] public float AreaFrontal { get; set; } = 1.8f;
	[Export] public float CoeficienteDrag { get; set; } = 0.4f;

	public override void _Ready()
	{
		combustible = CombustibleMax;
		oxidante = OxidanteMax;
		ActualizarMasa();

		CanSleep = false;
		Sleeping = false;
		GravityScale = 0.0f;

		// Activar Detección Continua de Colisiones para máxima precisión con Jolt
		ContinuousCd = true;

		AngularDamp = 0.4f;
		LinearDamp = 0.05f;

		motorEncendido = false;
		throttle = 0.0f;
		sasActivado = false;

		planeta = GetNodeOrNull<Planeta>(PlanetaPath);

		GD.Print("=== Cohete listo (C#) ===");
		if (planeta == null)
			GD.PushWarning("No se encontró el planeta (o el nodo no tiene el script Planeta.cs)");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		ManejarInput(dt);
		ActualizarMasa();

		if (planeta != null)
		{
			AplicarGravedad();
			AplicarDrag();
		}

		if (motorEncendido && throttle > 0.01f)
			AplicarEmpuje(dt);

		AplicarControlRotacion();

		if (sasActivado)
			AplicarSas();
	}

	private bool JustPressed(Key key)
	{
		bool esta = Input.IsPhysicalKeyPressed(key);
		bool estaba = teclasAnteriores.GetValueOrDefault(key, false);
		teclasAnteriores[key] = esta;
		return esta && !estaba;
	}

	private void ManejarInput(float delta)
	{
		if (JustPressed(Key.Space))
		{
			motorEncendido = !motorEncendido;
			GD.Print("Motor: ", motorEncendido ? "ENCENDIDO" : "APAGADO");
		}

		if (JustPressed(Key.T))
		{
			sasActivado = !sasActivado;
			GD.Print("SAS: ", sasActivado ? "ON" : "OFF");
		}

		if (motorEncendido)
		{
			if (Input.IsKeyPressed(Key.Shift))
				throttle = Mathf.Clamp(throttle + ThrottleSpeed * delta, 0.0f, 1.0f);
			else if (Input.IsKeyPressed(Key.Ctrl))
				throttle = Mathf.Clamp(throttle - ThrottleSpeed * delta, 0.0f, 1.0f);

			if (Input.IsKeyPressed(Key.Z))
				throttle = 1.0f;
			if (Input.IsKeyPressed(Key.X))
				throttle = 0.0f;
		}
		else
		{
			throttle = 0.0f;
		}
	}

	private void AplicarControlRotacion()
	{
		Vector3 torqueLocal = Vector3.Zero;

		if (Input.IsKeyPressed(Key.W)) torqueLocal.X += 1.0f;
		if (Input.IsKeyPressed(Key.S)) torqueLocal.X -= 1.0f;
		if (Input.IsKeyPressed(Key.A)) torqueLocal.Y += 1.0f;
		if (Input.IsKeyPressed(Key.D)) torqueLocal.Y -= 1.0f;
		if (Input.IsKeyPressed(Key.Q)) torqueLocal.Z += 1.0f;
		if (Input.IsKeyPressed(Key.E)) torqueLocal.Z -= 1.0f;

		if (torqueLocal != Vector3.Zero)
		{
			Vector3 torque = GlobalTransform.Basis * (torqueLocal * PotenciaRotacion);
			ApplyTorque(torque);
		}
	}

	private void AplicarSas()
	{
		Vector3 av = AngularVelocity;
		if (av.LengthSquared() > 0.0005f)
			ApplyTorque(-av * PotenciaSas);
	}

	private void AplicarEmpuje(float delta)
	{
		if (combustible <= 0.0f || oxidante <= 0.0f)
		{
			motorEncendido = false;
			throttle = 0.0f;
			return;
		}

		float densidad = planeta != null ? planeta.GetDensidadAtmosfera(GlobalPosition) : 0.0f;

		float isp = Mathf.Lerp(IspAtmosfera, IspVacio, 1.0f - densidad);

		// Factor de escala aplicado para adaptarlo a las unidades del mundo de Godot
		float empujeActual = EmpujeMax * throttle * EscalaEmpuje;

		float g0 = 9.81f;
		float consumoTotal = (EmpujeMax * throttle / (isp * g0)) * delta * 0.1f;
		float consumoComb = consumoTotal / (1.0f + RatioOxidante);
		float consumoOxi = consumoTotal - consumoComb;

		if (combustible < consumoComb || oxidante < consumoOxi)
		{
			combustible = 0.0f;
			oxidante = 0.0f;
			motorEncendido = false;
			throttle = 0.0f;
			return;
		}

		combustible -= consumoComb;
		oxidante -= consumoOxi;

		// CLAVE: el empuje se aplica en la dirección en la que apunta la nave
		// (su eje local +Y, hacia la "nariz"/cabina), NO hacia afuera del planeta.
		// Así, cuando rotás el cohete con WASD/QE, el empuje también gira con él
		// y podés inclinar la trayectoria para "hacer la gravedad" (gravity turn).
		Vector3 direccionEmpuje = GlobalTransform.Basis.Y.Normalized();

		ApplyCentralForce(direccionEmpuje * empujeActual);
	}

	private void AplicarGravedad()
	{
		// Gravedad newtoniana real (F = G*M*m/r^2), igual a la que usa el mapa
		// orbital para predecir la trayectoria. Antes acá había una gravedad
		// constante de 9.81 m/s² sin importar la altura, que no coincidía con
		// la línea verde del mapa orbital y no permitía órbitas reales.
		Vector3 fuerzaGravedad = planeta.GetGravedadEn(GlobalPosition, Mass);
		ApplyCentralForce(fuerzaGravedad);
	}

	private void AplicarDrag()
	{
		float densidad = planeta.GetDensidadAtmosfera(GlobalPosition);
		if (densidad <= 0.001f) return;

		Vector3 velocidad = LinearVelocity;
		if (velocidad.LengthSquared() < 0.5f) return;

		float fuerzaDrag = 0.5f * densidad * velocidad.LengthSquared() * CoeficienteDrag * AreaFrontal;
		ApplyCentralForce(-velocidad.Normalized() * fuerzaDrag);
	}

	private void ActualizarMasa()
	{
		Mass = MasaSeca + combustible + oxidante;
	}

	public float GetAltitud()
	{
		if (planeta == null) return 0.0f;
		return planeta.GetAltitud(GlobalPosition);
	}

	public float GetThrottle() => throttle;
	public float GetCombustiblePorcentaje() => CombustibleMax > 0 ? combustible / CombustibleMax : 0.0f;
	public float GetOxidantePorcentaje() => OxidanteMax > 0 ? oxidante / OxidanteMax : 0.0f;
	public float GetMasaTotal() => Mass;
	public bool EstaMotorEncendido() => motorEncendido && throttle > 0.01f && combustible > 0.0f;
	public bool GetSas() => sasActivado;
	public float GetVelocidad() => LinearVelocity.Length();
	public float GetAltitudPublic() => GetAltitud();
}
