using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Curved Hit Surface")]
    public bool useCurvedHitSurface = true;
    public float hitSurfaceHalfWidth = 0.75f;
    public float hitSurfaceForwardBulge = 0.45f;
    public float hitSurfaceCurvePower = 2f;

    [Header("Judgement Line")]
    public bool showJudgementLine = true;
    public float judgementLineLocalY = -0.4f;
    public float judgementLineHalfWidth = 0.7f;
    public int judgementLineSegments = 32;
    public float judgementLineThickness = 0.012f;
    public Color judgementLineColor = new Color(1f, 0.86f, 0.25f, 0.9f);
    public Material judgementLineMaterial;

    [Header("Music Progress UI")]
    public GameObject musicProgressRoot;
    public RectTransform musicProgressFill;
    public TextMeshProUGUI musicProgressTimeText;

    [Header("Tutorial Images")]
    public Graphic shortNotesYellowImage;
    public Graphic shortNotesBlueImage;
    public Graphic longNoteImage;
    public Graphic pauseImage;

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
    private LineRenderer judgementLine;
    private GameObject judgementLineObject;
    private bool hasLockedPlayArea;
    private Vector3 lockedPlayAreaOrigin;
    private Vector3 lockedPlayAreaForward;
    private Vector3 lockedPlayAreaRight;
    private float musicProgressFullWidth = -1f;
    private bool musicProgressFillPrepared;
    private Image musicProgressFillImage;

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
        EnsureJudgementLine();
        FindMusicProgressReferences();
        HideRuntimeUI();
    }

    void Update()
    {
        UpdateGameplayProgress();
        UpdateTutorialImages();
        UpdateJudgementLine();
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

        ResetMusicProgressUI();
        HideRuntimeUI();
        isPlaying = false;
        isPaused = false;
        hasLockedPlayArea = false;

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

        LockPlayAreaToCurrentCamera();
        ShowRuntimeUI();
        UpdateGameplayProgress();

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
            hasLockedPlayArea = false;
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
        HideTutorialImages();
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
        hasLockedPlayArea = false;

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
        return new Vector3(localPosition.x, localPosition.y, GetHitSurfaceDepth(localPosition.x));
    }

    float GetHitSurfaceDepth(float localX)
    {
        if (!useCurvedHitSurface)
        {
            return fixedHitDepth;
        }

        float halfWidth = Mathf.Max(0.01f, hitSurfaceHalfWidth);
        float normalizedDistanceFromCenter = Mathf.Clamp01(Mathf.Abs(localX) / halfWidth);
        float centerWeight = 1f - Mathf.Pow(normalizedDistanceFromCenter, Mathf.Max(0.01f, hitSurfaceCurvePower));
        return fixedHitDepth + hitSurfaceForwardBulge * centerWeight;
    }

    Vector3 GetWorldPositionFromCamera(Vector3 localPosition)
    {
        Vector3 origin = GetPlayAreaOrigin();
        Vector3 forward = GetPlayAreaForward();
        Vector3 right = GetPlayAreaRight();

        return origin
            + right * localPosition.x
            + Vector3.up * localPosition.y
            + forward * localPosition.z;
    }

    Vector3 GetSpawnPosition(Vector3 localPosition)
    {
        Vector3 origin = GetPlayAreaOrigin();
        Vector3 forward = GetPlayAreaForward();
        Vector3 right = GetPlayAreaRight();

        return origin
            + right * localPosition.x
            + Vector3.up * localPosition.y
            + forward * spawnForwardDistance;
    }

    Vector3 GetEndPosition(Vector3 hitPos)
    {
        return hitPos - GetPlayAreaForward() * endBehindDistance;
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

    void LockPlayAreaToCurrentCamera()
    {
        if (playerCamera == null) return;

        lockedPlayAreaOrigin = playerCamera.position;
        lockedPlayAreaForward = GetFlatForward();
        lockedPlayAreaRight = GetFlatRight();
        hasLockedPlayArea = true;
    }

    Vector3 GetPlayAreaOrigin()
    {
        if (hasLockedPlayArea)
        {
            return lockedPlayAreaOrigin;
        }

        return playerCamera != null ? playerCamera.position : transform.position;
    }

    Vector3 GetPlayAreaForward()
    {
        if (hasLockedPlayArea)
        {
            return lockedPlayAreaForward;
        }

        return playerCamera != null ? GetFlatForward() : transform.forward;
    }

    Vector3 GetPlayAreaRight()
    {
        if (hasLockedPlayArea)
        {
            return lockedPlayAreaRight;
        }

        return playerCamera != null ? GetFlatRight() : transform.right;
    }

    void UpdateGameplayProgress()
    {
        if (!isPlaying || audioSource == null || audioSource.clip == null) return;

        float progress = audioSource.clip.length > 0f ? audioSource.time / audioSource.clip.length : 0f;
        UpdateMusicProgressUI(progress, audioSource.time, audioSource.clip.length);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.SetMusicProgress(progress, audioSource.time, audioSource.clip.length);
        }
    }

    void ShowRuntimeUI()
    {
        ShowJudgementLine();
        ShowMusicProgressUI();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowGameplayUI();
        }
    }

    void HideRuntimeUI()
    {
        HideJudgementLine();
        HideMusicProgressUI();
        HideTutorialImages();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideGameplayUI();
        }
    }

    void FindMusicProgressReferences()
    {
        if (musicProgressRoot == null)
        {
            musicProgressRoot = FindGameObjectByExactOrTrimmedName("MusicProgressRoot");
        }

        if (musicProgressFill == null)
        {
            GameObject fill = FindGameObjectByExactOrTrimmedName("MusicProgressFill");
            if (fill != null)
            {
                musicProgressFill = fill.GetComponent<RectTransform>();
            }
        }

        if (musicProgressTimeText == null)
        {
            GameObject timeText = FindGameObjectByExactOrTrimmedName("MusicProgressTimeText");
            if (timeText != null)
            {
                musicProgressTimeText = timeText.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void FindTutorialImageReferences()
    {
        if (shortNotesYellowImage == null)
        {
            shortNotesYellowImage = FindGraphicByName("ShortNotes_Yellow");
        }

        if (shortNotesBlueImage == null)
        {
            shortNotesBlueImage = FindGraphicByName("ShortNotes_Blue");
        }

        if (longNoteImage == null)
        {
            longNoteImage = FindGraphicByName("LongNote");
        }

        if (pauseImage == null)
        {
            pauseImage = FindGraphicByName("Pause");
        }
    }

    Graphic FindGraphicByName(string objectName)
    {
        GameObject imageObject = FindGameObjectByExactOrTrimmedName(objectName);
        return imageObject != null ? imageObject.GetComponent<Graphic>() : null;
    }

    void UpdateTutorialImages()
    {
        if (!isPlaying || isPaused || track == null) return;

        FindTutorialImageReferences();

        float time = TrackTime;
        ApplyTutorialImageAlpha(shortNotesYellowImage, GetTutorialAlpha(track.shortNotesYellowCues, time));
        ApplyTutorialImageAlpha(shortNotesBlueImage, GetTutorialAlpha(track.shortNotesBlueCues, time));
        ApplyTutorialImageAlpha(longNoteImage, GetTutorialAlpha(track.longNoteCues, time));
        ApplyTutorialImageAlpha(pauseImage, GetTutorialAlpha(track.pauseCues, time));
    }

    float GetTutorialAlpha(TutorialImageCue[] cues, float time)
    {
        if (cues == null) return 0f;

        float alpha = 0f;
        foreach (TutorialImageCue cue in cues)
        {
            if (cue == null) continue;

            float start = Mathf.Min(cue.startTime, cue.endTime);
            float end = Mathf.Max(cue.startTime, cue.endTime);
            if (time < start || time > end) continue;

            float cueAlpha = 1f;
            if (cue.fadeInDuration > 0f && time < start + cue.fadeInDuration)
            {
                cueAlpha = Mathf.InverseLerp(start, start + cue.fadeInDuration, time);
            }

            if (cue.fadeOutDuration > 0f && time > end - cue.fadeOutDuration)
            {
                cueAlpha = Mathf.Min(cueAlpha, Mathf.InverseLerp(end, end - cue.fadeOutDuration, time));
            }

            alpha = Mathf.Max(alpha, cueAlpha);
        }

        return Mathf.Clamp01(alpha);
    }

    void ApplyTutorialImageAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;

        bool shouldBeActive = alpha > 0.001f;
        if (graphic.gameObject.activeSelf != shouldBeActive)
        {
            graphic.gameObject.SetActive(shouldBeActive);
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    void HideTutorialImages()
    {
        FindTutorialImageReferences();
        ApplyTutorialImageAlpha(shortNotesYellowImage, 0f);
        ApplyTutorialImageAlpha(shortNotesBlueImage, 0f);
        ApplyTutorialImageAlpha(longNoteImage, 0f);
        ApplyTutorialImageAlpha(pauseImage, 0f);
    }

    GameObject FindGameObjectByExactOrTrimmedName(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform foundTransform in transforms)
        {
            if (foundTransform.hideFlags != HideFlags.None) continue;

            string objectName = foundTransform.gameObject.name;
            if (objectName == targetName || objectName.Trim() == targetName)
            {
                return foundTransform.gameObject;
            }
        }

        return null;
    }

    void ShowMusicProgressUI()
    {
        FindMusicProgressReferences();

        if (musicProgressRoot != null)
        {
            musicProgressRoot.SetActive(true);
        }

        ResetMusicProgressUI();
    }

    void HideMusicProgressUI()
    {
        if (musicProgressRoot != null)
        {
            musicProgressRoot.SetActive(false);
        }
    }

    void ResetMusicProgressUI()
    {
        float totalTime = audioSource != null && audioSource.clip != null ? audioSource.clip.length : 0f;
        UpdateMusicProgressUI(0f, 0f, totalTime);
    }

    void UpdateMusicProgressUI(float progress, float currentTime, float totalTime)
    {
        FindMusicProgressReferences();

        if (musicProgressFill != null)
        {
            PrepareMusicProgressFill();
            float clampedProgress = Mathf.Clamp01(progress);

            if (musicProgressFillImage != null)
            {
                musicProgressFillImage.fillAmount = clampedProgress;
            }
            else
            {
                Vector2 size = musicProgressFill.sizeDelta;
                size.x = musicProgressFullWidth * clampedProgress;
                musicProgressFill.sizeDelta = size;
            }
        }

        if (musicProgressTimeText != null)
        {
            musicProgressTimeText.text = FormatTime(currentTime) + "/" + FormatTime(totalTime);
        }
    }

    void PrepareMusicProgressFill()
    {
        if (musicProgressFill == null || musicProgressFillPrepared) return;

        musicProgressFillImage = musicProgressFill.GetComponent<Image>();
        musicProgressFullWidth = musicProgressFill.sizeDelta.x;

        if (musicProgressFillImage != null)
        {
            musicProgressFillImage.type = Image.Type.Filled;
            musicProgressFillImage.fillMethod = Image.FillMethod.Horizontal;
            musicProgressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            musicProgressFillImage.fillAmount = 0f;
        }
        else
        {
            Vector2 pivot = musicProgressFill.pivot;
            if (!Mathf.Approximately(pivot.x, 0f))
            {
                Vector2 anchoredPosition = musicProgressFill.anchoredPosition;
                anchoredPosition.x -= pivot.x * musicProgressFullWidth;
                musicProgressFill.pivot = new Vector2(0f, pivot.y);
                musicProgressFill.anchoredPosition = anchoredPosition;
            }
        }

        musicProgressFillPrepared = true;
    }

    string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    void EnsureJudgementLine()
    {
        if (judgementLineObject != null) return;

        judgementLineObject = new GameObject("JudgementLine");
        judgementLineObject.transform.SetParent(transform, false);

        judgementLine = judgementLineObject.AddComponent<LineRenderer>();
        judgementLine.useWorldSpace = true;
        judgementLine.numCapVertices = 4;
        judgementLine.numCornerVertices = 4;
        judgementLine.alignment = LineAlignment.View;

        Material material = judgementLineMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                material = new Material(shader);
            }
        }

        if (material != null)
        {
            judgementLine.material = material;
        }
        ApplyJudgementLineStyle();
        HideJudgementLine();
    }

    void ApplyJudgementLineStyle()
    {
        if (judgementLine == null) return;

        judgementLine.startWidth = judgementLineThickness;
        judgementLine.endWidth = judgementLineThickness;
        judgementLine.startColor = judgementLineColor;
        judgementLine.endColor = judgementLineColor;
    }

    void ShowJudgementLine()
    {
        if (!showJudgementLine) return;

        EnsureJudgementLine();

        if (judgementLineObject != null)
        {
            judgementLineObject.SetActive(true);
            UpdateJudgementLine();
        }
    }

    void HideJudgementLine()
    {
        if (judgementLineObject != null)
        {
            judgementLineObject.SetActive(false);
        }
    }

    void UpdateJudgementLine()
    {
        if (judgementLineObject == null || !judgementLineObject.activeSelf) return;

        if (!showJudgementLine || playerCamera == null)
        {
            HideJudgementLine();
            return;
        }

        ApplyJudgementLineStyle();

        int segmentCount = Mathf.Max(1, judgementLineSegments);
        judgementLine.positionCount = segmentCount + 1;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            float localX = Mathf.Lerp(-judgementLineHalfWidth, judgementLineHalfWidth, t);
            float localZ = GetHitSurfaceDepth(localX);
            Vector3 point = GetWorldPositionFromCamera(new Vector3(localX, judgementLineLocalY, localZ));
            judgementLine.SetPosition(i, point);
        }
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
