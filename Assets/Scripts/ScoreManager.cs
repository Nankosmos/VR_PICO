using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score")]
    public int score = 0;

    [Header("Combo")]
    public int combo = 0;
    public int maxCombo = 0;

    [Header("Miss")]
    public int missCount = 0;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI missText;
    public Transform uiFollowCamera;
    public float uiFollowDistance = 1.25f;
    public float uiTopOffset = 0.42f;
    public float uiSideOffset = 0.55f;

    [Header("Hit Feedback")]
    public HitFeedbackText hitFeedbackText;

    [Header("Result Panel")]
    public ResultPanel resultPanel;

    [Header("End Button")]
    public GameObject endGameButton;

    [Header("Runtime")]
    public RhythmPlayer rhythmPlayer;

    private bool gameEnded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (rhythmPlayer == null)
        {
            rhythmPlayer = FindFirstObjectByType<RhythmPlayer>();
        }
    }

    void Start()
    {
        ValidateReferences();
        HideScoreUI();

        if (endGameButton != null)
        {
            endGameButton.SetActive(false);
        }

        UpdateUI();
    }

    public void AddPerfect()
    {
        AddPerfect(null);
    }

    public void AddPerfect(Vector3? popupPosition)
    {
        if (gameEnded) return;

        score += 100;
        combo++;
        UpdateMaxCombo();
        ShowHitFeedback("Perfect +100", popupPosition);
        UpdateUI();
    }

    public void AddGood()
    {
        AddGood(null);
    }

    public void AddGood(Vector3? popupPosition)
    {
        if (gameEnded) return;

        score += 60;
        combo++;
        UpdateMaxCombo();
        ShowHitFeedback("Good +60", popupPosition);
        UpdateUI();
    }

    public void AddBad()
    {
        AddBad(null);
    }

    public void AddBad(Vector3? popupPosition)
    {
        if (gameEnded) return;

        score += 20;
        combo = 0;
        ShowHitFeedback("Bad +20", popupPosition);
        UpdateUI();
    }

    public void AddMiss()
    {
        AddMiss(null);
    }

    public void AddMiss(Vector3? popupPosition)
    {
        if (gameEnded) return;

        missCount++;
        combo = 0;
        ShowHitFeedback("Miss", popupPosition);
        UpdateUI();
    }

    public void AddHoldScore(int value)
    {
        if (gameEnded) return;

        score += value;
        UpdateUI();
    }

    public void ShowScoreUI()
    {
        score = 0;
        combo = 0;
        maxCombo = 0;
        missCount = 0;
        gameEnded = false;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowGameplayUI();
        }
        else
        {
            if (scoreText != null) scoreText.gameObject.SetActive(true);
            if (comboText != null) comboText.gameObject.SetActive(true);
            if (missText != null) missText.gameObject.SetActive(false);
            ConfigureScoreUIFollow();
        }

        if (endGameButton != null)
        {
            endGameButton.SetActive(true);
        }

        if (resultPanel != null)
        {
            resultPanel.Hide();
        }

        UpdateUI();
    }

    public void HideScoreUI()
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideGameplayUI();
        }
        else
        {
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            if (comboText != null) comboText.gameObject.SetActive(false);
            if (missText != null) missText.gameObject.SetActive(false);
        }
    }

    public void EndGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        ClearActiveNotes();

        if (endGameButton != null)
        {
            endGameButton.SetActive(false);
        }

        HideScoreUI();

        if (resultPanel != null)
        {
            resultPanel.ShowResult(score, missCount, maxCombo);
        }
        else
        {
            Debug.LogWarning("ScoreManager: result panel is not assigned.");
        }
    }

    public void ForceEndGame()
    {
        if (gameEnded) return;

        if (rhythmPlayer == null)
        {
            rhythmPlayer = FindFirstObjectByType<RhythmPlayer>();
        }

        if (rhythmPlayer != null)
        {
            rhythmPlayer.StopTrack(false);
        }

        EndGame();
    }

    void ShowHitFeedback(string message, Vector3? popupPosition)
    {
        if (hitFeedbackText == null) return;

        if (popupPosition.HasValue)
        {
            hitFeedbackText.ShowFeedback(message, popupPosition.Value);
        }
        else
        {
            hitFeedbackText.ShowFeedback(message);
        }
    }

    void UpdateMaxCombo()
    {
        if (combo > maxCombo)
        {
            maxCombo = combo;
        }
    }

    void UpdateUI()
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(score, combo, missCount);
        }

        if (scoreText != null) scoreText.text = "Score: " + score;
        if (comboText != null) comboText.text = "Combo: " + combo;
        if (missText != null) missText.gameObject.SetActive(false);
    }

    void ConfigureScoreUIFollow()
    {
        Transform cameraTransform = GetUIFollowCamera();
        ConfigureFollow(scoreText, new Vector3(-uiSideOffset, uiTopOffset, uiFollowDistance), cameraTransform);
        ConfigureFollow(comboText, new Vector3(0f, uiTopOffset, uiFollowDistance), cameraTransform);
    }

    Transform GetUIFollowCamera()
    {
        if (uiFollowCamera != null) return uiFollowCamera;

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    void ConfigureFollow(TextMeshProUGUI text, Vector3 offset, Transform cameraTransform)
    {
        if (text == null) return;

        UIFollowCamera follow = text.GetComponent<UIFollowCamera>();
        if (follow == null)
        {
            follow = text.gameObject.AddComponent<UIFollowCamera>();
        }

        follow.targetCamera = cameraTransform;
        follow.cameraLocalOffset = offset;
    }

    void ClearActiveNotes()
    {
        NoteOrb[] orbs = FindObjectsByType<NoteOrb>(FindObjectsSortMode.None);
        foreach (NoteOrb orb in orbs)
        {
            orb.ClearWithoutScore();
        }

        HoldNote[] holdNotes = FindObjectsByType<HoldNote>(FindObjectsSortMode.None);
        foreach (HoldNote holdNote in holdNotes)
        {
            holdNote.ClearWithoutScore();
        }
    }

    void ValidateReferences()
    {
        if (resultPanel == null) Debug.LogWarning("ScoreManager: result panel is not assigned.");
        if (hitFeedbackText == null) Debug.LogWarning("ScoreManager: hit feedback text is not assigned.");
        if (rhythmPlayer == null) Debug.LogWarning("ScoreManager: rhythm player is not assigned and was not found.");
    }
}
