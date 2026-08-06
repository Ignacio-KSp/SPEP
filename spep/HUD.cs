using Godot;

public partial class HUD : Control
{
	[Export] public NodePath CohetePath { get; set; } = new NodePath("../../Cohete");

	private Cohete cohete;

	private Label labelAltitud;
	private Label labelVelocidad;
	private Label labelThrottle;
	private Label labelCombustible;
	private Label labelMotor;
	private Label labelSas;

	public override void _Ready()
	{
		cohete = GetNodeOrNull<Cohete>(CohetePath);

		labelAltitud = GetNodeOrNull<Label>("Altitud");
		labelVelocidad = GetNodeOrNull<Label>("Velocidad");
		labelThrottle = GetNodeOrNull<Label>("Throttle");
		labelCombustible = GetNodeOrNull<Label>("Combustible");
		labelMotor = GetNodeOrNull<Label>("Motor");
		labelSas = GetNodeOrNull<Label>("SAS");

		if (cohete == null)
			GD.PushError("HUD: No se encontró el Cohete");

		if (labelAltitud == null) GD.PushError("Falta Label Altitud");
		if (labelVelocidad == null) GD.PushError("Falta Label Velocidad");
		if (labelThrottle == null) GD.PushError("Falta Label Throttle");
		if (labelCombustible == null) GD.PushError("Falta Label Combustible");
		if (labelMotor == null) GD.PushError("Falta Label Motor");
		if (labelSas == null) GD.PushError("Falta Label SAS");
	}

	public override void _Process(double delta)
	{
		if (cohete == null)
			return;

		if (labelAltitud != null)
			labelAltitud.Text = $"Altitud: {cohete.GetAltitudPublic():0} m";

		if (labelVelocidad != null)
			labelVelocidad.Text = $"Velocidad: {cohete.GetVelocidad():0.0} m/s";

		if (labelThrottle != null)
			labelThrottle.Text = $"Throttle: {cohete.GetThrottle() * 100f:0}%";

		if (labelCombustible != null)
			labelCombustible.Text = $"Combustible: {cohete.GetCombustiblePorcentaje() * 100f:0}%";

		if (labelMotor != null)
			labelMotor.Text = "Motor: " + (cohete.EstaMotorEncendido() ? "ON" : "OFF");

		if (labelSas != null)
			labelSas.Text = "SAS: " + (cohete.GetSas() ? "ON" : "OFF");
	}
}
