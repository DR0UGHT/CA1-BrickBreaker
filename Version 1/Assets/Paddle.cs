using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paddle : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(5f * Time.deltaTime * Vector3.left);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(5f * Time.deltaTime * Vector3.right);
        }
    }
}
