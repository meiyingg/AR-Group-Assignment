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
    public string annotation = ""; // ����ע��
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI reference
    public Image backgroundImage;        // Note background image
    public TMP_Text annotationText;     // ע���ı���ʾ
    public NoteData data;               // Note data

    private BoxCollider noteCollider;
    private RectTransform contentRectTransform;
    private float minColliderSize = 0.1f; // ��С��ײ��ߴ�
    private Vector2 padding = new Vector2(0.15f, 0.15f); // ��ײ��߾�

    private void Awake()
    {
        if (data == null)
            data = new NoteData();
        
        // ȷ��Ĭ��ֵ
        data.color = Color.yellow;
        data.position = transform.position;
        data.rotation = transform.rotation;

        // ��ȡRectTransform
        contentRectTransform = contentText?.GetComponent<RectTransform>();
        
        // ������ײ��
        noteCollider = GetComponent<BoxCollider>();
        if (noteCollider == null)
        {
            noteCollider = gameObject.AddComponent<BoxCollider>();
            DebugLogger.Instance?.AddLog("Added BoxCollider to Note");
        }
        
        // ȷ����ײ��������ȷ
        noteCollider.isTrigger = false;
        
        // ��ʼ����ײ��ߴ�
        UpdateColliderSize();
        
        DebugLogger.Instance?.AddLog($"Note initialized with collider: {noteCollider.size}");
    }

    private void OnValidate()
    {
        // �ڱ༭�����޸�����ʱ������ײ��
        UpdateColliderSize();
    }

    private void UpdateColliderSize()
    {
        if (contentRectTransform != null && noteCollider != null)
        {
            // ��ȡCanvas�ͱ���ͼ��RectTransform
            RectTransform bgRectTransform = backgroundImage?.GetComponent<RectTransform>();
            
            if (bgRectTransform != null)
            {
                // ʹ�ñ���ͼ�ĳߴ���Ϊ��׼
                Vector2 bgSize = bgRectTransform.rect.size;
                float worldWidth = bgRectTransform.lossyScale.x * bgSize.x;
                float worldHeight = bgRectTransform.lossyScale.y * bgSize.y;
                
                // ���ӱ߾��Ա�����׵��
                worldWidth = Mathf.Max(worldWidth + padding.x, minColliderSize);
                worldHeight = Mathf.Max(worldHeight + padding.y, minColliderSize);
                
                // ������ײ���С����������Ա����߼��
                Vector3 newSize = new Vector3(worldWidth, worldHeight, 0.05f);
                Vector3 newCenter = new Vector3(0, 0, -0.025f); // ��ײ����У���΢��ǰƫ��
                
                if (noteCollider.size != newSize || noteCollider.center != newCenter)
                {
                    noteCollider.size = newSize;
                    noteCollider.center = newCenter;
                    DebugLogger.Instance?.AddLog($"Updated collider size to: {newSize}, center: {newCenter}");
                }
            }
            else
            {
                // ���û�б���ͼ��ʹ�ù̶���С
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
            // ���ݸı�������ײ��ߴ�
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

    // ���ñ�ǩ�ɼ���
    public void SetVisible(bool isVisible)
    {
        data.isVisible = isVisible;
        gameObject.SetActive(isVisible);
    }

    // �л���ǩ�ɼ���
    public void ToggleVisibility()
    {
        SetVisible(!data.isVisible);
    }

    // ����ע��
    public void SetAnnotation(string annotationText)
    {
        data.annotation = annotationText;
        UpdateAnnotationDisplay();
    }

    // ��ȡע��
    public string GetAnnotation()
    {
        return data.annotation;
    }

    // ����ע����ʾ
    private void UpdateAnnotationDisplay()
    {
        if (annotationText != null)
        {
            annotationText.text = data.annotation;
            annotationText.gameObject.SetActive(!string.IsNullOrEmpty(data.annotation));
        }
    }

    // �������лָ���ǩ״̬������ע�ͺͿɼ��ԣ�
    public void RestoreFromData(NoteData savedData)
    {
        data = savedData;
        if (contentText != null)
            contentText.text = data.content;
        if (backgroundImage != null)
            backgroundImage.color = data.color;
        if (annotationText != null)
            UpdateAnnotationDisplay();

        // ����λ�ú���ת
        transform.position = data.position;
        transform.rotation = data.rotation;

        // ���ÿɼ���
        gameObject.SetActive(data.isVisible);

        // ������ײ��
        UpdateColliderSize();
    }
}
