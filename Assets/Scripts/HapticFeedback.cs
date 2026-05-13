using UnityEngine;
using UnityEngine.XR;

public static class HapticFeedback
{
    private const float DefaultAmplitude = 0.18f;
    private const float DefaultDuration = 0.06f;

    public static void PlayLightHit(HandType hand)
    {
        XRNode node = hand == HandType.Left ? XRNode.LeftHand : XRNode.RightHand;
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities)
            && capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0u, DefaultAmplitude, DefaultDuration);
        }
    }
}
