using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Runtime.CompilerServices;

public class MashingGameScript : MonoBehaviour
{

    private int numberOfClicks = 0;
    private int minNumberOfClicks = 25;
    private int maxNumberOfClicks = 50;

    private int goalClicks;

    public TextMeshProUGUI maxScoreText;
    public TextMeshProUGUI currentScoreText;

    private KeyCode selectedKey;
    private KeyCode[] possibleKeys =
    {
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E,
        KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J,
        KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O,
        KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T,
        KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z
    };

    void Start()
    {
        goalClicks = Random.Range(minNumberOfClicks, maxNumberOfClicks);
        UpdateCount();
        SelectRandomKey();
        Debug.Log(selectedKey.ToString());
    }

    void Update()
    {
        if (numberOfClicks >= goalClicks)
        {
            WinGame();
        }

        if (Input.GetKeyDown(selectedKey))
        {
            numberOfClicks++;
            UpdateCount();
            Debug.Log(numberOfClicks.ToString());
        }
    }

    private void UpdateCount()
    {
        maxScoreText.text = ("Goal Clicks: " + goalClicks);
        currentScoreText.text = ("Current Clicks: " + numberOfClicks);
    }

    private void SelectRandomKey()
    {
        int randomKeyNumber = Random.Range(0, possibleKeys.Length);
        selectedKey = possibleKeys[randomKeyNumber];
    }

    private void WinGame()
    {
        Debug.Log("You Win");
    }
}
