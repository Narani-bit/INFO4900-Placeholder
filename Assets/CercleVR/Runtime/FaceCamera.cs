using UnityEngine;

namespace CercleVR
{
    public sealed class FaceCamera : MonoBehaviour
    {
        public bool keepUpright = true;
        private void LateUpdate() { FaceNow(); }
        public void FaceNow()
        {
            if (Camera.main == null) return;
            Vector3 direction = transform.position - Camera.main.transform.position;
            if (keepUpright) direction.y = 0;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
