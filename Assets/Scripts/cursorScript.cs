using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cursorScript : MonoBehaviour
{
    public Texture2D cursorTex;

    void Start()
    {
        Vector2 hotspot = new Vector2(0, 0); // tip of sword in pixels (top-left origin)
        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
    }
}
