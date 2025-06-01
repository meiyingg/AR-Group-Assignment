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
    public GameObject notePrefab;

    [Header("UI References")]
    public Canvas mainCanvas;
    public TMP_InputField noteInputField;
    public Button createButton;
    public Button cancelButton;
    public GameObject noteUI;
    public ColorPicker colorPicker;

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
        Initialize();
    }

    void Initialize()
    {
        if (createButton != null)
            createButton.onClick.AddListener(OnCreateNoteConfirmed);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCreateNoteCanceled);

        if (noteUI != null)
            noteUI.SetActive(false);
        else
            DebugLogger.Instance?.AddLog("Error: noteUI not found");

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
    }    void Update()
    {
        // ֻ��û�н����еĲ���ʱ������
        if (Input.touchCount > 0 && currentUIState == UIState.None)
        {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;
            
            if (touch.phase == TouchPhase.Began)
            {
                // ����Ƿ�����UI
                if (IsPointerOverUI(touch.position))
                {
                    return;
                }
                
                // �ȼ���Ƿ��������еıʼ�
                Ray ray = arCamera.ScreenPointToRay(touchPosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    Note hitNote = hit.collider.GetComponent<Note>();
                    if (hitNote != null)
                    {
                        SelectNote(hitNote);
                        DebugLogger.Instance?.AddLog($"Selected note: {hitNote.data.content}");
                        return;
                    }
                }

                // ���û�е�����бʼǣ�������ƽ���ϴ����±ʼ�
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    ARRaycastHit arHit = hits[0];
                    // ʹ���������е��ȷ��λ��
                    placementPosition = arHit.pose.position;
                      // ����ӱʼǵ�����ķ�������
                    Vector3 directionToCamera = arCamera.transform.position - placementPosition;
                    directionToCamera.y = 0; // ���ִ�ֱ����
                    
                    // ȷ��Note�������
                    if (directionToCamera != Vector3.zero)
                    {
                        // ��Ӹ��ţ���Note�������
                        placementRotation = Quaternion.LookRotation(-directionToCamera.normalized, Vector3.up);
                    }
                    else
                    {
                        // ������Ϸ������ʹ�������Y����ת�ķ�����
                        placementRotation = Quaternion.Euler(0, arCamera.transform.rotation.eulerAngles.y + 180f, 0);
                    }
                    
                    ShowCreateNoteUI();
                    DebugLogger.Instance?.AddLog("Ready to create new note at touch position");
                }
                else
                {
                    DebugLogger.Instance?.AddLog("No valid placement surface detected");
                }
            }
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
        
        DebugLogger.Instance?.AddLog($"Ready to create new note at position: {placementPosition}");
    }

    private void SaveCurrentNote()
    {
        if (selectedNote != null && !string.IsNullOrEmpty(noteInputField.text))
        {
            selectedNote.SetContent(noteInputField.text);
            if (colorPicker != null)
                selectedNote.SetColor(colorPicker.CurrentColor);
            DebugLogger.Instance?.AddLog($"����ʼǸ���: {noteInputField.text}");
        }
    }    public void OnCreateNoteConfirmed()
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
        if (currentUIState == UIState.Creating)
        {
            ClearUIState();
            DebugLogger.Instance?.AddLog("Note creation cancelled");
        }
        else if (currentUIState == UIState.Editing)
        {
            SaveCurrentNote();
            ClearUIState();
            DebugLogger.Instance?.AddLog("Exited edit mode");
        }
    }

    private void SelectNote(Note note)
    {
        // �����ǰ���ڴ����±ʼǣ�����ѡ��
        if (currentUIState == UIState.Creating)
        {
            DebugLogger.Instance?.AddLog("������ɵ�ǰ�ʼǵĴ���");
            return;
        }

        // ������ڱ༭�����ʼǣ��ȱ���
        if (currentUIState == UIState.Editing && selectedNote != null)
        {
            SaveCurrentNote();
        }

        selectedNote = note;
        currentUIState = UIState.Editing;
        ShowNoteDetails(note);
    }

    private void ShowNoteDetails(Note note)
    {
        if (noteUI != null && noteInputField != null)
        {
            noteUI.SetActive(true);
            noteInputField.text = note.data.content;
            noteInputField.ActivateInputField();
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

    public void SaveAllNotes()
    {
        foreach (Note note in activeNotes)
        {
            note.UpdateTransform();
        }
    }

    private bool IsPointerOverUI(Vector2 position)
    {
        if (mainCanvas == null) return false;

        // ������λ��ת��ΪCanvas�ϵ�λ��
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            position,
            mainCanvas.worldCamera,
            out Vector2 localPoint
        );

        // ����Ƿ�����UIԪ��
        UnityEngine.EventSystems.PointerEventData eventDataCurrentPosition 
            = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventDataCurrentPosition.position = position;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }
}
