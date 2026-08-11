extends Camera3D

@export var objetivo: Node3D
@export var distancia_base: float = 25.0
@export var sensibilidad_raton: float = 0.005

# --- Zoom ---
# Antes el zoom sumaba/restaba un valor fijo (velocidad_zoom) por click de rueda.
# Con un rango de 1 m a 150 km eso hubiera significado miles de clicks para
# recorrerlo entero. Ahora el zoom es MULTIPLICATIVO (porcentual): cada click
# acerca/aleja un % de la distancia actual, así funciona bien tanto cerca del
# suelo (metros) como en órbita alta (decenas de km).
@export var factor_zoom: float = 0.12
@export var distancia_min: float = 1.0
@export var distancia_max: float = 150000.0 # alcanza para ver todo el planeta+atmosfera desde afuera

# --- Plano de corte de la cámara (near/far) ---
# "far" tiene que cubrir todo lo que quieras poder ver: RadioPlaneta +
# AlturaAtmosfera + la altura de tus órbitas más altas. Si algo desaparece a
# lo lejos (el planeta, la nave, lo que sea), lo primero a revisar es esto:
# por defecto Godot pone far≈4000, mucho menos que un planeta de 10-80 km.
@export var near_clip: float = 0.05
@export var far_clip: float = 300000.0

var rotacion_x: float = 0.0
var rotacion_y: float = 0.3
var arrastrando: bool = false
var distancia_actual: float = 25.0

func _ready() -> void:
	distancia_actual = distancia_base
	near = near_clip
	far = far_clip

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando = event.pressed
		
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distancia_actual = clampf(distancia_actual * (1.0 - factor_zoom), distancia_min, distancia_max)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distancia_actual = clampf(distancia_actual * (1.0 + factor_zoom), distancia_min, distancia_max)
	
	if event is InputEventMouseMotion and arrastrando:
		rotacion_x -= event.relative.x * sensibilidad_raton
		rotacion_y -= event.relative.y * sensibilidad_raton
		# FIX: Ampliado a -1.5 para poder rotar por debajo de la nave
		rotacion_y = clampf(rotacion_y, -1.5, 1.5)

func _physics_process(_delta: float) -> void:
	if not objetivo:
		return
	
	var offset = Vector3(0, 0, distancia_actual)
	var base_rot = Basis().rotated(Vector3.RIGHT, rotacion_y).rotated(Vector3.UP, rotacion_x)
	
	global_position = objetivo.global_position + (base_rot * offset)
	look_at(objetivo.global_position)
