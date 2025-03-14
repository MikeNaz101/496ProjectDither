using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using TMPro;

//Added Below
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//Added Above

public class ComputerMiniGameTest : MonoBehaviour
{

    [SerializeField] float speed = 50f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private int score = 0;
    public TextMeshProUGUI scoreText;
    
    //Added Below
    public TextMeshProUGUI gameOverText;
    public bool isGameActive;
    public Button restartButton;
    //Added Above

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Added Below
        isGameActive = true;
        //Added Above

        rb = GetComponent<Rigidbody2D>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        scoreText.text = "Score: " + score;
    }


    // Update is called once per frame
    void Update()
    {
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(horizontalMovement, verticalMovement).normalized;



        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddScore(5);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = (moveInput * speed);
    }

    private void AddScore(int points)
    {
        score += points;
        scoreText.text = "Score: " + score;
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }

    //Added Below
    public void GameOver()
    {
        restartButton.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        isGameActive = false;
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    //Added Above
}
