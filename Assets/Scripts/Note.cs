using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class NoteData
{
    public string content;
    public Vector3 position;
    public Quaternion rotation;
    public Color color;
    public bool isVisible = true;
    public string annotation = ""; // 单条注释
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI reference
    public Image backgroundImage;        // Note background image
    public TMP_Text annotationText;     // 注释文本显示
    public NoteData data;               // Note data

    private BoxCollider noteCollider;
    private RectTransform contentRectTransform;
    private float minColliderSize = 0.1f; // 最小碰撞体尺寸
    private Vector2 padding = new Vector2(0.15f, 0.15f); // 碰撞体边距

    private void Awake()
    {
        if (data == null)
            data = new NoteData();
        
        // 确保默认值
        data.color = Color.yellow;
        data.position = transform.position;
        data.rotation = transform.rotation;

        // 获取RectTransform
        contentRectTransform = contentText?.GetComponent<RectTransform>();
        
        // 设置碰撞体
        noteCollider = GetComponent<BoxCollider>();
        if (noteCollider == null)
        {
            noteCollider = gameObject.AddComponent<BoxCollider>();
            DebugLogger.Instance?.AddLog("Added BoxCollider to Note");
        }
        
        // 确保碰撞体设置正确
        noteCollider.isTrigger = false;
        
        // 初始化碰撞体尺寸
        UpdateColliderSize();
        
        DebugLogger.Instance?.AddLog($"Note initialized with collider: {noteCollider.size}");
    }

    private void OnValidate()
    {
        // 在编辑器中修改内容时更新碰撞体
        UpdateColliderSize();
    }

    private void UpdateColliderSize()
    {
        if (contentRectTransform != null && noteCollider != null)
        {
            // 获取Canvas和背景图的RectTransform
            RectTransform bgRectTransform = backgroundImage?.GetComponent<RectTransform>();
            
            if (bgRectTransform != null)
            {
                // 使用背景图的尺寸作为基准
                Vector2 bgSize = bgRectTransform.rect.size;
                float worldWidth = bgRectTransform.lossyScale.x * bgSize.x;
                float worldHeight = bgRectTransform.lossyScale.y * bgSize.y;
                
                // 增加边距以便更容易点击
                worldWidth = Mathf.Max(worldWidth + padding.x, minColliderSize);
                worldHeight = Mathf.Max(worldHeight + padding.y, minColliderSize);
                
                // 设置碰撞体大小，增加深度以便射线检测
                Vector3 newSize = new Vector3(worldWidth, worldHeight, 0.05f);
                Vector3 newCenter = new Vector3(0, 0, -0.025f); // 碰撞体居中，稍微向前偏移
                
                if (noteCollider.size != newSize || noteCollider.center != newCenter)
                {
                    noteCollider.size = newSize;
                    noteCollider.center = newCenter;
                    DebugLogger.Instance?.AddLog($"Updated collider size to: {newSize}, center: {newCenter}");
                }
            }
            else
            {
                // 如果没有背景图，使用固定大小
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
            contentText.text = content;
            // 内容改变后更新碰撞体尺寸
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

    // 设置便签可见性
    public void SetVisible(bool isVisible)
    {
        data.isVisible = isVisible;
        gameObject.SetActive(isVisible);
    }

    // 切换便签可见性
    public void ToggleVisibility()
    {
        SetVisible(!data.isVisible);
    }

    // 设置注释
    public void SetAnnotation(string annotationText)
    {
        data.annotation = annotationText;
        UpdateAnnotationDisplay();
    }

    // 获取注释
    public string GetAnnotation()
    {
        return data.annotation;
    }

    // 更新注释显示
    private void UpdateAnnotationDisplay()
    {
        if (annotationText != null)
        {
            annotationText.text = data.annotation;
            annotationText.gameObject.SetActive(!string.IsNullOrEmpty(data.annotation));
        }
    }

    // 从数据中恢复便签状态（包括注释和可见性）
    public void RestoreFromData(NoteData savedData)
    {
        data = savedData;
        if (contentText != null)
            contentText.text = data.content;
        if (backgroundImage != null)
            backgroundImage.color = data.color;
        if (annotationText != null)
            UpdateAnnotationDisplay();

        // 设置位置和旋转
        transform.position = data.position;
        transform.rotation = data.rotation;

        // 设置可见性
        gameObject.SetActive(data.isVisible);

        // 更新碰撞体
        UpdateColliderSize();
    }
}
