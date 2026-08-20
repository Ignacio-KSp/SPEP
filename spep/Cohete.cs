using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Cohete : RigidBody3D
{
	// ======================
	// REFERENCIAS
	// ======================
	[Export] public NodePath PlanetaPath { get; set; } = new NodePath("../Planeta");
	private Planeta planeta;

	// ======================
	// CONTROL Y SAS
	// ======================
	[ExportGroup("Control")]
	[Export] public float ThrottleSpeed { get; set; } = 1.5f;
	[Export] public float EscalaEmpuje { get; set; } = 2.0f;
	[Export] public float RatioOxidante { get; set; } = 1.2f;

	// Valores de respaldo, SÓLO se usan si ninguna pieza está marcada como
	// "EsControlador" (por ejemplo, mientras probás una nave sin cabina real).
	// Con una cabina/sonda puesta, el cohete usa los valores de ESA pieza
	// (ver RecolectarPartes), no estos de acá.
	[Export] public float PotenciaRotacionRespaldo { get; set; } = 60.0f;
	[Export] public float SasRigidezRespaldo { get; set; } = 25.0f;
	[Export] public float SasAmortiguacionRespaldo { get; set; } = 12.0f;

	// Valores REALMENTE en uso ahora mismo (los calcula RecolectarPartes).
	private float potenciaRotacionActiva = 60.0f;
	private float sasRigidezActiva = 25.0f;
	private float sasAmortiguacionActiva = 12.0f;

	// Poné esto en true desde el Inspector para ver en la consola (pestaña
	// "Salida") la altitud/densidad/fuerza de drag en cada frame, y
	// confirmar si realmente no hay rozamiento o si el aire ya se hizo
	// despreciable por la altura (comportamiento normal, no bug).
	[Export] public bool DebugFisica { get; set; } = false;

	private Quaternion objetivoSas = Quaternion.Identity;
	private bool ultimoInputManual = false;

	private float throttle = 0.0f;
	private bool motorEncendido = false;
	private bool sasActivado = false;
	private Dictionary<Key, bool> teclasAnteriores = new Dictionary<Key, bool>();

	// ======================
	// DATOS AGREGADOS DE LAS PIEZAS
	// Estos NO se tocan a mano: se calculan solos recorriendo los nodos
	// "Parte" que cuelgan de este Cohete. Cambiaste las piezas -> cambian solos.
	// ======================
	private float masaSecaTotal = 0.5f;
	private float combustibleMax = 0.0f;
	private float oxidanteMax = 0.0f;
	private float combustible = 0.0f;
	private float oxidante = 0.0f;
	private float areaFrontalTotal = 1.0f;
	private float dragPromedio = 0.3f;

	private struct MotorActivo
	{
		public Parte Parte;
		public Node3D PuntoEmpuje;
	}
	private List<MotorActivo> motores = new List<MotorActivo>();

	public override void _Ready()
	{
		planeta = GetNodeOrNull<Planeta>(PlanetaPath);

		RecolectarPartes();
		combustible = combustibleMax;
		oxidante = oxidanteMax;
		ActualizarMasa();

		CanSleep = false;
		Sleeping = false;
		GravityScale = 0.0f;
		ContinuousCd = true;

		LinearDamp = 0.0f;
		LinearDampMode = DampMode.Replace;
		AngularDamp = 0.4f;
		AngularDampMode = DampMode.Replace;

		motorEncendido = false;
		throttle = 0.0f;
		sasActivado = false;

		AddToGroup("cohete_activo");

		GD.Print($"=== Cohete ensamblado: {motores.Count} motor(es), masa seca {masaSecaTotal:F1} kg, " +
			$"{combustibleMax + oxidanteMax:F0} kg de propelente ===");

		if (planeta == null)
			GD.PushWarning("No se encontró el planeta (o el nodo no tiene el script Planeta.cs)");
		if (motores.Count == 0)
			GD.PushWarning("Esta nave no tiene ninguna 'Parte' marcada como EsMotor=true");
	}

	// Recorre TODOS los descendientes buscando nodos "Parte" y suma sus datos.
	// Llamala de nuevo si desacoplás/agregás piezas en pleno vuelo.
	public void RecolectarPartes()
	{
		ReubicarColisiones();

		combustibleMax = 0f;
		oxidanteMax = 0f;
		masaSecaTotal = 0f;
		areaFrontalTotal = 0f;
		float dragAcumulado = 0f;
		motores.Clear();

		bool controladorEncontrado = false;

		foreach (Parte parte in ObtenerPartes(this))
		{
			masaSecaTotal += parte.MasaSeca;
			combustibleMax += parte.CapacidadCombustible;
			oxidanteMax += parte.CapacidadOxidante;
			areaFrontalTotal += parte.AreaFrontal;
			dragAcumulado += parte.CoeficienteDrag * parte.AreaFrontal;

			if (parte.EsMotor)
			{
				Node3D punto = parte.PuntoEmpujePath != null
					? parte.GetNodeOrNull<Node3D>(parte.PuntoEmpujePath)
					: null;
				motores.Add(new MotorActivo { Parte = parte, PuntoEmpuje = punto ?? parte });
			}

			// Usamos el PRIMER módulo de control que encontremos (cabina o
			// sonda). Si más adelante tenés varios y querés elegir cuál
			// "pilotea", esto es lo que habría que cambiar.
			if (parte.EsControlador && !controladorEncontrado)
			{
				potenciaRotacionActiva = parte.PotenciaRotacion;
				sasRigidezActiva = parte.SasRigidez;
				sasAmortiguacionActiva = parte.SasAmortiguacion;
				controladorEncontrado = true;
			}
		}

		if (!controladorEncontrado)
		{
			// Ninguna pieza está marcada como controlador (ej. probando una
			// nave sin cabina real): usamos los valores de respaldo.
			potenciaRotacionActiva = PotenciaRotacionRespaldo;
			sasRigidezActiva = SasRigidezRespaldo;
			sasAmortiguacionActiva = SasAmortiguacionRespaldo;
		}

		dragPromedio = areaFrontalTotal > 0f ? dragAcumulado / areaFrontalTotal : 0.3f;
		if (masaSecaTotal <= 0f) masaSecaTotal = 0.5f; // nunca 0, evita masa=0
	}

	private IEnumerable<Parte> ObtenerPartes(Node nodo)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is Parte p)
				yield return p;
			foreach (Parte nieta in ObtenerPartes(hijo))
				yield return nieta;
		}
	}

	// CollisionShape3D SOLO funciona si su padre DIRECTO es este RigidBody3D
	// (regla dura del motor). Las piezas guardan su colisión anidada adentro
	// porque es más cómodo diseñarlas así, pero acá la "sacamos" y la
	// reparentamos directo al cohete, conservando su posición/rotación exacta.
	private void ReubicarColisiones()
	{
		var formas = ObtenerColisiones(this).ToList();
		foreach (CollisionShape3D forma in formas)
		{
			if (forma.GetParent() == this) continue;

			Transform3D transformGlobal = forma.GlobalTransform;
			forma.GetParent().RemoveChild(forma);
			AddChild(forma);
			forma.GlobalTransform = transformGlobal;
		}
	}

	private IEnumerable<CollisionShape3D> ObtenerColisiones(Node nodo)
	{
		foreach (Node hijo in nodo.GetChildren())
		{
			if (hijo is CollisionShape3D cs)
				yield return cs;
			foreach (CollisionShape3D nieta in ObtenerColisiones(hijo))
				yield return nieta;
		}
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
			if (sasActivado)
				objetivoSas = GlobalTransform.Basis.GetRotationQuaternion();
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

		ultimoInputManual = torqueLocal != Vector3.Zero;

		if (ultimoInputManual)
		{
			Vector3 torque = GlobalTransform.Basis * (torqueLocal * potenciaRotacionActiva);
			ApplyTorque(torque);

			// Mientras piloteás a mano, el SAS "sigue" tu orientación actual.
			// Así, apenas soltás las teclas, te frena justo donde soltaste
			// (igual que en KSP, no te devuelve a la orientación de antes).
			objetivoSas = GlobalTransform.Basis.GetRotationQuaternion();
		}
	}

	private void AplicarSas()
	{
		if (ultimoInputManual) return; // no compite contra tu input manual

		Quaternion actual = GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion diferencia = (objetivoSas * actual.Inverse()).Normalized();

		Vector3 eje = new Vector3(diferencia.X, diferencia.Y, diferencia.Z);
		float senoMitad = eje.Length();
		float angulo = 2.0f * Mathf.Atan2(senoMitad, diferencia.W);
		if (angulo > Mathf.Pi) angulo -= Mathf.Tau;

		Vector3 direccionCorreccion = senoMitad > 0.0001f ? eje / senoMitad : Vector3.Zero;

		// Control PD: te atrae hacia el objetivo (Rigidez) y frena la
		// velocidad angular (Amortiguación), igual que un reaction wheel.
		Vector3 torque = direccionCorreccion * angulo * sasRigidezActiva - AngularVelocity * sasAmortiguacionActiva;
		ApplyTorque(torque);
	}

	private void AplicarEmpuje(float delta)
	{
		if (combustible <= 0.0f || oxidante <= 0.0f || motores.Count == 0)
		{
			motorEncendido = false;
			throttle = 0.0f;
			return;
		}

		float densidad = planeta != null ? planeta.GetDensidadAtmosfera(GlobalPosition) : 0.0f;
		float g0 = 9.81f;
		float consumoTotal = 0f;

		// Cada motor empuja en la dirección de SU PROPIO punto de empuje
		// (no necesariamente el eje de la nave entera: sirve para motores
		// radiales, vernier, etc, además del motor principal apilado).
		foreach (var m in motores)
		{
			float isp = Mathf.Lerp(m.Parte.IspAtmosfera, m.Parte.IspVacio, 1.0f - densidad);
			float empujeMotor = m.Parte.EmpujeMax * throttle * EscalaEmpuje;

			Vector3 direccionEmpuje = m.PuntoEmpuje.GlobalTransform.Basis.Y.Normalized();
			Vector3 brazoPalanca = m.PuntoEmpuje.GlobalPosition - GlobalPosition;
			ApplyForce(direccionEmpuje * empujeMotor, brazoPalanca);

			consumoTotal += (m.Parte.EmpujeMax * throttle / (isp * g0)) * delta * 0.1f;
		}

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
	}

	private void AplicarGravedad()
	{
		Vector3 fuerzaGravedad = planeta.GetGravedadEn(GlobalPosition, Mass);
		ApplyCentralForce(fuerzaGravedad);
	}

	private void AplicarDrag()
	{
		float densidad = planeta.GetDensidadAtmosfera(GlobalPosition);

		if (DebugFisica)
		{
			GD.Print($"Altitud: {planeta.GetAltitud(GlobalPosition):F0} m | Densidad: {densidad:F4} kg/m³ | Vel: {LinearVelocity.Length():F1} m/s");
		}

		if (densidad <= 0.001f) return;

		Vector3 velocidad = LinearVelocity;
		if (velocidad.LengthSquared() < 0.5f) return;

		float fuerzaDrag = 0.5f * densidad * velocidad.LengthSquared() * dragPromedio * areaFrontalTotal;
		ApplyCentralForce(-velocidad.Normalized() * fuerzaDrag);
	}

	private void ActualizarMasa()
	{
		Mass = masaSecaTotal + combustible + oxidante;
	}

	public float GetAltitud()
	{
		if (planeta == null) return 0.0f;
		return planeta.GetAltitud(GlobalPosition);
	}

	public float GetThrottle() => throttle;
	public float GetCombustiblePorcentaje() => combustibleMax > 0 ? combustible / combustibleMax : 0.0f;
	public float GetOxidantePorcentaje() => oxidanteMax > 0 ? oxidante / oxidanteMax : 0.0f;
	public float GetMasaTotal() => Mass;
	public bool EstaMotorEncendido() => motorEncendido && throttle > 0.01f && combustible > 0.0f;
	public bool GetSas() => sasActivado;
	public float GetVelocidad() => LinearVelocity.Length();
	public float GetAltitudPublic() => GetAltitud();
}
