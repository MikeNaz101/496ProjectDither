using UnityEngine;

public class Interactable1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(transform.position, transform.up, Color.red);
        Debug.DrawLine(transform.position, transform.right, Color.green);
        Debug.DrawLine(transform.position, transform.forward, Color.blue);
    }
}
