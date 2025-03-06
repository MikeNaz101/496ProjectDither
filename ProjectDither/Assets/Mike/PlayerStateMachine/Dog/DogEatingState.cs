using System.Collections;
using UnityEngine;

public class DogEatingState : DogBaseState
{
    public override void EnterState(DogStateManager dog)
    {
        dog.agent.SetDestination(dog.foodBowl.transform.position);
        dog.StartCoroutine(EatRoutine(dog));
    }

    public override void UpdateState(DogStateManager dog) { }

    private IEnumerator EatRoutine(DogStateManager dog)
    {
        yield return new WaitUntil(() => Vector3.Distance(dog.transform.position, dog.foodBowl.transform.position) < 1f);

        // Stop the dog before eating
        dog.agent.isStopped = true;
        Debug.Log("Dog is eating...");
        yield return new WaitForSeconds(Random.Range(5f, 10f));

        Debug.Log("Dog barks at the player.");
        yield return new WaitForSeconds(1f);

        Debug.Log("Dog howls at the door.");
        yield return new WaitForSeconds(2f);

        // Resume movement
        dog.agent.isStopped = false;

        dog.SwitchState(dog.dogAttack);
    }
}
