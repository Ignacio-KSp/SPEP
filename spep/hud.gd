extends Control

@export var cohete_path: NodePath = NodePath("../../Cohete")
@onready var cohete: Cohete = get_node_or_null(cohete_path)

@onready var label_altitud: Label = $Altitud
@onready var label_velocidad: Label = $Velocidad
@onready var label_throttle: Label = $Throttle
@onready var label_combustible: Label = $Combustible
@onready var label_motor: Label = $Motor
@onready var label_sas: Label = $SAS

func _ready() -> void:
	if not cohete:
		push_error("HUD: No se encontró el Cohete")
	
	if not label_altitud: push_error("Falta Label Altitud")
	if not label_velocidad: push_error("Falta Label Velocidad")
	if not label_throttle: push_error("Falta Label Throttle")
	if not label_combustible: push_error("Falta Label Combustible")
	if not label_motor: push_error("Falta Label Motor")
	if not label_sas: push_error("Falta Label SAS")

func _process(_delta: float) -> void:
	if not cohete:
		return
	
	if label_altitud:
		label_altitud.text = "Altitud: %.0f m" % cohete.get_altitud_public()
	
	if label_velocidad:
		label_velocidad.text = "Velocidad: %.1f m/s" % cohete.get_velocidad()
	
	if label_throttle:
		label_throttle.text = "Throttle: %.0f%%" % (cohete.get_throttle() * 100.0)
	
	if label_combustible:
		label_combustible.text = "Combustible: %.0f%%" % (cohete.get_combustible_porcentaje() * 100.0)
	
	if label_motor:
		label_motor.text = "Motor: " + ("ON" if cohete.esta_motor_encendido() else "OFF")
	
	if label_sas:
		label_sas.text = "SAS: " + ("ON" if cohete.get_sas() else "OFF")
