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
}

public class Note : MonoBehaviour
{
    public TMP_Text contentText;        // TextMeshPro UI 组件引用
    public Image backgroundImage;        // 笔记背景图片
    public NoteData data;               // 笔记数据

    private BoxCollider noteCollider;

    private void Awake()
    {
        if (data == null)
            data = new NoteData();
        
        // 确保默认值
        data.color = Color.yellow;
        data.position = transform.position;
        data.rotation = transform.rotation;

        // 确保有碰撞体用于交互
        noteCollider = GetComponent<BoxCollider>();
        if (noteCollider == null)
        {
            noteCollider = gameObject.AddComponent<BoxCollider>();
            noteCollider.size = new Vector3(1f, 1f, 0.1f); // 设置合适的碰撞体大小
        }
    }

    public void SetContent(string content)
    {
        data.content = content;
        if (contentText != null)
            contentText.text = content;
    }    public void SetColor(Color color)
    {
        data.color = color;
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    public void UpdateTransform()
    {
        data.position = transform.position;
        data.rotation = transform.rotation;
    }

    public void Delete()
    {
        Destroy(gameObject);
    }

    public void RestoreFromData(NoteData savedData)
    {
        data = savedData;
        transform.position = data.position;
        transform.rotation = data.rotation;
        SetContent(data.content);
        SetColor(data.color);
    }
}
