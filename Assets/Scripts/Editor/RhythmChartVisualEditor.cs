using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class RhythmChartVisualEditor : EditorWindow
{
    private const float DefaultMinX = -0.6f;
    private const float DefaultMaxX = 0.6f;
    private const float DefaultMinY = -0.55f;
    private const float DefaultMaxY = 0.2f;
    private const float TimelineHeight = 280f;
    private const float MinHoldDuration = 0.05f;
    private const float TapConflictWindow = 0.001f;
    private const float MarkerHitWidth = 8f;
    private const float TutorialCueMinDuration = 0.05f;

    private static readonly Color LeftHandColor = new Color(1f, 0.86f, 0.16f);
    private static readonly Color RightHandColor = new Color(0.16f, 0.62f, 1f);
    private static readonly Color ConflictColor = new Color(1f, 0.12f, 0.12f);
    private static readonly Color WaveColor = new Color(1f, 0.48f, 0.02f);
    private static readonly Color DefaultMarkerColor = new Color(1f, 0.2f, 0.8f);
    private static readonly Color TutorialYellowColor = new Color(1f, 0.86f, 0.16f);
    private static readonly Color TutorialBlueColor = new Color(0.16f, 0.62f, 1f);
    private static readonly Color TutorialLongColor = new Color(0.35f, 1f, 0.45f);
    private static readonly Color TutorialPauseColor = new Color(1f, 0.45f, 0.18f);

    private enum DragMode
    {
        None,
        MoveNote,
        HoldEnd,
        Playhead,
        PositionStart,
        PositionEnd
    }

    private NoteData track;
    private int selectedIndex = -1;
    private int selectedMarkerIndex = -1;
    private TutorialImageType selectedTutorialImageType = TutorialImageType.ShortNotes_Yellow;
    private int selectedTutorialCueIndex = -1;
    private Texture2D waveformTexture;
    private AudioClip waveformClip;
    private int waveformWidth;
    private float timelineStart;
    private float timelineLength = 20f;
    private float playheadTime;
    private bool isPlaying;
    private bool autoScroll = true;
    private bool editHoldEndPosition;
    private float previewVolume = 0.75f;
    private Vector2 noteScroll;
    private DragMode dragMode;
    private float dragMouseStartTime;
    private float dragNoteStartTime;
    private float dragNoteEndTime;
    private string timingWarning;

    private float minX = DefaultMinX;
    private float maxX = DefaultMaxX;
    private float minY = DefaultMinY;
    private float maxY = DefaultMaxY;

    [MenuItem("Tools/Rhythm/Visual Chart Editor")]
    public static void Open()
    {
        GetWindow<RhythmChartVisualEditor>("Visual Chart");
    }

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        StopPreview();
        EditorApplication.update -= EditorUpdate;
        ResetWaveform();
    }

    private void OnGUI()
    {
        HandleKeyboardShortcuts();
        DrawHeader();

        if (track == null || track.music == null)
        {
            EditorGUILayout.HelpBox("Select a NoteData track with a music clip.", MessageType.Info);
            return;
        }

        EnsureNotesArray();
        EnsureMarkersArray();
        EnsureTutorialCueArrays();
        EnsureWaveform(track.music);

        DrawTransport(track.music);
        DrawTimeline(track.music);
        DrawConflictSummary();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawNoteList();
            DrawSelectedNoteEditor();
            DrawPositionPad();
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Event evt = Event.current;
        if (evt == null) return;

        if (EditorGUIUtility.editingTextField) return;

        if (evt.type == EventType.ExecuteCommand &&
            (evt.commandName == "Delete" || evt.commandName == "SoftDelete") &&
            GetSelectedNote() != null)
        {
            DeleteSelectedNote();
            evt.Use();
            Repaint();
            return;
        }

        if (evt.type != EventType.KeyDown) return;

        if (evt.keyCode == KeyCode.Space && track != null && track.music != null)
        {
            TogglePreviewPlayback(track.music);
            evt.Use();
            Repaint();
        }
        else if ((evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) && GetSelectedNote() != null)
        {
            DeleteSelectedNote();
            evt.Use();
            Repaint();
        }
        else if (evt.shift && evt.keyCode == KeyCode.X && GetSelectedNote() != null)
        {
            CreateMirroredOppositeHandNoteAtPlayhead();
            evt.Use();
            Repaint();
        }
        else if (evt.shift && evt.keyCode == KeyCode.T && track != null && track.music != null)
        {
            AddNote(NoteType.Tap, HandType.Right);
            evt.Use();
            Repaint();
        }
        else if (evt.shift && evt.keyCode == KeyCode.H && track != null && track.music != null)
        {
            AddNote(NoteType.Hold, HandType.Right);
            evt.Use();
            Repaint();
        }
        else if (evt.keyCode == KeyCode.T && track != null && track.music != null)
        {
            AddNote(NoteType.Tap, null);
            evt.Use();
            Repaint();
        }
        else if (evt.keyCode == KeyCode.H && track != null && track.music != null)
        {
            AddNote(NoteType.Hold, null);
            evt.Use();
            Repaint();
        }
        else if (evt.keyCode == KeyCode.M && track != null && track.music != null)
        {
            AddMarkerAtPlayhead();
            evt.Use();
            Repaint();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6f);
        EditorGUI.BeginChangeCheck();
        track = (NoteData)EditorGUILayout.ObjectField("NoteData Track", track, typeof(NoteData), false);
        if (EditorGUI.EndChangeCheck())
        {
            selectedIndex = track != null && track.notes != null && track.notes.Length > 0 ? 0 : -1;
            selectedMarkerIndex = -1;
            selectedTutorialCueIndex = -1;
            playheadTime = 0f;
            timelineStart = 0f;
            timingWarning = null;
            ResetWaveform();
            StopPreview();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Tap", GUILayout.Width(80f)))
            {
                AddNote(NoteType.Tap, null);
            }

            if (GUILayout.Button("Add Hold", GUILayout.Width(80f)))
            {
                AddNote(NoteType.Hold, null);
            }

            if (GUILayout.Button("Duplicate", GUILayout.Width(85f)))
            {
                DuplicateSelectedNote();
            }

            if (GUILayout.Button("Delete", GUILayout.Width(70f)))
            {
                DeleteSelectedNote();
            }

            if (GUILayout.Button("Sort By Time", GUILayout.Width(100f)))
            {
                SortAndSave();
            }

            if (GUILayout.Button("Add Marker", GUILayout.Width(90f)))
            {
                AddMarkerAtPlayhead();
            }

            EditorGUI.BeginDisabledGroup(HasAnyHandConflict());
            if (GUILayout.Button("Save Track", GUILayout.Width(90f)))
            {
                SaveTrack();
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    private void DrawTransport(AudioClip clip)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button(isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                TogglePreviewPlayback(clip);
            }

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(55f)))
            {
                StopPreview();
            }

            GUILayout.Label(FormatTime(playheadTime), GUILayout.Width(72f));
            EditorGUI.BeginChangeCheck();
            float newTime = GUILayout.HorizontalSlider(playheadTime, 0f, clip.length, GUILayout.MinWidth(180f));
            if (EditorGUI.EndChangeCheck())
            {
                SetPlayhead(clip, newTime, true);
            }

            GUILayout.Label(FormatTime(clip.length), GUILayout.Width(72f));
            GUILayout.Space(10f);
            GUILayout.Label("Zoom", GUILayout.Width(38f));
            timelineLength = GUILayout.HorizontalSlider(timelineLength, 3f, Mathf.Max(3f, clip.length), GUILayout.Width(180f));
            GUILayout.Label(timelineLength.ToString("0.0") + "s", GUILayout.Width(55f));
            autoScroll = GUILayout.Toggle(autoScroll, "Auto Scroll", EditorStyles.toolbarButton, GUILayout.Width(92f));
            GUILayout.Label("Volume", GUILayout.Width(48f));
            previewVolume = GUILayout.HorizontalSlider(previewVolume, 0f, 1f, GUILayout.Width(90f));
        }
    }

    private void DrawTimeline(AudioClip clip)
    {
        float maxStart = Mathf.Max(0f, clip.length - timelineLength);
        timelineStart = Mathf.Clamp(timelineStart, 0f, maxStart);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(FormatTime(timelineStart), GUILayout.Width(66f));
            EditorGUI.BeginChangeCheck();
            timelineStart = GUILayout.HorizontalScrollbar(timelineStart, timelineLength, 0f, clip.length);
            if (EditorGUI.EndChangeCheck())
            {
                timelineStart = Mathf.Clamp(timelineStart, 0f, maxStart);
            }
            GUILayout.Label(FormatTime(Mathf.Min(clip.length, timelineStart + timelineLength)), GUILayout.Width(66f));
        }

        Rect rect = GUILayoutUtility.GetRect(800f, TimelineHeight, GUILayout.ExpandWidth(true));
        DrawTimelineBackground(rect);
        DrawWaveform(rect, clip);
        DrawTimeGrid(rect);
        DrawTutorialCuesOnTimeline(rect);
        DrawTimelineMarkers(rect);
        DrawNotesOnTimeline(rect);
        DrawPlayhead(rect);
        HandleTimelineInput(rect, clip);
    }

    private void DrawTimelineBackground(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.02f, 0.02f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), Color.black);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.black);
    }

    private void DrawWaveform(Rect rect, AudioClip clip)
    {
        if (waveformTexture == null)
        {
            GUI.Label(rect, "Waveform unavailable. Set the AudioClip load type to Decompress On Load if this stays blank.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Rect uv = new Rect(
            Mathf.Clamp01(timelineStart / clip.length),
            0f,
            Mathf.Clamp01(timelineLength / clip.length),
            1f
        );
        GUI.DrawTextureWithTexCoords(rect, waveformTexture, uv);
    }

    private void DrawTimeGrid(Rect rect)
    {
        float interval = PickGridInterval(timelineLength);
        float first = Mathf.Ceil(timelineStart / interval) * interval;
        for (float time = first; time <= timelineStart + timelineLength; time += interval)
        {
            float x = TimeToX(time, rect);
            DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(0.85f, 0.85f, 0.85f, 0.42f), 1f);
            GUI.Label(new Rect(x + 3f, rect.yMin + 2f, 70f, 18f), FormatTime(time), EditorStyles.miniLabel);
        }

        float leftLaneY = rect.yMax - 72f;
        float rightLaneY = rect.yMax - 34f;
        DrawLine(new Vector2(rect.xMin, leftLaneY), new Vector2(rect.xMax, leftLaneY), new Color(1f, 0.86f, 0.16f, 0.35f), 1f);
        DrawLine(new Vector2(rect.xMin, rightLaneY), new Vector2(rect.xMax, rightLaneY), new Color(0.16f, 0.62f, 1f, 0.35f), 1f);
        GUI.Label(new Rect(rect.xMin + 6f, leftLaneY - 20f, 120f, 18f), "Left Hand", EditorStyles.miniBoldLabel);
        GUI.Label(new Rect(rect.xMin + 6f, rightLaneY - 20f, 120f, 18f), "Right Hand", EditorStyles.miniBoldLabel);
    }

    private void DrawNotesOnTimeline(Rect rect)
    {
        if (track.notes == null) return;

        for (int i = 0; i < track.notes.Length; i++)
        {
            NoteEvent note = track.notes[i];
            float start = note.time;
            float end = GetNoteEndTime(note);
            if (end < timelineStart || start > timelineStart + timelineLength) continue;

            bool selected = i == selectedIndex;
            bool conflict = NoteHasHandConflict(i);
            Color color = conflict ? ConflictColor : note.hand == HandType.Left ? LeftHandColor : RightHandColor;
            float laneY = GetLaneY(note.hand, rect);
            float startX = TimeToX(start, rect);

            if (note.noteType == NoteType.Hold)
            {
                float endX = TimeToX(note.endTime, rect);
                Rect segment = Rect.MinMaxRect(Mathf.Min(startX, endX), laneY - 8f, Mathf.Max(startX, endX), laneY + 8f);
                EditorGUI.DrawRect(segment, new Color(color.r, color.g, color.b, selected ? 0.9f : 0.62f));
                EditorGUI.DrawRect(new Rect(startX - 3f, laneY - 18f, 6f, 36f), color);
                EditorGUI.DrawRect(new Rect(endX - 3f, laneY - 15f, 6f, 30f), color);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(startX - 3f, laneY - 22f, 6f, 44f), color);
            }

            if (selected)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(2f, new Vector3(startX, rect.yMin), new Vector3(startX, rect.yMax));
                Handles.EndGUI();
            }
        }
    }

    private void DrawPlayhead(Rect rect)
    {
        if (playheadTime < timelineStart || playheadTime > timelineStart + timelineLength) return;

        float x = TimeToX(playheadTime, rect);
        DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(0.2f, 1f, 0.45f), 2f);
    }

    private void DrawTimelineMarkers(Rect rect)
    {
        if (track.markers == null) return;

        for (int i = 0; i < track.markers.Length; i++)
        {
            TimelineMarker marker = track.markers[i];
            if (marker.time < timelineStart || marker.time > timelineStart + timelineLength) continue;

            float x = TimeToX(marker.time, rect);
            Color color = marker.color;
            DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(color.r, color.g, color.b, 0.7f), selectedMarkerIndex == i ? 2.5f : 1.5f);

            Rect headRect = new Rect(x - 5f, rect.yMin + 4f, 10f, 10f);
            EditorGUI.DrawRect(headRect, color);

            if (selectedMarkerIndex == i)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(
                    2f,
                    new Vector3(headRect.xMin, headRect.yMin),
                    new Vector3(headRect.xMax, headRect.yMin),
                    new Vector3(headRect.xMax, headRect.yMax),
                    new Vector3(headRect.xMin, headRect.yMax),
                    new Vector3(headRect.xMin, headRect.yMin));
                Handles.EndGUI();
            }

            GUI.Label(new Rect(x + 6f, rect.yMin + 2f, 40f, 18f), $"M{i + 1}", EditorStyles.miniBoldLabel);
        }
    }

    private void DrawTutorialCuesOnTimeline(Rect rect)
    {
        EnsureTutorialCueArrays();

        DrawTutorialCueLane(rect, TutorialImageType.ShortNotes_Yellow, rect.yMin + 32f, "ShortNotes_Yellow", TutorialYellowColor);
        DrawTutorialCueLane(rect, TutorialImageType.ShortNotes_Blue, rect.yMin + 56f, "ShortNotes_Blue", TutorialBlueColor);
        DrawTutorialCueLane(rect, TutorialImageType.LongNote, rect.yMin + 80f, "LongNote", TutorialLongColor);
        DrawTutorialCueLane(rect, TutorialImageType.Pause, rect.yMin + 104f, "Pause", TutorialPauseColor);
    }

    private void DrawTutorialCueLane(Rect rect, TutorialImageType imageType, float laneY, string label, Color color)
    {
        Rect laneRect = new Rect(rect.xMin, laneY - 8f, rect.width, 16f);
        EditorGUI.DrawRect(laneRect, new Color(color.r, color.g, color.b, 0.08f));
        GUI.Label(new Rect(rect.xMin + 6f, laneY - 10f, 130f, 18f), label, EditorStyles.miniBoldLabel);

        TutorialImageCue[] cues = GetTutorialCueArray(imageType);
        for (int i = 0; i < cues.Length; i++)
        {
            TutorialImageCue cue = cues[i];
            float start = Mathf.Min(cue.startTime, cue.endTime);
            float end = Mathf.Max(cue.startTime + TutorialCueMinDuration, cue.endTime);
            if (end < timelineStart || start > timelineStart + timelineLength) continue;

            float startX = TimeToX(start, rect);
            float endX = TimeToX(end, rect);
            Rect cueRect = Rect.MinMaxRect(Mathf.Min(startX, endX), laneY - 7f, Mathf.Max(startX, endX), laneY + 7f);
            bool selected = selectedTutorialImageType == imageType && selectedTutorialCueIndex == i;
            EditorGUI.DrawRect(cueRect, new Color(color.r, color.g, color.b, selected ? 0.9f : 0.55f));

            DrawTutorialFadeHandle(rect, laneY, cue.startTime, cue.fadeInDuration, color, true);
            DrawTutorialFadeHandle(rect, laneY, cue.endTime - cue.fadeOutDuration, cue.fadeOutDuration, color, false);

            if (selected)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(
                    2f,
                    new Vector3(cueRect.xMin, cueRect.yMin),
                    new Vector3(cueRect.xMax, cueRect.yMin),
                    new Vector3(cueRect.xMax, cueRect.yMax),
                    new Vector3(cueRect.xMin, cueRect.yMax),
                    new Vector3(cueRect.xMin, cueRect.yMin));
                Handles.EndGUI();
            }
        }
    }

    private void DrawTutorialFadeHandle(Rect rect, float laneY, float fadeStartTime, float fadeDuration, Color color, bool fadeIn)
    {
        if (fadeDuration <= 0f) return;

        float fadeStartX = TimeToX(fadeStartTime, rect);
        float fadeEndX = TimeToX(fadeStartTime + fadeDuration, rect);
        Rect fadeRect = Rect.MinMaxRect(Mathf.Min(fadeStartX, fadeEndX), laneY - 7f, Mathf.Max(fadeStartX, fadeEndX), laneY + 7f);
        Color fadeColor = new Color(color.r, color.g, color.b, 0.28f);
        EditorGUI.DrawRect(fadeRect, fadeColor);
        GUI.Label(new Rect(fadeIn ? fadeRect.xMin + 2f : fadeRect.xMax - 18f, laneY - 9f, 24f, 16f), fadeIn ? "In" : "Out", EditorStyles.miniLabel);
    }

    private void HandleTimelineInput(Rect rect, AudioClip clip)
    {
        Event evt = Event.current;

        if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
        {
            if (evt.control)
            {
                float pivotTime = XToTime(evt.mousePosition.x, rect);
                float zoomFactor = Mathf.Exp(evt.delta.y * 0.08f);
                float newTimelineLength = Mathf.Clamp(timelineLength * zoomFactor, 3f, clip.length);
                float pivotNormalized = Mathf.InverseLerp(timelineStart, timelineStart + timelineLength, pivotTime);
                timelineLength = newTimelineLength;
                float zoomMaxStart = Mathf.Max(0f, clip.length - timelineLength);
                timelineStart = Mathf.Clamp(pivotTime - timelineLength * pivotNormalized, 0f, zoomMaxStart);
                evt.Use();
                Repaint();
                return;
            }

            float scrollDelta = Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y) ? evt.delta.x : evt.delta.y;
            float direction = Mathf.Sign(scrollDelta);
            float amount = Mathf.Abs(scrollDelta) * timelineLength * 0.035f;
            float maxStart = Mathf.Max(0f, clip.length - timelineLength);
            timelineStart = Mathf.Clamp(timelineStart + direction * amount, 0f, maxStart);
            evt.Use();
            Repaint();
            return;
        }

        if (evt.type == EventType.MouseUp)
        {
            dragMode = DragMode.None;
            SaveTrack();
            return;
        }

        if (!rect.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            int tutorialHitIndex = FindTutorialCueAtPosition(evt.mousePosition, rect, out TutorialImageType tutorialHitType);
            if (tutorialHitIndex >= 0)
            {
                selectedTutorialImageType = tutorialHitType;
                selectedTutorialCueIndex = tutorialHitIndex;
                selectedIndex = -1;
                selectedMarkerIndex = -1;
                GUI.FocusControl(null);
                Focus();
                SetPlayhead(clip, GetTutorialCueArray(tutorialHitType)[tutorialHitIndex].startTime, true);
                evt.Use();
                Repaint();
                return;
            }

            int markerHitIndex = FindMarkerAtPosition(evt.mousePosition, rect);
            if (markerHitIndex >= 0)
            {
                selectedMarkerIndex = markerHitIndex;
                selectedIndex = -1;
                selectedTutorialCueIndex = -1;
                GUI.FocusControl(null);
                Focus();
                SetPlayhead(clip, track.markers[markerHitIndex].time, true);
                evt.Use();
                Repaint();
                return;
            }

            int hitIndex = FindNoteAtPosition(evt.mousePosition, rect, out DragMode noteDragMode);
            if (hitIndex >= 0)
            {
                selectedIndex = evt.alt ? DuplicateNoteForTimelineDrag(hitIndex) : hitIndex;
                selectedMarkerIndex = -1;
                selectedTutorialCueIndex = -1;
                dragMode = noteDragMode;
                NoteEvent note = track.notes[selectedIndex];
                GUI.FocusControl(null);
                Focus();
                dragMouseStartTime = XToTime(evt.mousePosition.x, rect);
                dragNoteStartTime = note.time;
                dragNoteEndTime = GetNoteEndTime(note);
                SetPlayhead(clip, note.time, true);
                evt.Use();
                Repaint();
                return;
            }

            dragMode = DragMode.Playhead;
            SetPlayhead(clip, XToTime(evt.mousePosition.x, rect), true);
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && evt.button == 0)
        {
            float currentTime = XToTime(evt.mousePosition.x, rect);
            if (dragMode == DragMode.MoveNote && selectedIndex >= 0)
            {
                float delta = currentTime - dragMouseStartTime;
                MoveSelectedNoteTo(dragNoteStartTime + delta, dragNoteEndTime + delta);
                evt.Use();
            }
            else if (dragMode == DragMode.HoldEnd && selectedIndex >= 0)
            {
                MoveSelectedHoldEndTo(currentTime);
                evt.Use();
            }
            else
            {
                SetPlayhead(clip, currentTime, true);
                evt.Use();
            }
        }
    }

    private void DrawConflictSummary()
    {
        if (!string.IsNullOrEmpty(timingWarning))
        {
            EditorGUILayout.HelpBox(timingWarning, MessageType.Warning);
        }

        string conflictMessage = GetFirstConflictMessage();
        if (!string.IsNullOrEmpty(conflictMessage))
        {
            EditorGUILayout.HelpBox(conflictMessage, MessageType.Error);
        }
    }

    private void DrawNoteList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
        {
            EditorGUILayout.LabelField("Notes", EditorStyles.boldLabel);
            noteScroll = EditorGUILayout.BeginScrollView(noteScroll);
            for (int i = 0; i < track.notes.Length; i++)
            {
                NoteEvent note = track.notes[i];
                string label = $"{i + 1:00}  {note.time:0.###}s  {note.hand}  {note.noteType}";
                GUIStyle style = i == selectedIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                GUI.color = NoteHasHandConflict(i) ? ConflictColor : Color.white;
                if (GUILayout.Button(label, style))
                {
                    selectedIndex = i;
                    selectedMarkerIndex = -1;
                    selectedTutorialCueIndex = -1;
                    editHoldEndPosition = false;
                    SetPlayhead(track.music, note.time, true);
                    CenterTimelineOn(note.time);
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Markers", EditorStyles.boldLabel);
            for (int i = 0; i < track.markers.Length; i++)
            {
                TimelineMarker marker = track.markers[i];
                GUI.color = marker.color;
                if (GUILayout.Button($"M{i + 1:00}  {marker.time:0.###}s", i == selectedMarkerIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton))
                {
                    selectedMarkerIndex = i;
                    selectedIndex = -1;
                    selectedTutorialCueIndex = -1;
                    SetPlayhead(track.music, marker.time, true);
                    CenterTimelineOn(marker.time);
                }
            }
            GUI.color = Color.white;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tutorial Images", EditorStyles.boldLabel);
            DrawTutorialCueList(TutorialImageType.ShortNotes_Yellow, "ShortNotes_Yellow", TutorialYellowColor);
            DrawTutorialCueList(TutorialImageType.ShortNotes_Blue, "ShortNotes_Blue", TutorialBlueColor);
            DrawTutorialCueList(TutorialImageType.LongNote, "LongNote", TutorialLongColor);
            DrawTutorialCueList(TutorialImageType.Pause, "Pause", TutorialPauseColor);
        }
    }

    private void DrawTutorialCueList(TutorialImageType imageType, string label, Color color)
    {
        TutorialImageCue[] cues = GetTutorialCueArray(imageType);
        GUI.color = color;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24f)))
            {
                AddTutorialCue(imageType);
            }
        }

        for (int i = 0; i < cues.Length; i++)
        {
            TutorialImageCue cue = cues[i];
            bool selected = selectedTutorialImageType == imageType && selectedTutorialCueIndex == i;
            string cueLabel = $"{i + 1:00}  {cue.startTime:0.###}-{cue.endTime:0.###}s";
            if (GUILayout.Button(cueLabel, selected ? EditorStyles.toolbarButton : EditorStyles.miniButton))
            {
                selectedTutorialImageType = imageType;
                selectedTutorialCueIndex = i;
                selectedIndex = -1;
                selectedMarkerIndex = -1;
                SetPlayhead(track.music, cue.startTime, true);
                CenterTimelineOn(cue.startTime);
            }
        }

        GUI.color = Color.white;
    }

    private void DrawSelectedNoteEditor()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(290f)))
        {
            EditorGUILayout.LabelField("Selected Note", EditorStyles.boldLabel);
            NoteEvent note = GetSelectedNote();
            if (note == null)
            {
                EditorGUILayout.HelpBox("Select a note in the waveform or note list.", MessageType.Info);
                DrawPadRangeFields();
                DrawSelectedMarkerEditor();
                DrawSelectedTutorialCueEditor();
                return;
            }

            EditorGUI.BeginChangeCheck();
            float time = EditorGUILayout.FloatField("Hit Time", note.time);
            NoteType noteType = (NoteType)EditorGUILayout.EnumPopup("Type", note.noteType);
            HandType hand = (HandType)EditorGUILayout.EnumPopup("Hand", note.hand);
            float endTime = note.endTime;
            if (noteType == NoteType.Hold)
            {
                endTime = EditorGUILayout.FloatField("End Time", Mathf.Max(note.time + MinHoldDuration, note.endTime));
                editHoldEndPosition = GUILayout.Toggle(editHoldEndPosition, "Edit Hold End Position", "Button");
            }
            else
            {
                editHoldEndPosition = false;
            }

            Vector2 targetPosition = EditorGUILayout.Vector2Field("Hit Position", note.targetPosition);
            Vector2 endPosition = note.endPosition;
            if (noteType == NoteType.Hold)
            {
                endPosition = EditorGUILayout.Vector2Field("End Position", note.endPosition);
            }

            if (EditorGUI.EndChangeCheck())
            {
                ApplySelectedNoteEdit(time, noteType, hand, endTime, targetPosition, endPosition);
            }

            if (GUILayout.Button("Center Timeline On Note"))
            {
                CenterTimelineOn(note.time);
            }

            DrawPadRangeFields();
            DrawSelectedMarkerEditor();
            DrawSelectedTutorialCueEditor();
        }
    }

    private void DrawSelectedTutorialCueEditor()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Selected Tutorial Image", EditorStyles.boldLabel);
        TutorialImageCue cue = GetSelectedTutorialCue();
        if (cue == null)
        {
            EditorGUILayout.HelpBox("Select a tutorial image segment in the waveform, or press + in the Tutorial Images list.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        TutorialImageType imageType = (TutorialImageType)EditorGUILayout.EnumPopup("Image", selectedTutorialImageType);
        float startTime = EditorGUILayout.FloatField("Start Time", cue.startTime);
        float endTime = EditorGUILayout.FloatField("End Time", Mathf.Max(cue.startTime + TutorialCueMinDuration, cue.endTime));
        float fadeIn = EditorGUILayout.FloatField("Fade In", Mathf.Max(0f, cue.fadeInDuration));
        float fadeOut = EditorGUILayout.FloatField("Fade Out", Mathf.Max(0f, cue.fadeOutDuration));
        if (EditorGUI.EndChangeCheck())
        {
            ApplySelectedTutorialCueEdit(imageType, startTime, endTime, fadeIn, fadeOut);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Center Timeline On Segment"))
            {
                CenterTimelineOn(cue.startTime);
                SetPlayhead(track.music, cue.startTime, true);
            }

            if (GUILayout.Button("Delete Segment"))
            {
                DeleteSelectedTutorialCue();
            }
        }
    }

    private void DrawSelectedMarkerEditor()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Selected Marker", EditorStyles.boldLabel);
        TimelineMarker marker = GetSelectedMarker();
        if (marker == null)
        {
            EditorGUILayout.HelpBox("Select a marker in the waveform or marker list.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        float time = EditorGUILayout.FloatField("Time", marker.time);
        Color color = EditorGUILayout.ColorField("Color", marker.color);
        if (EditorGUI.EndChangeCheck())
        {
            RecordTrackChange();
            marker.time = Mathf.Clamp(time, 0f, track.music != null ? track.music.length : time);
            marker.color = color;
            SaveTrack();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Center Timeline On Marker"))
            {
                CenterTimelineOn(marker.time);
                SetPlayhead(track.music, marker.time, true);
            }

            if (GUILayout.Button("Delete Marker"))
            {
                DeleteSelectedMarker();
            }
        }
    }

    private void DrawPadRangeFields()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Position Pad Range", EditorStyles.boldLabel);
        minX = EditorGUILayout.FloatField("Min X", minX);
        maxX = EditorGUILayout.FloatField("Max X", maxX);
        minY = EditorGUILayout.FloatField("Min Y", minY);
        maxY = EditorGUILayout.FloatField("Max Y", maxY);

        if (GUILayout.Button("Reset Pad Range"))
        {
            minX = DefaultMinX;
            maxX = DefaultMaxX;
            minY = DefaultMinY;
            maxY = DefaultMaxY;
        }
    }

    private void DrawPositionPad()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Note Position", EditorStyles.boldLabel);
            NoteEvent note = GetSelectedNote();
            string label = note != null && note.noteType == NoteType.Hold && editHoldEndPosition
                ? "Editing hold end position"
                : "Editing hit position";
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            Rect padRect = GUILayoutUtility.GetRect(360f, 360f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            DrawPadBackground(padRect);
            DrawVisibleNoteMarkers(padRect);
            DrawPlaybackNoteMarker(padRect);
            HandlePadInput(padRect);
        }
    }

    private void DrawPadBackground(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        Vector2 origin = ChartToGui(Vector2.zero, rect);

        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 4f);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(0.2f, 0.2f, 0.2f), 1f);
            DrawLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), new Color(0.2f, 0.2f, 0.2f), 1f);
        }

        DrawLine(new Vector2(rect.xMin, origin.y), new Vector2(rect.xMax, origin.y), new Color(0.75f, 0.75f, 0.75f), 1.5f);
        DrawLine(new Vector2(origin.x, rect.yMin), new Vector2(origin.x, rect.yMax), new Color(0.75f, 0.75f, 0.75f), 1.5f);
        DrawAxisArrow(new Vector2(rect.xMax - 26f, origin.y), new Vector2(rect.xMax - 8f, origin.y), "X+");
        DrawAxisArrow(new Vector2(origin.x, rect.yMin + 26f), new Vector2(origin.x, rect.yMin + 8f), "Y+");
        DrawCameraOrigin(origin);
        DrawAxisTicks(rect, origin);

        Handles.BeginGUI();
        Handles.color = new Color(1f, 0.86f, 0.25f, 0.9f);
        Handles.DrawAAPolyLine(2f, new Vector3(rect.xMin, rect.yMax - 48f), new Vector3(rect.xMax, rect.yMax - 48f));
        Handles.EndGUI();
    }

    private void DrawCameraOrigin(Vector2 origin)
    {
        Rect cameraRect = new Rect(origin.x - 8f, origin.y - 8f, 16f, 16f);
        EditorGUI.DrawRect(cameraRect, new Color(0.95f, 0.95f, 0.95f));
        GUI.Label(new Rect(origin.x + 10f, origin.y - 10f, 120f, 20f), "Camera (0,0)", EditorStyles.miniBoldLabel);
    }

    private void DrawAxisTicks(Rect rect, Vector2 origin)
    {
        float xStep = 0.2f;
        for (float x = Mathf.Ceil(minX / xStep) * xStep; x <= maxX + 0.001f; x += xStep)
        {
            Vector2 tick = ChartToGui(new Vector2(x, 0f), rect);
            DrawLine(new Vector2(tick.x, origin.y - 4f), new Vector2(tick.x, origin.y + 4f), Color.gray, 1f);
            if (!Mathf.Approximately(x, 0f))
            {
                GUI.Label(new Rect(tick.x - 18f, origin.y + 5f, 40f, 16f), x.ToString("0.0"), EditorStyles.miniLabel);
            }
        }

        float yStep = 0.2f;
        for (float y = Mathf.Ceil(minY / yStep) * yStep; y <= maxY + 0.001f; y += yStep)
        {
            Vector2 tick = ChartToGui(new Vector2(0f, y), rect);
            DrawLine(new Vector2(origin.x - 4f, tick.y), new Vector2(origin.x + 4f, tick.y), Color.gray, 1f);
            if (!Mathf.Approximately(y, 0f))
            {
                GUI.Label(new Rect(origin.x + 6f, tick.y - 8f, 40f, 16f), y.ToString("0.0"), EditorStyles.miniLabel);
            }
        }
    }

    private void DrawAxisArrow(Vector2 from, Vector2 to, string label)
    {
        DrawLine(from, to, Color.white, 2f);
        Vector2 direction = (to - from).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        DrawLine(to, to - direction * 6f + normal * 4f, Color.white, 2f);
        DrawLine(to, to - direction * 6f - normal * 4f, Color.white, 2f);
        GUI.Label(new Rect(to.x + 4f, to.y - 10f, 32f, 18f), label, EditorStyles.miniBoldLabel);
    }

    private void DrawVisibleNoteMarkers(Rect rect)
    {
        if (track.notes == null) return;

        float start = Mathf.Max(0f, playheadTime - 1.5f);
        float end = playheadTime + 1.5f;
        for (int i = 0; i < track.notes.Length; i++)
        {
            NoteEvent note = track.notes[i];
            if (note.noteType == NoteType.Hold)
            {
                if (note.time > end || playheadTime > note.endTime) continue;
            }
            else if (note.time < playheadTime || note.time > end)
            {
                continue;
            }

            bool selected = i == selectedIndex;
            Color color = NoteHasHandConflict(i) ? ConflictColor : note.hand == HandType.Left ? LeftHandColor : RightHandColor;
            Vector2 startGui = ChartToGui(note.targetPosition, rect);

            if (note.noteType == NoteType.Hold)
            {
                Vector2 endGui = ChartToGui(note.endPosition, rect);
                DrawLine(startGui, endGui, color, selected ? 3f : 1.5f);
                DrawMarker(endGui, color, selected && editHoldEndPosition, "E");
            }

            DrawMarker(startGui, color, selected && !editHoldEndPosition, note.noteType == NoteType.Hold ? "S" : "T");
        }
    }

    private void DrawPlaybackNoteMarker(Rect rect)
    {
        DrawPlaybackNoteMarkerForHand(rect, HandType.Left);
        DrawPlaybackNoteMarkerForHand(rect, HandType.Right);
    }

    private void DrawPlaybackNoteMarkerForHand(Rect rect, HandType hand)
    {
        NoteEvent note = FindCurrentPlaybackNote(hand);
        if (note == null) return;

        Color color = hand == HandType.Left ? LeftHandColor : RightHandColor;
        Vector2 position = note.targetPosition;
        if (note.noteType == NoteType.Hold && note.endTime > note.time && playheadTime >= note.time)
        {
            float t = Mathf.InverseLerp(note.time, note.endTime, playheadTime);
            position = Vector2.Lerp(note.targetPosition, note.endPosition, t);
        }

        DrawMarker(ChartToGui(position, rect), color, true, "Now");
    }

    private void HandlePadInput(Rect rect)
    {
        NoteEvent note = GetSelectedNote();
        if (note == null) return;

        Event evt = Event.current;

        if (evt.type == EventType.MouseUp && (dragMode == DragMode.PositionStart || dragMode == DragMode.PositionEnd))
        {
            dragMode = DragMode.None;
            SaveTrack();
            evt.Use();
            return;
        }

        if (!rect.Contains(evt.mousePosition)) return;
        if (evt.button != 0) return;

        if (evt.type == EventType.MouseDown)
        {
            dragMode = GetPositionDragMode(note, evt.mousePosition, rect);
            if (dragMode == DragMode.None)
            {
                dragMode = note.noteType == NoteType.Hold && editHoldEndPosition ? DragMode.PositionEnd : DragMode.PositionStart;
            }
        }
        else if (evt.type != EventType.MouseDrag)
        {
            return;
        }

        RecordTrackChange();
        Vector2 chartPosition = ClampToPad(GuiToChart(evt.mousePosition, rect));
        if (note.noteType == NoteType.Hold && dragMode == DragMode.PositionEnd)
        {
            note.endPosition = chartPosition;
        }
        else
        {
            note.targetPosition = chartPosition;
        }

        evt.Use();
        Repaint();
    }

    private DragMode GetPositionDragMode(NoteEvent note, Vector2 mousePosition, Rect rect)
    {
        Vector2 startGui = ChartToGui(note.targetPosition, rect);
        float startDistance = Vector2.Distance(mousePosition, startGui);

        if (note.noteType == NoteType.Hold)
        {
            Vector2 endGui = ChartToGui(note.endPosition, rect);
            float endDistance = Vector2.Distance(mousePosition, endGui);
            if (endDistance <= 18f && endDistance <= startDistance) return DragMode.PositionEnd;
        }

        if (startDistance <= 18f) return DragMode.PositionStart;
        return DragMode.None;
    }

    private void ApplySelectedNoteEdit(
        float time,
        NoteType noteType,
        HandType hand,
        float endTime,
        Vector2 targetPosition,
        Vector2 endPosition
    )
    {
        NoteEvent note = GetSelectedNote();
        if (note == null) return;

        float maxStart = noteType == NoteType.Hold ? Mathf.Max(0f, track.music.length - MinHoldDuration) : track.music.length;
        float newStart = Mathf.Clamp(time, 0f, maxStart);
        float newEnd = noteType == NoteType.Hold
            ? Mathf.Clamp(Mathf.Max(newStart + MinHoldDuration, endTime), newStart + MinHoldDuration, track.music.length)
            : 0f;

        if (CandidateHasHandConflict(selectedIndex, hand, noteType, newStart, newEnd))
        {
            timingWarning = "Edit rejected: one hand cannot touch two notes at the same time.";
            return;
        }

        RecordTrackChange();
        note.time = newStart;
        note.noteType = noteType;
        note.hand = hand;
        note.targetPosition = ClampToPad(targetPosition);

        if (noteType == NoteType.Hold)
        {
            note.endTime = newEnd;
            note.endPosition = ClampToPad(endPosition);
        }
        else
        {
            note.endTime = 0f;
            note.endPosition = Vector2.zero;
        }

        timingWarning = null;
        SaveTrack();
    }

    private void MoveSelectedNoteTo(float startTime, float endTime)
    {
        NoteEvent note = GetSelectedNote();
        if (note == null) return;

        float duration = note.noteType == NoteType.Hold ? Mathf.Max(MinHoldDuration, dragNoteEndTime - dragNoteStartTime) : 0f;
        float clampedStart = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, track.music.length - duration));
        float clampedEnd = note.noteType == NoteType.Hold ? clampedStart + duration : 0f;

        if (CandidateHasHandConflict(selectedIndex, note.hand, note.noteType, clampedStart, clampedEnd))
        {
            timingWarning = "Move rejected: this would overlap another " + note.hand + " note.";
            return;
        }

        RecordTrackChange();
        note.time = clampedStart;
        if (note.noteType == NoteType.Hold)
        {
            note.endTime = clampedEnd;
        }

        playheadTime = clampedStart;
        timingWarning = null;
        Repaint();
    }

    private void MoveSelectedHoldEndTo(float endTime)
    {
        NoteEvent note = GetSelectedNote();
        if (note == null || note.noteType != NoteType.Hold) return;

        float clampedEnd = Mathf.Clamp(Mathf.Max(note.time + MinHoldDuration, endTime), note.time + MinHoldDuration, track.music.length);
        if (CandidateHasHandConflict(selectedIndex, note.hand, note.noteType, note.time, clampedEnd))
        {
            timingWarning = "Hold edit rejected: this would overlap another " + note.hand + " note.";
            return;
        }

        RecordTrackChange();
        note.endTime = clampedEnd;
        timingWarning = null;
        Repaint();
    }

    private void AddNote(NoteType noteType, HandType? requestedHand)
    {
        EnsureNotesArray();

        float clipLength = track.music != null ? track.music.length : 999f;
        float time = noteType == NoteType.Hold
            ? Mathf.Clamp(playheadTime, 0f, Mathf.Max(0f, clipLength - MinHoldDuration))
            : Mathf.Clamp(playheadTime, 0f, clipLength);
        float endTime = noteType == NoteType.Hold ? Mathf.Min(clipLength, time + 1f) : 0f;
        HandType hand = requestedHand ?? FindAvailableHandAtTime(time, noteType, endTime);
        if (CandidateHasHandConflict(-1, hand, noteType, time, endTime))
        {
            timingWarning = "Cannot add " + hand + " note here: that hand already has a note at this time.";
            return;
        }

        RecordTrackChange();
        NoteEvent note = new NoteEvent
        {
            time = time,
            endTime = endTime,
            noteType = noteType,
            hand = hand,
            targetPosition = hand == HandType.Left ? new Vector2(-0.35f, -0.35f) : new Vector2(0.35f, -0.35f),
            endPosition = noteType == NoteType.Hold
                ? (hand == HandType.Left ? new Vector2(-0.1f, -0.25f) : new Vector2(0.1f, -0.25f))
                : Vector2.zero
        };

        Array.Resize(ref track.notes, track.notes.Length + 1);
        track.notes[track.notes.Length - 1] = note;
        selectedIndex = track.notes.Length - 1;
        timingWarning = null;
        SaveTrack();
    }

    private void AddMarkerAtPlayhead()
    {
        if (track == null || track.music == null) return;

        EnsureMarkersArray();
        RecordTrackChange();

        TimelineMarker marker = new TimelineMarker
        {
            time = Mathf.Clamp(playheadTime, 0f, track.music.length),
            color = DefaultMarkerColor
        };

        Array.Resize(ref track.markers, track.markers.Length + 1);
        track.markers[track.markers.Length - 1] = marker;
        selectedMarkerIndex = track.markers.Length - 1;
        selectedIndex = -1;
        SaveTrack();
    }

    private void AddTutorialCue(TutorialImageType imageType)
    {
        if (track == null || track.music == null) return;

        EnsureTutorialCueArrays();
        RecordTrackChange();

        TutorialImageCue[] cues = GetTutorialCueArray(imageType);
        float startTime = Mathf.Clamp(playheadTime, 0f, track.music.length);
        TutorialImageCue cue = new TutorialImageCue
        {
            startTime = startTime,
            endTime = Mathf.Min(track.music.length, startTime + 2f),
            fadeInDuration = 0.25f,
            fadeOutDuration = 0.25f
        };

        Array.Resize(ref cues, cues.Length + 1);
        cues[cues.Length - 1] = cue;
        SetTutorialCueArray(imageType, cues);
        selectedTutorialImageType = imageType;
        selectedTutorialCueIndex = cues.Length - 1;
        selectedIndex = -1;
        selectedMarkerIndex = -1;
        SaveTrack();
    }

    private void ApplySelectedTutorialCueEdit(TutorialImageType newImageType, float startTime, float endTime, float fadeInDuration, float fadeOutDuration)
    {
        TutorialImageCue cue = GetSelectedTutorialCue();
        if (cue == null || track == null || track.music == null) return;

        float clipLength = track.music.length;
        float clampedStart = Mathf.Clamp(startTime, 0f, clipLength);
        float clampedEnd = Mathf.Clamp(Mathf.Max(clampedStart + TutorialCueMinDuration, endTime), 0f, clipLength);
        float maxFade = Mathf.Max(0f, clampedEnd - clampedStart);

        RecordTrackChange();

        if (newImageType != selectedTutorialImageType)
        {
            TutorialImageCue movedCue = new TutorialImageCue
            {
                startTime = clampedStart,
                endTime = clampedEnd,
                fadeInDuration = Mathf.Clamp(fadeInDuration, 0f, maxFade),
                fadeOutDuration = Mathf.Clamp(fadeOutDuration, 0f, maxFade)
            };
            RemoveTutorialCueAt(selectedTutorialImageType, selectedTutorialCueIndex);
            TutorialImageCue[] destination = GetTutorialCueArray(newImageType);
            Array.Resize(ref destination, destination.Length + 1);
            destination[destination.Length - 1] = movedCue;
            SetTutorialCueArray(newImageType, destination);
            selectedTutorialImageType = newImageType;
            selectedTutorialCueIndex = destination.Length - 1;
        }
        else
        {
            cue.startTime = clampedStart;
            cue.endTime = clampedEnd;
            cue.fadeInDuration = Mathf.Clamp(fadeInDuration, 0f, maxFade);
            cue.fadeOutDuration = Mathf.Clamp(fadeOutDuration, 0f, maxFade);
        }

        SaveTrack();
    }

    private void DuplicateSelectedNote()
    {
        NoteEvent source = GetSelectedNote();
        if (source == null) return;

        float offset = 0.5f;
        float newStart = Mathf.Clamp(source.time + offset, 0f, track.music.length);
        float newEnd = source.noteType == NoteType.Hold ? Mathf.Min(track.music.length, source.endTime + offset) : 0f;

        HandType hand = FindAvailableHandAtTime(newStart, source.noteType, newEnd);
        if (CandidateHasHandConflict(-1, hand, source.noteType, newStart, newEnd))
        {
            timingWarning = "Cannot duplicate note here: both hands already have notes at this time.";
            return;
        }

        RecordTrackChange();
        NoteEvent copy = new NoteEvent
        {
            time = newStart,
            endTime = newEnd,
            noteType = source.noteType,
            hand = hand,
            targetPosition = source.targetPosition,
            endPosition = source.endPosition
        };

        Array.Resize(ref track.notes, track.notes.Length + 1);
        track.notes[track.notes.Length - 1] = copy;
        selectedIndex = track.notes.Length - 1;
        timingWarning = null;
        SaveTrack();
    }

    private int DuplicateNoteForTimelineDrag(int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= track.notes.Length) return sourceIndex;

        NoteEvent source = track.notes[sourceIndex];
        RecordTrackChange();
        NoteEvent copy = new NoteEvent
        {
            time = source.time,
            endTime = source.endTime,
            noteType = source.noteType,
            hand = source.hand,
            targetPosition = source.targetPosition,
            endPosition = source.endPosition
        };

        Array.Resize(ref track.notes, track.notes.Length + 1);
        track.notes[track.notes.Length - 1] = copy;
        timingWarning = null;
        return track.notes.Length - 1;
    }

    private void CreateMirroredOppositeHandNoteAtPlayhead()
    {
        NoteEvent source = GetSelectedNote();
        if (source == null || track == null || track.music == null) return;

        HandType mirroredHand = source.hand == HandType.Left ? HandType.Right : HandType.Left;
        float duration = source.noteType == NoteType.Hold ? Mathf.Max(MinHoldDuration, source.endTime - source.time) : 0f;
        float newStart = source.noteType == NoteType.Hold
            ? Mathf.Clamp(playheadTime, 0f, Mathf.Max(0f, track.music.length - duration))
            : Mathf.Clamp(playheadTime, 0f, track.music.length);
        float newEnd = source.noteType == NoteType.Hold ? newStart + duration : 0f;

        if (CandidateHasHandConflict(-1, mirroredHand, source.noteType, newStart, newEnd))
        {
            timingWarning = "Cannot create mirrored note: it would overlap another " + mirroredHand + " note.";
            return;
        }

        RecordTrackChange();
        NoteEvent mirrored = new NoteEvent
        {
            time = newStart,
            endTime = newEnd,
            noteType = source.noteType,
            hand = mirroredHand,
            targetPosition = MirrorX(source.targetPosition),
            endPosition = source.noteType == NoteType.Hold ? MirrorX(source.endPosition) : Vector2.zero
        };

        Array.Resize(ref track.notes, track.notes.Length + 1);
        track.notes[track.notes.Length - 1] = mirrored;
        selectedIndex = track.notes.Length - 1;
        timingWarning = null;
        SaveTrack();
        CenterTimelineOn(newStart);
    }

    private void DeleteSelectedNote()
    {
        if (selectedIndex < 0 || selectedIndex >= track.notes.Length) return;

        RecordTrackChange();
        NoteEvent[] newNotes = new NoteEvent[track.notes.Length - 1];
        int writeIndex = 0;
        for (int i = 0; i < track.notes.Length; i++)
        {
            if (i == selectedIndex) continue;
            newNotes[writeIndex++] = track.notes[i];
        }

        track.notes = newNotes;
        selectedIndex = Mathf.Clamp(selectedIndex, -1, track.notes.Length - 1);
        timingWarning = null;
        SaveTrack();
    }

    private void DeleteSelectedMarker()
    {
        if (selectedMarkerIndex < 0 || selectedMarkerIndex >= track.markers.Length) return;

        RecordTrackChange();
        TimelineMarker[] newMarkers = new TimelineMarker[track.markers.Length - 1];
        int writeIndex = 0;
        for (int i = 0; i < track.markers.Length; i++)
        {
            if (i == selectedMarkerIndex) continue;
            newMarkers[writeIndex++] = track.markers[i];
        }

        track.markers = newMarkers;
        selectedMarkerIndex = Mathf.Clamp(selectedMarkerIndex, -1, track.markers.Length - 1);
        SaveTrack();
    }

    private void DeleteSelectedTutorialCue()
    {
        if (GetSelectedTutorialCue() == null) return;

        RecordTrackChange();
        RemoveTutorialCueAt(selectedTutorialImageType, selectedTutorialCueIndex);
        selectedTutorialCueIndex = -1;
        SaveTrack();
    }

    private void RemoveTutorialCueAt(TutorialImageType imageType, int index)
    {
        TutorialImageCue[] cues = GetTutorialCueArray(imageType);
        if (index < 0 || index >= cues.Length) return;

        TutorialImageCue[] newCues = new TutorialImageCue[cues.Length - 1];
        int writeIndex = 0;
        for (int i = 0; i < cues.Length; i++)
        {
            if (i == index) continue;
            newCues[writeIndex++] = cues[i];
        }

        SetTutorialCueArray(imageType, newCues);
    }

    private void SortAndSave()
    {
        SaveTrack();
        selectedIndex = Mathf.Clamp(selectedIndex, -1, track.notes.Length - 1);
    }

    private int FindNoteAtPosition(Vector2 mousePosition, Rect rect, out DragMode noteDragMode)
    {
        noteDragMode = DragMode.None;
        int bestIndex = -1;
        float bestDistance = 12f;

        for (int i = 0; i < track.notes.Length; i++)
        {
            NoteEvent note = track.notes[i];
            float start = note.time;
            float end = GetNoteEndTime(note);
            if (end < timelineStart || start > timelineStart + timelineLength) continue;

            float laneY = GetLaneY(note.hand, rect);
            float startX = TimeToX(start, rect);
            float startDistance = Vector2.Distance(mousePosition, new Vector2(startX, laneY));
            if (startDistance < bestDistance)
            {
                bestDistance = startDistance;
                bestIndex = i;
                noteDragMode = DragMode.MoveNote;
            }

            if (note.noteType == NoteType.Hold)
            {
                float endX = TimeToX(note.endTime, rect);
                float endDistance = Vector2.Distance(mousePosition, new Vector2(endX, laneY));
                if (endDistance < bestDistance)
                {
                    bestDistance = endDistance;
                    bestIndex = i;
                    noteDragMode = DragMode.HoldEnd;
                }

                float segmentMinX = Mathf.Min(startX, endX);
                float segmentMaxX = Mathf.Max(startX, endX);
                float segmentDistance = Mathf.Abs(mousePosition.y - laneY);
                if (mousePosition.x >= segmentMinX && mousePosition.x <= segmentMaxX && segmentDistance < bestDistance)
                {
                    bestDistance = segmentDistance;
                    bestIndex = i;
                    noteDragMode = DragMode.MoveNote;
                }
            }
        }

        return bestIndex;
    }

    private int FindMarkerAtPosition(Vector2 mousePosition, Rect rect)
    {
        if (track.markers == null) return -1;

        int bestIndex = -1;
        float bestDistance = MarkerHitWidth;
        for (int i = 0; i < track.markers.Length; i++)
        {
            TimelineMarker marker = track.markers[i];
            if (marker.time < timelineStart || marker.time > timelineStart + timelineLength) continue;

            float x = TimeToX(marker.time, rect);
            float distance = Mathf.Abs(mousePosition.x - x);
            bool nearMarkerHead = mousePosition.y >= rect.yMin && mousePosition.y <= rect.yMin + 24f;
            if (nearMarkerHead && distance <= bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private int FindTutorialCueAtPosition(Vector2 mousePosition, Rect rect, out TutorialImageType imageType)
    {
        imageType = TutorialImageType.ShortNotes_Yellow;
        if (track == null) return -1;

        TutorialImageType[] types =
        {
            TutorialImageType.ShortNotes_Yellow,
            TutorialImageType.ShortNotes_Blue,
            TutorialImageType.LongNote,
            TutorialImageType.Pause
        };

        float[] laneYs =
        {
            rect.yMin + 32f,
            rect.yMin + 56f,
            rect.yMin + 80f,
            rect.yMin + 104f
        };

        for (int laneIndex = 0; laneIndex < types.Length; laneIndex++)
        {
            float laneY = laneYs[laneIndex];
            if (Mathf.Abs(mousePosition.y - laneY) > 10f) continue;

            TutorialImageCue[] cues = GetTutorialCueArray(types[laneIndex]);
            for (int i = 0; i < cues.Length; i++)
            {
                TutorialImageCue cue = cues[i];
                float start = Mathf.Min(cue.startTime, cue.endTime);
                float end = Mathf.Max(cue.startTime + TutorialCueMinDuration, cue.endTime);
                if (end < timelineStart || start > timelineStart + timelineLength) continue;

                float startX = TimeToX(start, rect);
                float endX = TimeToX(end, rect);
                Rect cueRect = Rect.MinMaxRect(Mathf.Min(startX, endX), laneY - 9f, Mathf.Max(startX, endX), laneY + 9f);
                if (!cueRect.Contains(mousePosition)) continue;

                imageType = types[laneIndex];
                return i;
            }
        }

        return -1;
    }

    private bool CandidateHasHandConflict(int ignoreIndex, HandType hand, NoteType noteType, float start, float end)
    {
        if (track == null || track.notes == null) return false;

        float candidateStart = start;
        float candidateEnd = noteType == NoteType.Hold ? Mathf.Max(start + MinHoldDuration, end) : start + TapConflictWindow;

        for (int i = 0; i < track.notes.Length; i++)
        {
            if (i == ignoreIndex) continue;

            NoteEvent other = track.notes[i];
            if (other.hand != hand) continue;

            float otherStart = other.time;
            float otherEnd = other.noteType == NoteType.Hold ? Mathf.Max(other.time + MinHoldDuration, other.endTime) : other.time + TapConflictWindow;
            if (IntervalsOverlap(candidateStart, candidateEnd, otherStart, otherEnd))
            {
                return true;
            }
        }

        return false;
    }

    private bool NoteHasHandConflict(int index)
    {
        if (index < 0 || index >= track.notes.Length) return false;

        NoteEvent note = track.notes[index];
        return CandidateHasHandConflict(index, note.hand, note.noteType, note.time, note.endTime);
    }

    private bool HasAnyHandConflict()
    {
        if (track == null || track.notes == null) return false;

        for (int i = 0; i < track.notes.Length; i++)
        {
            if (NoteHasHandConflict(i)) return true;
        }

        return false;
    }

    private string GetFirstConflictMessage()
    {
        if (track == null || track.notes == null) return null;

        for (int i = 0; i < track.notes.Length; i++)
        {
            if (NoteHasHandConflict(i))
            {
                NoteEvent note = track.notes[i];
                return "Timing conflict: " + note.hand + " has overlapping notes around " + note.time.ToString("0.###") + "s. Move or change one note before saving.";
            }
        }

        return null;
    }

    private static bool IntervalsOverlap(float aStart, float aEnd, float bStart, float bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }

    private HandType FindAvailableHandAtTime(float time, NoteType noteType, float endTime)
    {
        if (!CandidateHasHandConflict(-1, HandType.Left, noteType, time, endTime)) return HandType.Left;
        return HandType.Right;
    }

    private NoteEvent FindCurrentPlaybackNote(HandType hand)
    {
        if (track == null || track.notes == null) return null;

        NoteEvent best = null;
        float bestDistance = 0.25f;
        foreach (NoteEvent note in track.notes)
        {
            if (note.hand != hand) continue;

            if (note.noteType == NoteType.Hold && playheadTime >= note.time && playheadTime <= note.endTime)
            {
                return note;
            }

            float distance = Mathf.Abs(note.time - playheadTime);
            if (note.time >= playheadTime && distance < bestDistance)
            {
                best = note;
                bestDistance = distance;
            }
        }

        return best;
    }

    private NoteEvent GetSelectedNote()
    {
        if (track == null || track.notes == null) return null;
        if (selectedIndex < 0 || selectedIndex >= track.notes.Length) return null;
        return track.notes[selectedIndex];
    }

    private TimelineMarker GetSelectedMarker()
    {
        if (track == null || track.markers == null) return null;
        if (selectedMarkerIndex < 0 || selectedMarkerIndex >= track.markers.Length) return null;
        return track.markers[selectedMarkerIndex];
    }

    private TutorialImageCue GetSelectedTutorialCue()
    {
        if (track == null) return null;

        TutorialImageCue[] cues = GetTutorialCueArray(selectedTutorialImageType);
        if (selectedTutorialCueIndex < 0 || selectedTutorialCueIndex >= cues.Length) return null;
        return cues[selectedTutorialCueIndex];
    }

    private float GetNoteEndTime(NoteEvent note)
    {
        return note.noteType == NoteType.Hold ? Mathf.Max(note.time + MinHoldDuration, note.endTime) : note.time;
    }

    private float GetLaneY(HandType hand, Rect rect)
    {
        return hand == HandType.Left ? rect.yMax - 72f : rect.yMax - 34f;
    }

    private void EnsureNotesArray()
    {
        if (track != null && track.notes == null)
        {
            track.notes = Array.Empty<NoteEvent>();
        }
    }

    private void EnsureMarkersArray()
    {
        if (track != null && track.markers == null)
        {
            track.markers = Array.Empty<TimelineMarker>();
        }
    }

    private void EnsureTutorialCueArrays()
    {
        if (track == null) return;

        if (track.shortNotesYellowCues == null) track.shortNotesYellowCues = Array.Empty<TutorialImageCue>();
        if (track.shortNotesBlueCues == null) track.shortNotesBlueCues = Array.Empty<TutorialImageCue>();
        if (track.longNoteCues == null) track.longNoteCues = Array.Empty<TutorialImageCue>();
        if (track.pauseCues == null) track.pauseCues = Array.Empty<TutorialImageCue>();
    }

    private TutorialImageCue[] GetTutorialCueArray(TutorialImageType imageType)
    {
        EnsureTutorialCueArrays();

        switch (imageType)
        {
            case TutorialImageType.ShortNotes_Blue:
                return track.shortNotesBlueCues;
            case TutorialImageType.LongNote:
                return track.longNoteCues;
            case TutorialImageType.Pause:
                return track.pauseCues;
            default:
                return track.shortNotesYellowCues;
        }
    }

    private void SetTutorialCueArray(TutorialImageType imageType, TutorialImageCue[] cues)
    {
        switch (imageType)
        {
            case TutorialImageType.ShortNotes_Blue:
                track.shortNotesBlueCues = cues;
                break;
            case TutorialImageType.LongNote:
                track.longNoteCues = cues;
                break;
            case TutorialImageType.Pause:
                track.pauseCues = cues;
                break;
            default:
                track.shortNotesYellowCues = cues;
                break;
        }
    }

    private void RecordTrackChange()
    {
        Undo.RecordObject(track, "Edit Rhythm Chart");
        EditorUtility.SetDirty(track);
    }

    private void SaveTrack()
    {
        if (track == null) return;

        EnsureNotesArray();
        EnsureMarkersArray();
        EnsureTutorialCueArrays();
        NoteEvent selectedNote = GetSelectedNote();
        TimelineMarker selectedMarker = GetSelectedMarker();
        TutorialImageCue selectedTutorialCue = GetSelectedTutorialCue();
        Array.Sort(track.notes, (a, b) => a.time.CompareTo(b.time));
        Array.Sort(track.markers, (a, b) => a.time.CompareTo(b.time));
        SortTutorialCueArray(track.shortNotesYellowCues);
        SortTutorialCueArray(track.shortNotesBlueCues);
        SortTutorialCueArray(track.longNoteCues);
        SortTutorialCueArray(track.pauseCues);
        selectedIndex = selectedNote != null ? Array.IndexOf(track.notes, selectedNote) : selectedIndex;
        selectedMarkerIndex = selectedMarker != null ? Array.IndexOf(track.markers, selectedMarker) : selectedMarkerIndex;
        selectedTutorialCueIndex = selectedTutorialCue != null ? Array.IndexOf(GetTutorialCueArray(selectedTutorialImageType), selectedTutorialCue) : selectedTutorialCueIndex;
        EditorUtility.SetDirty(track);
        AssetDatabase.SaveAssets();
    }

    private void SortTutorialCueArray(TutorialImageCue[] cues)
    {
        if (cues == null) return;
        Array.Sort(cues, (a, b) => a.startTime.CompareTo(b.startTime));
    }

    private void EnsureWaveform(AudioClip clip)
    {
        int targetWidth = Mathf.Clamp(Mathf.CeilToInt(position.width * 2f), 1024, 8192);
        if (waveformTexture != null && waveformClip == clip && waveformWidth == targetWidth) return;

        ResetWaveform();
        waveformClip = clip;
        waveformWidth = targetWidth;

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        int channels = Mathf.Max(1, clip.channels);
        int samples = clip.samples;
        float[] data = new float[Mathf.Min(samples * channels, 12000000)];
        if (data.Length == 0 || !clip.GetData(data, 0))
        {
            return;
        }

        waveformTexture = new Texture2D(waveformWidth, 160, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color background = new Color(0.12f, 0.02f, 0.02f, 1f);
        Color[] pixels = new Color[waveformTexture.width * waveformTexture.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

        int height = waveformTexture.height;
        int midTop = height / 4;
        int midBottom = height * 3 / 4;
        int samplesPerColumn = Mathf.Max(1, samples / waveformWidth);

        for (int x = 0; x < waveformWidth; x++)
        {
            int startSample = x * samplesPerColumn;
            int endSample = Mathf.Min(samples, startSample + samplesPerColumn);
            float peak = 0f;
            for (int sample = startSample; sample < endSample; sample++)
            {
                for (int channel = 0; channel < channels; channel++)
                {
                    int dataIndex = sample * channels + channel;
                    if (dataIndex >= data.Length) break;
                    peak = Mathf.Max(peak, Mathf.Abs(data[dataIndex]));
                }
            }

            int topExtent = Mathf.RoundToInt(peak * (height / 4f - 4f));
            PaintVertical(pixels, waveformWidth, height, x, midTop - topExtent, midTop + topExtent, WaveColor);
            PaintVertical(pixels, waveformWidth, height, x, midBottom - topExtent, midBottom + topExtent, WaveColor);
        }

        waveformTexture.SetPixels(pixels);
        waveformTexture.Apply(false, true);
    }

    private static void PaintVertical(Color[] pixels, int width, int height, int x, int yMin, int yMax, Color color)
    {
        yMin = Mathf.Clamp(yMin, 0, height - 1);
        yMax = Mathf.Clamp(yMax, 0, height - 1);
        for (int y = yMin; y <= yMax; y++)
        {
            pixels[y * width + x] = color;
        }
    }

    private void ResetWaveform()
    {
        if (waveformTexture != null)
        {
            DestroyImmediate(waveformTexture);
        }

        waveformTexture = null;
        waveformClip = null;
        waveformWidth = 0;
    }

    private void EditorUpdate()
    {
        if (!isPlaying) return;

        AudioClip clip = track != null ? track.music : null;
        if (clip == null)
        {
            StopPreview();
            return;
        }

        int sample = AudioPreview.GetSamplePosition();
        if (sample >= 0)
        {
            playheadTime = Mathf.Clamp(sample / (float)clip.frequency, 0f, clip.length);
        }
        else
        {
            playheadTime += Time.deltaTime;
        }

        if (playheadTime >= clip.length || !AudioPreview.IsPlaying())
        {
            StopPreview();
            return;
        }

        if (autoScroll)
        {
            float padding = timelineLength * 0.25f;
            if (playheadTime > timelineStart + timelineLength - padding)
            {
                timelineStart = Mathf.Min(Mathf.Max(0f, clip.length - timelineLength), playheadTime - padding);
            }
        }

        Repaint();
    }

    private void PlayPreview(AudioClip clip, float startTime)
    {
        if (clip == null) return;

        int sample = Mathf.Clamp(Mathf.RoundToInt(startTime * clip.frequency), 0, Mathf.Max(0, clip.samples - 1));
        AudioPreview.StopAll();
        AudioPreview.SetVolume(previewVolume);
        AudioPreview.Play(clip, sample);
        playheadTime = startTime;
        isPlaying = true;
    }

    private void TogglePreviewPlayback(AudioClip clip)
    {
        if (clip == null) return;

        if (isPlaying)
        {
            PausePreview(clip);
            return;
        }

        PlayPreview(clip, playheadTime);
    }

    private void PausePreview(AudioClip clip)
    {
        if (clip == null)
        {
            isPlaying = false;
            return;
        }

        int sample = AudioPreview.GetSamplePosition();
        if (sample >= 0)
        {
            playheadTime = Mathf.Clamp(sample / (float)clip.frequency, 0f, clip.length);
        }

        AudioPreview.StopAll();
        isPlaying = false;
        Repaint();
    }

    private void PausePreview()
    {
        PausePreview(track != null ? track.music : null);
    }

    private void StopPreview()
    {
        AudioPreview.StopAll();
        isPlaying = false;
    }

    private void SetPlayhead(AudioClip clip, float time, bool updateAudio)
    {
        if (clip == null) return;

        playheadTime = Mathf.Clamp(time, 0f, clip.length);
        if (updateAudio && isPlaying)
        {
            PlayPreview(clip, playheadTime);
        }

        Repaint();
    }

    private float TimeToX(float time, Rect rect)
    {
        return Mathf.Lerp(rect.xMin, rect.xMax, Mathf.InverseLerp(timelineStart, timelineStart + timelineLength, time));
    }

    private float XToTime(float x, Rect rect)
    {
        return Mathf.Lerp(timelineStart, timelineStart + timelineLength, Mathf.InverseLerp(rect.xMin, rect.xMax, x));
    }

    private void CenterTimelineOn(float time)
    {
        if (track == null || track.music == null) return;

        timelineStart = Mathf.Clamp(time - timelineLength * 0.5f, 0f, Mathf.Max(0f, track.music.length - timelineLength));
    }

    private float PickGridInterval(float visibleLength)
    {
        if (visibleLength <= 5f) return 0.25f;
        if (visibleLength <= 12f) return 0.5f;
        if (visibleLength <= 30f) return 1f;
        if (visibleLength <= 90f) return 5f;
        return 10f;
    }

    private Vector2 ClampToPad(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX)),
            Mathf.Clamp(position.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY))
        );
    }

    private Vector2 MirrorX(Vector2 position)
    {
        return ClampToPad(new Vector2(-position.x, position.y));
    }

    private Vector2 ChartToGui(Vector2 chartPosition, Rect rect)
    {
        float x = Mathf.InverseLerp(minX, maxX, chartPosition.x);
        float y = Mathf.InverseLerp(minY, maxY, chartPosition.y);
        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, x),
            Mathf.Lerp(rect.yMax, rect.yMin, y)
        );
    }

    private Vector2 GuiToChart(Vector2 guiPosition, Rect rect)
    {
        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, guiPosition.x);
        float y = Mathf.InverseLerp(rect.yMax, rect.yMin, guiPosition.y);
        return new Vector2(
            Mathf.Lerp(minX, maxX, x),
            Mathf.Lerp(minY, maxY, y)
        );
    }

    private void DrawMarker(Vector2 position, Color color, bool selected, string text)
    {
        float size = selected ? 18f : 13f;
        Rect markerRect = new Rect(position.x - size * 0.5f, position.y - size * 0.5f, size, size);
        EditorGUI.DrawRect(markerRect, color);

        if (selected)
        {
            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(2f,
                new Vector3(markerRect.xMin, markerRect.yMin),
                new Vector3(markerRect.xMax, markerRect.yMin),
                new Vector3(markerRect.xMax, markerRect.yMax),
                new Vector3(markerRect.xMin, markerRect.yMax),
                new Vector3(markerRect.xMin, markerRect.yMin));
            Handles.EndGUI();
        }

        GUI.Label(new Rect(markerRect.xMax + 4f, markerRect.y - 7f, 44f, 18f), text, EditorStyles.miniBoldLabel);
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(width, a, b);
        Handles.EndGUI();
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remaining = seconds - minutes * 60f;
        return minutes.ToString("00") + ":" + remaining.ToString("00.00");
    }

    private static class AudioPreview
    {
        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayPreviewClip = FindMethod("PlayPreviewClip", 3) ?? FindMethod("PlayClip", 3);
        private static readonly MethodInfo StopAllPreviewClips = FindMethod("StopAllPreviewClips", 0) ?? FindMethod("StopAllClips", 0);
        private static readonly MethodInfo PausePreviewClip = FindMethod("PausePreviewClip", 0) ?? FindMethod("PauseClip", 1);
        private static readonly MethodInfo IsPreviewClipPlaying = FindMethod("IsPreviewClipPlaying", 0) ?? FindMethod("IsClipPlaying", 1);
        private static readonly MethodInfo GetPreviewClipSamplePosition = FindMethod("GetPreviewClipSamplePosition", 0);
        private static readonly MethodInfo SetPreviewClipVolume = FindMethod("SetPreviewClipVolume", 1);

        public static void Play(AudioClip clip, int startSample)
        {
            if (clip == null || PlayPreviewClip == null) return;

            if (PlayPreviewClip.GetParameters().Length == 3)
            {
                PlayPreviewClip.Invoke(null, new object[] { clip, startSample, false });
            }
        }

        public static void Pause()
        {
            if (PausePreviewClip == null) return;

            ParameterInfo[] parameters = PausePreviewClip.GetParameters();
            PausePreviewClip.Invoke(null, parameters.Length == 0 ? null : new object[] { null });
        }

        public static void StopAll()
        {
            StopAllPreviewClips?.Invoke(null, null);
        }

        public static bool IsPlaying()
        {
            if (IsPreviewClipPlaying == null) return true;

            ParameterInfo[] parameters = IsPreviewClipPlaying.GetParameters();
            object result = IsPreviewClipPlaying.Invoke(null, parameters.Length == 0 ? null : new object[] { null });
            return result is bool playing && playing;
        }

        public static int GetSamplePosition()
        {
            if (GetPreviewClipSamplePosition == null) return -1;

            object result = GetPreviewClipSamplePosition.Invoke(null, null);
            return result is int sample ? sample : -1;
        }

        public static void SetVolume(float volume)
        {
            if (SetPreviewClipVolume == null) return;
            SetPreviewClipVolume.Invoke(null, new object[] { volume });
        }

        private static MethodInfo FindMethod(string name, int parameterCount)
        {
            if (AudioUtilType == null) return null;

            foreach (MethodInfo method in AudioUtilType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
