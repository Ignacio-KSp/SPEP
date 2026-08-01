extends RigidBody3D

@export_category("Física Espacial")
@export var masa_planeta: float = 500000.0   # Fuerza ajustada
@export var fuerza_empuje_max: float = 3000.0  # Potencia del motor
@export var potencia_rcs: float = 15.0         # Control de giro

@export_category("Combustible")
@export var combustible: float = 100.0
@export var consumo_combustible: float = 2.0

var acelerador: float = 0.0

func _ready() -> void:
	# 1. Desactivar la gravedad por defecto del motor de Godot
	gravity_scale = 0.0
	
	# 2. Agregar amortiguación (freno) para evitar giros y saltos infinitos
	linear_damp = 0.8
	angular_damp = 3.0

func _physics_process(delta: float) -> void:
	_aplicar_gravedad_planetaria()
	_procesar_controles(delta)

func _aplicar_gravedad_planetaria() -> void:
	var distancia = global_position.length()
	
	if distancia > 1.0:
		var direccion_al_centro = -global_position.normalized()
		# Evita que la fuerza se vuelva gigante cuando toca la superficie (radio 10m)
		var distancia_segura = max(distancia, 10.0) 
		var fuerza = masa_planeta / (distancia_segura * distancia_segura)
		
		apply_central_force(direccion_al_centro * fuerza)

func _procesar_controles(delta: float) -> void:
	# Aumentar o reducir acelerador con Flecha Arriba / Abajo
	if Input.is_action_pressed("ui_up"):
		acelerador = move_toward(acelerador, 1.0, delta)
	elif Input.is_action_pressed("ui_down"):
		acelerador = move_toward(acelerador, 0.0, delta)
	
	# Empuje del motor principal
	if acelerador > 0.0 and combustible > 0.0:
		apply_central_force(transform.basis.y * (fuerza_empuje_max * acelerador))
		combustible -= consumo_combustible * acelerador * delta
		combustible = max(combustible, 0.0)
	
	# Rotación RCS (Giro con Flecha Izquierda / Derecha)
	var torque = Vector3.ZERO
	if Input.is_action_pressed("ui_left"):
		torque.z += potencia_rcs
	if Input.is_action_pressed("ui_right"):
		torque.z -= potencia_rcs
		
	apply_torque(torque)
