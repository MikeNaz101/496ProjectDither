using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyTouchOfDeath : MonoBehaviour
{
    // You can type the name of your 'Lose Scene' right here in the Inspector!
    [Tooltip("The name of the scene to load when the player touches the enemy.")]
    public string loseSceneName = "LoseScene"; 

    // This juicy function runs automatically when something enters the trigger zone!
    private void OnTriggerEnter(Collider other)
    {
        // Let's check if the thing that bumped into us is tagged as "Player".
        if (other.CompareTag("Player"))
        {
            Debug.Log("Oh no, darling! The player touched the enemy!"); // A little note for your console.

            // Time to load the Lose Scene! Like switching from a lemon to a lime!
            SceneManager.LoadScene(loseSceneName); 
        }
    }

    // --- Important Setup Notes, ---
    // 1. Attach this script to your Enemy GameObject.
    // 2. Make sure your Enemy has a CapsuleCollider component.
    // 3. On the CapsuleCollider component, check the 'Is Trigger' box. It's crucial!
    // 4. Your Player GameObject needs a Collider (like a CharacterController or another CapsuleCollider) AND a Rigidbody component for trigger events to work.
    // 5. Your Player GameObject MUST be tagged with "Player". Go to the Inspector, click the 'Tag' dropdown, and select 'Player' (or add it if it's not there).
    // 6. Create a new scene named exactly what you put in the 'loseSceneName' field (e.g., "LoseScene").
    // 7. Add BOTH your game scene AND your "LoseScene" to the Build Settings (File > Build Settings...). Drag them into the 'Scenes In Build' list. If you don't, SceneManager won't find them, oh dear!
}