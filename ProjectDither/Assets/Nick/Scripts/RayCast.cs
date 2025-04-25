using UnityEngine;
using UnityEngine.InputSystem;

public class RayCast : MonoBehaviour
{
    [SerializeField]
    float speed = 2.0f;
    [SerializeField]
    float mouseSensitivity = 100;
    [SerializeField]
    Camera playerCam;
    [SerializeField]
    float interactionRange = 3.0f;

    public Transform rayray;

    [SerializeField]

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        //chara = GetComponent<CharacterController>();
        playerCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {




        //Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        //Ray ray = new Ray(playerCam.transform.position, playerCam.transform.right);

        //Debug.DrawLine(ray.origin, playerCam.transform.up, Color.red);
        //Debug.DrawLine(ray.origin, playerCam.transform.right, Color.green);
        //Debug.DrawLine(ray.origin, playerCam.transform.forward, Color.blue);

        //Debug.Log("Forward: " + playerCam.transform.forward);
        //Debug.Log("Right: " + playerCam.transform.right);
        //Debug.Log("Up: " + playerCam.transform.up);

        RaycastHit hit;
        Debug.DrawLine(rayray.position, rayray.forward, Color.red);
        if (Physics.Raycast(rayray.position, rayray.forward, out hit, 5))
        {
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.tag == "Interactable")
                {
                    Debug.Log("hit");
                }
            }
            else
            {
                //hui.makeInteractVisible(false);
            }
            //Debug.Log("Interacted with: " + hit.collider.gameObject.name);
        }
        //Interact when clicked
    }



}