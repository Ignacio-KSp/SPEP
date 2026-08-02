extends RigidBody3D

@export_category("Interfaz")
@export var hud_label: Label 

@export_category("Mecánica Orbital")
@export var radio_planeta: float = 1000.0
@export var masa_planeta: float = 9800000.0  # Gravedad de ~9.8 en la superficie
@export var altura_atmosfera: float = 2000.0 # Límite del espacio

@export_category("Ingeniería de la Nave")
@export var fuerza_empuje_max: float = 45000.0 # Más masa, más potencia
@export var potencia_rcs: float = 60.0
@export var combustible_maximo: float = 500.0
@export var consumo_combustible: float = 5.0

var combustible: float = 0.0
var acelerador: float = 0.0
var sas_activo: bool = true

func _ready() -> void:
	combustible = combustible_maximo
	gravity_scale = 0.0
	linear_damp = 0.0
	angular_damp = 0.0

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_T:
		sas_activo = !sas_activo

func _physics_process(delta: float) -> void:
	var altitud = max(0.0, global_position.length() - radio_planeta)
	
	_aplicar_gravedad(altitud)
	_aplicar_aerodinamica(altitud)
	_procesar_motor(delta)
	_procesar_rcs(delta)
	_actualizar_hud(altitud)

func _aplicar_gravedad(altitud: float) -> void:
	var distancia = global_position.length()
	if distancia > 1.0:
		var direccion_centro = -global_position.normalized()
		var dist_segura = max(distancia, radio_planeta)
		var fuerza_g = masa_planeta / (dist_segura * dist_segura)
		apply_central_force(direccion_centro * fuerza_g)

func _aplicar_aerodinamica(altitud: float) -> void:
	if altitud < altura_atmosfera:
		# Fórmula exponencial: la densidad cae rápido al principio y suave al final
		var factor_densidad = exp(-altitud / (altura_atmosfera * 0.3)) 
		var velocidad_cuad = linear_velocity.length_squared()
		var direccion_opuesta = -linear_velocity.normalized()
		
		# Coeficiente de arrastre ajustado
		var arrastre = direccion_opuesta * velocidad_cuad * factor_densidad * 0.005
		apply_central_force(arrastre)

func _procesar_motor(delta: float) -> void:
	# Acelerador suave tipo Shift/Ctrl (usando Arriba/Abajo por ahora)
	if Input.is_action_pressed("ui_up"):
		acelerador = move_toward(acelerador, 1.0, delta * 0.3)
	elif Input.is_action_pressed("ui_down"):
		acelerador = move_toward(acelerador, 0.0, delta * 0.5)

	if acelerador > 0.0 and combustible > 0.0:
		apply_central_force(transform.basis.y * (fuerza_empuje_max * acelerador))
		combustible -= consumo_combustible * acelerador * delta
		combustible = max(combustible, 0.0)

func _procesar_rcs(delta: float) -> void:
	var hay_input_rotacion = false
	var torque = Vector3.ZERO
	
	if Input.is_action_pressed("ui_left"):
		torque.z += potencia_rcs
		hay_input_rotacion = true
	if Input.is_action_pressed("ui_right"):
		torque.z -= potencia_rcs
		hay_input_rotacion = true
		
	if hay_input_rotacion:
		apply_torque(torque)
	elif sas_activo:
		# SAS Mejorado: Frenado de rotación estricto si no tocas teclas
		angular_velocity = angular_velocity.move_toward(Vector3.ZERO, delta * 10.0)

func _actualizar_hud(altitud: float) -> void:
	if not hud_label: return
	var velocidad = linear_velocity.length()
	var porcentaje_comb = (combustible / combustible_maximo) * 100.0
	var estado_sas = "ENCENDIDO" if sas_activo else "APAGADO"
	
	hud_label.text = "ALTITUD: %d m\nVELOCIDAD: %.1f m/s\nMOTOR: %d%%\nCOMBUSTIBLE: %d%%\nSAS (T): %s" % [
		int(altitud), velocidad, int(acelerador * 100), int(porcentaje_comb), estado_sas
	]
