using Godot;

// Menú de pausa en mundo.tscn: ESC lo abre/cierra, con opciones para
// volver al VAB (rearmar la nave) o relanzar la misma nave desde cero.
public partial class MenuPausa : CanvasLayer
{
	[Export] public NodePath PanelPath { get; set; } = new NodePath("Panel");

	private Control panel;
	private bool pausado = false;

	public override void _Ready()
	{
		panel = GetNodeOrNull<Control>(PanelPath);
		if (panel != null) panel.Visible = false;

		// Para que el menú siga funcionando (y sus botones respondan)
		// aunque pongamos el árbol en pausa.
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _UnhandledInput(InputEvent evento)
	{
		if (evento is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
		{
			TogglePausa();
		}
	}

	private void TogglePausa()
	{
		pausado = !pausado;
		if (panel != null) panel.Visible = pausado;
		GetTree().Paused = pausado;
	}

	public void Continuar()
	{
		if (pausado) TogglePausa();
	}

	public void VolverAArmar()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://VAB.tscn");
	}

	public void VolverALanzar()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}
}
