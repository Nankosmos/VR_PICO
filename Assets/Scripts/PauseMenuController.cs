using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class PauseMenuController : MonoBehaviour
{
    private const string MasterVolumePrefKey = "MasterVolume";

    public static PauseMenuController Instance;

    public enum GameMenuState
    {
        StartMenu,
        Playing,
        Paused,
        Result
    }

    [Header("Start Menu")]
    public GameObject startMenuRoot;
    public GameObject startMenuContentRoot;
    public GameObject startSettingsRoot;
    public Slider masterVolumeSlider;

    [Header("Pause Menu")]
    public GameObject pauseMenuRoot;
    public GameObject menuDimOverlay;
    public bool useLegacyMenuDimOverlay = false;

    [Header("Menu Scene Dimming")]
    public Volume menuDimVolume;
    public bool createMenuDimVolumeIfMissing = true;
    public bool configureUICameraForDimming = true;
    public bool syncUICameraToBaseCamera = true;
    public string uiCameraName = "UICamera";
    public string uiNoPostLayerName = "UI_NoPost";
    public string controllerCameraName = "ControllerCamera";
    public string controllerNoPostLayerName = "Controller_NoPost";
    public float menuDimPostExposure = -2.2f;
    public float menuDimSaturation = -20f;
    public float menuDimVignetteIntensity = 0.28f;
    public float menuDimFadeSpeed = 6f;

    [Header("Ray Interaction Objects")]
    public GameObject[] rayInteractionObjects;

    [Header("Pause Input")]
    public bool useXRDeviceSecondaryButtons = true;
    public bool useKeyboardPauseFallback = true;
    public Key keyboardRightBKey = Key.B;
    public Key keyboardLeftYKey = Key.Y;

    [Header("Runtime")]
    public RhythmPlayer rhythmPlayer;

    private GameMenuState state = GameMenuState.StartMenu;
    private int lastToggleFrame = -1;
    private bool wasRightBPressed;
    private bool wasLeftYPressed;
    private GameObject[] hiddenPauseNotes = new GameObject[0];
    private float masterVolume = 1f;
    private float targetMenuDimWeight;
    private Camera baseCameraForUI;
    private Camera uiCameraForMenu;
    private Camera controllerCameraForMenu;
    private int uiNoPostLayer = -1;
    private int controllerNoPostLayer = -1;

    void Awake()
    {
        ConfigureSingleton();
        if (Instance != this) return;

        ResolveRuntimeReferences();
        LoadMasterVolume();
        ConfigureMenuDimVolume();
        ConfigureUICameraForMenuDimming();
        ApplyStartMenuState();
    }

    void Update()
    {
        UpdatePauseInput();
        UpdateMenuDimVolume();
    }

    void LateUpdate()
    {
        SyncOverlayCameraTransforms();
    }

    public void EnterStartMenu()
    {
        state = GameMenuState.StartMenu;
        ApplyStartMenuState();
    }

    public void EnterGameplay()
    {
        state = GameMenuState.Playing;
        ApplyGameplayState();
    }

    public void EnterResult()
    {
        state = GameMenuState.Result;
        ApplyResultState();
    }

    public void TogglePauseMenu()
    {
        if (state == GameMenuState.Playing)
        {
            PauseGame();
        }
        else if (state == GameMenuState.Paused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (state != GameMenuState.Playing) return;

        ResolveRuntimeReferences();
        rhythmPlayer?.PauseTrack();

        state = GameMenuState.Paused;
        SetGameplayUIActive(false);
        SetActiveNotesVisible(false);
        SetRayInteractionActive(true);
        SetMenuDimOverlayActive(true);
        SetMenuDimActive(true, true);
        SetPauseMenuActive(true);
    }

    public void ResumeGame()
    {
        if (state != GameMenuState.Paused) return;

        SetPauseMenuActive(false);
        SetRayInteractionActive(false);
        SetMenuDimOverlayActive(false);
        SetMenuDimActive(false, true);
        SetActiveNotesVisible(true);
        SetGameplayUIActive(true);
        rhythmPlayer?.ResumeTrack();

        state = GameMenuState.Playing;
    }

    public void EndGame()
    {
        SetPauseMenuActive(false);
        hiddenPauseNotes = new GameObject[0];
        SetMenuDimActive(true, true);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ForceEndGame();
        }
        else
        {
            rhythmPlayer?.StopTrack(false);
            EnterResult();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenSettings()
    {
        if (state != GameMenuState.StartMenu) return;

        SetStartMenuContentActive(false);
        SetStartSettingsActive(true);
        SetRayInteractionActive(true);
    }

    public void CloseSettings()
    {
        SetStartSettingsActive(false);
        if (state == GameMenuState.StartMenu)
        {
            SetStartMenuContentActive(true);
        }

        SetRayInteractionActive(true);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat(MasterVolumePrefKey, masterVolume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void EnsureAudibleVolume(float fallbackVolume = 1f)
    {
        if (AudioListener.volume > 0.001f) return;

        SetMasterVolume(fallbackVolume);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        System.Type editorApplicationType = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
        editorApplicationType?.GetProperty("isPlaying")?.SetValue(null, false);
#else
        Application.Quit();
#endif
    }

    void ConfigureSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void ResolveRuntimeReferences()
    {
        if (rhythmPlayer == null)
        {
            rhythmPlayer = FindFirstObjectByType<RhythmPlayer>();
        }
    }

    void ApplyStartMenuState()
    {
        SetStartMenuActive(true);
        SetStartMenuContentActive(true);
        SetPauseMenuActive(false);
        SetStartSettingsActive(false);
        SetMenuDimOverlayActive(true);
        SetMenuDimActive(true, false);
        SetRayInteractionActive(true);
    }

    void ApplyGameplayState()
    {
        SetStartMenuActive(false);
        SetStartMenuContentActive(false);
        SetPauseMenuActive(false);
        SetStartSettingsActive(false);
        SetMenuDimOverlayActive(false);
        SetMenuDimActive(false, true);
        SetRayInteractionActive(false);
    }

    void ApplyResultState()
    {
        SetStartMenuActive(false);
        SetStartMenuContentActive(false);
        SetPauseMenuActive(false);
        SetStartSettingsActive(false);
        SetMenuDimOverlayActive(true);
        SetMenuDimActive(true, true);
        SetRayInteractionActive(true);
    }

    void ConfigureMenuDimVolume()
    {
        if (menuDimVolume == null)
        {
            menuDimVolume = FindMenuDimVolume();
        }

        if (menuDimVolume == null && createMenuDimVolumeIfMissing)
        {
            GameObject volumeObject = new GameObject("MenuDimVolume");
            menuDimVolume = volumeObject.AddComponent<Volume>();
            menuDimVolume.isGlobal = true;
            menuDimVolume.priority = 100f;
            menuDimVolume.weight = 0f;
        }

        if (menuDimVolume == null) return;

        menuDimVolume.isGlobal = true;
        menuDimVolume.priority = Mathf.Max(menuDimVolume.priority, 100f);
        menuDimVolume.weight = 0f;

        VolumeProfile profile = menuDimVolume.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Menu Dim Volume Profile";
            menuDimVolume.profile = profile;
        }

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        colorAdjustments.postExposure.Override(menuDimPostExposure);
        colorAdjustments.saturation.Override(menuDimSaturation);

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }

        vignette.intensity.Override(menuDimVignetteIntensity);
        vignette.smoothness.Override(0.45f);
    }

    Volume FindMenuDimVolume()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Volume volume in volumes)
        {
            if (volume != null && volume.name == "MenuDimVolume")
            {
                return volume;
            }
        }

        return null;
    }

    void ConfigureUICameraForMenuDimming()
    {
        if (!configureUICameraForDimming) return;

        uiNoPostLayer = LayerMask.NameToLayer(uiNoPostLayerName);
        controllerNoPostLayer = LayerMask.NameToLayer(controllerNoPostLayerName);
        if (uiNoPostLayer < 0)
        {
            Debug.LogWarning("PauseMenuController: UI no-post layer was not found: " + uiNoPostLayerName);
            return;
        }

        uiCameraForMenu = FindUICamera();
        if (uiCameraForMenu == null) return;

        Camera uiCamera = uiCameraForMenu;
        ConfigureOverlayCamera(uiCamera, uiNoPostLayer);

        controllerCameraForMenu = FindCameraByName(controllerCameraName);
        if (controllerCameraForMenu != null && controllerNoPostLayer >= 0)
        {
            ConfigureOverlayCamera(controllerCameraForMenu, controllerNoPostLayer);
            ApplyControllerLayer();
        }

        baseCameraForUI = FindBaseCamera(uiCamera);
        if (baseCameraForUI == null) return;

        Camera baseCamera = baseCameraForUI;
        baseCamera.cullingMask &= ~(1 << uiNoPostLayer);
        if (controllerNoPostLayer >= 0)
        {
            baseCamera.cullingMask &= ~(1 << controllerNoPostLayer);
        }

        UniversalAdditionalCameraData baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
        baseCameraData.enabled = true;
        baseCameraData.renderType = CameraRenderType.Base;
        baseCameraData.renderPostProcessing = true;

        if (menuDimVolume != null)
        {
            int volumeMask = baseCameraData.volumeLayerMask.value;
            volumeMask |= 1 << menuDimVolume.gameObject.layer;
            baseCameraData.volumeLayerMask = volumeMask;
        }

        if (!baseCameraData.cameraStack.Contains(uiCamera))
        {
            baseCameraData.cameraStack.Add(uiCamera);
        }

        if (controllerCameraForMenu != null && !baseCameraData.cameraStack.Contains(controllerCameraForMenu))
        {
            baseCameraData.cameraStack.Add(controllerCameraForMenu);
        }

        ConfigureCanvasForUICamera(uiCamera);
        SyncOverlayCameraTransforms();
    }

    void ConfigureOverlayCamera(Camera overlayCamera, int layer)
    {
        overlayCamera.enabled = true;
        overlayCamera.cullingMask = 1 << layer;

        UniversalAdditionalCameraData overlayCameraData = overlayCamera.GetUniversalAdditionalCameraData();
        overlayCameraData.enabled = true;
        overlayCameraData.renderType = CameraRenderType.Overlay;
        overlayCameraData.renderPostProcessing = false;

        AudioListener audioListener = overlayCamera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
    }

    Camera FindUICamera()
    {
        Camera namedCamera = FindCameraByName(uiCameraName);
        if (namedCamera != null)
        {
            return namedCamera;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.cullingMask == LayerMask.GetMask("UI"))
            {
                return camera;
            }
        }

        return null;
    }

    Camera FindCameraByName(string cameraName)
    {
        GameObject cameraObject = GameObject.Find(cameraName);
        if (cameraObject != null && cameraObject.TryGetComponent(out Camera namedCamera))
        {
            return namedCamera;
        }

        return null;
    }

    void ConfigureCanvasForUICamera(Camera uiCamera)
    {
        ApplyUILayer(startMenuRoot);
        ApplyUILayer(startMenuContentRoot);
        ApplyUILayer(startSettingsRoot);
        ApplyUILayer(pauseMenuRoot);

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;

            ApplyUILayer(canvas.gameObject);

            if (canvas.renderMode == RenderMode.WorldSpace || canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = uiCamera;
            }
        }
    }

    void ApplyUILayer(GameObject root)
    {
        if (root == null || uiNoPostLayer < 0) return;

        ApplyLayerRecursively(root, uiNoPostLayer);
    }

    void ApplyControllerLayer()
    {
        if (controllerNoPostLayer < 0) return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform == null) continue;

            string objectName = transform.name;
            bool isControllerRoot =
                objectName == "Left Controller" ||
                objectName == "Right Controller" ||
                objectName == "[Building Block] Left Controller" ||
                objectName == "[Building Block] Right Controller";

            string objectTag = transform.gameObject.tag;
            bool isHandTarget = objectTag == "LeftHand" || objectTag == "RightHand";
            if (!isControllerRoot && !isHandTarget) continue;

            ApplyLayerRecursively(transform.gameObject, controllerNoPostLayer);
        }
    }

    void ApplyLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0) return;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
        {
            child.gameObject.layer = layer;
        }
    }

    void SyncOverlayCameraTransforms()
    {
        if (!syncUICameraToBaseCamera) return;
        if (baseCameraForUI == null) return;

        SyncOverlayCameraTransform(uiCameraForMenu);
        SyncOverlayCameraTransform(controllerCameraForMenu);
    }

    void SyncOverlayCameraTransform(Camera overlayCamera)
    {
        if (overlayCamera == null) return;

        Transform baseTransform = baseCameraForUI.transform;
        Transform overlayTransform = overlayCamera.transform;
        overlayTransform.SetPositionAndRotation(baseTransform.position, baseTransform.rotation);
        overlayCamera.fieldOfView = baseCameraForUI.fieldOfView;
        overlayCamera.nearClipPlane = baseCameraForUI.nearClipPlane;
        overlayCamera.farClipPlane = baseCameraForUI.farClipPlane;
    }

    Camera FindBaseCamera(Camera uiCamera)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera != uiCamera)
        {
            return mainCamera;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera != uiCamera && camera.cullingMask != LayerMask.GetMask("UI"))
            {
                return camera;
            }
        }

        return null;
    }

    void SetMenuDimActive(bool active, bool fade)
    {
        targetMenuDimWeight = active ? 1f : 0f;

        if (!fade && menuDimVolume != null)
        {
            menuDimVolume.weight = targetMenuDimWeight;
        }
    }

    void UpdateMenuDimVolume()
    {
        if (menuDimVolume == null) return;

        menuDimVolume.weight = Mathf.MoveTowards(
            menuDimVolume.weight,
            targetMenuDimWeight,
            menuDimFadeSpeed * Time.unscaledDeltaTime
        );
    }

    void UpdatePauseInput()
    {
        if (IsKeyboardPausePressed())
        {
            TogglePauseMenuOncePerFrame();
        }

        if (!useXRDeviceSecondaryButtons) return;

        bool rightBPressed = IsSecondaryButtonPressed(XRNode.RightHand);
        bool leftYPressed = IsSecondaryButtonPressed(XRNode.LeftHand);

        if ((rightBPressed && !wasRightBPressed) || (leftYPressed && !wasLeftYPressed))
        {
            TogglePauseMenuOncePerFrame();
        }

        wasRightBPressed = rightBPressed;
        wasLeftYPressed = leftYPressed;
    }

    void TogglePauseMenuOncePerFrame()
    {
        if (lastToggleFrame == Time.frameCount) return;

        lastToggleFrame = Time.frameCount;
        TogglePauseMenu();
    }

    void SetStartMenuActive(bool active)
    {
        if (startMenuRoot != null)
        {
            startMenuRoot.SetActive(active);
        }
    }

    void SetStartMenuContentActive(bool active)
    {
        if (startMenuContentRoot != null)
        {
            startMenuContentRoot.SetActive(active);
        }
    }

    void SetStartSettingsActive(bool active)
    {
        if (startSettingsRoot != null)
        {
            startSettingsRoot.SetActive(active);
        }
    }

    void SetPauseMenuActive(bool active)
    {
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(active);
        }
    }

    void SetMenuDimOverlayActive(bool active)
    {
        if (!useLegacyMenuDimOverlay)
        {
            active = false;
        }

        if (menuDimOverlay != null)
        {
            menuDimOverlay.SetActive(active);
        }
    }

    void SetRayInteractionActive(bool active)
    {
        if (rayInteractionObjects == null) return;

        foreach (GameObject rayObject in rayInteractionObjects)
        {
            if (rayObject != null)
            {
                rayObject.SetActive(active);
            }
        }
    }

    void SetGameplayUIActive(bool active)
    {
        if (GameUIManager.Instance != null)
        {
            if (active)
            {
                GameUIManager.Instance.ShowGameplayUI();
            }
            else
            {
                GameUIManager.Instance.HideGameplayUI();
            }
        }
        else if (ScoreManager.Instance != null)
        {
            if (active)
            {
                ScoreManager.Instance.ResumeScoreUI();
            }
            else
            {
                ScoreManager.Instance.HideScoreUI();
            }
        }
    }

    void SetActiveNotesVisible(bool visible)
    {
        if (visible)
        {
            RestorePauseHiddenNotes();
            return;
        }

        HideActiveNotesForPause();
    }

    void HideActiveNotesForPause()
    {
        NoteOrb[] noteOrbs = FindObjectsByType<NoteOrb>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        HoldNote[] holdNotes = FindObjectsByType<HoldNote>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        hiddenPauseNotes = new GameObject[noteOrbs.Length + holdNotes.Length];

        int index = 0;
        foreach (NoteOrb noteOrb in noteOrbs)
        {
            hiddenPauseNotes[index] = noteOrb.gameObject;
            noteOrb.gameObject.SetActive(false);
            index++;
        }

        foreach (HoldNote holdNote in holdNotes)
        {
            hiddenPauseNotes[index] = holdNote.gameObject;
            holdNote.gameObject.SetActive(false);
            index++;
        }
    }

    void RestorePauseHiddenNotes()
    {
        foreach (GameObject noteObject in hiddenPauseNotes)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
        }

        hiddenPauseNotes = new GameObject[0];
    }

    bool IsSecondaryButtonPressed(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid
            && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool pressed)
            && pressed;
    }

    bool IsKeyboardPausePressed()
    {
        if (!useKeyboardPauseFallback || Keyboard.current == null) return false;

        return WasKeyPressedThisFrame(keyboardRightBKey)
            || WasKeyPressedThisFrame(keyboardLeftYKey);
    }

    bool WasKeyPressedThisFrame(Key key)
    {
        UnityEngine.InputSystem.Controls.KeyControl keyControl = Keyboard.current[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    void LoadMasterVolume()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f));
        AudioListener.volume = masterVolume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }
    }
}
