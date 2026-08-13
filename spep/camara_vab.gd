extends Camera3D

# Cámara orbital para el VAB.
# - Rueda del mouse: zoom (acerca/aleja)
# - Shift + rueda del mouse: sube/baja el punto donde mira la cámara (eje Y)
# - Click derecho + arrastrar: rota alrededor de la nave

@export var objetivo: Node3D  # normalmente la RaizNave
@export var sensibilidad_raton: float = 0.005

@export var distancia_base: float = 8.0
@export var factor_zoom: float = 0.12
@export var distancia_min: float = 1.5
@export var distancia_max: float = 60.0

@export var velocidad_altura: float = 0.4
@export var altura_min: float = -2.0
@export var altura_max: float = 15.0

var rotacion_x: float = 0.0
var rotacion_y: float = 0.3
var arrastrando: bool = false
var distancia_actual: float = 8.0
var altura_objetivo: float = 2.0

func _ready() -> void:
	distancia_actual = distancia_base

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando = event.pressed

		var con_shift = Input.is_key_pressed(KEY_SHIFT)

		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			if con_shift:
				altura_objetivo = clampf(altura_objetivo + velocidad_altura, altura_min, altura_max)
			else:
				distancia_actual = clampf(distancia_actual * (1.0 - factor_zoom), distancia_min, distancia_max)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			if con_shift:
				altura_objetivo = clampf(altura_objetivo - velocidad_altura, altura_min, altura_max)
			else:
				distancia_actual = clampf(distancia_actual * (1.0 + factor_zoom), distancia_min, distancia_max)

	if event is InputEventMouseMotion and arrastrando:
		rotacion_x -= event.relative.x * sensibilidad_raton
		rotacion_y -= event.relative.y * sensibilidad_raton
		rotacion_y = clampf(rotacion_y, -1.5, 1.5)

func _physics_process(_delta: float) -> void:
	var centro := Vector3(0.0, altura_objetivo, 0.0)
	if objetivo:
		centro = objetivo.global_position + Vector3.UP * altura_objetivo

	var offset := Vector3(0, 0, distancia_actual)
	var base_rot := Basis().rotated(Vector3.RIGHT, rotacion_y).rotated(Vector3.UP, rotacion_x)

	global_position = centro + (base_rot * offset)
	look_at(centro)
