using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultPanel : MonoBehaviour
{
    [Header("Result Text")]
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI missCountText;
    public TextMeshProUGUI maxComboText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI bestComboText;

    [Header("Rank Image")]
    public GameObject rankImageRoot;
    public GameObject rankBImage;
    public GameObject rankAImage;
    public GameObject rankSImage;
    public GameObject rankSSImage;
    public GameObject rankSSSImage;

    [Header("Fonts")]
    public Font scoreFontSource;

    [Header("Restart Button")]
    public GameObject restartButton;

    private TMP_FontAsset runtimeScoreFontAsset;

    void Awake()
    {
        CacheResultReferences();
        Hide();
    }

    public void ShowResult(int score, int missCount, int maxCombo)
    {
        ShowResult(score, missCount, maxCombo, "");
    }

    public void ShowResult(int score, int missCount, int maxCombo, string grade)
    {
        CacheResultReferences();
        ApplyScoreFont();

        if (gameOverText != null) gameOverText.text = "Game Over";
        if (finalScoreText != null) finalScoreText.text = score.ToString();
        if (missCountText != null) missCountText.gameObject.SetActive(false);
        if (maxComboText != null) maxComboText.text = maxCombo.ToString();
        if (bestComboText != null) bestComboText.text = maxCombo.ToString();
        if (gradeText != null) gradeText.gameObject.SetActive(false);
        UpdateRankImage(grade);

        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        HideRankImages();

        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void OnRestartClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CacheResultReferences()
    {
        if (finalScoreText == null)
        {
            finalScoreText = FindText("ScoreText");
        }

        if (bestComboText == null)
        {
            bestComboText = FindText("BestComboText");
        }

        if (bestComboText == null && maxComboText == null)
        {
            maxComboText = FindText("ComboText");
        }

        if (rankImageRoot == null)
        {
            Transform rankRoot = FindChild(transform, "RankImage");
            if (rankRoot != null)
            {
                rankImageRoot = rankRoot.gameObject;
            }
        }
        else if (!rankImageRoot.transform.IsChildOf(transform))
        {
            Transform rankRoot = FindChild(transform, "RankImage");
            if (rankRoot != null)
            {
                rankImageRoot = rankRoot.gameObject;
            }
        }

        CacheRankChild(ref rankBImage, "B");
        CacheRankChild(ref rankAImage, "A");
        CacheRankChild(ref rankSImage, "S");
        CacheRankChild(ref rankSSImage, "SS");
        CacheRankChild(ref rankSSSImage, "SSS");
    }

    TextMeshProUGUI FindText(string childName)
    {
        Transform child = FindChild(transform, childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    void CacheRankChild(ref GameObject rankObject, string childName)
    {
        Transform searchRoot = rankImageRoot != null ? rankImageRoot.transform : transform.root;
        if (rankObject != null && rankObject.transform.IsChildOf(searchRoot)) return;

        rankObject = null;
        Transform child = FindChild(searchRoot, childName);
        if (child != null)
        {
            rankObject = child.gameObject;
        }
    }

    Transform FindChild(Transform root, string childName)
    {
        if (root == null) return null;

        string normalizedChildName = childName.Trim();
        if (root.name.Trim() == normalizedChildName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), normalizedChildName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    void ApplyScoreFont()
    {
        TMP_FontAsset fontAsset = null;
        if (scoreFontSource != null)
        {
            if (runtimeScoreFontAsset == null)
            {
                runtimeScoreFontAsset = TMP_FontAsset.CreateFontAsset(scoreFontSource);
            }

            fontAsset = runtimeScoreFontAsset;
        }

        ApplyFont(finalScoreText, fontAsset);
        ApplyFont(maxComboText, fontAsset);
        ApplyFont(bestComboText, fontAsset);
    }

    void ApplyFont(TextMeshProUGUI text, TMP_FontAsset fontAsset)
    {
        if (text == null || fontAsset == null) return;

        text.font = fontAsset;
    }

    void UpdateRankImage(string grade)
    {
        grade = string.IsNullOrWhiteSpace(grade) ? "B" : grade.Trim().ToUpperInvariant();
        HideRankImages();

        if (rankImageRoot != null)
        {
            if (rankImageRoot.transform.parent != null)
            {
                rankImageRoot.transform.parent.gameObject.SetActive(true);
            }

            rankImageRoot.SetActive(true);
        }

        switch (grade)
        {
            case "SSS":
                SetRankActive(rankSSSImage);
                break;
            case "SS":
                SetRankActive(rankSSImage);
                break;
            case "S":
                SetRankActive(rankSImage);
                break;
            case "A":
                SetRankActive(rankAImage);
                break;
            default:
                SetRankActive(rankBImage);
                break;
        }
    }

    void HideRankImages()
    {
        if (rankBImage != null) rankBImage.SetActive(false);
        if (rankAImage != null) rankAImage.SetActive(false);
        if (rankSImage != null) rankSImage.SetActive(false);
        if (rankSSImage != null) rankSSImage.SetActive(false);
        if (rankSSSImage != null) rankSSSImage.SetActive(false);
    }

    void SetRankActive(GameObject rankObject)
    {
        if (rankObject != null)
        {
            rankObject.SetActive(true);
        }
    }
}
