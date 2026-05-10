using System;
using UnityEngine;

public class NoteOrb : MonoBehaviour
{
    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;

    [Header("Hit Sound")]
    public AudioClip hitSound;

    [Header("Judgement Range")]
    public float perfectDistance = 0.25f;
    public float goodDistance = 0.55f;
    public float badDistance = 0.9f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private Vector3 hitPosition;
    private Vector3 endPosition;
    private Vector3 startPosition;
    private float travelTime;
    private float totalTravelTime;
    private float timer;
    private float spawnSongTime;
    private AudioSource songAudioSource;
    private HandType targetHand;
    private bool isHit;
    private Action releaseToPool;

    public void Init(Vector3 hitPos, Vector3 endPos, float flyTimeToHitPoint, HandType hand)
    {
        Init(hitPos, endPos, flyTimeToHitPoint, hand, null, flyTimeToHitPoint);
    }

    public void Init(Vector3 hitPos, Vector3 endPos, float flyTimeToHitPoint, HandType hand, AudioSource audioSource, float hitSongTime)
    {
        hitPosition = hitPos;
        endPosition = endPos;
        travelTime = Mathf.Max(0.05f, flyTimeToHitPoint);
        targetHand = hand;
        songAudioSource = audioSource;
        spawnSongTime = hitSongTime - travelTime;
        startPosition = transform.position;
        timer = 0f;
        isHit = false;

        float startToHit = Vector3.Distance(startPosition, hitPosition);
        float startToEnd = Vector3.Distance(startPosition, endPosition);
        totalTravelTime = startToHit > 0.01f
            ? travelTime * (startToEnd / startToHit)
            : travelTime + 0.5f;
    }

    public void SetPoolReleaseHandler(Action releaseHandler)
    {
        releaseToPool = releaseHandler;
    }

    public void ClearWithoutScore()
    {
        isHit = true;
        Finish();
    }

    void Update()
    {
        if (isHit) return;

        timer += Time.deltaTime;

        float elapsedTime = timer;
        if (songAudioSource != null)
        {
            elapsedTime = songAudioSource.time - spawnSongTime;
        }

        float t = Mathf.Clamp01(elapsedTime / totalTravelTime);
        transform.position = Vector3.Lerp(startPosition, endPosition, t);

        if (t >= 1f)
        {
            ScoreManager.Instance?.AddMiss(endPosition);
            Finish();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (isHit) return;

        if (targetHand == HandType.Left && other.CompareTag("LeftHand"))
        {
            Hit();
        }

        if (targetHand == HandType.Right && other.CompareTag("RightHand"))
        {
            Hit();
        }
    }

    void Hit()
    {
        isHit = true;

        float distance = Vector3.Distance(transform.position, hitPosition);

        if (distance < perfectDistance)
        {
            ScoreManager.Instance?.AddPerfect(hitPosition);
        }
        else if (distance < goodDistance)
        {
            ScoreManager.Instance?.AddGood(hitPosition);
        }
        else if (distance < badDistance)
        {
            ScoreManager.Instance?.AddBad(hitPosition);
        }
        else
        {
            ScoreManager.Instance?.AddBad(hitPosition);
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioPlayback.PlaySfx(hitSound);
        }

        Finish();
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

        Vector3 center = Application.isPlaying ? hitPosition : transform.position;

        Gizmos.color = new Color(0.3f, 1f, 1f, 0.55f);
        Gizmos.DrawWireSphere(center, perfectDistance);

        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.45f);
        Gizmos.DrawWireSphere(center, goodDistance);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(center, badDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, hitPosition);
        }
    }
}
