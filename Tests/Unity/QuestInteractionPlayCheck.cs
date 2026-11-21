// Run from a fresh CercleQuest Play session in an editor without a connected XR runtime.
using System;
using System.Linq;
using System.Threading.Tasks;
using CercleVR;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public static class QuestInteractionPlayCheck
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static async Task Until(Func<bool> condition, string message)
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Task.Delay(15);
        Check(condition(), message);
    }

    private static void SetButton(InputDevice hand, string button, bool pressed)
    {
        using (StateEvent.From(hand, out var state))
        {
            hand.GetChildControl<ButtonControl>(button).WriteValueIntoEvent(pressed ? 1f : 0f, state);
            if (button == "triggerPressed" || button == "gripPressed")
                hand.GetChildControl<AxisControl>(button.Replace("Pressed", "")).WriteValueIntoEvent(pressed ? 1f : 0f, state);
            InputSystem.QueueEvent(state);
        }
    }

    private static async Task Press(InputDevice hand, string button)
    {
        SetButton(hand, button, true);
        await Until(() => hand.GetChildControl<ButtonControl>(button).isPressed, "Button did not press: " + button);
        await Task.Delay(60);
        SetButton(hand, button, false);
        await Until(() => !hand.GetChildControl<ButtonControl>(button).isPressed, "Button did not release: " + button);
    }

    private static void CheckRay(Vector3 start, Vector3 destination, WorldAction target)
    {
        Check(Physics.Raycast(start, (destination - start).normalized, out var hit, 10f) &&
            hit.collider.GetComponent<WorldAction>() == target, target.name + " obscured by " + hit.collider?.name);
    }

    public static async Task<object> Main()
    {
        Check(Application.isPlaying && InputSystem.GetDevice<XRHMD>() == null, "Use a fresh simulated Quest scene.");
        var eye = Camera.main.transform;
        var origin = eye.GetComponentInParent<XROrigin>();
        var game = UnityEngine.Object.FindAnyObjectByType<CercleGame>();
        var mount = UnityEngine.Object.FindAnyObjectByType<ClipboardMount>(FindObjectsInactive.Include);
        var modality = origin.GetComponentInChildren<XRInputModalityManager>(true);
        var drivers = origin.GetComponentsInChildren<TrackedPoseDriver>(true);
        var enabledDrivers = drivers.Where(d => d.enabled).ToArray();
        var hands = origin.GetComponentsInChildren<NearFarInteractor>(true);
        var right = hands.Single(h => h.transform.parent.name == "Right Controller");
        var left = hands.Single(h => h.transform.parent.name == "Left Controller");
        var leftCaster = (CurveInteractionCaster)left.farInteractionCaster;
        bool leftStabilized = leftCaster.enableStabilization;
        var rightRoot = right.transform.parent.gameObject;
        var leftRoot = mount.anchorOverride.parent.gameObject;
        bool rightActive = rightRoot.activeSelf, leftActive = leftRoot.activeSelf, modalityEnabled = modality.enabled;
        var caster = (CurveInteractionCaster)right.farInteractionCaster;
        var oldAim = caster.castOrigin;
        bool stabilized = caster.enableStabilization;
        var aim = new GameObject("Simulated controller aim");
        var hmd = InputSystem.AddDevice<XRHMD>();
        var leftHand = InputSystem.AddDevice("QuestTouchPlusController");
        var rightHand = InputSystem.AddDevice("QuestTouchPlusController");
        try
        {
            modality.enabled = false;
            foreach (var driver in enabledDrivers) driver.enabled = false;
            InputSystem.SetDeviceUsage(leftHand, UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(rightHand, UnityEngine.InputSystem.CommonUsages.RightHand);
            using (StateEvent.From(hmd, out var state))
            {
                hmd.isTracked.WriteValueIntoEvent(1f, state);
                hmd.trackingState.WriteValueIntoEvent(3, state);
                InputSystem.QueueEvent(state);
            }
            await Until(() => XrTrackingGuard.CanInteract, "Headset tracking did not enable interactions.");
            leftRoot.SetActive(true);
            rightRoot.SetActive(true);
            eye.SetPositionAndRotation(origin.transform.position + Vector3.up * 1.65f, Quaternion.identity);
            leftRoot.transform.SetPositionAndRotation(eye.position + new Vector3(-0.10f, -0.02f, 0.03f), Quaternion.identity);
            mount.AttachNow();
            Check(!mount.Reading && mount.transform.lossyScale.x < 0.5f &&
                Vector3.Distance(eye.position, mount.transform.position) >= 0.64f, "Compact board crowds the face.");
            leftRoot.transform.position = eye.position + new Vector3(-0.5f, -0.4f, 0.3f);
            mount.AttachNow();
            var start = origin.transform.position + new Vector3(0.15f, 1.45f, 0.05f);
            rightRoot.transform.position = start;
            caster.enableStabilization = false;
            caster.castOrigin = aim.transform;
            var actions = game.GetComponentsInChildren<WorldAction>();
            var characters = actions.Where(a => a.name.StartsWith("PNJ ")).ToArray();
            var tokens = actions.Where(a => a.name.StartsWith("Jeton ")).ToArray();
            var names = actions.Where(a => a.name == "Support nom").ToArray();
            Check(characters.Length == 7 && tokens.Length == characters.Length && names.Length == characters.Length, "Opening level is not ready.");
            foreach (var plate in names) plate.GetComponentInParent<FaceCamera>().FaceNow();
            Physics.SyncTransforms();
            foreach (var person in characters) CheckRay(start, person.transform.position + Vector3.up * 1.45f, person);
            foreach (var target in tokens.Concat(names)) CheckRay(start, target.transform.position, target);
            var first = characters[0];
            aim.transform.SetPositionAndRotation(start, Quaternion.LookRotation(first.transform.position + Vector3.up * 1.45f - start));
            await Until(() => right.interactablesHovered.Contains(first), "Interactor did not target the first NPC.");
            await Press(rightHand, "triggerPressed");
            var dialogue = game.transform.Find("Dialogue du personnage selectionne");
            Check(dialogue.gameObject.activeSelf && dialogue.Find("Texte fixe du dialogue/Nom personnage") != null, "Right trigger did not open the dialogue.");

            var second = characters[1];
            var chest = second.transform.position + Vector3.up * 1.45f;
            var leftStart = eye.position + new Vector3(-0.24f, -0.21f, 0.46f);
            leftCaster.enableStabilization = false;
            leftRoot.transform.SetPositionAndRotation(leftStart, Quaternion.LookRotation(chest - leftStart));
            var leftRay = leftCaster.castOrigin;
            leftRoot.transform.rotation = Quaternion.FromToRotation(leftRay.forward, chest - leftRay.position) * leftRoot.transform.rotation;
            mount.AttachNow();
            Physics.SyncTransforms();
            CheckRay(leftRay.position, chest, second);
            await Until(() => left.interactablesHovered.Contains(second), "The hand carrying the board cannot aim at an NPC.");
            await Press(leftHand, "triggerPressed");
            Check(dialogue.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.Contains("Basile")), "Left trigger did not select the second NPC.");
            leftRoot.transform.SetPositionAndRotation(eye.position + new Vector3(-0.5f, -0.4f, 0.3f), Quaternion.identity);
            mount.AttachNow();

            var cover = mount.transform.Find("Planche clipboard").GetComponent<WorldAction>();
            aim.transform.rotation = Quaternion.LookRotation(cover.transform.position - start);
            await Until(() => right.interactablesHovered.Contains(cover), "Compact board cover was not targetable.");
            await Press(rightHand, "triggerPressed");
            Check(mount.Reading && Vector3.Distance(eye.position, mount.transform.position) >= 1.34f, "Cover did not open at reading distance.");
            var help = mount.transform.Find("Action Aide/Aide").GetComponent<WorldAction>();
            aim.transform.rotation = Quaternion.LookRotation(help.transform.position - start);
            await Until(() => right.interactablesHovered.Contains(help), "Interactor did not target Help.");
            await Press(rightHand, "gripPressed");
            Check(mount.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text == "REGLES ET COMMANDES"), "Grip did not open Help.");
            var next = mount.transform.Find("Action Page +/Page +").GetComponent<WorldAction>();
            aim.transform.rotation = Quaternion.LookRotation(next.transform.position - start);
            await Until(() => right.interactablesHovered.Contains(next), "Page button was not targetable.");
            await Press(rightHand, "triggerPressed");
            Check(mount.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.StartsWith("PAGE 2 /")), "Trigger did not change the page.");
            var pinned = mount.transform.position;
            leftRoot.SetActive(false);
            eye.SetPositionAndRotation(eye.position + Vector3.right * 0.75f, Quaternion.Euler(0, 90, 0));
            mount.AttachNow();
            Check(mount.isActiveAndEnabled && Vector3.Distance(pinned, mount.transform.position) < 0.001f, "Reading moved on its own.");
            await Press(leftHand, "primaryButton");
            var recalled = eye.InverseTransformPoint(mount.transform.position);
            Check(Mathf.Abs(recalled.x) < 0.01f && Mathf.Abs(recalled.z - 1.35f) < 0.01f, "X did not bring the board back after a click and a turn.");
            var reduce = mount.transform.Find("Action Lire/Lire").GetComponent<WorldAction>();
            aim.transform.SetPositionAndRotation(eye.position, Quaternion.LookRotation(reduce.transform.position - eye.position));
            await Until(() => right.interactablesHovered.Contains(reduce), "Reduce was not targetable after recall.");
            await Press(rightHand, "triggerPressed");
            Check(!mount.Reading && mount.transform.lossyScale.x < 0.5f, "Reduce did not restore the compact board.");
            await Press(leftHand, "primaryButton");
            Check(mount.Reading, "Recall stopped when the hand object was inactive.");
            return new { characters = characters.Length, tokens = tokens.Length, nameplates = names.Length,
                rightTrigger = true, leftTriggerWithBoard = true, grip = true, leftXAfterPageTurn = true, readingDistanceMetres = recalled.z };
        }
        finally
        {
            caster.castOrigin = oldAim;
            caster.enableStabilization = stabilized;
            leftCaster.enableStabilization = leftStabilized;
            UnityEngine.Object.Destroy(aim);
            InputSystem.RemoveDevice(rightHand);
            InputSystem.RemoveDevice(leftHand);
            InputSystem.RemoveDevice(hmd);
            rightRoot.SetActive(rightActive);
            leftRoot.SetActive(leftActive);
            foreach (var driver in enabledDrivers) driver.enabled = true;
            modality.enabled = modalityEnabled;
        }
    }
}
