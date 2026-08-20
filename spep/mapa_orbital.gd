extends Node3D

@export var cohete_path: NodePath = NodePath("../Cohete")
@export var planeta_path: NodePath = NodePath("../Planeta")
@export var camara_normal_path: NodePath = NodePath("../Camera3D")

@onready var cohete: RigidBody3D = get_node_or_null(cohete_path)
@onready var planeta: Node3D = get_node_or_null(planeta_path)
@onready var camara_normal: Camera3D = get_node_or_null(camara_normal_path)

@export var puntos_elipse: int = 180

# Plano de corte de la cámara del mapa. Si "far" es menor que la distancia a
# la que está la cámara del centro del planeta (o a la que puede llegar el
# zoom), el planeta y la línea de órbita directamente no se dibujan.
# Lo dejamos MUY grande a propósito: el proyecto todavía pesa poco, no hace
# falta optimizar esto por ahora.
@export var mapa_near_clip: float = 1.0
@export var mapa_far_clip: float = 5000000.0

var mapa_activo: bool = false
var linea: MeshInstance3D
var marcador_cohete: MeshInstance3D
var marcador_periapsis: MeshInstance3D
var marcador_apoapsis: MeshInstance3D
var label_periapsis: Label3D
var label_apoapsis: Label3D
var camara_mapa: Camera3D

var rotacion_mapa_x: float = 0.0
var rotacion_mapa_y: float = 0.6
var distancia_mapa: float = 2800.0
@export var distancia_mapa_min: float = 500.0
@export var distancia_mapa_max: float = 4000000.0
var factor_zoom_mapa: float = 0.12
var arrastrando_izq: bool = false
var arrastrando_der: bool = false

@export var altura_inicial_mapa: float = 40000.0

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

	var imm = ImmediateMesh.new()
	linea.mesh = imm

	# Evita que la línea "desaparezca" por culling cuando el mesh dinámico
	# queda con una caja de límites (AABB) mal calculada en algún frame.
	# Le damos un límite generoso fijo, cubre cualquier órbita razonable.
	linea.custom_aabb = AABB(Vector3(-2e7, -2e7, -2e7), Vector3(4e7, 4e7, 4e7))

	linea.visible = false

	# === Marcador del cohete (punto) ===
	marcador_cohete = _crear_marcador(Color(1.0, 0.5, 0.1))

	# === Marcadores de Periapsis / Apoapsis ===
	marcador_periapsis = _crear_marcador(Color(0.2, 0.9, 1.0))
	marcador_apoapsis = _crear_marcador(Color(1.0, 0.9, 0.2))

	label_periapsis = _crear_label()
	label_apoapsis = _crear_label()

	# === Cámara del mapa ===
	camara_mapa = Camera3D.new()
	camara_mapa.name = "CamaraMapa"
	add_child(camara_mapa)
	camara_mapa.current = false
	camara_mapa.near = mapa_near_clip
	camara_mapa.far = mapa_far_clip
	distancia_mapa = altura_inicial_mapa

func _crear_marcador(color: Color) -> MeshInstance3D:
	var m = MeshInstance3D.new()
	add_child(m)
	var esfera = SphereMesh.new()
	esfera.radius = 8.0
	esfera.height = 16.0
	m.mesh = esfera
	var mat = StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = color
	m.material_override = mat
	m.visible = false
	return m

func _crear_label() -> Label3D:
	var l = Label3D.new()
	add_child(l)
	l.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	l.no_depth_test = true
	l.font_size = 32
	l.outline_size = 8
	l.visible = false
	return l

func _unhandled_input(event: InputEvent) -> void:
	if not mapa_activo:
		return

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			arrastrando_izq = event.pressed
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			arrastrando_der = event.pressed

		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distancia_mapa = clampf(distancia_mapa * (1.0 - factor_zoom_mapa), distancia_mapa_min, distancia_mapa_max)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distancia_mapa = clampf(distancia_mapa * (1.0 + factor_zoom_mapa), distancia_mapa_min, distancia_mapa_max)

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
	if not activo:
		marcador_periapsis.visible = false
		marcador_apoapsis.visible = false
		label_periapsis.visible = false
		label_apoapsis.visible = false

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
	if not cohete:
		return
	marcador_cohete.global_position = cohete.global_position

	# El punto se agranda con el zoom del mapa para que siga siendo visible
	# aunque estés viendo el planeta entero desde muy lejos.
	var escala = clampf(distancia_mapa * 0.004, 1.0, 4000.0)
	marcador_cohete.scale = Vector3.ONE * escala
	marcador_periapsis.scale = Vector3.ONE * escala
	marcador_apoapsis.scale = Vector3.ONE * escala

func actualizar_trayectoria() -> void:
	if not cohete or not planeta:
		var imm_vacia = linea.mesh as ImmediateMesh
		if imm_vacia:
			imm_vacia.clear_surfaces()
		marcador_periapsis.visible = false
		marcador_apoapsis.visible = false
		label_periapsis.visible = false
		label_apoapsis.visible = false
		return

	var centro_planeta = planeta.global_position
	var r_vec = cohete.global_position - centro_planeta
	var v_vec = cohete.linear_velocity
	var r = r_vec.length()

	var masa_planeta = 1.47e19
	var radio_p = 10000.0
	var constante_g = 0.00000000006674

	# Planeta.cs (C#) expone estas propiedades directamente como
	# "MasaPlaneta" / "RadioPlaneta" / "G" (no como métodos get_masa()/get_radio()).
	if "MasaPlaneta" in planeta:
		masa_planeta = planeta.MasaPlaneta
	elif planeta.has_method("get_masa"):
		masa_planeta = planeta.get_masa()

	if "RadioPlaneta" in planeta:
		radio_p = planeta.RadioPlaneta
	elif planeta.has_method("get_radio"):
		radio_p = planeta.get_radio()

	if "G" in planeta:
		constante_g = planeta.G
	elif "factor_gravedad" in planeta:
		constante_g = planeta.factor_gravedad

	var mu = constante_g * masa_planeta

	if r < 1.0 or mu <= 0.0:
		return

	# === Elementos orbitales exactos (geometría de cónicas, no integración) ===
	var h_vec = r_vec.cross(v_vec)
	var h2 = h_vec.length_squared()

	if h2 < 0.001:
		var imm_r = linea.mesh as ImmediateMesh
		imm_r.clear_surfaces()
		marcador_periapsis.visible = false
		marcador_apoapsis.visible = false
		label_periapsis.visible = false
		label_apoapsis.visible = false
		return

	var h = sqrt(h2)
	var h_hat = h_vec / h

	var e_vec = ((v_vec.length_squared() - mu / r) * r_vec - r_vec.dot(v_vec) * v_vec) / mu
	var e = e_vec.length()

	var e_hat: Vector3
	if e > 0.0005:
		e_hat = e_vec.normalized()
	else:
		e_hat = r_vec.normalized()

	var p_hat = h_hat.cross(e_hat).normalized()
	var semilatus = h2 / mu

	var puntos: PackedVector3Array = []

	if e < 1.0:
		var n = puntos_elipse
		for i in range(n + 1):
			var theta = TAU * float(i) / float(n)
			var radio = semilatus / (1.0 + e * cos(theta))
			var punto_rel = radio * (cos(theta) * e_hat + sin(theta) * p_hat)
			puntos.append(centro_planeta + punto_rel)

		var a = semilatus / (1.0 - e * e)
		var rp = a * (1.0 - e)
		var ra = a * (1.0 + e)

		marcador_periapsis.global_position = centro_planeta + rp * e_hat
		marcador_apoapsis.global_position = centro_planeta - ra * e_hat

		label_periapsis.global_position = marcador_periapsis.global_position
		label_apoapsis.global_position = marcador_apoapsis.global_position
		label_periapsis.text = "PE: %d m" % int(rp - radio_p)
		label_apoapsis.text = "AP: %d m" % int(ra - radio_p)

		marcador_periapsis.visible = true
		marcador_apoapsis.visible = true
		label_periapsis.visible = true
		label_apoapsis.visible = true
	else:
		var theta_max = acos(clampf(-1.0 / e, -0.999, 0.999)) * 0.97
		var n = puntos_elipse
		for i in range(n + 1):
			var theta = -theta_max + (2.0 * theta_max) * float(i) / float(n)
			var denom = 1.0 + e * cos(theta)
			if denom < 0.02:
				continue
			var radio = semilatus / denom
			var punto_rel = radio * (cos(theta) * e_hat + sin(theta) * p_hat)
			puntos.append(centro_planeta + punto_rel)

		var rp = semilatus / (1.0 + e)
		marcador_periapsis.global_position = centro_planeta + rp * e_hat
		label_periapsis.global_position = marcador_periapsis.global_position
		label_periapsis.text = "PE: %d m" % int(rp - radio_p)
		marcador_periapsis.visible = true
		label_periapsis.visible = true

		marcador_apoapsis.visible = false
		label_apoapsis.visible = false

	if puntos.size() < 2:
		var imm_vacia2 = linea.mesh as ImmediateMesh
		imm_vacia2.clear_surfaces()
		return

	var imm = linea.mesh as ImmediateMesh
	imm.clear_surfaces()
	imm.surface_begin(Mesh.PRIMITIVE_LINE_STRIP)
	for p in puntos:
		imm.surface_add_vertex(to_local(p))
	imm.surface_end()
