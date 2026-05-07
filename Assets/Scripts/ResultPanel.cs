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

    [Header("Restart Button")]
    public GameObject restartButton;

    void Awake()
    {
        Hide();
    }

    public void ShowResult(int score, int missCount, int maxCombo)
    {
        if (gameOverText != null) gameOverText.text = "Game Over";
        if (finalScoreText != null) finalScoreText.text = "Score: " + score;
        if (missCountText != null) missCountText.gameObject.SetActive(false);
        if (maxComboText != null) maxComboText.text = "Max Combo: " + maxCombo;

        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
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
}
