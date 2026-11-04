using UnityEngine;
using UnityEngine.InputSystem;

namespace CercleVR
{
    // Editor/desktop fallback only. A real headset uses the official XR Origin prefab.
    public sealed class DesktopCamera : MonoBehaviour
    {
        private float yaw, pitch;
        private void Start() { yaw = transform.eulerAngles.y; }
        private void Update()
        {
            if (Mouse.current == null || Keyboard.current == null) return;
            var mouse = Mouse.current; var keys = Keyboard.current;
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.12f; pitch = Mathf.Clamp(pitch - delta.y * 0.12f, -70f, 70f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            }
            float x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.qKey.isPressed ? 1 : 0);
            float z = (keys.wKey.isPressed || keys.zKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            Vector3 move = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);
            transform.position += Vector3.ClampMagnitude(move, 1) * Time.deltaTime * 2;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                RaycastHit hit;
                Ray ray = GetComponent<Camera>().ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out hit, 30f))
                {
                    var action = hit.collider.GetComponent<WorldAction>();
                    if (action != null) action.Invoke();
                }
            }
        }
    }
}
