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
    public string annotation = ""; // Single annotation
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI reference
    public Image backgroundImage;        // Note background image
    public TMP_Text annotationText;     // Annotation text display
    public NoteData data;               // Note data

    private BoxCollider noteCollider;
    private RectTransform contentRectTransform;    private float minColliderSize = 0.1f; // Minimum collider size
    private Vector2 padding = new Vector2(0.15f, 0.15f); // Collider padding

    private void Awake()
    {
        if (data == null)
            data = new NoteData();
          // Ensure default values
        data.color = Color.yellow;
        data.position = transform.position;
        data.rotation = transform.rotation;        // Get RectTransform
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
        
        DebugLogger.Instance?.AddLog($"Note initialized with collider: {noteCollider.size}");
    }    private void OnValidate()
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
    }    // Set note visibility
    public void SetVisible(bool isVisible)
    {
        data.isVisible = isVisible;
        gameObject.SetActive(isVisible);
    }

    // Toggle note visibility
    public void ToggleVisibility()
    {
        SetVisible(!data.isVisible);
    }    // Set annotation
    public void SetAnnotation(string annotationText)
    {
        data.annotation = annotationText;
        UpdateAnnotationDisplay();
    }

    // Get annotation
    public string GetAnnotation()
    {
        return data.annotation;
    }    // Update annotation display
    private void UpdateAnnotationDisplay()
    {
        if (annotationText != null)
        {
            annotationText.text = data.annotation;
            annotationText.gameObject.SetActive(!string.IsNullOrEmpty(data.annotation));
        }
    }    // Restore note state from data (including annotation and visibility)
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
        gameObject.SetActive(data.isVisible);

        // Update collider
        UpdateColliderSize();
    }
}
