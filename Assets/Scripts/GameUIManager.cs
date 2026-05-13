using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Gameplay UI")]
    public TextMeshProUGUI scoreText;
    public GameObject comboRoot;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI missText;
    public TMP_FontAsset scoreFontAsset;
    public Font scoreFontSource;

    [Header("Music Progress UI")]
    public GameObject musicProgressRoot;
    public RectTransform musicProgressFill;
    public TextMeshProUGUI musicProgressTimeText;

    private TMP_FontAsset runtimeScoreFontAsset;
    private float musicProgressFullWidth = -1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShowGameplayUI()
    {
        FindMusicProgressReferences();
        SetGameplayUIActive(true);
        ConfigureFollow();
    }

    public void HideGameplayUI()
    {
        FindMusicProgressReferences();
        SetGameplayUIActive(false);
    }

    public void UpdateScore(int score, int combo, int missCount, string grade)
    {
        ApplyScoreFont();

        if (scoreText != null) scoreText.text = score.ToString();
        if (comboText != null) comboText.text = combo.ToString();
        SetComboVisible(combo >= 3);
        if (missText != null) missText.gameObject.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        SetMusicProgress(progress, 0f, 0f);
    }

    public void SetMusicProgress(float progress, float currentTime, float totalTime)
    {
        FindMusicProgressReferences();

        if (musicProgressRoot != null)
        {
            musicProgressRoot.SetActive(true);
        }

        if (musicProgressFill != null)
        {
            if (musicProgressFullWidth < 0f)
            {
                musicProgressFullWidth = musicProgressFill.sizeDelta.x;
            }

            Vector2 size = musicProgressFill.sizeDelta;
            size.x = musicProgressFullWidth * Mathf.Clamp01(progress);
            musicProgressFill.sizeDelta = size;
        }

        if (musicProgressTimeText != null)
        {
            musicProgressTimeText.text = FormatTime(currentTime) + "/" + FormatTime(totalTime);
        }
    }

    void SetGameplayUIActive(bool active)
    {
        FindMusicProgressReferences();
        ApplyScoreFont();

        if (scoreText != null) scoreText.gameObject.SetActive(active);
        if (!active)
        {
            SetComboVisible(false);
        }
        if (missText != null) missText.gameObject.SetActive(false);
        if (musicProgressRoot != null) musicProgressRoot.SetActive(active);
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

    void ConfigureFollow()
    {
        DisableFollow(scoreText);
        DisableFollow(comboText);
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

    void DisableFollow(TextMeshProUGUI text)
    {
        if (text == null) return;

        UIFollowCamera follow = text.GetComponent<UIFollowCamera>();
        if (follow != null)
        {
            follow.enabled = false;
        }
    }

    void FindMusicProgressReferences()
    {
        if (musicProgressRoot == null)
        {
            GameObject root = GameObject.Find("MusicProgressRoot");
            if (root != null)
            {
                musicProgressRoot = root;
            }
        }

        if (musicProgressFill == null)
        {
            GameObject fill = GameObject.Find("MusicProgressFill");
            if (fill != null)
            {
                musicProgressFill = fill.GetComponent<RectTransform>();
            }
        }

        if (musicProgressTimeText == null)
        {
            GameObject timeText = GameObject.Find("MusicProgressTimeText");
            if (timeText != null)
            {
                musicProgressTimeText = timeText.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }
}
