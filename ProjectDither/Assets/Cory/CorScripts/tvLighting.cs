using UnityEngine;

public class tvLighting : MonoBehaviour
{
    [SerializeField] private GameObject tvLight;
    void Start()
    {
        tvLight.GetComponentInChildren<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        /*while (light)
        {

        }*/
    }
}
