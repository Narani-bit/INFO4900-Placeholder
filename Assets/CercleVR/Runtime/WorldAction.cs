using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CercleVR
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldAction : MonoBehaviour
    {
        private Action callback;
        private XRSimpleInteractable interactable;
        private int lastFrame = -1;
        public void Bind(Action action) { callback = action; }
        private void Awake()
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(Selected);
        }
        private void Selected(SelectEnterEventArgs args) { Invoke(); }
        public void Invoke()
        {
            if (!XrTrackingGuard.CanInteract) return;
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (callback != null) callback();
        }
        private void OnDestroy()
        {
            if (interactable != null) interactable.selectEntered.RemoveListener(Selected);
        }
    }
}
