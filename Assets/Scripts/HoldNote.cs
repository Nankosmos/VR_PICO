using System;
using UnityEngine;

public class HoldNote : MonoBehaviour
{
    [Header("Path Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Start Hit Effect")]
    public GameObject startHitEffectPrefab;

    [Header("Hold Loop Effect")]
    public GameObject holdLoopEffectPrefab;
    public bool useBuiltInHoldLoopEffect = true;

    [Header("Start Hit Sound")]
    public AudioClip startHitSound;

    [Header("Judgement")]
    public float followRadius = 0.5f;
    public float failToleranceTime = 0.5f;
    public float startHitLateWindow = 0.25f;
    public float startHitRadius = 0.9f;

    [Header("Magnet")]
    public float magnetRadius = 1.2f;
    public float magnetStrength = 0.6f;

    [Header("Score")]
    public float holdScorePerSecond = 60f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private HoldNoteView view;
    private HandType targetHand;
    private Transform handTransform;
    private Vector3 startSpawnPos;
    private Vector3 endSpawnPos;
    private Vector3 startHitPos;
    private Vector3 endHitPos;
    private float flyDuration = 1.5f;
    private float holdDuration = 1f;
    private float totalTailFlyDuration = 2.5f;
    private float startSongTime;
    private float endSongTime = 1f;
    private float spawnSongTime;
    private float flyTimer;
    private float startHitTimer;
    private float holdTimer;
    private float failTimer;
    private float pendingHoldScore;
    private float lastHoldSongTime;
    private AudioSource songAudioSource;
    private bool isInitialized;
    private bool hasReachedStart;
    private bool isWaitingForStartHit;
    private bool isHolding;
    private bool hasEnded;
    private Action releaseToPool;

    void Awake()
    {
        CacheView();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Init(
        Vector3 startSpawn,
        Vector3 endSpawn,
        Vector3 startHit,
        Vector3 endHit,
        float flyTime,
        float holdTime,
        HandType hand,
        Transform targetHandTransform
    )
    {
        Init(startSpawn, endSpawn, startHit, endHit, flyTime, holdTime, hand, targetHandTransform, null, flyTime, flyTime + holdTime);
    }

    public void Init(
        Vector3 startSpawn,
        Vector3 endSpawn,
        Vector3 startHit,
        Vector3 endHit,
        float flyTime,
        float holdTime,
        HandType hand,
        Transform targetHandTransform,
        AudioSource audioSource,
        float startTime,
        float endTime
    )
    {
        CacheView();

        startSpawnPos = startSpawn;
        endSpawnPos = endSpawn;
        startHitPos = startHit;
        endHitPos = endHit;
        flyDuration = Mathf.Max(0.1f, flyTime);
        holdDuration = Mathf.Max(0.1f, holdTime);
        totalTailFlyDuration = flyDuration + holdDuration;
        targetHand = hand;
        handTransform = targetHandTransform;
        songAudioSource = audioSource;
        startSongTime = startTime;
        endSongTime = endTime;
        spawnSongTime = startSongTime - flyDuration;
        flyTimer = 0f;
        startHitTimer = 0f;
        holdTimer = 0f;
        failTimer = 0f;
        pendingHoldScore = 0f;
        lastHoldSongTime = startSongTime;
        hasReachedStart = false;
        isWaitingForStartHit = false;
        isHolding = false;
        hasEnded = false;
        isInitialized = true;

        if (startPoint != null) startPoint.position = startSpawnPos;
        if (endPoint != null) endPoint.position = endSpawnPos;
        if (view != null)
        {
            view.SetStartOrbActive(true);
            view.SetEndOrbActive(true);
        }

        UpdateLineRenderer();
    }

    public void ApplyDifficulty(
        float newFollowRadius,
        float newFailToleranceTime,
        float newStartHitLateWindow,
        float newStartHitRadius,
        float newMagnetRadius,
        float newMagnetStrength
    )
    {
        followRadius = newFollowRadius;
        failToleranceTime = newFailToleranceTime;
        startHitLateWindow = newStartHitLateWindow;
        startHitRadius = newStartHitRadius;
        magnetRadius = newMagnetRadius;
        magnetStrength = newMagnetStrength;
    }

    public void SetPoolReleaseHandler(Action releaseHandler)
    {
        releaseToPool = releaseHandler;
    }

    public void ClearWithoutScore()
    {
        if (hasEnded) return;

        hasEnded = true;
        StopViewHoldLoopEffect();
        Finish();
    }

    public void TryStartHold(Collider other)
    {
        if (hasEnded || !hasReachedStart || !isWaitingForStartHit) return;

        if (targetHand == HandType.Left && other.CompareTag("LeftHand"))
        {
            StartHold();
        }
        else if (targetHand == HandType.Right && other.CompareTag("RightHand"))
        {
            StartHold();
        }
    }

    void Update()
    {
        if (!isInitialized || hasEnded) return;

        if (!hasReachedStart)
        {
            UpdateFlying();
        }
        else if (isWaitingForStartHit)
        {
            UpdateWaitingForStartHit();
            UpdateStartHitWindow();
        }
        else if (isHolding)
        {
            UpdateHolding();
        }

        UpdateLineRenderer();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartHold(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartHold(other);
    }

    void UpdateFlying()
    {
        flyTimer = songAudioSource != null ? GetSongTime() - spawnSongTime : flyTimer + Time.deltaTime;
        float t = Mathf.Clamp01(flyTimer / flyDuration);
        float tailT = Mathf.Clamp01(flyTimer / totalTailFlyDuration);

        SetViewEndpoints(
            Vector3.Lerp(startSpawnPos, startHitPos, t),
            Vector3.Lerp(endSpawnPos, endHitPos, tailT)
        );

        if (t >= 1f)
        {
            hasReachedStart = true;
            isWaitingForStartHit = true;
            startHitTimer = 0f;
        }
    }

    void UpdateWaitingForStartHit()
    {
        float elapsed = flyDuration + startHitTimer;
        float tailT = Mathf.Clamp01(elapsed / totalTailFlyDuration);
        SetViewEndpoints(startHitPos, Vector3.Lerp(endSpawnPos, endHitPos, tailT));
    }

    void UpdateStartHitWindow()
    {
        if (handTransform != null && Vector3.Distance(handTransform.position, startHitPos) <= startHitRadius)
        {
            StartHold();
            return;
        }

        startHitTimer = songAudioSource != null ? GetSongTime() - startSongTime : startHitTimer + Time.deltaTime;

        if (startHitTimer > startHitLateWindow)
        {
            float distance = handTransform != null ? Vector3.Distance(handTransform.position, startHitPos) : -1f;
            HoldMiss("Start timeout. distance=" + distance);
        }
    }

    void StartHold()
    {
        isWaitingForStartHit = false;
        isHolding = true;
        HapticFeedback.PlayLightHit(targetHand);
        holdTimer = 0f;
        failTimer = 0f;
        pendingHoldScore = 0f;
        lastHoldSongTime = GetSongTime();

        Vector3 startEffectPosition = startPoint != null ? startPoint.position : startHitPos;
        view.PlayStartHitFeedback(startEffectPosition);
        view.SetStartOrbActive(false);
        view.StartHoldLoopEffect(startEffectPosition);
    }

    void UpdateHolding()
    {
        if (handTransform == null)
        {
            Debug.LogWarning("HoldNote is missing the hand transform: " + targetHand);
            return;
        }

        float deltaSongTime;
        if (songAudioSource != null)
        {
            float songTime = GetSongTime();
            deltaSongTime = Mathf.Max(0f, songTime - lastHoldSongTime);
            lastHoldSongTime = songTime;
            holdTimer = songTime - startSongTime;
        }
        else
        {
            deltaSongTime = Time.deltaTime;
            holdTimer += deltaSongTime;
        }

        float t = Mathf.Clamp01(holdTimer / holdDuration);
        Vector3 rawPoint = Vector3.Lerp(startHitPos, endHitPos, t);
        float tailT = Mathf.Clamp01((flyDuration + holdTimer) / totalTailFlyDuration);
        Vector3 tailPosition = Vector3.Lerp(endSpawnPos, endHitPos, tailT);

        SetViewEndpoints(rawPoint, tailPosition);
        view.UpdateHoldLoopEffect(rawPoint);

        Vector3 finalPoint = ApplyMagnet(rawPoint);
        float distance = Vector3.Distance(handTransform.position, finalPoint);

        if (distance <= followRadius)
        {
            failTimer = 0f;
            AddHoldScoreByTime(deltaSongTime);
        }
        else
        {
            failTimer += deltaSongTime;
            if (failTimer >= failToleranceTime)
            {
                HoldMiss("Follow lost. distance=" + distance);
                return;
            }
        }

        if (t >= 1f)
        {
            CompleteHold();
        }
    }

    Vector3 ApplyMagnet(Vector3 rawPoint)
    {
        float distanceToHand = Vector3.Distance(handTransform.position, rawPoint);
        if (distanceToHand >= magnetRadius) return rawPoint;

        float magnetFactor = 1f - distanceToHand / magnetRadius;
        return Vector3.Lerp(rawPoint, handTransform.position, magnetFactor * magnetStrength);
    }

    void AddHoldScoreByTime(float deltaTime)
    {
        pendingHoldScore += holdScorePerSecond * deltaTime;
        int scoreToAdd = Mathf.FloorToInt(pendingHoldScore);
        if (scoreToAdd <= 0) return;

        pendingHoldScore -= scoreToAdd;
        ScoreManager.Instance?.AddHoldScore(scoreToAdd);
    }

    float GetSongTime()
    {
        return songAudioSource != null ? songAudioSource.time : startSongTime + holdTimer;
    }

    void HoldMiss(string reason)
    {
        Debug.Log("Hold miss: " + targetHand + " | " + reason);
        HoldMiss();
    }

    void HoldMiss()
    {
        if (hasEnded) return;

        hasEnded = true;
        Vector3 missPosition = startPoint != null ? startPoint.position : startHitPos;
        ScoreManager.Instance?.AddMiss(missPosition);
        StopViewHoldLoopEffect();
        Finish();
    }

    void CompleteHold()
    {
        if (hasEnded) return;

        hasEnded = true;
        Vector3 completePosition = startPoint != null ? startPoint.position : endHitPos;
        ScoreManager.Instance?.AddPerfect(completePosition);
        StopViewHoldLoopEffect();
        Finish();
    }

    void CacheView()
    {
        view = GetComponent<HoldNoteView>();
        if (view == null)
        {
            view = gameObject.AddComponent<HoldNoteView>();
        }

        view.Configure(startPoint, endPoint, startHitEffectPrefab, holdLoopEffectPrefab, useBuiltInHoldLoopEffect, startHitSound);
    }

    void SetViewEndpoints(Vector3 startPosition, Vector3 endPosition)
    {
        if (view != null) view.SetEndpoints(startPosition, endPosition);
    }

    void UpdateLineRenderer()
    {
        if (view != null) view.UpdateLineRenderer();
    }

    void StopViewHoldLoopEffect()
    {
        if (view != null) view.StopHoldLoopEffect();
    }

    void Finish()
    {
        if (releaseToPool != null)
        {
            releaseToPool.Invoke();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 start = Application.isPlaying
            ? startHitPos
            : startPoint != null ? startPoint.position : transform.position;

        Vector3 end = Application.isPlaying
            ? endHitPos
            : endPoint != null ? endPoint.position : transform.position;

        Gizmos.color = new Color(1f, 0.88f, 0.25f, 0.55f);
        Gizmos.DrawWireSphere(start, startHitRadius);

        Gizmos.color = new Color(0.3f, 1f, 1f, 0.55f);
        Gizmos.DrawLine(start, end);

        Vector3 currentPoint = startPoint != null ? startPoint.position : start;
        Gizmos.DrawWireSphere(currentPoint, followRadius);

        if (handTransform != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(handTransform.position, currentPoint);
        }
    }
}
