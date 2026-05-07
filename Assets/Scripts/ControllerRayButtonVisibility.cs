using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ControllerRayButtonVisibility : MonoBehaviour
{
    public bool showOnTrigger = true;
    public bool showOnGrip = true;
    public bool showOnPrimaryButton = true;
    public bool showOnSecondaryButton = true;

#if ENABLE_INPUT_SYSTEM
    public Key editorPreviewKey = Key.R;
#else
    public KeyCode editorPreviewKey = KeyCode.R;
#endif
    public float refreshInterval = 1f;

    private readonly List<LineRenderer> rayLines = new List<LineRenderer>();
    private readonly List<Renderer> rayRenderers = new List<Renderer>();
    private readonly List<InputDevice> leftDevices = new List<InputDevice>();
    private readonly List<InputDevice> rightDevices = new List<InputDevice>();

    private float refreshTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeController()
    {
        GameObject controller = new GameObject("Controller Ray Button Visibility");
        DontDestroyOnLoad(controller);
        controller.AddComponent<ControllerRayButtonVisibility>();
    }

    void Start()
    {
        RefreshRayVisuals();
        SetRayVisible(false);
    }

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshRayVisuals();
        }

        SetRayVisible(ShouldShowRay());
    }

    bool ShouldShowRay()
    {
        if (IsEditorPreviewPressed())
        {
            return true;
        }

        return IsAnyRayButtonPressed(XRNode.LeftHand, leftDevices)
            || IsAnyRayButtonPressed(XRNode.RightHand, rightDevices);
    }

    bool IsEditorPreviewPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[editorPreviewKey].isPressed;
#else
        return Input.GetKey(editorPreviewKey);
#endif
    }

    bool IsAnyRayButtonPressed(XRNode node, List<InputDevice> devices)
    {
        devices.Clear();
        InputDevices.GetDevicesAtXRNode(node, devices);

        foreach (InputDevice device in devices)
        {
            if (!device.isValid) continue;

            if (showOnTrigger && IsPressed(device, CommonUsages.triggerButton)) return true;
            if (showOnGrip && IsPressed(device, CommonUsages.gripButton)) return true;
            if (showOnPrimaryButton && IsPressed(device, CommonUsages.primaryButton)) return true;
            if (showOnSecondaryButton && IsPressed(device, CommonUsages.secondaryButton)) return true;
        }

        return false;
    }

    bool IsPressed(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }

    void RefreshRayVisuals()
    {
        refreshTimer = refreshInterval;
        rayLines.Clear();
        rayRenderers.Clear();

        LineRenderer[] lines = FindObjectsByType<LineRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (LineRenderer line in lines)
        {
            if (line == null) continue;
            if (!IsControllerRayVisual(line.transform)) continue;

            rayLines.Add(line);
        }

        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (!IsControllerRayVisual(renderer.transform)) continue;

            rayRenderers.Add(renderer);
        }
    }

    bool IsControllerRayVisual(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            string name = current.name;

            if (name.Contains("Near-Far Interactor")
                || name.Contains("Teleport Interactor")
                || name.Contains("Ray Interactor"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void SetRayVisible(bool visible)
    {
        foreach (LineRenderer line in rayLines)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }

        foreach (Renderer renderer in rayRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }
}
