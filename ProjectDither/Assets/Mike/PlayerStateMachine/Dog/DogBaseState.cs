using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public abstract class DogBaseState
{
    public abstract void EnterState(DogStateManager player);

    public abstract void UpdateState(DogStateManager player);
}
