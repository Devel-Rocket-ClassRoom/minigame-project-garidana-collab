using UnityEngine;
using UnityEngine.UI;

public class SoftwareCursor : MonoBehaviour
{
    [SerializeField] private RectTransform cursorImage;
    [SerializeField] private float cursorSize = 64f; // 원하는 크기로 조절

    void Start()
    {
        Cursor.visible = false; // 하드웨어 커서 숨김
        cursorImage.sizeDelta = new Vector2(cursorSize, cursorSize);
    }

    void Update()
    {
        cursorImage.position = Input.mousePosition;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Cursor.visible = !hasFocus;
    }
}