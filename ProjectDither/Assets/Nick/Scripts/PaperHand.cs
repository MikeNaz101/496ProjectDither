using TMPro;
using UnityEngine;

public class PaperHand : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI[] tmp;
    [SerializeField]
    TMPro.TMP_FontAsset font;

    public void setup(string[] arr)
    {
        if (arr.Length >= tmp.Length) {
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i].text = arr[i];
            }
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
