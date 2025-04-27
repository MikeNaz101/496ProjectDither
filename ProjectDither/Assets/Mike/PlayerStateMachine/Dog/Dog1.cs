using UnityEngine;
using UnityEngine.InputSystem;

public class Dog1 : MonoBehaviour
{
    private enum State
    {
        Pace,
        Follow,
        Melee,
        Panic,
        Confusion,
        Fear,
        Hurt
    }

    [SerializeField]
    GameObject[] route;
    GameObject target;
    public PlayerStateManager player;
    int routeIndex = 0;
    private State currentState = State.Pace;
    float speed = 2f;
    float health = 100f;
    Renderer enemyRenderer;
    //[SerializeField] GameObject bullet;
    //[SerializeField] GameObject bulletSpawner;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Pace:
                OnPace();
                break;
            case State.Follow:
                OnFollow();
                break;
            case State.Melee:
                OnMelee();
                break;
            case State.Panic:
                OnPanic();
                break;
            case State.Confusion:
                OnConfusion();
                break;
            case State.Fear:
                OnFear();
                break;
            case State.Hurt:
                OnHurt();
                break;
        }
    }

    void OnPace()
    {
        enemyRenderer.material.color = Color.green;
        print("I'm Pacing!");
        target = route[routeIndex];
        MoveTo(target);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.1f)
        {
            routeIndex = (routeIndex + 1) % route.Length;
        }

        GameObject obstacle = CheckForward();
        if (obstacle != null)
        {
            target = obstacle;
            currentState = State.Follow;
        }
    }

    void OnFollow()
    {
        enemyRenderer.material.color = new Color(0.5f, 0, 0);
        print("I'm Following!");
        MoveTo(target);

        GameObject obstacle = CheckForward();
        if (obstacle == null)
        {
            currentState = State.Pace;
        }

        if (Vector3.Distance(transform.position, target.transform.position) < 1.5f)
        {
            currentState = State.Melee;
        }
    }

    void OnMelee()
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        enemyRenderer.material.color = Color.black;
        print("Melee Attack!");
        // Apply force to the player (Assume Rigidbody on player)
        //targetRb.GetComponent<Rigidbody>()?.AddForce(transform.forward * 5f, ForceMode.Impulse);
        GameObject obstacle = CheckForward();
        if (obstacle == null)
        {
            currentState = State.Confusion;
        }
        currentState = State.Follow;
    }

    void OnPanic()
    {
        enemyRenderer.material.color = Color.grey;
        speed = 0.5f;
        print("I'm Panicking!");
        Vector3 awayFromPlayer = transform.position - target.transform.position;
        MoveTo(transform.position + awayFromPlayer.normalized * 5f);
        Invoke("OnPace", 3f);
    }

    void OnConfusion()
    {
        enemyRenderer.material.color = Color.yellow;
        print("I'm Confused!");
        currentState = State.Pace;
    }

    void OnFear()
    {
        enemyRenderer.material.color = new Color(0.5f, 0, 0.5f);
        print("I'm Shooting!");
        //Attack();
        // Implement shooting behavior
    }

    void OnHurt()
    {
        enemyRenderer.material.color = Color.red;
        print("I'm Hurt!");
        Invoke("RecoverFromHurt", 2f);
    }

    void RecoverFromHurt()
    {
        currentState = health < 20 ? State.Panic : State.Pace;
    }

    void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
        transform.LookAt(position);
    }

    void MoveTo(GameObject t)
    {
        MoveTo(t.transform.position);
    }

    GameObject CheckForward()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);

        if (Physics.Raycast(transform.position, transform.forward, out hit, 10))
        {
            PlayerStateManager player = hit.transform.gameObject.GetComponent<PlayerStateManager>();
            if (player != null)
            {
                if (player.currentState != player.sneakState)
                {
                    print(hit.transform.gameObject.name);
                    return hit.transform.gameObject;
                }
                
            }
        }
        return null;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        currentState = State.Hurt;
        if (health < 20)
            currentState = State.Panic;
        else if (health < 50)
            currentState = State.Fear;
    }

    /*void Attack()
    {
        GameObject newBullet = Instantiate(bullet, bulletSpawner.transform.position, bulletSpawner.transform.rotation);
        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        rb.AddForce(bulletSpawner.transform.forward * 30f, ForceMode.Impulse);
    }*/
}

/*using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private enum State
    {
        Pace, // walks blindly from point to point, color is Green.
        Follow, // chaces the player -> when withing reach -> melee attack, color changes to Dark Red.
        Melee, // Stops them -> exerts a force on play and causes damage -> color changes to Black.
        Panic, // when their health drops below 20% -> they run away from player, color changes to Grey.
        Confusion, // when they go from follow to not being able to see the player, color changes to yellow
        Fear, // when their health drops below 50% -> they start shooting the player, color changes to Purple.
        Hurt, // Stops them for a second, changes their color while they are paused to bright red, takes damage
    }

    [SerializeField]
    GameObject[] route;
    GameObject target;
    int routeIndex = 0;
    private State currentState = State.Pace;
    float speed = 2f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case State.Pace:
            OnPace();
                break;
            case State.Follow:
            OnFollow();
                break;
        }
    }

    void OnPace()
    {
        print("I'm Pacing!");
        target = route[routeIndex];

        MoveTo(target);
        
        if (Vector3.Distance(transform.position, target.transform.position) < 0.1)
        {
            routeIndex+=1;
            if (routeIndex >= route.Length)
            {
                routeIndex = 0;
            }
        }
        GameObject obstacle = CheckForward();
        if (obstacle != null)
        {
            target = obstacle;
            currentState = State.Follow;
        }
    }

    void OnFollow()
    {
        print("I'm Following!");
        MoveTo(target);

        GameObject obstacle = CheckForward();
        if (obstacle == null)
        {
            currentState = State.Pace;
        }
    }

    void MoveTo(GameObject t)
    {
        transform.position = Vector3.MoveTowards(transform.position, t.transform.position, speed * Time.deltaTime);
        transform.LookAt(t.transform, Vector3.up);
    }

    GameObject CheckForward()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);

        if (Physics.Raycast(transform.position, transform.forward, out hit, 10))
        {
            FirstPersonController player = hit.transform.gameObject.GetComponent<FirstPersonController>();

            if (player != null)
            {
                print(hit.transform.gameObject.name);
                return hit.transform.gameObject;
            }
        }
        return null;
    }
}
*/