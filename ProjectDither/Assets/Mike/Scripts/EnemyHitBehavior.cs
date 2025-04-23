using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyHitBehavior : MonoBehaviour
{
    [Tooltip("The prefab of this enemy to instantiate when hit.")]
    public GameObject enemyPrefabToSpawn;
    [Tooltip("Tag of the object that represents a 'bullet'.")]
    public string bulletTag = "Bullet";
    [Tooltip("Initial multiplier for speed increase per hit.")]
    public float speedIncreasePerHit = 2f;

    private MikesWeepingAngel weepingAngelScript;
    private int hitCount = 0;
    private GameObject player;
    private RoomGenerator7 roomGenerator;

    void Start()
    {
        weepingAngelScript = GetComponent<MikesWeepingAngel>();
        player = GameObject.FindGameObjectWithTag("Player");
        roomGenerator = FindFirstObjectByType<RoomGenerator7>();

        if (roomGenerator == null)
        {
            Debug.LogError("RoomGenerator7 not found in the scene for " + gameObject.name);
            enabled = false;
        }
        if (player == null)
        {
            Debug.LogError("Player with tag 'Player' not found in the scene.");
            enabled = false;
        }
        if (enemyPrefabToSpawn == null)
        {
            Debug.LogError("Enemy Prefab to Spawn not assigned in the inspector for " + gameObject.name);
            enabled = false;
        }
        if (weepingAngelScript == null)
        {
            Debug.LogError("MikesWeepingAngel script not found on " + gameObject.name);
            enabled = false;
        }
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
        hitCount++;

        // Increase speed on the current enemy
        weepingAngelScript.aiSpeed += speedIncreasePerHit * hitCount;

        // Lessen freeze ability (modify the base speed when looked at)
        weepingAngelScript.lookFreezeFactor = Mathf.Max(0f, 1f - (0.1f * hitCount)); // Example: reduces freeze effect by 10% per hit

        // Get all rooms from the room generator
        List<RoomGenerator7.Room> allRooms = roomGenerator.GetAllRooms();
        if (allRooms == null || allRooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to respawn/spawn new enemy.");
            return;
        }

        // Find the player's current room
        RoomGenerator7.Room playerRoom = GetCurrentRoom(player.transform.position, allRooms);
        if (playerRoom == null)
        {
            Debug.LogWarning("Could not determine the player's current room.");
            return;
        }

        // Move this enemy to a random room that isn't the player's
        RoomGenerator7.Room newRoomForThisEnemy = GetRandomRoomExcluding(allRooms, playerRoom);
        if (newRoomForThisEnemy != null)
        {
            // Find a random point within the new room's bounds
            Vector3 respawnPosition = GetRandomPointInRoom(newRoomForThisEnemy);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(respawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                weepingAngelScript.ai.Warp(hit.position); // Use Warp for immediate position change
                Debug.Log(gameObject.name + " moved to room " + newRoomForThisEnemy.id);
            }
            else
            {
                Debug.LogWarning("Could not find a valid NavMesh position in the new room for " + gameObject.name);
                transform.position = respawnPosition; // Fallback if NavMesh fails
                weepingAngelScript.ai.Warp(respawnPosition);
            }
        }
        else
        {
            Debug.LogWarning("Could not find a room to move to (excluding player's room).");
        }

        // Instantiate another of itself in another room
        RoomGenerator7.Room spawnRoomForNewEnemy = GetRandomRoomExcluding(allRooms, playerRoom, newRoomForThisEnemy);
        if (spawnRoomForNewEnemy != null)
        {
            Vector3 spawnPosition = GetRandomPointInRoom(spawnRoomForNewEnemy);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject newEnemy = Instantiate(enemyPrefabToSpawn, hit.position, Quaternion.identity);
                EnemyHitBehavior newEnemyHitScript = newEnemy.GetComponent<EnemyHitBehavior>();
                if (newEnemyHitScript != null)
                {
                    newEnemyHitScript.roomGenerator = roomGenerator; // Ensure the new enemy has a reference to the room generator
                }
                MikesWeepingAngel newEnemyWeepingAngelScript = newEnemy.GetComponent<MikesWeepingAngel>();
                if (newEnemyWeepingAngelScript != null)
                {
                    newEnemyWeepingAngelScript.playerCam = player.GetComponentInChildren<Camera>(); // Ensure new enemy has player camera reference
                }
                Debug.Log("New " + newEnemy.name + " spawned in room " + spawnRoomForNewEnemy.id);
            }
            else
            {
                GameObject newEnemy = Instantiate(enemyPrefabToSpawn, spawnPosition, Quaternion.identity);
                EnemyHitBehavior newEnemyHitScript = newEnemy.GetComponent<EnemyHitBehavior>();
                if (newEnemyHitScript != null)
                {
                    newEnemyHitScript.roomGenerator = roomGenerator;
                }
                MikesWeepingAngel newEnemyWeepingAngelScript = newEnemy.GetComponent<MikesWeepingAngel>();
                if (newEnemyWeepingAngelScript != null)
                {
                    newEnemyWeepingAngelScript.playerCam = player.GetComponentInChildren<Camera>();
                }
                Debug.LogWarning("Could not find a valid NavMesh position in the spawn room for the new enemy. Spawned at " + spawnPosition);
            }
        }
        else
        {
            Debug.LogWarning("Could not find a second room to spawn a new enemy in (excluding player's and current enemy's room).");
        }

        // Update the hit count on the Weeping Angel script
        weepingAngelScript.hitCount = hitCount;
    }

    private RoomGenerator7.Room GetCurrentRoom(Vector3 position, List<RoomGenerator7.Room> allRooms)
    {
        foreach (var room in allRooms)
        {
            if (room.roomBounds.Contains(position))
            {
                return room;
            }
        }
        return null;
    }

    private RoomGenerator7.Room GetRandomRoomExcluding(List<RoomGenerator7.Room> allRooms, RoomGenerator7.Room excludedRoom1, RoomGenerator7.Room excludedRoom2 = null)
    {
        List<RoomGenerator7.Room> validRooms = new List<RoomGenerator7.Room>();
        foreach (var room in allRooms)
        {
            if (room != excludedRoom1 && room != excludedRoom2)
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
        float randomX = Random.Range(room.roomBounds.min.x + 1f, room.roomBounds.max.x - 1f);
        float randomZ = Random.Range(room.roomBounds.min.z + 1f, room.roomBounds.max.z - 1f);
        return new Vector3(randomX, 0f, randomZ);
    }
}