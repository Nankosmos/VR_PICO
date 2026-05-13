using System;
using System.Collections.Generic;
using UnityEngine;

public class HitFeedbackText : MonoBehaviour
{
    public GameObject popupPrefab;
    public Sprite perfectSprite;
    public Sprite greatSprite;
    public Sprite niceSprite;
    public Sprite breakSprite;
    public float popupLifetime = 1.1f;
    public float popupRiseDistance = 0.18f;
    public float popupImageHeight = 0.22f;

    private readonly Queue<GameObject> popupPool = new Queue<GameObject>();

    public void ShowFeedback(string message, Vector3 worldPosition)
    {
        GameObject popup = GetPopup(worldPosition);
        WorldHitFeedbackPopup popupAnimation = popup.GetComponent<WorldHitFeedbackPopup>();

        if (popupAnimation == null)
        {
            popupAnimation = popup.AddComponent<WorldHitFeedbackPopup>();
        }

        popupAnimation.SetReleaseHandler(ReturnPopup);
        popupAnimation.SetSprite(GetFeedbackSprite(message), popupImageHeight);
        popupAnimation.Init(popupLifetime, popupRiseDistance);
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

        GameObject imageObject = new GameObject("FeedbackImage");
        imageObject.transform.SetParent(popup.transform, false);
        imageObject.transform.localPosition = Vector3.zero;
        SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 100;

        WorldHitFeedbackPopup animation = popup.AddComponent<WorldHitFeedbackPopup>();
        animation.feedbackSprite = spriteRenderer;
        return popup;
    }

    void ReturnPopup(GameObject popup)
    {
        if (popup == null) return;

        popup.SetActive(false);
        popupPool.Enqueue(popup);
    }

    Sprite GetFeedbackSprite(string message)
    {
        if (message.StartsWith("Perfect")) return perfectSprite;
        if (message.StartsWith("Great")) return greatSprite;
        if (message.StartsWith("Nice")) return niceSprite;
        if (message.StartsWith("Break")) return breakSprite;
        return null;
    }
}

public class WorldHitFeedbackPopup : MonoBehaviour
{
    public SpriteRenderer feedbackSprite;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float lifetime = 0.75f;
    private float riseDistance = 0.35f;
    private float timer;
    private Action<GameObject> releaseHandler;

    public void Init(float popupLifetime, float popupRiseDistance)
    {
        lifetime = Mathf.Max(0.05f, popupLifetime);
        riseDistance = popupRiseDistance;
        timer = 0f;
        startPosition = transform.position;
        startScale = transform.localScale;
        ApplySpriteAlpha(1f);
    }

    public void SetReleaseHandler(Action<GameObject> handler)
    {
        releaseHandler = handler;
    }

    public void SetSprite(Sprite sprite, float worldHeight)
    {
        if (feedbackSprite == null)
        {
            feedbackSprite = GetComponentInChildren<SpriteRenderer>(true);
        }

        bool useSprite = feedbackSprite != null && sprite != null;
        if (feedbackSprite != null)
        {
            feedbackSprite.sprite = sprite;
            feedbackSprite.gameObject.SetActive(useSprite);

            if (useSprite)
            {
                Vector2 spriteSize = sprite.bounds.size;
                float uniformScale = spriteSize.y > 0f ? worldHeight / spriteSize.y : 1f;
                feedbackSprite.transform.localScale = Vector3.one * uniformScale;
            }
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

        ApplySpriteAlpha(Mathf.Lerp(1f, 0f, t * t));

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

    void ApplySpriteAlpha(float alpha)
    {
        if (feedbackSprite == null) return;

        Color color = feedbackSprite.color;
        color.a = alpha;
        feedbackSprite.color = color;
    }
}
