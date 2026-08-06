extends StaticBody3D
class_name Planeta

@export_group("Propiedades del Planeta")
@export var radio: float = 1000.0
@export var masa: float = 15000000.0          # Masa un poco más alta

@export_group("Atmósfera")
@export var altura_atmosfera: float = 150.0   # Atmósfera hasta 150 unidades
@export var scale_height: float = 30.0        # Densidad baja de forma realista
@export var densidad_superficie: float = 1.2

@export_group("Gravedad")
@export var factor_gravedad: float = 0.28     # Valor clave para poder orbitar

# ======================
# API pública
# ======================

func get_radio() -> float:
	return radio

func get_masa() -> float:
	return masa

func get_altitud(posicion_global: Vector3) -> float:
	return posicion_global.distance_to(global_position) - radio

func get_densidad_atmosfera(posicion_global: Vector3) -> float:
	var altitud = get_altitud(posicion_global)
	
	if altitud >= altura_atmosfera:
		return 0.0
	if altitud < 0.0:
		return densidad_superficie
	
	return densidad_superficie * exp(-altitud / scale_height)

func get_gravedad_en(posicion_global: Vector3, masa_objeto: float) -> Vector3:
	var direccion = global_position - posicion_global
	var distancia = direccion.length()
	
	if distancia < 1.0:
		return Vector3.ZERO
	
	direccion = direccion.normalized()
	
	var fuerza = (masa * masa_objeto) / (distancia * distancia) * factor_gravedad
	return direccion * fuerza
