using UnityEngine;

public class DogDeadState : DogBaseState
{
    public override void EnterState(DogStateManager dog)
    {
        Debug.Log("Dog has died...");
        //GameObject.Destroy(dog.gameObject, 2f); // Wait 2 seconds before destroying for dramatic effect
    }

    public override void UpdateState(DogStateManager dog) { }
}
