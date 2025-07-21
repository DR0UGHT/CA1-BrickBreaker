using UnityEngine;

public class Paddle : MonoBehaviour
{
    
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
