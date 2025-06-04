using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TodoItemUI : MonoBehaviour
{
    public Toggle completeToggle;
    public TMP_Text contentText;
    public Button deleteButton;
    public TMP_Dropdown priorityDropdown;

    private TodoItem todoItem;
    private Note parentNote;

    private void Awake()
    {
        // ��ʼ����ť�¼�
        if (completeToggle != null)
            completeToggle.onValueChanged.AddListener(OnCompleteToggleChanged);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        if (priorityDropdown != null)
            priorityDropdown.onValueChanged.AddListener(OnPriorityChanged);
    }

    public void Initialize(TodoItem item, Note note)
    {
        todoItem = item;
        parentNote = note;
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.RemoveAllListeners();
            completeToggle.onValueChanged.AddListener(OnCompleteToggleChanged);
        }
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
        if (priorityDropdown != null)
        {
            priorityDropdown.onValueChanged.RemoveAllListeners();
            priorityDropdown.onValueChanged.AddListener(OnPriorityChanged);
        }
        UpdateUI();
    }

    private void UpdatePriorityColor(int priority)
    {
        if (priorityDropdown == null) return;
        var img = priorityDropdown.GetComponentInChildren<Image>();
        if (img == null) return;
        switch (priority)
        {
            case 1: // ��
                img.color = new Color(1f, 0.3f, 0.3f); // ��
                break;
            case 2: // ��
                img.color = new Color(1f, 0.6f, 0.2f); // ��
                break;
            case 3: // ��
                img.color = new Color(0.3f, 1f, 0.3f); // ��
                break;
            default:
                img.color = Color.white;
                break;
        }
    }

    private void UpdateUI()
    {
        if (contentText != null)
        {
            contentText.text = todoItem.content;
            contentText.fontStyle = todoItem.isCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
        }
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.RemoveListener(OnCompleteToggleChanged);
            completeToggle.isOn = todoItem.isCompleted;
            completeToggle.onValueChanged.AddListener(OnCompleteToggleChanged);
        }
        if (priorityDropdown != null)
        {
            priorityDropdown.onValueChanged.RemoveListener(OnPriorityChanged);
            priorityDropdown.value = todoItem.priority - 1;
            priorityDropdown.onValueChanged.AddListener(OnPriorityChanged);
            UpdatePriorityColor(todoItem.priority);
        }
    }

    private void OnCompleteToggleChanged(bool isCompleted)
    {
        if (parentNote != null)
            parentNote.UpdateTodoItem(todoItem, isCompleted);
    }

    private void OnDeleteButtonClicked()
    {
        if (parentNote != null)
            parentNote.DeleteTodoItem(todoItem);
    }

    private void OnPriorityChanged(int value)
    {
        if (parentNote != null)
            parentNote.UpdateTodoItemPriority(todoItem, value + 1);
        UpdatePriorityColor(value + 1);
    }
} 