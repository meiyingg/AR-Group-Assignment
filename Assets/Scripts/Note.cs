using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[System.Serializable]
public class TodoItem
{
    public string content;        // ��������
    public bool isCompleted;      // ���״̬
    public DateTime dueDate;      // ��ֹ���ڣ���ѡ��
    public int priority;          // ���ȼ���1-3��
}

[System.Serializable]
public class NoteData
{
    public string content;
    public Vector3 position;
    public Quaternion rotation;
    public Color color;
    public bool isVisible = true;
    public string annotation = ""; // Single annotation
    public bool isTodoList = false;  // �Ƿ���todo list
    public List<TodoItem> todoItems = new List<TodoItem>();  // �����б�
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI reference
    public Image backgroundImage;        // Note background image
    public TMP_Text annotationText;     // Annotation text display
    public NoteData data;               // Note data
    public Button hideButton; // ��ť������Note
    public Button showIconButton; // ����ʱ��ʾ��Сicon��Button��
    public Button deleteButton; // ɾ����ť
    public GameObject todoListContainer;  // �����б�����
    public GameObject todoItemPrefab;     // ������Ԥ����
    public Button addTodoButton;          // �������ť
    public GameObject newTodoInputPanel;        // �½������������
    public TMP_InputField newTodoInput;         // �½����������
    public TMP_Dropdown newTodoPriorityDropdown;// �½��������ȼ�Dropdown
    public Button confirmAddButton;             // ȷ����Ӱ�ť
    public Button cancelAddButton;              // ȡ����Ӱ�ť
    public GameObject todoListScrollView; // ָ��TodoListScroll_Scroll View������

    private BoxCollider noteCollider;
    private RectTransform contentRectTransform;    private float minColliderSize = 0.1f; // Minimum collider size
    private Vector2 padding = new Vector2(0.15f, 0.15f); // Collider padding

    // ��¼ShowIconԭʼλ�ò���
    private Vector2 showIconOrigAnchorMin, showIconOrigAnchorMax, showIconOrigPivot, showIconOrigAnchoredPos;
    private bool showIconOrigSaved = false;

    private void Awake()
    {
        if (data == null)
            data = new NoteData();
        // Ensure default values
        data.color = Color.yellow;
        data.position = transform.position;
        data.rotation = transform.rotation;
        
        // Get RectTransform
        contentRectTransform = contentText?.GetComponent<RectTransform>();
        
        // Set up collider
        noteCollider = GetComponent<BoxCollider>();
        if (noteCollider == null)
        {
            noteCollider = gameObject.AddComponent<BoxCollider>();
            DebugLogger.Instance?.AddLog("Added BoxCollider to Note");
        }
        
        // Ensure collider is set up correctly
        noteCollider.isTrigger = false;
        
        // Initialize collider size
        UpdateColliderSize();
        
        // ��ť�¼���
        if (hideButton != null)
            hideButton.onClick.AddListener(() => SetVisible(false));
        if (showIconButton != null)
        {
            showIconButton.onClick.AddListener(() => SetVisible(true));
            showIconButton.gameObject.SetActive(false); // ��ʼ����
            // ��¼ԭʼλ�ò���
            var rt = showIconButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                showIconOrigAnchorMin = rt.anchorMin;
                showIconOrigAnchorMax = rt.anchorMax;
                showIconOrigPivot = rt.pivot;
                showIconOrigAnchoredPos = rt.anchoredPosition;
                showIconOrigSaved = true;
            }
        }
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        
        // ��ʼ��todo list������
        if (addTodoButton != null)
            addTodoButton.onClick.AddListener(OnAddTodoButtonClicked);
        if (confirmAddButton != null)
            confirmAddButton.onClick.AddListener(OnConfirmAddTodo);
        if (cancelAddButton != null)
            cancelAddButton.onClick.AddListener(OnCancelAddTodo);
            
        // ��ʼ����todo list���UI
        if (todoListContainer != null)
            todoListContainer.SetActive(false);
        if (newTodoInputPanel != null)
            newTodoInputPanel.SetActive(false);
        if (newTodoInput != null)
            newTodoInput.gameObject.SetActive(false);
        if (newTodoPriorityDropdown != null)
            newTodoPriorityDropdown.gameObject.SetActive(false);
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(false);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(false);
        
        // ˢ�������б�
        RefreshTodoList();
        
        DebugLogger.Instance?.AddLog($"Note initialized with collider: {noteCollider.size}");
    }

    private void OnValidate()
    {
        // Update collider when content is modified in the editor
        UpdateColliderSize();
    }

    private void UpdateColliderSize()
    {
        if (contentRectTransform != null && noteCollider != null)
        {            // Get Canvas and background image RectTransform
            RectTransform bgRectTransform = backgroundImage?.GetComponent<RectTransform>();
            
            if (bgRectTransform != null)
            {
                // Use background image size as reference
                Vector2 bgSize = bgRectTransform.rect.size;
                float worldWidth = bgRectTransform.lossyScale.x * bgSize.x;
                float worldHeight = bgRectTransform.lossyScale.y * bgSize.y;
                  // Add padding to make it easier to click
                worldWidth = Mathf.Max(worldWidth + padding.x, minColliderSize);
                worldHeight = Mathf.Max(worldHeight + padding.y, minColliderSize);
                
                // Set collider size, add depth for raycast detection
                Vector3 newSize = new Vector3(worldWidth, worldHeight, 0.05f);
                Vector3 newCenter = new Vector3(0, 0, -0.025f); // Center collider, slightly offset forward
                
                if (noteCollider.size != newSize || noteCollider.center != newCenter)
                {
                    noteCollider.size = newSize;
                    noteCollider.center = newCenter;
                    DebugLogger.Instance?.AddLog($"Updated collider size to: {newSize}, center: {newCenter}");
                }
            }
            else
            {                // If no background image, use fixed size
                Vector3 defaultSize = new Vector3(0.5f, 0.5f, 0.05f);
                noteCollider.size = defaultSize;
                noteCollider.center = new Vector3(0, 0, -0.025f);
                DebugLogger.Instance?.AddLog($"Using default collider size: {defaultSize}");
            }
        }
    }

    public void SetContent(string content)
    {
        data.content = content;
        if (contentText != null)
        {
            contentText.text = content;            // Update collider size after content change
            UpdateColliderSize();
        }
    }

    public void SetColor(Color color)
    {
        data.color = color;
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    public void Delete()
    {
        Destroy(gameObject);
    }

    // Set note visibility with fade effect
    public void SetVisible(bool isVisible)
    {
        data.isVisible = isVisible;
        // �������
        if (contentText != null) contentText.gameObject.SetActive(isVisible);
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(isVisible);
        if (annotationText != null) annotationText.gameObject.SetActive(isVisible);
        if (hideButton != null) hideButton.gameObject.SetActive(isVisible);
        if (deleteButton != null) deleteButton.gameObject.SetActive(isVisible);
        if (todoListScrollView != null) todoListScrollView.SetActive(isVisible);
        if (todoListContainer != null) todoListContainer.SetActive(isVisible);
        if (addTodoButton != null) addTodoButton.gameObject.SetActive(isVisible);
        // ֻ������ʱ����newTodoInputPanel
        if (!isVisible && newTodoInputPanel != null) newTodoInputPanel.SetActive(false);
        if (newTodoInput != null) newTodoInput.gameObject.SetActive(isVisible);
        if (newTodoPriorityDropdown != null) newTodoPriorityDropdown.gameObject.SetActive(isVisible);
        if (confirmAddButton != null) confirmAddButton.gameObject.SetActive(isVisible);
        if (cancelAddButton != null) cancelAddButton.gameObject.SetActive(isVisible);
        // С�۾�icon
        if (showIconButton != null)
        {
            showIconButton.gameObject.SetActive(!isVisible);
            var rt = showIconButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (isVisible && showIconOrigSaved)
                {
                    rt.anchorMin = showIconOrigAnchorMin;
                    rt.anchorMax = showIconOrigAnchorMax;
                    rt.pivot = showIconOrigPivot;
                    rt.anchoredPosition = showIconOrigAnchoredPos;
                }
                else if (!isVisible)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }
        // ��ײ��
        if (noteCollider != null)
            noteCollider.enabled = isVisible;
    }

    // Toggle note visibility with fade effect
    public void ToggleVisibility()
    {
        SetVisible(!data.isVisible);
    }

    // Set annotation
    public void SetAnnotation(string annotationText)
    {
        data.annotation = annotationText;
        UpdateAnnotationDisplay();
    }

    // Get annotation
    public string GetAnnotation()
    {
        return data.annotation;
    }

    // Update annotation display
    private void UpdateAnnotationDisplay()
    {
        if (annotationText != null)
        {
            annotationText.text = data.annotation;
            annotationText.gameObject.SetActive(!string.IsNullOrEmpty(data.annotation));
        }
    }

    // Restore note state from data (including annotation and visibility)
    public void RestoreFromData(NoteData savedData)
    {
        data = savedData;
        if (contentText != null)
            contentText.text = data.content;
        if (backgroundImage != null)
            backgroundImage.color = data.color;
        if (annotationText != null)
            UpdateAnnotationDisplay();        // Set position and rotation
        transform.position = data.position;
        transform.rotation = data.rotation;

        // Set visibility
        SetVisible(data.isVisible);

        // Update collider
        UpdateColliderSize();

        // �ָ�todo list״̬
        data.isTodoList = savedData.isTodoList;
        data.todoItems = savedData.todoItems;
        UpdateNoteType();
    }

    private void OnDeleteButtonClicked()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.DeleteNote(this);
        else
            Delete(); // ����
    }

    // ����Note������ʾ
    private void UpdateNoteType()
    {
        if (todoListContainer != null)
            todoListContainer.SetActive(data.isTodoList);
        
        // �����todo list����ʾ�����б�
        if (data.isTodoList)
        {
            RefreshTodoList();
        }
    }

    // ˢ�������б�
    private void RefreshTodoList()
    {
        if (todoListContainer == null || todoItemPrefab == null)
            return;

        // �����ȼ����򣨸�=1, ��=2, ��=3��
        data.todoItems.Sort((a, b) => a.priority.CompareTo(b.priority));

        // ��ȫ����������������
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in todoListContainer.transform)
        {
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
        {
            Destroy(go);
        }

        // �����µ�������
        foreach (var item in data.todoItems)
        {
            GameObject todoItemObj = Instantiate(todoItemPrefab, todoListContainer.transform);
            TodoItemUI todoItemUI = todoItemObj.GetComponent<TodoItemUI>();
            if (todoItemUI != null)
            {
                todoItemUI.Initialize(item, this);
            }
        }
    }

    // ���������ť����¼�
    private void OnAddTodoButtonClicked()
    {
        if (newTodoInputPanel != null)
            newTodoInputPanel.SetActive(true);
        if (newTodoInput != null)
        {
            newTodoInput.gameObject.SetActive(true);
            newTodoInput.text = "";
            newTodoInput.ActivateInputField();
        }
        if (newTodoPriorityDropdown != null)
        {
            newTodoPriorityDropdown.gameObject.SetActive(true);
            newTodoPriorityDropdown.value = 1; // Ĭ�������ȼ�
        }
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(true);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(true);
    }

    // ȷ���������
    private void OnConfirmAddTodo()
    {
        if (newTodoInput != null && !string.IsNullOrEmpty(newTodoInput.text))
        {
            int priority = 2; // Ĭ����
            if (newTodoPriorityDropdown != null)
                priority = newTodoPriorityDropdown.value + 1; // 0=��,1=��,2=��
            TodoItem newItem = new TodoItem
            {
                content = newTodoInput.text,
                isCompleted = false,
                priority = priority
            };
            data.todoItems.Add(newItem);
            // ���������������ˢ���б���ֹUI�¼���ͻ
            if (newTodoInputPanel != null) newTodoInputPanel.SetActive(false);
            RefreshTodoList();
        }
        // ��������UI
        if (newTodoInput != null)
            newTodoInput.gameObject.SetActive(false);
        if (newTodoPriorityDropdown != null)
            newTodoPriorityDropdown.gameObject.SetActive(false);
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(false);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(false);
    }

    // ȡ���������
    private void OnCancelAddTodo()
    {
        if (newTodoInputPanel != null)
            newTodoInputPanel.SetActive(false);
        if (newTodoInput != null)
            newTodoInput.gameObject.SetActive(false);
        if (newTodoPriorityDropdown != null)
            newTodoPriorityDropdown.gameObject.SetActive(false);
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(false);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(false);
    }

    // ��������״̬
    public void UpdateTodoItem(TodoItem item, bool isCompleted)
    {
        item.isCompleted = isCompleted;
        RefreshTodoList();
    }

    // ɾ������
    public void DeleteTodoItem(TodoItem item)
    {
        data.todoItems.Remove(item);
        RefreshTodoList();
    }

    // �༭��������
    public void EditTodoItem(TodoItem item, string newContent)
    {
        item.content = newContent;
        RefreshTodoList();
    }

    // �����������ȼ�
    public void UpdateTodoItemPriority(TodoItem item, int priority)
    {
        item.priority = priority;
        RefreshTodoList();
    }
}
