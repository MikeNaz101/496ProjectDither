using UnityEngine;
using UnityEngine.AI;

public class DogStateManager : MonoBehaviour
{
    public DogBaseState currentState;
    public float default_speed = 100;
    public NavMeshAgent agent;
    public GameObject player;
    public Transform enemy;
    public GameObject foodBowl;
    public Vector3[] corners;
    public bool isFed = false;
    public bool enemyInRoom = false;
    [HideInInspector]
    public DogIdleState dogIdle = new DogIdleState();
    [HideInInspector]
    public DogActiveState dogActive = new DogActiveState();
    [HideInInspector]
    public DogDeadState dogDead = new DogDeadState();
    [HideInInspector]
    public DogEatingState dogEating = new DogEatingState();
    [HideInInspector]
    public DogAttackState dogAttack = new DogAttackState();
    [HideInInspector]
    public Vector2 movement;
    [HideInInspector]
    CharacterController controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = dogIdle;
        currentState.EnterState(this);
    }

    // Update is called once per frame
    void Update()
    {
        currentState.UpdateState(this);
    }

    public void SwitchState(DogBaseState newState)
    {
        currentState = newState;
        currentState.EnterState(this);
    }
}
