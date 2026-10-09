using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SecretOfHegra
{
    /// <summary>Short controller vibrations (no-ops where the device has no haptics, e.g. the simulator).</summary>
    public static class Haptics
    {
        public static void Pulse(Handedness hand, float amplitude, float seconds) =>
            Send(hand == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand, amplitude, seconds);

        public static void Both(float amplitude, float seconds)
        {
            Send(XRNode.LeftHand, amplitude, seconds);
            Send(XRNode.RightHand, amplitude, seconds);
        }

        static void Send(XRNode node, float amplitude, float seconds)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), seconds);
        }
    }

    /// <summary>
    /// Gives every hand interaction a feel: a soft tick when the hand or ray first touches something usable, and a
    /// firmer pulse when it is grabbed or selected. Sits on the player rig and listens to its hand interactors.
    /// </summary>
    public class InteractionHaptics : MonoBehaviour
    {
        public float hoverAmplitude = 0.12f, hoverSeconds = 0.03f;
        public float selectAmplitude = 0.35f, selectSeconds = 0.07f;

        void Start()
        {
            foreach (var interactor in GetComponentsInChildren<XRBaseInteractor>(true))
            {
                var hand = interactor.handedness;
                if (hand != InteractorHandedness.Left && hand != InteractorHandedness.Right) continue;   // sockets, belt
                var h = hand == InteractorHandedness.Left ? Handedness.Left : Handedness.Right;
                interactor.hoverEntered.AddListener(args =>
                {
                    if (args.interactableObject is XRBaseInteractable) Haptics.Pulse(h, hoverAmplitude, hoverSeconds);
                });
                interactor.selectEntered.AddListener(_ => Haptics.Pulse(h, selectAmplitude, selectSeconds));
            }
        }
    }
}
