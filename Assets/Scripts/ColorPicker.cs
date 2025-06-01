using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorPicker : MonoBehaviour
{
    public List<Button> colorButtons;
    private Color currentColor = Color.yellow; // Default color

    void Start()
    {
        InitializeColorButtons();
    }

    void InitializeColorButtons()
    {
        for (int i = 0; i < colorButtons.Count; i++)
        {
            Button btn = colorButtons[i];
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                Color buttonColor = img.color;
                btn.onClick.AddListener(() => SetCurrentColor(buttonColor));
            }
        }
    }

    public void SetCurrentColor(Color color)
    {
        currentColor = color;
    }

    public Color CurrentColor
    {
        get { return currentColor; }
    }
}
