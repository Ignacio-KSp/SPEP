extends Camera3D

@export var objetivo: Node3D
@export var distancia_camara: float = 25.0
@export var sensibilidad_raton: float = 0.005

var rotacion_x: float = 0.0
var rotacion_y: float = 0.0
var arrastrando: bool = false

func _unhandled_input(event: InputEvent) -> void:
	# Detectar clic derecho
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando = event.pressed
	
	# Rotar cámara al mover el ratón con clic derecho presionado
	if event is InputEventMouseMotion and arrastrando:
		rotacion_x -= event.relative.x * sensibilidad_raton
		rotacion_y -= event.relative.y * sensibilidad_raton
		# Limitar la rotación vertical para no dar volteretas
		rotacion_y = clamp(rotacion_y, -PI/2.0 + 0.1, PI/2.0 - 0.1)

func _physics_process(delta: float) -> void:
	if not objetivo: return
	
	# 1. Copiar la posición exacta (evita que se aleje a altas velocidades)
	var pos_objetivo = objetivo.global_position
	
	# 2. Calcular la rotación esférica de la cámara
	var offset = Vector3(0, 0, distancia_camara)
	var base_rotacion = Basis().rotated(Vector3.RIGHT, rotacion_y).rotated(Vector3.UP, rotacion_x)
	
	# 3. Aplicar posición y mirar a la nave
	global_position = pos_objetivo + (base_rotacion * offset)
	look_at(pos_objetivo)
