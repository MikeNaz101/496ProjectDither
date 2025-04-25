using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager1 : MonoBehaviour
{
    public AudioSource theMusic;

    public bool startPlaying;

    public BeatScroller theBS;

    public static GameManager1 instance;


    public int currentScore;
    public int scorePerNote = 100;
    public int scorePerGoodNote = 125;
    public int scorePerPerfectNote = 150;

    public int currentMultiplier;
    public int multiplierTracker;
    public int[] multiplierThresholds;

    public Text scoreText;
    public Text multiText;

    public float totalNotes;
    public float normalHits;
    public float goodHits;
    public float perfectHits;
    public float missedHits;

    public GameObject resultsScreen;
    public Text percentHitText, normalsText, goodsText, perfectsText, missesText, rankText, finalScoreText;

    public int winScore = 10350;
    private bool gameEnded = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;

        scoreText.text = "Score: 0";
        currentMultiplier = 1;

        //totalNotes = FindFirstObjectByType<NoteObject>().Length;
    }

    void ResetGame()
    {
        currentScore = 0;
        currentMultiplier = 1;
        multiplierTracker = 0;

        scoreText.text = "Score: 0";
        multiText.text = "Multiplier: x1";

        normalHits = goodHits = perfectHits = missedHits = 0;

        startPlaying = false;
        gameEnded = false;

        resultsScreen.SetActive(false);

        /*foreach (NoteObject note in FindObjectsOfType<NoteObject>())
        {
            note.gameObject.SetActive(true);
            note.hasBeenHit = false;
        }*/

        theBS.hasStarted = false;
        theMusic.Stop();
        theMusic.time = 0f;

        FindFirstObjectByType<NoteSpawner>().SpawnNotes();
    }

    // Update is called once per frame
    void Update()
    {
     if (!startPlaying)
     {
            if(Input.anyKeyDown)
            {
                startPlaying = true;
                theBS.hasStarted = true;

                theMusic.Play();
            }
        }
     else if (!gameEnded)
        {
            if (currentScore >= winScore)
            {
                gameEnded = true;
                theMusic.Stop();
                theBS.hasStarted = false;

                Debug.Log("You Win!");
                resultsScreen.SetActive(false);
            }

            /*  if(!theMusic.isPlaying && !resultsScreen.activeInHierarchy)
                {
                    resultsScreen.SetActive(true);

                    normalsText.text = "" + normalHits;
                    goodsText.text = goodHits.ToString();
                    perfectsText.text = perfectHits.ToString();
                    missesText.text = "" + missedHits;

                    float totalHit = normalHits + goodHits + perfectHits;
                    float percentHit = (totalHit / totalNotes) * 100f;

                    percentHitText.text = percentHit.ToString("F1") + "%";

                    string rankVal = "F";
                    if(percentHit > 40)
                    {
                        rankVal = "D";
                        if(percentHit > 55)
                        {
                            rankVal = "C";
                            if(percentHit > 70)
                            {
                                rankVal = "B";
                                    if(percentHit > 85)
                                    {
                                        rankVal = "A";
                                        if(percentHit > 95)
                                        {
                                            rankVal = "S";
                                        }
                                    }
                            }
                        }
                    } 
                    rankText.text = rankVal;

                    finalScoreText.text = currentScore.ToString();
                } */

            else if (!theMusic.isPlaying) 
            {
                Debug.Log("You lose! Restarting. . .");
                ResetGame();
            }
     }
    }

    public void NoteHit()
    {
        Debug.Log("Hit On Time");
        if (currentMultiplier - 1 < multiplierThresholds.Length)
        {
            multiplierTracker++;

            if (multiplierThresholds[currentMultiplier - 1] <= multiplierTracker)
            {
                multiplierTracker = 0;
                currentMultiplier++;
            }
        }

        multiText.text = "Multiplier: x" + currentMultiplier;

        //currentScore += scorePerNote * currentMultiplier;
        scoreText.text = "Score: " + currentScore;

    }

    public void NormalHit()
    {
        currentScore += scorePerNote * currentMultiplier;
        NoteHit();

        normalHits++;
    }

    public void GoodHit()
    {
        currentScore += scorePerGoodNote * currentMultiplier;
        NoteHit();

        goodHits++;
    }

    public void PerfectHit()
    {
        currentScore += scorePerPerfectNote * currentMultiplier;
        NoteHit();

        perfectHits++;
    }

    public void NoteMissed()
    {
        Debug.Log("Missed Note");

        currentMultiplier = 1;
        multiplierTracker = 0;

        multiText.text = "Multiplier: x" + currentMultiplier;

        missedHits++;
    }

    
}
