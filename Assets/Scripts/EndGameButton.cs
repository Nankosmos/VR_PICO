using UnityEngine;

public class EndGameButton : MonoBehaviour
{
    public void OnEndGameClick()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ForceEndGame();
        }
        else
        {
            Debug.LogWarning("EndGameButton: score manager was not found.");
        }
    }
}
