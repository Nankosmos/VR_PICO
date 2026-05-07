using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

    [Header("Pause Menu")]
    public GameObject pauseMenuRoot;

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

    void Awake()
    {
        ConfigureSingleton();
        if (Instance != this) return;

        ResolveRuntimeReferences();
        LoadMasterVolume();
        ApplyStartMenuState();
    }

    void Update()
    {
        UpdatePauseInput();
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
        SetPauseMenuActive(true);
    }

    public void ResumeGame()
    {
        if (state != GameMenuState.Paused) return;

        SetPauseMenuActive(false);
        SetRayInteractionActive(false);
        SetActiveNotesVisible(true);
        SetGameplayUIActive(true);
        rhythmPlayer?.ResumeTrack();

        state = GameMenuState.Playing;
    }

    public void EndGame()
    {
        SetPauseMenuActive(false);
        hiddenPauseNotes = new GameObject[0];

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
        SetRayInteractionActive(true);
    }

    void ApplyGameplayState()
    {
        SetStartMenuActive(false);
        SetStartMenuContentActive(false);
        SetPauseMenuActive(false);
        SetStartSettingsActive(false);
        SetRayInteractionActive(false);
    }

    void ApplyResultState()
    {
        SetStartMenuActive(false);
        SetStartMenuContentActive(false);
        SetPauseMenuActive(false);
        SetStartSettingsActive(false);
        SetRayInteractionActive(true);
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
                ScoreManager.Instance.ShowScoreUI();
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
    }
}
