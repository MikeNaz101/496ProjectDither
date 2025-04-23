using System;
using UnityEngine;
using UnityEngine.AI;

public class MikesWeepingAngel : MonoBehaviour
{
    public NavMeshAgent ai;
    public GameObject player;
    public Camera playerCam;
    public float aiSpeed = 20f;
    public LayerMask obstacleMask; // Assign this in the inspector to detect walls, objects, etc.
    [Tooltip("Factor that reduces speed when looked at. 1 means full speed, 0 means stopped.")]
    [Range(0f, 1f)]
    public float lookFreezeFactor = 0.1f; // Initial freeze factor
    [HideInInspector]
    public int hitCount = 0; // Track hit count from EnemyHitBehavior

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player with tag 'Player' not found in the scene.");
            enabled = false;
        }

        if (ai == null)
        {
            ai = GetComponent<NavMeshAgent>();
            if (ai == null)
            {
                Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
                enabled = false;
            }
        }

        if (playerCam == null && player != null)
        {
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCam = cam;
            }
            else
            {
                Debug.LogWarning("Player Camera not found for " + gameObject.name + ". Please ensure the Player prefab has a Camera component as a child.");
            }
        }
        else if (playerCam == null && player == null)
        {
            enabled = false; // Script won't work without a player
        }
    }

    void Update()
    {
        if (player == null || playerCam == null || ai == null)
        {
            return; // Safety check if player or camera is missing
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCam);
        bool inView = GeometryUtility.TestPlanesAABB(planes, GetComponent<Renderer>().bounds);

        if (inView && !IsObstructed())
        {
            ai.isStopped = false; // Allow stopping by setting speed to 0
            ai.speed = aiSpeed * lookFreezeFactor;
            ai.SetDestination(transform.position);
        }
        else
        {
            ai.isStopped = false;
            ai.speed = aiSpeed;
            ai.SetDestination(player.transform.position);
        }
    }

    bool IsObstructed()
    {
        if (player == null) return false; // If no player, can't be obstructed

        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Perform a raycast, ignoring the enemy itself
        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleMask))
        {
            // Optionally ignore hits on the player itself if your obstacle mask doesn't handle it
            if (hit.collider.gameObject != player)
            {
                Debug.Log("Enemy is behind an obstacle: " + hit.collider.name);
                return true;
            }
        }

        return false; // No obstacles
    }
}