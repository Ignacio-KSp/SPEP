extends Node3D

@export var cohete_path: NodePath = NodePath("../Cohete")
@export var planeta_path: NodePath = NodePath("../Planeta")
@export var camara_normal_path: NodePath = NodePath("../Camera3D")

@onready var cohete: RigidBody3D = get_node_or_null(cohete_path)
@onready var planeta: Node3D = get_node_or_null(planeta_path)
@onready var camara_normal: Camera3D = get_node_or_null(camara_normal_path)

@export var puntos_trayectoria: int = 600
@export var dt_prediccion: float = 0.15
@export var altura_inicial_mapa: float = 2800.0

var mapa_activo: bool = false
var linea: MeshInstance3D
var marcador_cohete: MeshInstance3D
var camara_mapa: Camera3D

var rotacion_mapa_x: float = 0.0
var rotacion_mapa_y: float = 0.6
var distancia_mapa: float = 2800.0
var arrastrando_izq: bool = false
var arrastrando_der: bool = false

var _m_anterior: bool = false

func _ready() -> void:
	# === Línea de órbita ===
	linea = MeshInstance3D.new()
	add_child(linea)
	
	var mat = StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(0.1, 1.0, 0.4, 0.95)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	linea.material_override = mat
	
	# Corrección 1: Crear el ImmediateMesh una sola vez para evitar pérdida de memoria
	var imm = ImmediateMesh.new()
	linea.mesh = imm
	
	linea.visible = false
	
	# === Marcador del cohete (punto) ===
	marcador_cohete = MeshInstance3D.new()
	add_child(marcador_cohete)
	
	var esfera = SphereMesh.new()
	esfera.radius = 8.0
	esfera.height = 16.0
	marcador_cohete.mesh = esfera
	
	var mat_marcador = StandardMaterial3D.new()
	mat_marcador.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat_marcador.albedo_color = Color(1.0, 0.3, 0.1) # Naranja/rojo
	marcador_cohete.material_override = mat_marcador
	marcador_cohete.visible = false
	
	# === Cámara del mapa ===
	camara_mapa = Camera3D.new()
	camara_mapa.name = "CamaraMapa"
	add_child(camara_mapa)
	camara_mapa.current = false
	distancia_mapa = altura_inicial_mapa

func _unhandled_input(event: InputEvent) -> void:
	if not mapa_activo:
		return
	
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			arrastrando_izq = event.pressed
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando_der = event.pressed
		
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distancia_mapa = clampf(distancia_mapa - 90.0, 400.0, 9000.0)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distancia_mapa = clampf(distancia_mapa + 90.0, 400.0, 9000.0)
	
	if event is InputEventMouseMotion:
		if arrastrando_izq or arrastrando_der:
			rotacion_mapa_x -= event.relative.x * 0.005
			rotacion_mapa_y -= event.relative.y * 0.005
			rotacion_mapa_y = clampf(rotacion_mapa_y, 0.12, 1.5)

func _process(_delta: float) -> void:
	if _just_pressed_M():
		mapa_activo = not mapa_activo
		_activar_mapa(mapa_activo)
		print("Mapa Orbital: ", "ON" if mapa_activo else "OFF")
	
	if mapa_activo and cohete and planeta:
		actualizar_trayectoria()
		actualizar_camara_mapa()
		actualizar_marcador()

func _just_pressed_M() -> bool:
	var actual = Input.is_physical_key_pressed(KEY_M)
	var resultado = actual and not _m_anterior
	_m_anterior = actual
	return resultado

func _activar_mapa(activo: bool) -> void:
	linea.visible = activo
	marcador_cohete.visible = activo
	
	camara_mapa.current = activo
	if camara_normal:
		camara_normal.current = not activo

func actualizar_camara_mapa() -> void:
	if not planeta:
		return
	
	var centro = planeta.global_position
	var offset = Vector3(0, 0, distancia_mapa)
	var base = Basis().rotated(Vector3.RIGHT, rotacion_mapa_y).rotated(Vector3.UP, rotacion_mapa_x)
	
	camara_mapa.global_position = centro + (base * offset)
	camara_mapa.look_at(centro)

func actualizar_marcador() -> void:
	if cohete:
		marcador_cohete.global_position = cohete.global_position

func actualizar_trayectoria() -> void:
	if not cohete or not planeta:
		# En lugar de null, limpiamos la malla existente
		var imm_vacia = linea.mesh as ImmediateMesh
		if imm_vacia:
			imm_vacia.clear_surfaces()
		return
	
	var puntos: PackedVector3Array = []
	var pos = cohete.global_position
	var vel = cohete.linear_velocity
	
	var masa_planeta = 15000000.0
	var radio_p = 1000.0
	var constante_g = 0.00000000006674 # Constante G por defecto
	
	if planeta.has_method("get_masa"):
		masa_planeta = planeta.get_masa()
		
	# Buscamos 'G' o mantenemos retrocompatibilidad con 'factor_gravedad'
	if "G" in planeta:
		constante_g = planeta.G
	elif "factor_gravedad" in planeta:
		constante_g = planeta.factor_gravedad
		
	if planeta.has_method("get_radio"):
		radio_p = planeta.get_radio()
	
	# Predicción más larga y estable
	for i in range(puntos_trayectoria):
		puntos.append(pos)
		
		var dir = planeta.global_position - pos
		var dist = dir.length()
		
		if dist < radio_p + 2.0:
			break
		
		# Corrección 2: Gravedad Newtoniana sincronizada
		var acc = dir.normalized() * ((constante_g * masa_planeta) / (dist * dist))
		
		# Integración un poco más estable
		vel += acc * dt_prediccion
		pos += vel * dt_prediccion
	
	if puntos.size() < 2:
		var imm_vacia = linea.mesh as ImmediateMesh
		imm_vacia.clear_surfaces()
		return
	
	# Corrección 1 (continuación): Reutilizar la malla existente
	var imm = linea.mesh as ImmediateMesh
	imm.clear_surfaces()
	imm.surface_begin(Mesh.PRIMITIVE_LINE_STRIP)
	
	for p in puntos:
		imm.surface_add_vertex(to_local(p))
	
	imm.surface_end()
