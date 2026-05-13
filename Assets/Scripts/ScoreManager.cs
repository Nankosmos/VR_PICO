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
    public GameObject scoreRankPanel;
    public TextMeshProUGUI scoreText;
    public GameObject comboRoot;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI missText;
    public TMP_FontAsset scoreFontAsset;
    public Font scoreFontSource;

    [Header("Rank Images")]
    public GameObject rankBImage;
    public GameObject rankAImage;
    public GameObject rankSImage;
    public GameObject rankSSImage;
    public GameObject rankSSSImage;

    [Header("Grade")]
    [Range(0f, 1f)] public float gradeAPercent = 0.7f;
    [Range(0f, 1f)] public float gradeSPercent = 0.8f;
    [Range(0f, 1f)] public float gradeSSPercent = 0.9f;
    [Range(0f, 1f)] public float gradeSSSPercent = 0.98f;
    public float holdGradeScorePerSecond = 60f;

    [Header("Hit Feedback")]
    public HitFeedbackText hitFeedbackText;

    [Header("Result Panel")]
    public ResultPanel resultPanel;

    [Header("End Button")]
    public GameObject endGameButton;
    public bool showEndGameButtonDuringGameplay = false;

    [Header("Runtime")]
    public RhythmPlayer rhythmPlayer;

    private bool gameEnded;
    private TMP_FontAsset runtimeScoreFontAsset;
    private bool scoreUiVisible;
    private bool hasRankJudgement;

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

        if (scoreRankPanel == null)
        {
            scoreRankPanel = GameObject.Find("ScoreRankPanel");
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
        hasRankJudgement = true;
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
        hasRankJudgement = true;
        UpdateMaxCombo();
        ShowHitFeedback("Great +60", popupPosition);
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
        hasRankJudgement = true;
        ShowHitFeedback("Nice +20", popupPosition);
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
        hasRankJudgement = true;
        ShowHitFeedback("Break", popupPosition);
        UpdateUI();
    }

    public void AddHoldScore(int value)
    {
        if (gameEnded) return;

        score += value;
        if (value > 0)
        {
            hasRankJudgement = true;
        }

        UpdateUI();
    }

    public void ShowScoreUI()
    {
        ShowScoreUI(true);
    }

    public void ResumeScoreUI()
    {
        ShowScoreUI(false);
    }

    void ShowScoreUI(bool resetStats)
    {
        if (resetStats)
        {
            score = 0;
            combo = 0;
            maxCombo = 0;
            missCount = 0;
            hasRankJudgement = false;
        }

        gameEnded = false;
        scoreUiVisible = true;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowGameplayUI();
        }
        else
        {
            if (scoreRankPanel != null) scoreRankPanel.SetActive(true);
            if (scoreText != null) scoreText.gameObject.SetActive(true);
            SetComboVisible(false);
            if (missText != null) missText.gameObject.SetActive(false);
            ConfigureScoreUIFollow();
        }

        if (endGameButton != null)
        {
            endGameButton.SetActive(showEndGameButtonDuringGameplay);
        }

        if (resultPanel != null)
        {
            resultPanel.Hide();
        }

        UpdateUI();
    }

    public void HideScoreUI()
    {
        scoreUiVisible = false;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideGameplayUI();
        }
        else
        {
            if (scoreRankPanel != null) scoreRankPanel.SetActive(false);
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            SetComboVisible(false);
            if (missText != null) missText.gameObject.SetActive(false);
        }

        if (scoreRankPanel != null) scoreRankPanel.SetActive(false);
        HideRankImages();
    }

    public void EndGame()
    {
        if (gameEnded) return;

        string finalGrade = hasRankJudgement ? GetCurrentGrade() : "B";
        gameEnded = true;
        ClearActiveNotes();

        if (endGameButton != null)
        {
            endGameButton.SetActive(false);
        }

        HideScoreUI();

        if (resultPanel != null)
        {
            resultPanel.ShowResult(score, missCount, maxCombo, finalGrade);
        }
        else
        {
            Debug.LogWarning("ScoreManager: result panel is not assigned.");
        }

        PauseMenuController.Instance?.EnterResult();
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
        if (!popupPosition.HasValue) return;

        hitFeedbackText.ShowFeedback(message, popupPosition.Value);
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
        ApplyScoreFont();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateScore(score, combo, missCount, GetCurrentGrade());
        }

        if (scoreText != null) scoreText.text = score.ToString();
        if (scoreRankPanel != null) scoreRankPanel.SetActive(scoreUiVisible);
        if (comboText != null) comboText.text = combo.ToString();
        SetComboVisible(scoreUiVisible && combo >= 3);
        if (missText != null) missText.gameObject.SetActive(false);

        if (scoreUiVisible && hasRankJudgement)
        {
            UpdateRankImage(GetCurrentGrade());
        }
        else
        {
            HideRankImages();
        }
    }

    void ConfigureScoreUIFollow()
    {
        ApplyScoreFont();
        DisableFollow(scoreText);
        DisableFollow(comboText);
    }

    void UpdateRankImage(string grade)
    {
        SetRankImagesActive(
            grade == "B",
            grade == "A",
            grade == "S",
            grade == "SS",
            grade == "SSS"
        );
    }

    void HideRankImages()
    {
        SetRankImagesActive(false, false, false, false, false);
    }

    void SetRankImagesActive(bool showB, bool showA, bool showS, bool showSS, bool showSSS)
    {
        if (rankBImage != null) rankBImage.SetActive(showB);
        if (rankAImage != null) rankAImage.SetActive(showA);
        if (rankSImage != null) rankSImage.SetActive(showS);
        if (rankSSImage != null) rankSSImage.SetActive(showSS);
        if (rankSSSImage != null) rankSSSImage.SetActive(showSSS);
    }

    void ApplyScoreFont()
    {
        TMP_FontAsset fontAsset = scoreFontAsset;
        if (fontAsset == null && scoreFontSource != null)
        {
            if (runtimeScoreFontAsset == null)
            {
                runtimeScoreFontAsset = TMP_FontAsset.CreateFontAsset(scoreFontSource);
            }

            fontAsset = runtimeScoreFontAsset;
        }

        if (fontAsset != null && scoreText != null && scoreText.font != fontAsset)
        {
            scoreText.font = fontAsset;
        }

        if (fontAsset != null && comboText != null && comboText.font != fontAsset)
        {
            comboText.font = fontAsset;
        }
    }

    void SetComboVisible(bool visible)
    {
        if (comboRoot != null)
        {
            comboRoot.SetActive(visible);
        }
        else if (comboText != null)
        {
            comboText.gameObject.SetActive(visible);
        }
    }

    string GetCurrentGrade()
    {
        float gradeRatio = GetCurrentGradeRatio();

        if (gradeRatio >= gradeSSSPercent) return "SSS";
        if (gradeRatio >= gradeSSPercent) return "SS";
        if (gradeRatio >= gradeSPercent) return "S";
        if (gradeRatio >= gradeAPercent) return "A";
        return "B";
    }

    float GetCurrentGradeRatio()
    {
        float maxScore = gameEnded ? GetTotalMaxScore() : GetMaxScoreUntil(GetCurrentTrackTime());
        if (maxScore <= 0f) return 1f;

        return Mathf.Clamp01(score / maxScore);
    }

    float GetCurrentTrackTime()
    {
        if (rhythmPlayer == null)
        {
            rhythmPlayer = FindFirstObjectByType<RhythmPlayer>();
        }

        return rhythmPlayer != null ? rhythmPlayer.TrackTime : 0f;
    }

    float GetTotalMaxScore()
    {
        NoteData track = rhythmPlayer != null ? rhythmPlayer.track : null;
        if (track == null || track.notes == null) return 0f;

        float total = 0f;
        foreach (NoteEvent note in track.notes)
        {
            total += GetNoteMaxScore(note);
        }

        return total;
    }

    float GetMaxScoreUntil(float trackTime)
    {
        NoteData track = rhythmPlayer != null ? rhythmPlayer.track : null;
        if (track == null || track.notes == null) return 0f;

        float total = 0f;
        foreach (NoteEvent note in track.notes)
        {
            total += GetNoteMaxScoreUntil(note, trackTime);
        }

        return total;
    }

    float GetNoteMaxScore(NoteEvent note)
    {
        if (note.noteType == NoteType.Tap)
        {
            return 100f;
        }

        if (note.noteType == NoteType.Hold)
        {
            float holdDuration = Mathf.Max(0f, note.endTime - note.time);
            return holdDuration * holdGradeScorePerSecond + 100f;
        }

        return 0f;
    }

    float GetNoteMaxScoreUntil(NoteEvent note, float trackTime)
    {
        if (note.noteType == NoteType.Tap)
        {
            return trackTime >= note.time ? 100f : 0f;
        }

        if (note.noteType == NoteType.Hold)
        {
            if (trackTime <= note.time) return 0f;

            float holdEndTime = Mathf.Max(note.time, note.endTime);
            float scoredHoldTime = Mathf.Clamp(trackTime, note.time, holdEndTime) - note.time;
            float maxScore = scoredHoldTime * holdGradeScorePerSecond;

            if (trackTime >= holdEndTime)
            {
                maxScore += 100f;
            }

            return maxScore;
        }

        return 0f;
    }

    void DisableFollow(TextMeshProUGUI text)
    {
        if (text == null) return;

        UIFollowCamera follow = text.GetComponent<UIFollowCamera>();
        if (follow != null)
        {
            follow.enabled = false;
        }
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
