using UnityEngine;
using UnityEngine.InputSystem;

public class HandUI : MonoBehaviour
{
    [SerializeField]
    GameObject handSprite;
    [SerializeField]
    GameObject handParent;

    Animator Am;
    public bool moving = false;
    Vector2 mouseMovement = new Vector2();
    Vector2 movement = new Vector2();
    Vector2 InitHandPos;
    public float threshold = 60;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Am = handSprite.GetComponent<Animator>();
        InitHandPos = handParent.transform.position;
    }

    void Update()
    {
        handParent.transform.position += new Vector3(-mouseMovement.x, -mouseMovement.y,0);

        CheckHand();
        CenterHand();

        if ((movement.x > 0.1f || movement.y > 0.1f) || (movement.x < -0.1f || movement.y < -0.1f))
        {
            Am.SetBool("Moving", true);
        }
        else
        {
            Am.SetBool("Moving", false);
        }
    }

    void CheckHand()
    {
        if (handParent.transform.position.x > InitHandPos.x + threshold)
        {
            handParent.transform.position = new Vector3(InitHandPos.x + threshold, handParent.transform.position.y, 0);
        }
        if (handParent.transform.position.x < InitHandPos.x - threshold)
        {
            handParent.transform.position = new Vector3(InitHandPos.x - threshold, handParent.transform.position.y, 0);
        }

        if (handParent.transform.position.y > InitHandPos.y + threshold)
        {
            handParent.transform.position = new Vector3(handParent.transform.position.x, InitHandPos.y + threshold, 0);
        }
        if (handParent.transform.position.y < InitHandPos.y - threshold)
        {
            handParent.transform.position = new Vector3(handParent.transform.position.x, InitHandPos.y - threshold, 0);
        }
    }

void OnMove(InputValue moveVal)
{
    movement = moveVal.Get<Vector2>();
}

void CenterHand()
    {
        Vector3 distance = handParent.transform.position - new Vector3(InitHandPos.x, InitHandPos.y, 0);
        handParent.transform.position -= distance * Time.deltaTime * 2;
    }

    void OnLook(InputValue lookVal)
    {
        mouseMovement = lookVal.Get<Vector2>();
        //Debug.Log(mouseMovement);
    }

}
