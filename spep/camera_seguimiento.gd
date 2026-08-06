extends Camera3D

@export var objetivo: Node3D
@export var distancia_base: float = 25.0
@export var sensibilidad_raton: float = 0.005
@export var velocidad_zoom: float = 2.5
@export var distancia_min: float = 4.0
@export var distancia_max: float = 120.0

var rotacion_x: float = 0.0
var rotacion_y: float = 0.3
var arrastrando: bool = false
var distancia_actual: float = 25.0

func _ready() -> void:
	distancia_actual = distancia_base

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando = event.pressed
		
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distancia_actual = clampf(distancia_actual - velocidad_zoom, distancia_min, distancia_max)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distancia_actual = clampf(distancia_actual + velocidad_zoom, distancia_min, distancia_max)
	
	if event is InputEventMouseMotion and arrastrando:
		rotacion_x -= event.relative.x * sensibilidad_raton
		rotacion_y -= event.relative.y * sensibilidad_raton
		rotacion_y = clampf(rotacion_y, -1.4, 1.4)

func _physics_process(_delta: float) -> void:
	if not objetivo:
		return
	
	var offset = Vector3(0, 0, distancia_actual)
	var base_rot = Basis().rotated(Vector3.RIGHT, rotacion_y).rotated(Vector3.UP, rotacion_x)
	
	global_position = objetivo.global_position + (base_rot * offset)
	look_at(objetivo.global_position)
