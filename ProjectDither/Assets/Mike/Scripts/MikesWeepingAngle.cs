using System;
using UnityEngine;
using UnityEngine.AI;

public class MikesWeepingAngel : MonoBehaviour
{
    public NavMeshAgent ai;
    public GameObject player;
    public Camera playerCam;
    private float aiSpeed = 20f;
    public LayerMask obstacleMask; // Assign this in the inspector to detect walls, objects, etc.

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCam);
        bool inView = GeometryUtility.TestPlanesAABB(planes, GetComponent<Renderer>().bounds);

        if (inView && !IsObstructed())
        {
            ai.isStopped = true;
            //ai.speed = 0;
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
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Perform a raycast, ignoring the enemy itself
        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleMask))
        {
            Debug.Log("Enemy is behind an obstacle: " + hit.collider.name);
            return true;
        }

        return false; // No obstacles
    }

}
