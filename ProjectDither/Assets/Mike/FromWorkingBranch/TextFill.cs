using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextFill : MonoBehaviour
{
    [SerializeField]
    string text;

    [Range(0f,0.5f)]
    public float speed = .25f;

    [SerializeField]
    public bool active = true;

    [SerializeField]
    TextFill toTrigger;

    [SerializeField]
    Button button;

    float t = 0;
    int letnum = 0;
    TextMeshProUGUI tmp;

    private void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = "";
    }


    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            t += Time.deltaTime;

            if (t > speed)
            {
                updateText();
            }
        }
    }

    void updateText()
    {
        letnum++;
        tmp.text = text.Substring(0,letnum);
        t = 0;
        if (letnum >= text.Length)
        {
            active = false;
            if (toTrigger != null)
            {
                toTrigger.active = true;
            }
            else if (button != null)
            {
                button.interactable = true;
            }
        }
    }
}
