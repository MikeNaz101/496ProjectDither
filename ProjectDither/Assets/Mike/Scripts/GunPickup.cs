using UnityEngine;

public class GunPickup : MonoBehaviour
{
    [Header("Gun Settings")]
    public GameObject bulletPrefab; // Prefab of the bullet to spawn
    public Transform bulletSpawnPoint; // Point where the bullet spawns
    public float fireForce = 20f;

    private Transform playerHandTransform;
    private bool isHeld = false;
    private Rigidbody rb;
    private Collider coll;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();

        // Ensure these components exist
        if (rb == null)
        {
            Debug.LogError("GunPickup: Rigidbody component not found on this GameObject!");
            enabled = false;
        }
        if (coll == null)
        {
            Debug.LogError("GunPickup: Collider component not found on this GameObject!");
            enabled = false;
        }
        if (bulletSpawnPoint == null)
        {
            Debug.LogError("GunPickup: bulletSpawnPoint not assigned!");
            enabled = false;
        }
        if (bulletPrefab == null)
        {
            Debug.LogError("GunPickup: bulletPrefab not assigned!");
            enabled = false;
        }

        // Find the player's hand transform using the tag
        GameObject handObject = GameObject.FindGameObjectWithTag("Hand");
        if (handObject != null)
        {
            playerHandTransform = handObject.transform;
        }
        else
        {
            Debug.LogError("GunPickup: No GameObject found with the tag 'Hand'! Make sure your player's hand point has this tag.");
            enabled = false; // Disable the script if the hand isn't found
        }
    }

    public void Pickup()
    {
        if (!isHeld && playerHandTransform != null)
        {
            isHeld = true;
            rb.isKinematic = true;
            coll.enabled = false;
            transform.SetParent(playerHandTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity; // You might want to adjust this
            Debug.Log("Gun picked up!");
        }
    }

    void Update()
    {
        if (isHeld)
        {
            if (Input.GetMouseButtonDown(0)) // Left mouse button for shooting
            {
                Shoot();
            }

            if (Input.GetMouseButtonDown(1)) // Right mouse button for dropping
            {
                Drop();
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.AddForce(bulletSpawnPoint.forward * fireForce, ForceMode.Impulse);
            }
            else
            {
                Debug.LogError("GunPickup: Bullet prefab does not have a Rigidbody component!");
            }
        }
    }

    void Drop()
    {
        if (isHeld)
        {
            isHeld = false;
            transform.SetParent(null);
            rb.isKinematic = false;
            coll.enabled = true;
            rb.linearVelocity = Vector3.zero; // Reset velocity when dropped
            rb.angularVelocity = Vector3.zero; // Reset angular velocity
            // Optionally add a small force/torque when dropping for a more natural feel
            // rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            Debug.Log("Gun dropped!");
        }
    }
}