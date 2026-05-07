using UnityEngine;

public class GameStarter : MonoBehaviour
{
    public RhythmPlayer rhythmPlayer;
    public ScoreManager scoreManager;
    public GameObject gameTitleText;

    public void OnStartButtonClick()
    {
        if (gameTitleText != null)
        {
            gameTitleText.SetActive(false);
        }

        if (scoreManager == null)
        {
            scoreManager = ScoreManager.Instance;
        }

        if (scoreManager != null)
        {
            scoreManager.ShowScoreUI();
        }
        else
        {
            Debug.LogWarning("GameStarter: score manager is not assigned.");
        }

        if (rhythmPlayer == null)
        {
            rhythmPlayer = FindFirstObjectByType<RhythmPlayer>();
        }

        if (rhythmPlayer == null)
        {
            Debug.LogWarning("GameStarter: rhythm player is not assigned.");
            return;
        }

        rhythmPlayer.PlayTrack();
        gameObject.SetActive(false);
    }
}
