using UnityEngine;
using UnityEngine.AI; // Keep if needed, though not directly used here
using System.Collections.Generic; // Keep for room logic
using Random = UnityEngine.Random; // Keep for room logic

// Require AudioSource component to ensure sounds can play
[RequireComponent(typeof(AudioSource))]
public class EnemyHitBehavior : MonoBehaviour
{
    [Header("Enemy References")]
    [Tooltip("The prefab of this enemy to instantiate when hit.")]
    public GameObject enemyPrefabToSpawn;

    [Header("Behavior Settings")]
    [Tooltip("Tag of the object that represents a 'bullet'.")]
    public string bulletTag = "Bullet";
    [Tooltip("Initial multiplier for speed increase per hit.")]
    public float speedIncreasePerHit = 2f;

    [Header("Sound Effects")]
    [Tooltip("Sound to play when the enemy is hit by a bullet.")]
    public AudioClip hitSound; // Assign this in the Inspector

    // Component References
    private MikesWeepingAngel weepingAngelScript;
    private AudioSource audioSource; // Reference to the AudioSource
    private GameObject player;
    private RoomGenerator7 roomGenerator;

    // State
    private int hitCount = 0;

    void Start()
    {
        // Get components
        weepingAngelScript = GetComponent<MikesWeepingAngel>();
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource
        player = GameObject.FindGameObjectWithTag("Player");
        roomGenerator = FindFirstObjectByType<RoomGenerator7>(); // Use FindFirstObjectByType for newer Unity versions

        // --- Null Checks ---
        bool errorFound = false;
        if (audioSource == null) // Check if AudioSource exists
        {
            Debug.LogError("EnemyHitBehavior: AudioSource component not found on " + gameObject.name + "! Add one to the prefab.");
            errorFound = true;
        }
         if (roomGenerator == null)
        {
            Debug.LogError("RoomGenerator7 not found in the scene for " + gameObject.name);
            errorFound = true;
        }
        if (player == null)
        {
            Debug.LogError("Player with tag 'Player' not found in the scene.");
            errorFound = true;
        }
        if (enemyPrefabToSpawn == null)
        {
            Debug.LogError("Enemy Prefab to Spawn not assigned in the inspector for " + gameObject.name);
            errorFound = true;
        }
        if (weepingAngelScript == null)
        {
            Debug.LogError("MikesWeepingAngel script not found on " + gameObject.name);
            errorFound = true;
        }

        if (errorFound)
        {
            enabled = false; // Disable script if critical components are missing
        }
        // --- End Null Checks ---
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(bulletTag))
        {
            HandleHit();
            // Destroy the bullet
            Destroy(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(bulletTag))
        {
            HandleHit();
            // Destroy the bullet
            Destroy(other.gameObject);
        }
    }

    private void HandleHit()
    {
        // --- Play Hit Sound ---
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound); // Play the assigned hit sound
        }
        // ----------------------

        hitCount++;

        // Increase speed on the current enemy
        if (weepingAngelScript != null) // Check if script exists before accessing
        {
             weepingAngelScript.aiSpeed += speedIncreasePerHit * hitCount;
             weepingAngelScript.lookFreezeFactor = Mathf.Max(0f, 1f - (0.1f * hitCount));
             weepingAngelScript.hitCount = hitCount; // Update hit count on Weeping Angel script
        }
        else {
             Debug.LogWarning("HandleHit: weepingAngelScript reference is null. Cannot update speed/freeze factor.");
             // Decide if you should return here or continue spawning/moving
        }


        // Get all rooms from the room generator
        if (roomGenerator == null) {
            Debug.LogWarning("HandleHit: roomGenerator reference is null. Cannot move/spawn enemies.");
            return; // Cannot proceed without room generator
        }
        List<RoomGenerator7.Room> allRooms = roomGenerator.GetAllRooms();
        if (allRooms == null || allRooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to respawn/spawn new enemy.");
            return;
        }

        // Find the player's current room
        if (player == null) {
             Debug.LogWarning("HandleHit: player reference is null. Cannot determine player room.");
            return;
        }
        RoomGenerator7.Room playerRoom = GetCurrentRoom(player.transform.position, allRooms);
        if (playerRoom == null)
        {
            Debug.LogWarning("Could not determine the player's current room.");
             // Decide if you should return or continue without player room knowledge
        }


        // Move this enemy to a random room that isn't the player's
        RoomGenerator7.Room newRoomForThisEnemy = GetRandomRoomExcluding(allRooms, playerRoom);
        if (newRoomForThisEnemy != null)
        {
            Vector3 respawnPosition = GetRandomPointInRoom(newRoomForThisEnemy);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(respawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if(weepingAngelScript != null && weepingAngelScript.ai != null) weepingAngelScript.ai.Warp(hit.position);
                Debug.Log(gameObject.name + " moved to room " + newRoomForThisEnemy.id);
            }
            else
            {
                Debug.LogWarning("Could not find a valid NavMesh position in the new room for " + gameObject.name);
                transform.position = respawnPosition;
                 if(weepingAngelScript != null && weepingAngelScript.ai != null) weepingAngelScript.ai.Warp(respawnPosition);
            }
        }
        else
        {
            Debug.LogWarning("Could not find a room to move to (excluding player's room).");
        }

        // Instantiate another of itself in another room
        if (enemyPrefabToSpawn == null) {
            Debug.LogWarning("HandleHit: enemyPrefabToSpawn is null. Cannot spawn new enemy.");
            return;
        }
        RoomGenerator7.Room spawnRoomForNewEnemy = GetRandomRoomExcluding(allRooms, playerRoom, newRoomForThisEnemy);
        if (spawnRoomForNewEnemy != null)
        {
            Vector3 spawnPosition = GetRandomPointInRoom(spawnRoomForNewEnemy);
            NavMeshHit hit;
            GameObject newEnemy = null; // Declare outside the if/else
            if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                 newEnemy = Instantiate(enemyPrefabToSpawn, hit.position, Quaternion.identity);
                 Debug.Log("New " + newEnemy.name + " spawned in room " + spawnRoomForNewEnemy.id + " on NavMesh.");
            }
            else
            {
                 newEnemy = Instantiate(enemyPrefabToSpawn, spawnPosition, Quaternion.identity);
                 Debug.LogWarning("Could not find valid NavMesh position for new enemy. Spawned at raw position in room " + spawnRoomForNewEnemy.id);
            }

            // Configure the newly spawned enemy
            if (newEnemy != null)
            {
                EnemyHitBehavior newEnemyHitScript = newEnemy.GetComponent<EnemyHitBehavior>();
                if (newEnemyHitScript != null)
                {
                    newEnemyHitScript.roomGenerator = roomGenerator; // Pass reference
                }
                 MikesWeepingAngel newEnemyWeepingAngelScript = newEnemy.GetComponent<MikesWeepingAngel>();
                 if (newEnemyWeepingAngelScript != null && player != null)
                 {
                     // Find camera on the player object or its children
                     Camera playerCamera = player.GetComponentInChildren<Camera>();
                     if (playerCamera != null) {
                        newEnemyWeepingAngelScript.playerCam = playerCamera;
                     } else {
                         Debug.LogWarning($"Could not find Camera component on Player object '{player.name}' or its children for new enemy '{newEnemy.name}'.");
                     }
                 }
            }
        }
        else
        {
            Debug.LogWarning("Could not find a second room to spawn a new enemy in (excluding player's and current enemy's room).");
        }
    }

    // --- Helper Methods for Room Logic (Keep these as they are) ---
    private RoomGenerator7.Room GetCurrentRoom(Vector3 position, List<RoomGenerator7.Room> allRooms)
    {
        foreach (var room in allRooms)
        {
            Bounds b = room.roomBounds;
            if (position.x >= b.min.x && position.x <= b.max.x &&
                position.z >= b.min.z && position.z <= b.max.z)
            {
                // Optional: Add a small tolerance check for Y if needed
                // if (Mathf.Abs(position.y - b.center.y) <= b.extents.y)
                return room;
            }
        }
        return null;
    }

    private RoomGenerator7.Room GetRandomRoomExcluding(List<RoomGenerator7.Room> allRooms, RoomGenerator7.Room excludedRoom1, RoomGenerator7.Room excludedRoom2 = null)
    {
         if (allRooms == null) return null;
        List<RoomGenerator7.Room> validRooms = new List<RoomGenerator7.Room>();
        foreach (var room in allRooms)
        {
             // Ensure room is not null before comparison
             if (room != null && room != excludedRoom1 && room != excludedRoom2)
            {
                validRooms.Add(room);
            }
        }

        if (validRooms.Count > 0)
        {
            return validRooms[Random.Range(0, validRooms.Count)];
        }
        return null;
    }

     private Vector3 GetRandomPointInRoom(RoomGenerator7.Room room)
    {
        // Check if room and bounds exist
        if (room == null || room.roomBounds == null) {
            Debug.LogError("GetRandomPointInRoom: Room or Room Bounds are null!");
            return Vector3.zero; // Return a default value
        }
        // Add small padding to avoid spawning exactly on the edge
        float padding = 1f;
        float randomX = Random.Range(room.roomBounds.min.x + padding, room.roomBounds.max.x - padding);
        float randomZ = Random.Range(room.roomBounds.min.z + padding, room.roomBounds.max.z - padding);
        // Assuming ground level is Y=0, adjust if needed
        return new Vector3(randomX, 0f, randomZ);
    }
    // --- End Helper Methods ---
}