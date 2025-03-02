using UnityEngine;

public class DogIdleState : DogBaseState
{
    private float wanderTimer = 0f;
    private float wanderInterval = 3f; // Time interval to pick a new random destination
    private Vector3 randomDestination;

    public override void EnterState(DogStateManager dog)
    {
        Debug.Log("Dog is now idle.");
        wanderTimer = 0f; // Reset wander timer
    }

    public override void UpdateState(DogStateManager dog)
    {
        wanderTimer += Time.deltaTime;

        // Check if the player has entered the room
        if (dog.player != null && IsPlayerInRoom(dog))
        {
            dog.SwitchState(dog.dogActive);
        }

        // Wander randomly within the room's corners
        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f; // Reset timer for next wander

            // Pick a random destination within the room's corners
            randomDestination = GetRandomDestination(dog);

            // Set the destination for the dog's NavMeshAgent
            dog.agent.SetDestination(randomDestination);
            Debug.Log("Dog is wandering to: " + randomDestination);
        }
    }

    // Get a random position within the room's corners
    private Vector3 GetRandomDestination(DogStateManager dog)
    {
        Vector3 corner1 = dog.corners[Random.Range(0, dog.corners.Length)];
        Vector3 corner2 = dog.corners[Random.Range(0, dog.corners.Length)];

        // Ensure they are different corners
        while (corner1 == corner2)
        {
            corner2 = dog.corners[Random.Range(0, dog.corners.Length)];
        }

        // Generate a random point within the two corners
        float randomX = Random.Range(Mathf.Min(corner1.x, corner2.x), Mathf.Max(corner1.x, corner2.x));
        float randomZ = Random.Range(Mathf.Min(corner1.z, corner2.z), Mathf.Max(corner1.z, corner2.z));

        // Return the random position, maintaining the current Y height of the dog
        return new Vector3(randomX, dog.transform.position.y, randomZ);
    }

    // Helper method to check if the player is inside the room's boundaries
    private bool IsPlayerInRoom(DogStateManager dog)
    {
        // Get the min and max X and Z values from the corners
        float minX = Mathf.Min(dog.corners[0].x, dog.corners[1].x, dog.corners[2].x, dog.corners[3].x);
        float maxX = Mathf.Max(dog.corners[0].x, dog.corners[1].x, dog.corners[2].x, dog.corners[3].x);
        float minZ = Mathf.Min(dog.corners[0].z, dog.corners[1].z, dog.corners[2].z, dog.corners[3].z);
        float maxZ = Mathf.Max(dog.corners[0].z, dog.corners[1].z, dog.corners[2].z, dog.corners[3].z);

        // Check if the player's position is inside the X and Z bounds of the room
        return dog.player.transform.position.x >= minX && dog.player.transform.position.x <= maxX &&
               dog.player.transform.position.z >= minZ && dog.player.transform.position.z <= maxZ;
    }
}
