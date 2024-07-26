using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DesktopControlsHints : MonoBehaviour
{
    public List<Sprite> Hints;
    public Image image;

    public Button next;
    public Button hide;

    private int index = 0;

    private void Start()
    {
        next.onClick.AddListener(Next);
        hide.onClick.AddListener(() => { this.gameObject.SetActive(false); });
    }

    public void Next()
    {
        index = (index + 1) % Hints.Count;
        image.sprite = Hints[index];
    }
}
