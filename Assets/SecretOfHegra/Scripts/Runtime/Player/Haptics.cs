using UnityEngine;
using UnityEngine.XR;

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
}
