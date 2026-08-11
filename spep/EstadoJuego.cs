using Godot;

// Autoload (Singleton). Guarda datos que tienen que sobrevivir el cambio
// de escena entre el VAB y el mundo: en este caso, qué nave armó/guardó
// el jugador para lanzar.
public partial class EstadoJuego : Node
{
	public static EstadoJuego Instancia { get; private set; }

	public string NaveActivaPath = "";

	public override void _Ready()
	{
		Instancia = this;
	}
}
