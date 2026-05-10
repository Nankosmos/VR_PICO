using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class RhythmPlayer : MonoBehaviour
{
    [Header("Track")]
    public NoteData track;

    [Header("Tap Note Prefabs")]
    public GameObject leftOrbPrefab;
    public GameObject rightOrbPrefab;

    [Header("Hold Note Prefabs")]
    public GameObject leftHoldPrefab;
    public GameObject rightHoldPrefab;

    [Header("Hand References")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("PICO Controller Tracking")]
    public bool usePicoControllerTrackingOnDevice = false;
    public float picoControllerColliderRadius = 0.2f;

    [Header("Music")]
    public float spawnLeadTime = 1.5f;

    [Header("Camera")]
    public Transform playerCamera;

    [Header("Spawn Layout")]
    public float spawnForwardDistance = 8f;
    public float endBehindDistance = 0.5f;

    [Header("Track Simplify")]
    public float fixedHitDepth = 0.5f;

    [Header("Progress Ring")]
    public GameObject progressRingRoot;
    public Image progressRing;
    public Vector3 progressRingCameraOffset = new Vector3(0.65f, -0.12f, 1.2f);
    public float progressRingScale = 0.25f;

    [Header("Hold Difficulty")]
    public float holdStartHitLateWindow = 0.25f;
    public float holdStartHitRadius = 0.9f;
    public float holdFollowRadius = 0.5f;
    public float holdFailToleranceTime = 0.5f;
    public float holdMagnetRadius = 1.2f;
    public float holdMagnetStrength = 0.6f;

    private readonly Queue<NoteEvent> noteQueue = new Queue<NoteEvent>();
    private readonly Dictionary<GameObject, Stack<GameObject>> notePools = new Dictionary<GameObject, Stack<GameObject>>();

    private AudioSource audioSource;
    private Coroutine spawnCoroutine;
    private Coroutine musicEndCoroutine;
    private Coroutine startPlaybackCoroutine;
    private bool isPlaying;
    private bool isPaused;

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public float TrackTime => audioSource != null ? audioSource.time : 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 64;

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        ConfigureDeviceControllerTracking();
    }

    void Start()
    {
        PrepareTrack();
        ValidateReferences();
        HideRuntimeUI();
    }

    void Update()
    {
        UpdateProgressRing();
    }

    public void PlayTrack()
    {
        if (isPlaying || startPlaybackCoroutine != null)
        {
            Debug.LogWarning("Track is already playing.");
            return;
        }

        if (!PrepareTrack())
        {
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("RhythmPlayer is missing the player camera.");
            return;
        }

        startPlaybackCoroutine = StartCoroutine(StartPlaybackWhenReady());
    }

    public void StopTrack(bool notifyScoreManager)
    {
        StopTrackCoroutines();
        noteQueue.Clear();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        HideRuntimeUI();
        isPlaying = false;
        isPaused = false;

        if (notifyScoreManager)
        {
            ScoreManager.Instance?.EndGame();
        }
    }

    IEnumerator StartPlaybackWhenReady()
    {
        AudioClip clip = audioSource.clip;

        if (clip == null)
        {
            Debug.LogWarning("RhythmPlayer cannot start because the audio clip is missing.");
            startPlaybackCoroutine = null;
            yield break;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        while (clip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("RhythmPlayer audio clip failed to load: " + clip.name + " (" + clip.loadState + ")");
            startPlaybackCoroutine = null;
            yield break;
        }

        ShowRuntimeUI();

        if (progressRing != null)
        {
            progressRing.fillAmount = 0f;
        }

        if (AudioListener.volume <= 0.001f)
        {
            PauseMenuController.Instance?.EnsureAudibleVolume();
        }

        if (AudioListener.volume <= 0.001f)
        {
            AudioListener.volume = 1f;
            Debug.LogWarning("Master audio volume was 0. It has been reset so the track can be heard.");
        }

        isPlaying = true;
        isPaused = false;
        audioSource.time = 0f;
        audioSource.Play();

        if (!audioSource.isPlaying)
        {
            Debug.LogWarning("RhythmPlayer called Play, but AudioSource did not start. clip=" + clip.name
                + ", volume=" + audioSource.volume
                + ", listenerVolume=" + AudioListener.volume
                + ", enabled=" + audioSource.enabled);
            HideRuntimeUI();
            isPlaying = false;
            startPlaybackCoroutine = null;
            yield break;
        }

        spawnCoroutine = StartCoroutine(SpawnNotes());
        musicEndCoroutine = StartCoroutine(WaitForMusicEnd());
        startPlaybackCoroutine = null;
    }

    public void PauseTrack()
    {
        if (!isPlaying || isPaused) return;

        if (audioSource != null)
        {
            audioSource.Pause();
        }

        isPaused = true;
    }

    public void ResumeTrack()
    {
        if (!isPlaying || !isPaused) return;

        isPaused = false;

        if (audioSource != null)
        {
            audioSource.UnPause();
        }
    }

    bool PrepareTrack()
    {
        noteQueue.Clear();

        if (track == null)
        {
            Debug.LogWarning("RhythmPlayer is missing a track.");
            return false;
        }

        if (track.music == null)
        {
            Debug.LogWarning("Track is missing its music clip.");
            return false;
        }

        if (track.notes == null || track.notes.Length == 0)
        {
            Debug.LogWarning("Track has no note data.");
            return false;
        }

        audioSource.clip = track.music;
        if (track.music.loadState == AudioDataLoadState.Unloaded)
        {
            track.music.LoadAudioData();
        }

        List<NoteEvent> sortedNotes = new List<NoteEvent>(track.notes);
        sortedNotes.Sort((a, b) => a.time.CompareTo(b.time));

        foreach (NoteEvent note in sortedNotes)
        {
            noteQueue.Enqueue(note);
        }

        return true;
    }

    IEnumerator SpawnNotes()
    {
        while (noteQueue.Count > 0 && isPlaying)
        {
            if (isPaused)
            {
                yield return null;
                continue;
            }

            NoteEvent nextNote = noteQueue.Peek();

            if (audioSource.time >= nextNote.time - spawnLeadTime)
            {
                noteQueue.Dequeue();
                SpawnNote(nextNote);
            }
            else
            {
                yield return null;
            }
        }

        spawnCoroutine = null;
    }

    IEnumerator WaitForMusicEnd()
    {
        yield return null;

        while (audioSource != null && (audioSource.isPlaying || isPaused))
        {
            yield return null;
        }

        musicEndCoroutine = null;
        HideRuntimeUI();
        isPlaying = false;
        isPaused = false;

        ScoreManager.Instance?.EndGame();
    }

    void SpawnNote(NoteEvent note)
    {
        if (note.noteType == NoteType.Tap)
        {
            SpawnTapNote(note);
        }
        else if (note.noteType == NoteType.Hold)
        {
            SpawnHoldNote(note);
        }
    }

    void SpawnTapNote(NoteEvent note)
    {
        GameObject prefab = note.hand == HandType.Left ? leftOrbPrefab : rightOrbPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Tap note prefab is missing for hand: " + note.hand);
            return;
        }

        Vector3 hitLocalPos = GetPlayableLocalPosition(note.targetPosition);
        Vector3 hitPos = GetWorldPositionFromCamera(hitLocalPos);
        Vector3 spawnPos = GetSpawnPosition(hitLocalPos);
        Vector3 endPos = GetEndPosition(hitPos);

        GameObject orb = GetPooledObject(prefab, spawnPos, Quaternion.identity);
        NoteOrb orbScript = orb.GetComponent<NoteOrb>();

        if (orbScript == null)
        {
            Debug.LogWarning("Tap note prefab is missing NoteOrb.");
            ReturnToPool(prefab, orb);
            return;
        }

        orbScript.SetPoolReleaseHandler(() => ReturnToPool(prefab, orb));
        orbScript.Init(hitPos, endPos, spawnLeadTime, note.hand, audioSource, note.time);
    }

    void SpawnHoldNote(NoteEvent note)
    {
        GameObject prefab = note.hand == HandType.Left ? leftHoldPrefab : rightHoldPrefab;
        Transform handTransform = GetHandTransform(note.hand);

        if (prefab == null)
        {
            Debug.LogWarning("Hold note prefab is missing for hand: " + note.hand);
            return;
        }

        if (handTransform == null)
        {
            Debug.LogWarning("Hand transform is missing for hold note: " + note.hand);
            return;
        }

        if (note.endTime <= note.time)
        {
            Debug.LogWarning("Hold note end time must be greater than start time.");
            return;
        }

        Vector3 startLocalPos = GetPlayableLocalPosition(note.targetPosition);
        Vector3 endLocalPos = GetPlayableLocalPosition(note.endPosition);

        Vector3 startHitPos = GetWorldPositionFromCamera(startLocalPos);
        Vector3 endHitPos = GetWorldPositionFromCamera(endLocalPos);

        Vector3 startSpawnPos = GetSpawnPosition(startLocalPos);
        Vector3 endSpawnPos = GetSpawnPosition(endLocalPos);

        GameObject hold = GetPooledObject(prefab, Vector3.zero, Quaternion.identity);
        HoldNote holdScript = hold.GetComponent<HoldNote>();

        if (holdScript == null)
        {
            Debug.LogWarning("Hold note prefab is missing HoldNote.");
            ReturnToPool(prefab, hold);
            return;
        }

        holdScript.ApplyDifficulty(
            holdFollowRadius,
            holdFailToleranceTime,
            holdStartHitLateWindow,
            holdStartHitRadius,
            holdMagnetRadius,
            holdMagnetStrength
        );
        holdScript.SetPoolReleaseHandler(() => ReturnToPool(prefab, hold));
        holdScript.Init(
            startSpawnPos,
            endSpawnPos,
            startHitPos,
            endHitPos,
            spawnLeadTime,
            note.endTime - note.time,
            note.hand,
            handTransform,
            audioSource,
            note.time,
            note.endTime
        );
    }

    Vector3 GetPlayableLocalPosition(Vector2 localPosition)
    {
        return new Vector3(localPosition.x, localPosition.y, fixedHitDepth);
    }

    Vector3 GetWorldPositionFromCamera(Vector3 localPosition)
    {
        Vector3 camPos = playerCamera.position;
        Vector3 forward = GetFlatForward();
        Vector3 right = GetFlatRight();

        return camPos
            + right * localPosition.x
            + Vector3.up * localPosition.y
            + forward * localPosition.z;
    }

    Vector3 GetSpawnPosition(Vector3 localPosition)
    {
        Vector3 camPos = playerCamera.position;
        Vector3 forward = GetFlatForward();
        Vector3 right = GetFlatRight();

        return camPos
            + right * localPosition.x
            + Vector3.up * localPosition.y
            + forward * spawnForwardDistance;
    }

    Vector3 GetEndPosition(Vector3 hitPos)
    {
        return hitPos - GetFlatForward() * endBehindDistance;
    }

    Vector3 GetFlatForward()
    {
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = playerCamera.forward;
        }
        return forward.normalized;
    }

    Vector3 GetFlatRight()
    {
        Vector3 right = playerCamera.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
        {
            right = playerCamera.right;
        }
        return right.normalized;
    }

    void UpdateProgressRing()
    {
        if (!isPlaying || isPaused || audioSource == null || audioSource.clip == null) return;

        float progress = audioSource.clip.length > 0f ? audioSource.time / audioSource.clip.length : 0f;

        if (progressRing != null)
        {
            progressRing.fillAmount = progress;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.SetProgress(progress);
        }
    }

    void ShowRuntimeUI()
    {
        if (progressRingRoot != null)
        {
            progressRingRoot.SetActive(true);
            ConfigureProgressRingFollow();
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowGameplayUI();
        }
    }

    void HideRuntimeUI()
    {
        if (progressRingRoot != null)
        {
            progressRingRoot.SetActive(false);
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideGameplayUI();
        }
    }

    void ConfigureProgressRingFollow()
    {
        if (progressRingRoot == null) return;

        UIFollowCamera follow = progressRingRoot.GetComponent<UIFollowCamera>();
        if (follow == null)
        {
            follow = progressRingRoot.AddComponent<UIFollowCamera>();
        }

        follow.targetCamera = playerCamera;
        follow.cameraLocalOffset = progressRingCameraOffset;
        progressRingRoot.transform.localScale = Vector3.one * progressRingScale;
    }

    Transform GetHandTransform(HandType hand)
    {
        return hand == HandType.Left ? leftHand : rightHand;
    }

    void ConfigureDeviceControllerTracking()
    {
        if (!usePicoControllerTrackingOnDevice) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Transform leftParent = leftHand != null ? leftHand.parent : null;
        Transform rightParent = rightHand != null ? rightHand.parent : null;

        PicoControllerTrackingProxy leftProxy = PicoControllerTrackingProxy.Create(
            XRNode.LeftHand,
            "LeftHand",
            picoControllerColliderRadius,
            leftParent
        );

        PicoControllerTrackingProxy rightProxy = PicoControllerTrackingProxy.Create(
            XRNode.RightHand,
            "RightHand",
            picoControllerColliderRadius,
            rightParent
        );

        leftHand = leftProxy.transform;
        rightHand = rightProxy.transform;
#endif
    }

    GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!notePools.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            notePools.Add(prefab, pool);
        }

        GameObject instance = pool.Count > 0 ? pool.Pop() : Instantiate(prefab);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        if (!notePools.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            notePools.Add(prefab, pool);
        }

        instance.SetActive(false);
        pool.Push(instance);
    }

    void StopTrackCoroutines()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (musicEndCoroutine != null)
        {
            StopCoroutine(musicEndCoroutine);
            musicEndCoroutine = null;
        }

        if (startPlaybackCoroutine != null)
        {
            StopCoroutine(startPlaybackCoroutine);
            startPlaybackCoroutine = null;
        }
    }

    void ValidateReferences()
    {
        if (track == null) Debug.LogWarning("RhythmPlayer: track is not assigned.");
        if (leftOrbPrefab == null) Debug.LogWarning("RhythmPlayer: left tap note prefab is not assigned.");
        if (rightOrbPrefab == null) Debug.LogWarning("RhythmPlayer: right tap note prefab is not assigned.");
        if (leftHoldPrefab == null) Debug.LogWarning("RhythmPlayer: left hold note prefab is not assigned.");
        if (rightHoldPrefab == null) Debug.LogWarning("RhythmPlayer: right hold note prefab is not assigned.");
        if (leftHand == null) Debug.LogWarning("RhythmPlayer: left hand transform is not assigned.");
        if (rightHand == null) Debug.LogWarning("RhythmPlayer: right hand transform is not assigned.");
        if (playerCamera == null) Debug.LogWarning("RhythmPlayer: player camera is not assigned and no main camera was found.");
    }
}
