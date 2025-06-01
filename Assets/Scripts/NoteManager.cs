using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using TMPro;
using System;

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
    
    [Header("��ǿ����UI")]
    public Button toggleVisibilityButton; // ��ʾ/�������б�ǩ��ť
    public TMP_InputField annotationInputField; // ע�������

    [Header("Raycast Settings")]
    public float maxRaycastDistance = 10f;
    public float touchRadius = 0.1f; 
    public bool debugRaycast = true;
    private int noteLayerMask; // �������߼��Notes�������

    private List<Note> activeNotes = new List<Note>();
    private Camera arCamera;
    private Vector2 touchPosition;
    private Note selectedNote;
    private Vector3 placementPosition;
    private Quaternion placementRotation;
    private static NoteManager instance;
    private UIState currentUIState = UIState.None;

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
        noteLayerMask = 1 << 6; // ����Notes��(Layer 6)������
        Initialize();
    }    // ��¼���б�ǩ�Ƿ�ɼ�
    private bool allNotesVisible = true;
    
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
            
        // ��ʼ����ǿ����UI
        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleAllNotesVisibility);
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
    {
        // Debug����
        if (debugRaycast && arCamera != null)
        {
            Debug.DrawRay(arCamera.transform.position, arCamera.transform.forward * maxRaycastDistance, Color.red);
        }

        // ����������
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                // 1. ��ǰ���ڱ༭�򴴽�ʱ���������µĴ���
                if (currentUIState != UIState.None) 
                {
                    DebugLogger.Instance?.AddLog($"Ignoring touch - current state: {currentUIState}");
                    return;
                }

                // 2. ���Note���
                Ray ray = arCamera.ScreenPointToRay(touchPosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, maxRaycastDistance, noteLayerMask))
                {
                    Note note = hit.collider.GetComponent<Note>();
                    if (note != null)
                    {
                        // ����Ƿ�����UI��ť
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
                }

                // 3. �����UI���
                if (CheckUIClick(touchPosition, out bool hitMainUI) && hitMainUI)
                {
                    DebugLogger.Instance?.AddLog("Main UI clicked, skipping raycast");
                    return;
                }

                // 4. ���û���������������Դ�����Note
                List<ARRaycastHit> arHits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touchPosition, arHits, TrackableType.Planes))
                {
                    placementPosition = arHits[0].pose.position;
                    Vector3 cameraForward = arCamera.transform.forward;
                    cameraForward.y = 0;
                    placementRotation = cameraForward != Vector3.zero ? 
                        Quaternion.LookRotation(cameraForward, Vector3.up) :
                        Quaternion.identity;
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
            {
                // ����Ƿ�����UI Canvas��Ԫ��
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
    {
        // ȷ��UI���������
        if (noteUI == null || noteInputField == null)
        {
            DebugLogger.Instance?.AddLog("Error: Required UI components are missing");
            return;
        }

        // ����Ѿ��ڴ���״̬����Ҫ�ظ���ʼ��
        if (currentUIState == UIState.Creating)
        {
            return;
        }

        // ������ڱ༭���ȱ���
        if (currentUIState == UIState.Editing && selectedNote != null)
        {
            SaveCurrentNote();
        }
        
        // ���ô���״̬
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
                
            // ����ע�ͣ�����У�
            if (annotationInputField != null && !string.IsNullOrEmpty(annotationInputField.text))
            {
                selectedNote.SetAnnotation(annotationInputField.text);
                DebugLogger.Instance?.AddLog($"����ע��: {annotationInputField.text}");
            }
            
            DebugLogger.Instance?.AddLog($"����ʼǸ���: {noteInputField.text}");
        }
    }public void OnCreateNoteConfirmed()
    {
        // �ȼ���������
        if (noteInputField == null)
        {
            DebugLogger.Instance?.AddLog("Error: Note input field is missing");
            return;
        }

        DebugLogger.Instance?.AddLog($"Creating note with state: {currentUIState}, position: {placementPosition}");

        // ���״̬��λ��
        if (currentUIState != UIState.Creating)
        {
            DebugLogger.Instance?.AddLog("Error: Not in note creation state. Current state: " + currentUIState);
            return;
        }

        if (placementPosition == Vector3.zero)
        {
            DebugLogger.Instance?.AddLog("Error: Invalid placement position");
            return;
        }

        // ��ȡ����֤��������
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
            
            // ��ʾע�ͣ�����У�
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
    }

    // �л����б�ǩ�Ŀɼ���
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
        DebugLogger.Instance?.AddLog($"���б�ǩ�ɼ�������Ϊ: {allNotesVisible}");
    }
    
    // ������ʾ/���ذ�ť�ı�
    private void UpdateToggleVisibilityButtonText()
    {
        if (toggleVisibilityButton != null)
        {
            TMP_Text buttonText = toggleVisibilityButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = allNotesVisible ? "Hide Notes" : "Display Notes";
            }
        }
    }

    private bool IsPointerOverUI(Vector2 position)
    {
        if (mainCanvas == null) return false;

        UnityEngine.EventSystems.PointerEventData eventData = 
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = position;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        // ֱ�ӷ����Ƿ���UI������������ӹ���
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
