extends Resource
class_name CameraState

var angles : Vector3;
var camera_position : Vector3;


func SetFromTransform(eulerAngles : Vector3, position : Vector3) -> void:
	angles = eulerAngles;
	camera_position = position;


func Translate(translation : Vector3) -> void:
	var yaw_rotation = Quaternion.from_euler(Vector3(0, angles.y, 0))

	var horizontal_input = Vector3(translation.x, 0, translation.z)

	var rotated_horizontal_translation = yaw_rotation * horizontal_input

	camera_position += rotated_horizontal_translation + Vector3(0, translation.y, 0)

func LerpTowards(target : CameraState, positionLerpPct : float, rotationLerpPct : float):
	angles = angles.lerp(target.angles, rotationLerpPct);

	camera_position = camera_position.lerp(target.camera_position, positionLerpPct);

func GetEulerAngles() -> Vector3:
	return angles;

func GetCameraPosition() -> Vector3:
	return camera_position;
