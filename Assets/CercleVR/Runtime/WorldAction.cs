using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CercleVR
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldAction : XRSimpleInteractable
    {
        private Action callback;
        private int lastFrame = -1;
        public void Bind(Action action) { callback = action; }
        protected override void Awake()
        {
            // Register only this target. A chair or a visual child is a separate object.
            foreach (var shape in GetComponents<Collider>())
                if (shape.enabled) colliders.Add(shape);
            base.Awake();
        }
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            Invoke();
        }
        public void Invoke()
        {
            if (!XrTrackingGuard.CanInteract) return;
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (callback != null) callback();
        }
    }
}
