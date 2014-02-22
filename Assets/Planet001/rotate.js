var RotateSpeedAlongY = -10.0;
var RotateSpeedAlongZ = 0.0;
var RotateSpeedAlongX = 0.0;

function Update() {
    // Slowly rotate the object around its axis at 1 degree/second * variable.
    transform.Rotate(Vector3.up * Time.deltaTime * RotateSpeedAlongY);
    transform.Rotate(Vector3.forward * Time.deltaTime * RotateSpeedAlongZ);
    transform.Rotate(Vector3.right * Time.deltaTime * RotateSpeedAlongX);
}