using UnityEngine;

[System.Serializable]
public class NoteEvent
{
    [Header("Timing")]
    public float time;
    public float endTime;

    [Header("Type")]
    public NoteType noteType;
    public HandType hand;

    [Header("Camera Local Position")]
    public Vector2 targetPosition;
    public Vector2 endPosition;
}

[System.Serializable]
public class TimelineMarker
{
    public float time;
    public Color color = Color.magenta;
}

[System.Serializable]
public class TutorialImageCue
{
    public float startTime;
    public float endTime = 2f;
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.25f;
}

public enum TutorialImageType
{
    ShortNotes_Yellow,
    ShortNotes_Blue,
    LongNote,
    Pause
}

public enum HandType
{
    Left,
    Right
}

public enum NoteType
{
    Tap,
    Hold
}

[CreateAssetMenu(fileName = "NewTrack", menuName = "Rhythm/Track")]
public class NoteData : ScriptableObject
{
    public AudioClip music;
    public NoteEvent[] notes;
    public TimelineMarker[] markers;

    [Header("Tutorial Images")]
    public TutorialImageCue[] shortNotesYellowCues;
    public TutorialImageCue[] shortNotesBlueCues;
    public TutorialImageCue[] longNoteCues;
    public TutorialImageCue[] pauseCues;
}
