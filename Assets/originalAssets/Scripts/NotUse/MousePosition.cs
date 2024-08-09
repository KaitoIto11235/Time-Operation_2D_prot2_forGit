using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MousePosition : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            Debug.Log("x:" + screen_mousePos.x + "    y:" + screen_mousePos.y);
            this.transform.position = screen_mousePos;
        }
    }
}
