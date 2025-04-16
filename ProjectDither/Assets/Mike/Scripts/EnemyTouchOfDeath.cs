using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; 

[RequireComponent(typeof(AudioSource))] 
public class EnemyTouchOfDeath : MonoBehaviour
{
    [Tooltip("The EXACT name of the scene file to load (e.g., LoseScene). Must be in Build Settings!")]
    public string loseSceneName = "LoseScene"; 

    [Tooltip("Drag your 'Lose' sound effect audio clip here, my sweet cherry!")]
    public AudioClip loseSoundEffect; 

    // *** IMPORTANT: Replace 'PlayerMovement' with the ACTUAL name of YOUR player's movement script! ***
    [Tooltip("The exact name of the script component on your Player that handles movement (e.g., PlayerMovement, CharacterControllerScript). Case-sensitive!")]
    private string playerMovementScriptName = "NiPlayerMovement"; // <--- CHANGE THIS TO YOUR SCRIPT NAME

    private AudioSource enemyAudioSource; 
    private bool isLosing = false; 

    void Awake()
    {
        enemyAudioSource = GetComponent<AudioSource>(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player AND if we aren't already processing a loss.
        if (!isLosing && other.CompareTag("Player"))
        {
            Debug.Log("Player collision! Starting lose sequence."); 
            isLosing = true; 
            
            // --- Disable Player Movement ---
            // Find the component using the name you provided in the Inspector.
            // This requires your player script to inherit from MonoBehaviour (which it almost certainly does).
            MonoBehaviour playerScript = other.GetComponent(playerMovementScriptName) as MonoBehaviour; 

            if (playerScript != null)
            {
                playerScript.enabled = false; // Turn the script off! Freeze!
                Debug.Log($"Disabled player movement script: {playerMovementScriptName}");
            }
            else
            {
                // Uh oh, couldn't find the script! Maybe the name in the Inspector is wrong?
                Debug.LogError($"Could not find player movement script component named '{playerMovementScriptName}' on the player object '{other.gameObject.name}'! Cannot disable movement. Check the script name in the Inspector!"); 
            }
            // --- Movement Disabled (or warning shown) ---

            // Start the coroutine to handle sound and scene switch.
            StartCoroutine(LoseSequence()); 
        }
    }

    IEnumerator LoseSequence()
    {
        float delay = 0f; 

        // Play the sound!
        if (loseSoundEffect != null)
        {
            enemyAudioSource.PlayOneShot(loseSoundEffect);
            Debug.Log($"Playing sound: {loseSoundEffect.name}. Waiting for {loseSoundEffect.length} seconds.");
            delay = loseSoundEffect.length; 
        }
        else
        {
            Debug.LogWarning("No lose sound effect assigned. Switching scene immediately.");
        }

        // Wait after playing sound (or immediately if no sound)
        yield return new WaitForSeconds(delay); 

        // Switch Scene (Check Build Settings & scene name if this fails!)
        Debug.Log($"Wait finished! Attempting to load scene: {loseSceneName}");
        SceneManager.LoadScene(loseSceneName); 
    }

    // --- Setup Notes ---
    // 1. Attach to Enemy, ensure CapsuleCollider (Is Trigger), AudioSource.
    // 2. Player needs Collider, Rigidbody, "Player" tag.
    // 3. *** CRITICAL: *** Find the script on your Player GameObject that controls its movement. Note its EXACT name (e.g., `PlayerController`, `ThirdPersonMover`).
    // 4. *** CRITICAL: *** On the Enemy GameObject in the Inspector, find the `Enemy Touch Of Death` script component. In the `Player Movement Script Name` field, type the EXACT name of your player's movement script you found in step 3.
    // 5. Ensure `Lose Scene Name` is correct and the scene exists and is in Build Settings!
    // 6. Assign `Lose Sound Effect`.
}