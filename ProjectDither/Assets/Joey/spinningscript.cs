using UnityEngine;

public class spinningscript : MonoBehaviour
{
    public float speedspin = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, speedspin, 0); 
    }
}
