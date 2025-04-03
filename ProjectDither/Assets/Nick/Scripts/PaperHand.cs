using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PaperHand : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI[] tmp;
    [SerializeField]
    TMPro.TMP_FontAsset font;


    private void Start()
    {
        string[] strings = new string[2];
        strings[0] = "Test1";
        strings[1] = "Test2";
        setup(strings);
    }
    public void setup(string[] arr)
    {
        int i = 0;
        for (i = 0; i < arr.Length; i++)
        {
            tmp[i].text = arr[i];
        }
        while (i < 5)
        {
            Debug.Log(i);
            tmp[i].text = "";
            i++;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Completed("Task4");
        }
    }

    public void Completed(string name)
    {
        for (int i = 0; i < tmp.Length; i++)
        {
            if (tmp[i].text == name)
            {
                tmp[i].font = font;
            }
        }
    }
}
