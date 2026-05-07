using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Gameplay UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI missText;
    public GameObject progressRingRoot;
    public Image progressRing;

    [Header("Camera Follow")]
    public Transform followCamera;
    public float followDistance = 1.25f;
    public float topOffset = 0.42f;
    public float sideOffset = 0.55f;
    public Vector3 progressRingOffset = new Vector3(0.65f, -0.12f, 1.2f);
    public float progressRingScale = 0.25f;

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
        SetGameplayUIActive(true);
        ConfigureFollow();
    }

    public void HideGameplayUI()
    {
        SetGameplayUIActive(false);
    }

    public void UpdateScore(int score, int combo, int missCount, string grade)
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (comboText != null) comboText.text = "Combo: " + combo;
        if (gradeText != null) gradeText.text = "Rank: " + grade;
        if (missText != null) missText.gameObject.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        if (progressRing != null)
        {
            progressRing.fillAmount = progress;
        }
    }

    void SetGameplayUIActive(bool active)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(active);
        if (comboText != null) comboText.gameObject.SetActive(active);
        if (gradeText != null) gradeText.gameObject.SetActive(active);
        if (missText != null) missText.gameObject.SetActive(false);
        if (progressRingRoot != null) progressRingRoot.SetActive(active);
    }

    void ConfigureFollow()
    {
        Transform cameraTransform = GetFollowCamera();

        ConfigureFollow(scoreText, new Vector3(-sideOffset, topOffset, followDistance), cameraTransform);
        ConfigureFollow(comboText, new Vector3(0f, topOffset, followDistance), cameraTransform);
        ConfigureFollow(gradeText, new Vector3(sideOffset, topOffset, followDistance), cameraTransform);

        if (progressRingRoot != null)
        {
            UIFollowCamera follow = progressRingRoot.GetComponent<UIFollowCamera>();
            if (follow == null)
            {
                follow = progressRingRoot.AddComponent<UIFollowCamera>();
            }

            follow.targetCamera = cameraTransform;
            follow.cameraLocalOffset = progressRingOffset;

            progressRingRoot.transform.localScale = Vector3.one * progressRingScale;
        }
    }

    Transform GetFollowCamera()
    {
        if (followCamera != null) return followCamera;

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
}
