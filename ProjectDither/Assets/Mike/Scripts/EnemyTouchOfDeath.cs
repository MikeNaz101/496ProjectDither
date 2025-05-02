using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnemyTouchOfDeath : MonoBehaviour
{
    [Tooltip("The EXACT name of the scene file to load (e.g., LoseScene). Must be in Build Settings!")]
    public string loseSceneName = "LoseScene";

    [Tooltip("Drag your 'Lose' sound effect audio clip here!")]
    public AudioClip loseSoundEffect;

    [Tooltip("The name of the player's movement script.")]
    public string playerMovementScriptName = "NiPlayerMovement";

    [Tooltip("The speed at which the player turns to face the enemy.  A value like 0.1 will be very slow.")]
    public float playerTurnSpeed = 0.1f; // Keep this slow

    private AudioSource enemyAudioSource;
    private bool isLosing = false;
    private Transform enemyFaceTransform;
    private GameObject playerObject;
    private NiPlayerMovement playerMovementScript;

    void Awake()
    {
        enemyAudioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLosing && other.CompareTag("Player"))
        {
            Debug.Log("Player collision! Starting lose sequence.");
            isLosing = true;
            playerObject = other.gameObject;

            playerMovementScript = playerObject.GetComponent<NiPlayerMovement>();
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = false;
                playerMovementScript.canShoot = false;
                Debug.Log("Disabled player movement and shooting.");
            }
            else
            {
                Debug.LogError($"Could not find player movement script '{playerMovementScriptName}' on the player.");
            }

            if (transform.Find("face") != null)
            {
                enemyFaceTransform = transform.Find("face");
                Debug.Log($"Found enemy face: {enemyFaceTransform.name}");
                // Start turning AND play the sound
                if (loseSoundEffect != null)
                {
                    enemyAudioSource.PlayOneShot(loseSoundEffect);
                    Debug.Log($"Playing sound: {loseSoundEffect.name}.");
                }
                else
                {
                    Debug.LogWarning("No lose sound effect assigned.");
                }
                StartCoroutine(TurnAndLoseSequence());
            }
            else
            {
                Debug.LogWarning($"Enemy '{gameObject.name}' does not have a child object named 'face'. Loading lose scene immediately.");
                SceneManager.LoadScene(loseSceneName);
            }
        }
    }

    IEnumerator TurnAndLoseSequence()
    {
        if (playerObject == null || enemyFaceTransform == null)
        {
            Debug.LogError("Player or enemy face is null. Loading lose scene immediately.");
            SceneManager.LoadScene(loseSceneName);
            yield break;
        }

        Quaternion targetRotation = Quaternion.LookRotation(enemyFaceTransform.position - playerObject.transform.position);
        float rotationProgress = 0f;

        while (rotationProgress < 1f)
        {
            playerObject.transform.rotation = Quaternion.Slerp(playerObject.transform.rotation, targetRotation, rotationProgress);
            rotationProgress += Time.deltaTime * playerTurnSpeed;
            yield return null;
        }

        playerObject.transform.rotation = targetRotation; // Ensure final rotation

        // Turning is complete, load the lose scene immediately
        Debug.Log($"Turning complete! Loading scene: {loseSceneName}");
        SceneManager.LoadScene(loseSceneName);
    }
}