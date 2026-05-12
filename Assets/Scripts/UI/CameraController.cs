using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float distance = 2.2f;
    public float orbitSpeed = 0.15f;
    public float minPitch = 20f;
    public float maxPitch = 90f;

    private float _yaw = 0f;
    private float _pitch = 70f;

    private void LateUpdate()
    {
        if (target == null || Mouse.current == null)
            return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            _yaw += mouseDelta.x * orbitSpeed;
            _pitch -= mouseDelta.y * orbitSpeed;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = target.position + rotation * new Vector3(0f, 0f, -distance);
        transform.LookAt(target);
    }
}