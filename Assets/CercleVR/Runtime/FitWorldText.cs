using UnityEngine;

namespace CercleVR
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class FitWorldText : MonoBehaviour
    {
        public float width = 1f;
        public float height = 0.3f;
        private Renderer textRenderer;
        private void Awake() { textRenderer = GetComponent<Renderer>(); }
        private void LateUpdate()
        {
            Vector3 size = textRenderer.localBounds.size;
            if (size.x < 0.001f || size.y < 0.001f) return;
            float scale = Mathf.Min(width / size.x, height / size.y);
            transform.localScale = Vector3.one * scale;
        }
    }
}
