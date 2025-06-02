using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class NoteManager : MonoBehaviour
{
    private enum UIState
    {
        None,
        Creating,
        Editing
    }

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;    

    [Header("Note Properties")]
    public GameObject notePrefab;    [Header("UI References")]
    public Canvas mainCanvas;
    public TMP_InputField noteInputField;
    public Button createButton;
    public Button cancelButton;
    public GameObject noteUI;
    public ColorPicker colorPicker;
      [Header("Enhanced Features UI")]
    public Button toggleVisibilityButton; // Button to show/hide all notes
    public TMP_InputField annotationInputField; // Annotation input field
    private TMP_Text toggleVisibilityText; // Text component for the toggle button

    [Header("Raycast Settings")]
    public float maxRaycastDistance = 10f;
    public float touchRadius = 0.1f; 
    public bool debugRaycast = true;
    private int noteLayerMask; // Layer mask for Notes layer raycasting

    private List<Note> activeNotes = new List<Note>();
    private Camera arCamera;
    private Vector2 touchPosition;
    private Note selectedNote;
    private Vector3 placementPosition;
    private Quaternion placementRotation;
    private static NoteManager instance;
    private UIState currentUIState = UIState.None;

    // Track if all notes are visible
    private bool allNotesVisible = true;

    public static NoteManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        
        arCamera = Camera.main;
        noteLayerMask = 1 << 6; // Set Notes layer (Layer 6) mask
        Initialize();    }

    void Initialize()
    {
        if (createButton != null)
        {
            createButton.onClick.AddListener(OnCreateOrSaveButtonClicked);
            UpdateCreateButtonText("Create");
        }
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCreateNoteCanceled);

        if (noteUI != null)
            noteUI.SetActive(false);
        else
            DebugLogger.Instance?.AddLog("Error: noteUI not found");

        // Initialize enhanced features UI
        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleAllNotesVisibility);
            // Automatically find the text component on the button
            toggleVisibilityText = toggleVisibilityButton.GetComponentInChildren<TMP_Text>();
            if (toggleVisibilityText == null)
            {
                DebugLogger.Instance?.AddLog("Warning: No TextMeshPro text component found on toggle visibility button");
            }
            UpdateToggleVisibilityButtonText();
        }

        if (planeManager != null)
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
        else
            DebugLogger.Instance?.AddLog("Error: planeManager not found");

        if (raycastManager == null)
            DebugLogger.Instance?.AddLog("Error: raycastManager not found");
        if (notePrefab == null)
            DebugLogger.Instance?.AddLog("Error: notePrefab not found");
        if (noteInputField == null)
            DebugLogger.Instance?.AddLog("Error: noteInputField not found");
        if (colorPicker == null)
            DebugLogger.Instance?.AddLog("Error: colorPicker not found");

        DebugLogger.Instance?.AddLog("NoteManager initialized");
    }

    void Update()
    {        // Debug ray
        if (debugRaycast && arCamera != null)
        {
            Debug.DrawRay(arCamera.transform.position, arCamera.transform.forward * maxRaycastDistance, Color.red);
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                // 检查是否点在ShowIconButton（Tag为ShowIconButton）上
                if (EventSystem.current != null)
                {
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    eventData.position = touch.position;
                    var results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);
                    foreach (var r in results)
                    {
                        if (r.gameObject.CompareTag("ShowIconButton"))
                        {
                            DebugLogger.Instance?.AddLog("Clicked ShowIconButton, only trigger onClick.");
                            return;
                        }
                    }
                }
                // 1. If currently editing or creating, ignore new touches
                if (currentUIState != UIState.None) 
                {
                    DebugLogger.Instance?.AddLog($"Ignoring touch - current state: {currentUIState}");
                    return;
                }

                // 2. Check for Note click
                Ray ray = arCamera.ScreenPointToRay(touchPosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, maxRaycastDistance, noteLayerMask))
                {
                    Note note = hit.collider.GetComponent<Note>();
                    if (note != null)
                    {                        // Check if UI button was clicked
                        if (CheckUIClick(touchPosition, out bool clickedMainUI))
                        {
                            if (clickedMainUI)
                            {
                                DebugLogger.Instance?.AddLog("Main UI clicked, skipping raycast");
                                return;
                            }
                        }
                        
                        SelectNote(note);
                        return;
                    }
                }                // 3. Check for main UI click
                if (CheckUIClick(touchPosition, out bool hitMainUI) && hitMainUI)
                {
                    DebugLogger.Instance?.AddLog("Main UI clicked, skipping raycast");
                    return;
                }

                // 4. If no other interaction, try to create new Note
                List<ARRaycastHit> arHits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touchPosition, arHits, TrackableType.Planes))
                {
                    ARRaycastHit arHit = arHits[0];
                    placementPosition = arHit.pose.position;
                    
                    // Get the normal of the detected plane
                    Vector3 planeNormal = arHit.pose.up;
                    
                    // Calculate rotation based on the plane normal
                    if (Mathf.Abs(planeNormal.y) > 0.9f) // Horizontal surface (floor/ceiling)
                    {
                        // For horizontal surfaces, make the note face the camera
                        Vector3 cameraForward = arCamera.transform.forward;
                        cameraForward.y = 0;
                        placementRotation = cameraForward != Vector3.zero ? 
                            Quaternion.LookRotation(cameraForward, Vector3.up) :
                            Quaternion.identity;
                    }
                    else // Vertical surface (wall)
                    {
                        // For vertical surfaces, make the note face outward from the wall
                        placementRotation = Quaternion.LookRotation(-planeNormal, Vector3.up);
                    }
                    
                    DebugLogger.Instance?.AddLog($"Plane detected - Normal: {planeNormal}, Position: {placementPosition}");
                    ShowCreateNoteUI();
                }
            }
        }
    }

    private bool CheckUIClick(Vector2 position, out bool hitMainUI)
    {
        hitMainUI = false;
        if (mainCanvas == null) return false;

        UnityEngine.EventSystems.PointerEventData eventData = 
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = position;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        bool hitAnyUI = false;

        foreach (var result in results)
        {
            if (result.gameObject != null)
            {                // Check if it's an element of the main UI Canvas
                if (result.gameObject.transform.IsChildOf(mainCanvas.transform))
                {
                    hitMainUI = true;
                    hitAnyUI = true;
                    DebugLogger.Instance?.AddLog($"Hit main UI: {result.gameObject.name}");
                }
                else
                {
                    hitAnyUI = true;
                    DebugLogger.Instance?.AddLog($"Hit other UI: {result.gameObject.name}");
                }
            }
        }

        return hitAnyUI;
    }

    private void UpdateCreateButtonText(string text)
    {
        if (createButton != null && createButton.GetComponentInChildren<TMP_Text>() != null)
        {
            createButton.GetComponentInChildren<TMP_Text>().text = text;
        }
    }

    private void ClearUIState()
    {
        selectedNote = null;
        noteInputField.text = "";
        placementPosition = Vector3.zero;
        placementRotation = Quaternion.identity;
        currentUIState = UIState.None;
        noteUI.SetActive(false);
        UpdateCreateButtonText("Create");
    }    private void ShowCreateNoteUI()
    {        // Ensure UI components exist
        if (noteUI == null || noteInputField == null)
        {
            DebugLogger.Instance?.AddLog("Error: Required UI components are missing");
            return;
        }

        // If already in creation state, don't initialize again
        if (currentUIState == UIState.Creating)
        {
            return;
        }

        // If currently editing, save first
        if (currentUIState == UIState.Editing && selectedNote != null)
        {
            SaveCurrentNote();
        }
          // Set creation state
        currentUIState = UIState.Creating;
        noteUI.SetActive(true);
        noteInputField.text = "";
        noteInputField.ActivateInputField();
        UpdateCreateButtonText("Create");
        
        DebugLogger.Instance?.AddLog($"Ready to create new note at position: {placementPosition}");
    }    private void SaveCurrentNote()
    {
        if (selectedNote != null && !string.IsNullOrEmpty(noteInputField.text))
        {
            selectedNote.SetContent(noteInputField.text);
            if (colorPicker != null)
                selectedNote.SetColor(colorPicker.CurrentColor);
                  // Save annotation (if any)
            if (annotationInputField != null && !string.IsNullOrEmpty(annotationInputField.text))
            {
                selectedNote.SetAnnotation(annotationInputField.text);
                DebugLogger.Instance?.AddLog($"Saved annotation: {annotationInputField.text}");
            }
            
            DebugLogger.Instance?.AddLog($"Saved note changes: {noteInputField.text}");
        }
    }public void OnCreateNoteConfirmed()    {
        // First check required components
        if (noteInputField == null)
        {
            DebugLogger.Instance?.AddLog("Error: Note input field is missing");
            return;
        }

        DebugLogger.Instance?.AddLog($"Creating note with state: {currentUIState}, position: {placementPosition}");        // Check state and position
        if (currentUIState != UIState.Creating)
        {
            DebugLogger.Instance?.AddLog("Error: Not in note creation state. Current state: " + currentUIState);
            return;
        }

        if (placementPosition == Vector3.zero)
        {
            DebugLogger.Instance?.AddLog("Error: Invalid placement position");
            return;
        }        // Get and validate input content
        string noteText = noteInputField.text?.Trim();
        if (string.IsNullOrEmpty(noteText))
        {
            DebugLogger.Instance?.AddLog("Error: Note content cannot be empty");
            return;
        }

        try
        {
            GameObject noteObj = Instantiate(notePrefab, placementPosition, placementRotation);
            Note note = noteObj.GetComponent<Note>();
            if (note == null)
            {
                DebugLogger.Instance?.AddLog("Error: Note component not found");
                Destroy(noteObj);
                return;
            }

            note.SetContent(noteText);
            if (colorPicker != null)
                note.SetColor(colorPicker.CurrentColor);
            activeNotes.Add(note);
            DebugLogger.Instance?.AddLog($"Successfully created note: {noteText}");
            ClearUIState();
        }
        catch (Exception e)
        {
            DebugLogger.Instance?.AddLog($"Error creating note: {e.Message}");
            ClearUIState();
        }
    }

    public void OnCreateNoteCanceled()
    {
        if (currentUIState == UIState.Creating || currentUIState == UIState.Editing)
        {
            ClearUIState();
            DebugLogger.Instance?.AddLog($"Cancelled {currentUIState} operation");
        }
    }

    private void SelectNote(Note note)
    {
        if (note == null) return;

        selectedNote = note;
        currentUIState = UIState.Editing;
        UpdateCreateButtonText("Save");
        ShowNoteDetails(note);
        
        DebugLogger.Instance?.AddLog($"Selected note for editing: {note.data.content}");
    }    private void ShowNoteDetails(Note note)
    {
        if (noteUI != null && noteInputField != null)
        {
            noteUI.SetActive(true);
            noteInputField.text = note.data.content;
            noteInputField.ActivateInputField();
            
            // Update color picker if available
            if (colorPicker != null)
            {
                colorPicker.SetCurrentColor(note.data.color);
            }
            
            // 显示注释（如果有）
            if (annotationInputField != null)
            {
                annotationInputField.text = note.GetAnnotation();
            }
        }
    }

    public void OnCreateOrSaveButtonClicked()
    {
        if (currentUIState == UIState.Editing)
        {
            SaveCurrentNote();
            ClearUIState();
            DebugLogger.Instance?.AddLog("Note changes saved");
        }
        else if (currentUIState == UIState.Creating)
        {
            OnCreateNoteConfirmed();
        }
    }

    public void DeleteSelectedNote()
    {
        if (selectedNote != null)
        {
            DeleteNote(selectedNote);
            selectedNote = null;
            noteUI.SetActive(false);
        }
    }

    public void DeleteNote(Note note)
    {
        if (activeNotes.Contains(note))
        {
            activeNotes.Remove(note);
            note.Delete();
        }
    }    // Toggle visibility of all notes
    public void ToggleAllNotesVisibility()
    {
        allNotesVisible = !allNotesVisible;
        
        foreach (Note note in activeNotes)
        {
            if (note != null)
            {
                note.SetVisible(allNotesVisible);
            }
        }
        
        UpdateToggleVisibilityButtonText();
        DebugLogger.Instance?.AddLog($"All notes visibility set to: {allNotesVisible}");
    }
      // Update show/hide button text
    private void UpdateToggleVisibilityButtonText()
    {
        if (toggleVisibilityText != null)
        {
            toggleVisibilityText.text = allNotesVisible ? "Hide All Notes" : "Show All Notes";
        }
    }

    private bool IsPointerOverUI(Vector2 position)
    {
        if (mainCanvas == null) return false;

        UnityEngine.EventSystems.PointerEventData eventData = 
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = position;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);        // Directly return whether there are any UI clicks, no complex filtering
        return results.Count > 0;
    }

    private void OnDrawGizmos()
    {
        if (debugRaycast && arCamera != null)
        {
            Gizmos.color = Color.yellow;
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            Gizmos.DrawRay(ray.origin, ray.direction * maxRaycastDistance);
        }
    }
}
