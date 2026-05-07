using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HitFeedbackText : MonoBehaviour
{
    public TextMeshProUGUI feedbackText;
    public GameObject popupPrefab;
    public float showTime = 0.5f;
    public float popupLifetime = 1.1f;
    public float popupRiseDistance = 0.18f;
    public float popupFontSize = 0.36f;
    public float popupScoreFontSize = 0.24f;
    public float popupGlowScale = 1.08f;
    public float popupFlareHalfLength = 0.38f;
    public float popupFlareWidth = 0.008f;
    public float previewForwardDistance = 1.2f;
    public float previewVerticalOffset = -0.1f;

    private readonly Queue<GameObject> popupPool = new Queue<GameObject>();
    private float timer;

    void Start()
    {
        Hide();
    }

    void Update()
    {
        if (feedbackText != null && feedbackText.gameObject.activeSelf)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                Hide();
            }
        }

        UpdatePreviewHotkeys();
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        timer = showTime;
    }

    public void ShowFeedback(string message, Vector3 worldPosition)
    {
        GameObject popup = GetPopup(worldPosition);
        WorldHitFeedbackPopup popupAnimation = popup.GetComponent<WorldHitFeedbackPopup>();

        if (popupAnimation == null)
        {
            popupAnimation = popup.AddComponent<WorldHitFeedbackPopup>();
        }

        popupAnimation.SetReleaseHandler(ReturnPopup);
        popupAnimation.SetMessage(message);
        popupAnimation.Init(popupLifetime, popupRiseDistance, GetFeedbackColor(message));
    }

    GameObject GetPopup(Vector3 position)
    {
        GameObject popup;

        if (popupPool.Count > 0)
        {
            popup = popupPool.Dequeue();
        }
        else if (popupPrefab != null)
        {
            popup = Instantiate(popupPrefab);
        }
        else
        {
            popup = CreateBuiltInPopup();
        }

        popup.transform.position = position;
        popup.transform.rotation = Quaternion.identity;
        popup.transform.localScale = Vector3.one;
        popup.SetActive(true);
        return popup;
    }

    GameObject CreateBuiltInPopup()
    {
        GameObject popup = new GameObject("HitFeedbackPopup");

        TextMeshPro judgementText = CreatePopupText(
            popup.transform,
            "Perfect",
            popupFontSize,
            new Vector3(0f, 0.03f, 0f)
        );

        TextMeshPro scoreText = CreatePopupText(
            popup.transform,
            "+100",
            popupScoreFontSize,
            new Vector3(0f, -0.1f, 0f)
        );

        LineRenderer flare = popup.AddComponent<LineRenderer>();
        flare.positionCount = 2;
        flare.useWorldSpace = false;
        flare.SetPosition(0, new Vector3(-popupFlareHalfLength, 0.02f, 0.02f));
        flare.SetPosition(1, new Vector3(popupFlareHalfLength, 0.02f, 0.02f));
        flare.startWidth = popupFlareWidth;
        flare.endWidth = popupFlareWidth;
        flare.material = new Material(Shader.Find("Sprites/Default"));

        WorldHitFeedbackPopup animation = popup.AddComponent<WorldHitFeedbackPopup>();
        animation.judgementText = judgementText;
        animation.scoreText = scoreText;
        animation.presetFlare = flare;
        return popup;
    }

    TextMeshPro CreatePopupText(Transform parent, string message, float fontSize, Vector3 localPosition)
    {
        GameObject textObject = new GameObject("PopupText");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Italic;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;
        text.outlineColor = new Color(1f, 0.88f, 0.48f, 0.9f);
        text.outlineWidth = 0.18f * popupGlowScale;
        return text;
    }

    void ReturnPopup(GameObject popup)
    {
        if (popup == null) return;

        popup.SetActive(false);
        popupPool.Enqueue(popup);
    }

    Color GetFeedbackColor(string message)
    {
        if (message.StartsWith("Break")) return new Color(1f, 0.35f, 0.35f);
        return new Color(1f, 0.88f, 0.48f);
    }

    void Hide()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    void UpdatePreviewHotkeys()
    {
        if (WasPreviewKeyPressed(PreviewKey.Perfect)) Preview("Perfect +100");
        if (WasPreviewKeyPressed(PreviewKey.Good)) Preview("Great +60");
        if (WasPreviewKeyPressed(PreviewKey.Bad)) Preview("Nice +20");
        if (WasPreviewKeyPressed(PreviewKey.Miss)) Preview("Break");
    }

    bool WasPreviewKeyPressed(PreviewKey previewKey)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        switch (previewKey)
        {
            case PreviewKey.Perfect:
                return keyboard.pKey.wasPressedThisFrame;
            case PreviewKey.Good:
                return keyboard.gKey.wasPressedThisFrame;
            case PreviewKey.Bad:
                return keyboard.bKey.wasPressedThisFrame;
            case PreviewKey.Miss:
                return keyboard.mKey.wasPressedThisFrame;
        }

        return false;
#else
        switch (previewKey)
        {
            case PreviewKey.Perfect:
                return Input.GetKeyDown(KeyCode.P);
            case PreviewKey.Good:
                return Input.GetKeyDown(KeyCode.G);
            case PreviewKey.Bad:
                return Input.GetKeyDown(KeyCode.B);
            case PreviewKey.Miss:
                return Input.GetKeyDown(KeyCode.M);
        }

        return false;
#endif
    }

    void Preview(string message)
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 position = camera.transform.position
            + camera.transform.forward * previewForwardDistance
            + Vector3.up * previewVerticalOffset;

        ShowFeedback(message, position);
    }
}

public class WorldHitFeedbackPopup : MonoBehaviour
{
    public TextMeshPro judgementText;
    public TextMeshPro scoreText;
    public LineRenderer presetFlare;

    private readonly List<TextMeshPro> texts = new List<TextMeshPro>();
    private LineRenderer flare;
    private Vector3 startPosition;
    private Vector3 startScale;
    private float lifetime = 0.75f;
    private float riseDistance = 0.35f;
    private float timer;
    private Color baseColor = Color.white;
    private Action<GameObject> releaseHandler;

    public void Init(float popupLifetime, float popupRiseDistance, Color popupColor)
    {
        lifetime = Mathf.Max(0.05f, popupLifetime);
        riseDistance = popupRiseDistance;
        baseColor = popupColor;
        timer = 0f;
        startPosition = transform.position;
        startScale = transform.localScale;
        flare = presetFlare;
        CacheTexts();
        ApplyTextAlpha(1f);
    }

    public void SetReleaseHandler(Action<GameObject> handler)
    {
        releaseHandler = handler;
    }

    public void SetMessage(string message)
    {
        string judgement = message;
        string score = "";
        int scoreIndex = message.IndexOf(" +");

        if (scoreIndex >= 0)
        {
            judgement = message.Substring(0, scoreIndex);
            score = message.Substring(scoreIndex + 1);
        }

        CacheTexts();

        if (judgementText != null)
        {
            judgementText.text = judgement;
        }

        if (scoreText != null)
        {
            scoreText.text = score;
            scoreText.gameObject.SetActive(!string.IsNullOrEmpty(score));
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / lifetime);
        transform.position = startPosition + Vector3.up * (riseDistance * t);
        transform.localScale = startScale * Mathf.Lerp(0.96f, 1.04f, Mathf.Sin(t * Mathf.PI));

        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        }

        float fade = 1f - t;
        ApplyTextAlpha(Mathf.Lerp(1f, 0f, t * t));

        if (flare != null)
        {
            float flareAlpha = Mathf.Sin(t * Mathf.PI) * fade * 0.75f;
            Color flareColor = new Color(baseColor.r, baseColor.g, baseColor.b, flareAlpha);
            flare.startColor = flareColor;
            flare.endColor = flareColor;
        }

        if (t >= 1f)
        {
            if (releaseHandler != null)
            {
                releaseHandler.Invoke(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void CacheTexts()
    {
        if (judgementText == null || scoreText == null)
        {
            TextMeshPro[] childTexts = GetComponentsInChildren<TextMeshPro>(true);
            if (judgementText == null && childTexts.Length > 0) judgementText = childTexts[0];
            if (scoreText == null && childTexts.Length > 1) scoreText = childTexts[1];
        }

        texts.Clear();
        if (judgementText != null) texts.Add(judgementText);
        if (scoreText != null) texts.Add(scoreText);
    }

    void ApplyTextAlpha(float alpha)
    {
        foreach (TextMeshPro text in texts)
        {
            if (text == null) continue;

            Color color = text.color;
            color.a = alpha;
            text.color = color;

            Color outlineColor = text.outlineColor;
            outlineColor.a = alpha * 0.9f;
            text.outlineColor = outlineColor;
        }
    }
}

public enum PreviewKey
{
    Perfect,
    Good,
    Bad,
    Miss
}
