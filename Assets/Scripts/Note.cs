using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[System.Serializable]
public class TodoItem
{
    public string content;        // 任务内容
    public bool isCompleted;      // 完成状态
    public DateTime dueDate;      // 截止日期（可选）
    public int priority;          // 优先级（1-3）
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
    public bool isTodoList = false;  // 是否是todo list
    public List<TodoItem> todoItems = new List<TodoItem>();  // 任务列表
    public int backgroundIndex = 0;  // 背景图片索引
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI reference
    public Image backgroundImage;        // Note background image
    public TMP_Text annotationText;     // Annotation text display
    public NoteData data;               // Note data
    public Button hideButton; // 按钮：隐藏Note
    public Button showIconButton; // 隐藏时显示的小icon（Button）
    public Button deleteButton; // 删除按钮
    public GameObject todoListContainer;  // 任务列表容器
    public GameObject todoItemPrefab;     // 任务项预制体
    public Button addTodoButton;          // 添加任务按钮
    public GameObject newTodoInputPanel;        // 新建任务输入面板
    public TMP_InputField newTodoInput;         // 新建任务输入框
    public TMP_Dropdown newTodoPriorityDropdown;// 新建任务优先级Dropdown
    public Button confirmAddButton;             // 确认添加按钮
    public Button cancelAddButton;              // 取消添加按钮
    public GameObject todoListScrollView; // 指向TodoListScroll_Scroll View根对象
    public GameObject backgroundSelectPanel;  // 背景选择面板
    public Transform backgroundButtonContainer;  // 背景按钮容器
    public GameObject backgroundButtonPrefab;  // 背景按钮预制体
    public Button changeBackgroundButton;  // 更换背景按钮

    private BoxCollider noteCollider;
    private RectTransform contentRectTransform;    private float minColliderSize = 0.1f; // Minimum collider size
    private Vector2 padding = new Vector2(0.15f, 0.15f); // Collider padding

    // 记录ShowIcon原始位置参数
    private Vector2 showIconOrigAnchorMin, showIconOrigAnchorMax, showIconOrigPivot, showIconOrigAnchoredPos;
    private bool showIconOrigSaved = false;

    private List<Sprite> backgroundSprites = new List<Sprite>();

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
        
        // 按钮事件绑定
        if (hideButton != null)
            hideButton.onClick.AddListener(() => SetVisible(false));
        if (showIconButton != null)
        {
            showIconButton.onClick.AddListener(() => SetVisible(true));
            showIconButton.gameObject.SetActive(false); // 初始隐藏
            // 记录原始位置参数
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
        
        // 初始化todo list相关组件
        if (addTodoButton != null)
            addTodoButton.onClick.AddListener(OnAddTodoButtonClicked);
        if (confirmAddButton != null)
            confirmAddButton.onClick.AddListener(OnConfirmAddTodo);
        if (cancelAddButton != null)
            cancelAddButton.onClick.AddListener(OnCancelAddTodo);
            
        // 初始隐藏todo list相关UI
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
        
        // 刷新任务列表
        RefreshTodoList();
        
        DebugLogger.Instance?.AddLog($"Note initialized with collider: {noteCollider.size}");
        
        // 初始化背景选择相关
        if (changeBackgroundButton != null)
            changeBackgroundButton.onClick.AddListener(OnChangeBackgroundButtonClicked);
        if (backgroundSelectPanel != null)
            backgroundSelectPanel.SetActive(false);
            
        // 加载背景图片
        LoadBackgroundSprites();
    }

    private void OnValidate()
    {
        // Update collider when content is modified in the editor
        UpdateColliderSize();
    }

    private void UpdateColliderSize()
    {
        if (contentRectTransform != null && noteCollider != null)
        {
            RectTransform bgRectTransform = backgroundImage?.GetComponent<RectTransform>();
            if (bgRectTransform != null)
            {
                Vector3[] worldCorners = new Vector3[4];
                bgRectTransform.GetWorldCorners(worldCorners);
                float worldWidth = Vector3.Distance(worldCorners[0], worldCorners[3]);
                float worldHeight = Vector3.Distance(worldCorners[0], worldCorners[1]);
                // 缩小75%
                worldWidth *= 0.75f;
                worldHeight *= 0.75f;
                worldWidth = Mathf.Max(worldWidth + padding.x, minColliderSize);
                worldHeight = Mathf.Max(worldHeight + padding.y, minColliderSize);
                Vector3 newSize = new Vector3(worldWidth, worldHeight, 0.05f);
                Vector3 newCenter = Vector3.zero; // 无偏移
                if (noteCollider.size != newSize || noteCollider.center != newCenter)
                {
                    noteCollider.size = newSize;
                    noteCollider.center = newCenter;
                    DebugLogger.Instance?.AddLog($"Updated collider size to: {newSize}, center: {newCenter}");
                }
            }
            else
            {
                Vector3 defaultSize = new Vector3(0.5f, 0.5f, 0.05f);
                noteCollider.size = defaultSize;
                noteCollider.center = Vector3.zero;
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
        // 内容相关
        if (contentText != null) contentText.gameObject.SetActive(isVisible);
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(isVisible);
        if (annotationText != null)
        {
            bool showAnno = isVisible && !string.IsNullOrEmpty(data.annotation);
            annotationText.gameObject.SetActive(showAnno);
        }
        if (hideButton != null) hideButton.gameObject.SetActive(isVisible);
        if (deleteButton != null) deleteButton.gameObject.SetActive(isVisible);
        if (todoListScrollView != null) todoListScrollView.SetActive(isVisible);
        if (todoListContainer != null) todoListContainer.SetActive(isVisible);
        if (addTodoButton != null) addTodoButton.gameObject.SetActive(isVisible);
        // 只在隐藏时隐藏newTodoInputPanel
        if (!isVisible && newTodoInputPanel != null) newTodoInputPanel.SetActive(false);
        if (newTodoInput != null) newTodoInput.gameObject.SetActive(isVisible);
        if (newTodoPriorityDropdown != null) newTodoPriorityDropdown.gameObject.SetActive(isVisible);
        if (confirmAddButton != null) confirmAddButton.gameObject.SetActive(isVisible);
        if (cancelAddButton != null) cancelAddButton.gameObject.SetActive(isVisible);
        // 新增：隐藏/显示背景选择面板
        if (backgroundSelectPanel != null) backgroundSelectPanel.SetActive(isVisible);
        // 新增：隐藏/显示ChangeBackgroundButton
        if (changeBackgroundButton != null) changeBackgroundButton.gameObject.SetActive(isVisible);
        // 小眼睛icon
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
        // 碰撞体
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

        // 恢复todo list状态
        data.isTodoList = savedData.isTodoList;
        data.todoItems = savedData.todoItems;
        UpdateNoteType();

        // 恢复背景图片
        if (backgroundSprites.Count > 0 && savedData.backgroundIndex < backgroundSprites.Count)
        {
            backgroundImage.sprite = backgroundSprites[savedData.backgroundIndex];
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.DeleteNote(this);
        else
            Delete(); // 兜底
    }

    // 更新Note类型显示
    private void UpdateNoteType()
    {
        if (todoListContainer != null)
            todoListContainer.SetActive(data.isTodoList);
        
        // 如果是todo list，显示任务列表
        if (data.isTodoList)
        {
            RefreshTodoList();
        }
    }

    // 刷新任务列表
    private void RefreshTodoList()
    {
        if (todoListContainer == null || todoItemPrefab == null)
            return;

        // 按优先级排序（高=1, 中=2, 低=3）
        data.todoItems.Sort((a, b) => a.priority.CompareTo(b.priority));

        // 安全地销毁现有任务项
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in todoListContainer.transform)
        {
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
        {
            Destroy(go);
        }

        // 创建新的任务项
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

    // 添加新任务按钮点击事件
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
            newTodoPriorityDropdown.value = 1; // 默认中优先级
        }
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(true);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(true);
    }

    // 确认添加任务
    private void OnConfirmAddTodo()
    {
        if (newTodoInput != null && !string.IsNullOrEmpty(newTodoInput.text))
        {
            int priority = 2; // 默认中
            if (newTodoPriorityDropdown != null)
                priority = newTodoPriorityDropdown.value + 1; // 0=高,1=中,2=低
            TodoItem newItem = new TodoItem
            {
                content = newTodoInput.text,
                isCompleted = false,
                priority = priority
            };
            data.todoItems.Add(newItem);
            // 先隐藏输入面板再刷新列表，防止UI事件冲突
            if (newTodoInputPanel != null) newTodoInputPanel.SetActive(false);
            RefreshTodoList();
        }
        // 隐藏输入UI
        if (newTodoInput != null)
            newTodoInput.gameObject.SetActive(false);
        if (newTodoPriorityDropdown != null)
            newTodoPriorityDropdown.gameObject.SetActive(false);
        if (confirmAddButton != null)
            confirmAddButton.gameObject.SetActive(false);
        if (cancelAddButton != null)
            cancelAddButton.gameObject.SetActive(false);
    }

    // 取消添加任务
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

    // 更新任务状态
    public void UpdateTodoItem(TodoItem item, bool isCompleted)
    {
        item.isCompleted = isCompleted;
        RefreshTodoList();
    }

    // 删除任务
    public void DeleteTodoItem(TodoItem item)
    {
        data.todoItems.Remove(item);
        RefreshTodoList();
    }

    // 编辑任务内容
    public void EditTodoItem(TodoItem item, string newContent)
    {
        item.content = newContent;
        RefreshTodoList();
    }

    // 更新任务优先级
    public void UpdateTodoItemPriority(TodoItem item, int priority)
    {
        item.priority = priority;
        RefreshTodoList();
    }

    private void LoadBackgroundSprites()
    {
        backgroundSprites.Clear();
        Sprite[] sprites = Resources.LoadAll<Sprite>("NoteBackgrounds");
        backgroundSprites.AddRange(sprites);
        // 如果有背景图片，设置初始背景
        if (backgroundSprites.Count > 0 && data.backgroundIndex < backgroundSprites.Count)
        {
            backgroundImage.sprite = backgroundSprites[data.backgroundIndex];
        }
    }

    private void PopulateBackgroundButtons()
    {
        // 清除现有按钮
        foreach (Transform child in backgroundButtonContainer)
        {
            Destroy(child.gameObject);
        }
        // 创建新按钮
        for (int i = 0; i < backgroundSprites.Count; i++)
        {
            int index = i; // 闭包
            GameObject btnObj = Instantiate(backgroundButtonPrefab, backgroundButtonContainer);
            btnObj.GetComponent<Image>().sprite = backgroundSprites[i];
            btnObj.GetComponent<Button>().onClick.RemoveAllListeners();
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnSelectBackground(index));
        }
    }

    public void OnChangeBackgroundButtonClicked()
    {
        if (backgroundSelectPanel != null)
        {
            bool isActive = backgroundSelectPanel.activeSelf;
            backgroundSelectPanel.SetActive(!isActive);
            if (!isActive)
            {
                PopulateBackgroundButtons();
            }
        }
    }

    public void OnSelectBackground(int index)
    {
        if (index >= 0 && index < backgroundSprites.Count)
        {
            backgroundImage.sprite = backgroundSprites[index];
            data.backgroundIndex = index;
        }
        backgroundSelectPanel.SetActive(false);
    }
}
