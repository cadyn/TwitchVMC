using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleScrolling : MonoBehaviour
{
    public RectTransform content;
    private RectTransform selfTransform;
    
    void Start()
    {
        selfTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        selfTransform.anchoredPosition = new Vector2(content.anchoredPosition.x, selfTransform.anchoredPosition.y);
    }
}
