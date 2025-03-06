using System.Collections;
using UnityEngine;

public class DogActiveState : DogBaseState
{
    private int runCount;
    private bool runningAroundPlayer = false;

    public override void EnterState(DogStateManager dog)
    {
        runCount = Random.Range(1, 4);
        Debug.Log($"Dog is active, running around the player {runCount} times.");
        dog.agent.speed = 5f;
        dog.agent.SetDestination(dog.player.transform.position);
        runningAroundPlayer = true;
        dog.StartCoroutine(RunAroundPlayer(dog));
    }

    public override void UpdateState(DogStateManager dog)
    {
        if (!runningAroundPlayer)
        {
            Vector3 randomCorner = dog.corners[Random.Range(0, dog.corners.Length)];
            dog.agent.SetDestination(randomCorner);
            runningAroundPlayer = true;
            dog.StartCoroutine(PauseBeforeRepeating(dog));
        }
    }

    private IEnumerator RunAroundPlayer(DogStateManager dog)
    {
        for (int i = 0; i < runCount; i++)
        {
            dog.agent.SetDestination(dog.player.transform.position);
            yield return new WaitForSeconds(2f);
        }
        runningAroundPlayer = false;
    }

    private IEnumerator PauseBeforeRepeating(DogStateManager dog)
    {
        yield return new WaitForSeconds(2f);
        runningAroundPlayer = false;
    }
}
