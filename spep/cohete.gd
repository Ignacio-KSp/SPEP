extends RigidBody3D
class_name Cohete

# ======================
# REFERENCIAS
# ======================
@export var planeta_path: NodePath = NodePath("../Planeta")
@onready var planeta: Node3D = get_node_or_null(planeta_path)

@export var thrust_point: Node3D

# ======================
# TANQUE
# ======================
@export_group("Tanque")
@export var combustible_max: float = 500.0
@export var oxidante_max: float = 500.0
@export var masa_seca: float = 2.5

var combustible: float
var oxidante: float

# ======================
# MOTOR
# ======================
@export_group("Motor")
@export var empuje_max: float = 18000.0
@export var isp_vacio: float = 320.0
@export var isp_atmosfera: float = 260.0
@export var ratio_oxidante: float = 1.2

# ======================
# CONTROL Y SAS
# ======================
@export_group("Control")
@export var potencia_rotacion: float = 25.0
@export var potencia_sas: float = 30.0
@export var throttle_speed: float = 1.5

var throttle: float = 0.0
var motor_encendido: bool = false
var sas_activado: bool = false

# Para detectar solo el momento de pulsar una tecla
var _teclas_anteriores = {}

# ======================
# FÍSICA
# ======================
@export_group("Física")
@export var area_frontal: float = 1.8
@export var coeficiente_drag: float = 0.4
@export var altura_atmosfera: float = 5500.0
@export var scale_height: float = 700.0
@export var usar_gravedad_custom: bool = true
@export var factor_gravedad: float = 0.25

func _ready() -> void:
	combustible = combustible_max
	oxidante = oxidante_max
	_actualizar_masa()
	
	can_sleep = false
	sleeping = false
	gravity_scale = 0.0
	
	motor_encendido = false
	throttle = 0.0
	sas_activado = false
	
	print("=== Cohete listo ===")

func _physics_process(delta: float) -> void:
	_manejar_input(delta)
	_actualizar_masa()
	
	if usar_gravedad_custom and planeta:
		aplicar_gravedad()
	
	aplicar_drag()
	
	# Solo impulsa si el motor está ENCENDIDO
	if motor_encendido and throttle > 0.01:
		aplicar_empuje(delta)
	
	aplicar_control_rotacion()
	
	if sas_activado:
		aplicar_sas()

# ======================
# DETECCIÓN DE TECLA (solo un click)
# ======================
func _just_pressed(key: Key) -> bool:
	var esta_presionada = Input.is_physical_key_pressed(key)
	var estaba_presionada = _teclas_anteriores.get(key, false)
	_teclas_anteriores[key] = esta_presionada
	return esta_presionada and not estaba_presionada

# ======================
# INPUT
# ======================
func _manejar_input(delta: float) -> void:
	# Motor ON/OFF con Space (solo un click)
	if _just_pressed(KEY_SPACE):
		motor_encendido = not motor_encendido
		print("Motor: ", "ENCENDIDO" if motor_encendido else "APAGADO")
	
	# SAS ON/OFF con T (solo un click)
	if _just_pressed(KEY_T):
		sas_activado = not sas_activado
		print("SAS: ", "ON" if sas_activado else "OFF")
	
	# Throttle solo funciona si el motor está encendido
	if motor_encendido:
		if Input.is_key_pressed(KEY_SHIFT):
			throttle = clampf(throttle + throttle_speed * delta, 0.0, 1.0)
		elif Input.is_key_pressed(KEY_CTRL):
			throttle = clampf(throttle - throttle_speed * delta, 0.0, 1.0)
		
		if Input.is_key_pressed(KEY_Z):
			throttle = 1.0
		if Input.is_key_pressed(KEY_X):
			throttle = 0.0
	else:
		throttle = 0.0

# ======================
# ROTACIÓN estilo KSP (W A S D Q E)
# ======================
func aplicar_control_rotacion() -> void:
	var torque_local = Vector3.ZERO
	
	# W / S → Pitch
	if Input.is_key_pressed(KEY_W):
		torque_local.x += potencia_rotacion
	if Input.is_key_pressed(KEY_S):
		torque_local.x -= potencia_rotacion
	
	# A / D → Yaw
	if Input.is_key_pressed(KEY_A):
		torque_local.y += potencia_rotacion
	if Input.is_key_pressed(KEY_D):
		torque_local.y -= potencia_rotacion
	
	# Q / E → Roll
	if Input.is_key_pressed(KEY_Q):
		torque_local.z += potencia_rotacion
	if Input.is_key_pressed(KEY_E):
		torque_local.z -= potencia_rotacion
	
	if torque_local != Vector3.ZERO:
		# Convertimos el torque local a global y lo aplicamos
		var torque_global = global_transform.basis * torque_local
		apply_torque(torque_global * mass)  # Multiplicamos por masa para que se note
func aplicar_sas() -> void:
	var av = angular_velocity
	if av.length() > 0.02:
		apply_torque(-av * potencia_sas * mass)

# ======================
# EMPUJE
# ======================
func aplicar_empuje(delta: float) -> void:
	if combustible <= 0.0 or oxidante <= 0.0:
		motor_encendido = false
		throttle = 0.0
		return
	
	var altitud = get_altitud()
	var densidad = get_densidad_atmosfera(altitud)
	var isp = lerpf(isp_atmosfera, isp_vacio, 1.0 - densidad)
	
	var empuje_actual = empuje_max * throttle
	
	var g0 = 9.81
	var consumo_total = (empuje_actual / (isp * g0)) * delta
	var consumo_comb = consumo_total / (1.0 + ratio_oxidante)
	var consumo_oxi  = consumo_total - consumo_comb
	
	if combustible < consumo_comb or oxidante < consumo_oxi:
		combustible = 0.0
		oxidante = 0.0
		motor_encendido = false
		throttle = 0.0
		return
	
	combustible -= consumo_comb
	oxidante -= consumo_oxi
	
	var direccion: Vector3
	if thrust_point and is_instance_valid(thrust_point):
		direccion = thrust_point.global_transform.basis.y
	else:
		direccion = global_transform.basis.y
	
	apply_central_force(direccion * empuje_actual)

# ======================
# GRAVEDAD Y DRAG
# ======================
func aplicar_gravedad() -> void:
	if not planeta:
		return
	
	var direccion = planeta.global_position - global_position
	var distancia = direccion.length()
	if distancia < 5.0:
		return
	
	direccion = direccion.normalized()
	
	var masa_planeta = 9800000.0
	if planeta.has_method("get_masa"):
		masa_planeta = planeta.get_masa()
	
	var fuerza = (masa_planeta * mass) / (distancia * distancia) * factor_gravedad
	apply_central_force(direccion * fuerza)

func aplicar_drag() -> void:
	var densidad = get_densidad_atmosfera(get_altitud())
	if densidad <= 0.001:
		return
	
	var velocidad = linear_velocity
	if velocidad.length_squared() < 0.5:
		return
	
	var fuerza_drag = 0.5 * densidad * velocidad.length_squared() * coeficiente_drag * area_frontal
	apply_central_force(-velocidad.normalized() * fuerza_drag)

func _actualizar_masa() -> void:
	mass = masa_seca + combustible + oxidante

func get_altitud() -> float:
	if not planeta:
		return 0.0
	return global_position.distance_to(planeta.global_position) - get_radio_planeta()

func get_radio_planeta() -> float:
	if planeta and planeta.has_method("get_radio"):
		return planeta.get_radio()
	return 1000.0

func get_densidad_atmosfera(altitud: float) -> float:
	if altitud >= altura_atmosfera:
		return 0.0
	if altitud < 0.0:
		return 1.0
	return exp(-altitud / scale_height)

# ======================
# API PARA EL HUD
# ======================
func get_throttle() -> float:
	return throttle

func get_combustible_porcentaje() -> float:
	return combustible / combustible_max if combustible_max > 0.0 else 0.0

func get_oxidante_porcentaje() -> float:
	return oxidante / oxidante_max if oxidante_max > 0.0 else 0.0

func get_masa_total() -> float:
	return mass

func esta_motor_encendido() -> bool:
	return motor_encendido and throttle > 0.01 and combustible > 0.0

func get_sas() -> bool:
	return sas_activado

func get_velocidad() -> float:
	return linear_velocity.length()

func get_altitud_public() -> float:
	return get_altitud()
