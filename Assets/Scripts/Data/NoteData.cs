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
}
