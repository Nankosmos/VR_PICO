using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PicoControllerTrackingProxy : MonoBehaviour
{
    public XRNode controllerNode = XRNode.LeftHand;
    public float colliderRadius = 0.2f;
    private readonly List<InputDevice> devices = new List<InputDevice>();
    private InputDevice controllerDevice;
    private SphereCollider triggerCollider;

    public static PicoControllerTrackingProxy Create(XRNode node, string handTag, float radius, Transform parent)
    {
        GameObject proxyObject = new GameObject(node == XRNode.LeftHand
            ? "PICO Left Controller Tracking"
            : "PICO Right Controller Tracking");

        if (parent != null)
        {
            proxyObject.transform.SetParent(parent, false);
        }

        proxyObject.tag = handTag;

        PicoControllerTrackingProxy proxy = proxyObject.AddComponent<PicoControllerTrackingProxy>();
        proxy.controllerNode = node;
        proxy.colliderRadius = radius;

        SphereCollider sphere = proxyObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        Rigidbody rb = proxyObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        return proxy;
    }

    void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
        }

        triggerCollider.radius = colliderRadius;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        RefreshDevice();
    }

    void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        RefreshDevice();
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
    }

    void Update()
    {
        if (!controllerDevice.isValid)
        {
            RefreshDevice();
        }

        bool hasPosition = controllerDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        bool hasRotation = controllerDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

        if (hasPosition)
        {
            transform.localPosition = position;
        }

        if (hasRotation)
        {
            transform.localRotation = rotation;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = hasPosition;
        }
    }

    void OnDeviceChanged(InputDevice device)
    {
        RefreshDevice();
    }

    void RefreshDevice()
    {
        devices.Clear();
        InputDevices.GetDevicesAtXRNode(controllerNode, devices);
        controllerDevice = devices.Count > 0 ? devices[0] : default;
    }
}
