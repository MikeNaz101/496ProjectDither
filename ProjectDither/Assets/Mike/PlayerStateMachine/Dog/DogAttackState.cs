using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DogAttackState : DogBaseState
{
    public override void EnterState(DogStateManager dog)
    {
        Debug.Log("Dog is attacking the enemy!");
        dog.agent.SetDestination(dog.enemy.position);
        dog.StartCoroutine(AttackRoutine(dog));
    }

    public override void UpdateState(DogStateManager dog) { }

    private IEnumerator AttackRoutine(DogStateManager dog)
    {
        yield return new WaitUntil(() => Vector3.Distance(dog.transform.position, dog.enemy.position) < 1f);
        Debug.Log("Dog is stopping the enemy!");

        // Stop enemy movement
        NavMeshAgent enemyAgent = dog.enemy.GetComponent<NavMeshAgent>();
        if (enemyAgent != null)
        {
            enemyAgent.isStopped = true;
        }

        // Play attack sounds
        yield return new WaitForSeconds(Random.Range(5f, 10f));

        // Resume enemy movement
        if (enemyAgent != null)
        {
            enemyAgent.isStopped = false;
        }

        dog.SwitchState(dog.dogDead);
    }
}
