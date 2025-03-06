using System;
using UnityEngine;
using UnityEngine.AI;

public class WeepingAngel : MonoBehaviour
{
    public NavMeshAgent ai;
    public Transform player;
    public Camera playerCam;
    public float aiSpeed = 0.1f;
    public LayerMask obstacleMask; // Assign this in the inspector to detect walls, objects, etc.

    void Update()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCam);
        bool inView = GeometryUtility.TestPlanesAABB(planes, this.GetComponent<Renderer>().bounds);

        if (inView && !IsObstructed())
        {
            ai.speed = 0;
            ai.SetDestination(transform.position);
        }
        else
        {
            ai.speed = aiSpeed;
            ai.destination = player.position;
        }
    }

    bool IsObstructed()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
        {
            return true; // There is an obstacle blocking the angel
        }
        return false; // No obstacles, player can see the angel
    }
}
