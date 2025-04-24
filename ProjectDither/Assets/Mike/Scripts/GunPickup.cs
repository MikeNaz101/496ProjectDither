using UnityEngine;
using UnityEngine.UI; // Required for Graphic check

public class GunPickup : MonoBehaviour
{
    [Header("Gun Settings")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint; // Point where the bullet spawns (relative to the gun)
    public float fireForce = 20f;

    // Found at runtime using "Hand" tag
    private GameObject handImageObject;

    private Transform attachPointTransform; // Found at runtime using "AttachPoint" tag
    private bool isHeld = false;
    private Rigidbody rb;
    private Collider coll;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();

        // Ensure components exist
        if (rb == null || coll == null || bulletSpawnPoint == null || bulletPrefab == null)
        {
            Debug.LogError("GunPickup: Missing component references on the gun itself!");
            enabled = false;
            return;
        }

        // Find attach point
        GameObject attachPointObject = GameObject.FindGameObjectWithTag("AttachPoint");
        if (attachPointObject != null)
        {
            attachPointTransform = attachPointObject.transform;
            Debug.Log("GunPickup: Found attach point object: " + attachPointObject.name);
        }
        else
        {
            Debug.LogError("GunPickup: No GameObject found with the tag 'AttachPoint'!");
            enabled = false;
            return; // Stop if attach point is missing
        }

        // Find Hand UI Object by Tag
        handImageObject = GameObject.FindGameObjectWithTag("Hand");
        if (handImageObject == null)
        {
            Debug.LogWarning("GunPickup: Could not find GameObject with tag 'Hand'. Make sure your hand UI GameObject exists, is active, and has the 'Hand' tag assigned. Hand UI will not be hidden/shown.");
        }
        else
        {
             Debug.Log("GunPickup: Found hand UI object by tag: " + handImageObject.name);
             // Optional sanity check
             if (handImageObject.GetComponent<Graphic>() == null)
             {
                 Debug.LogWarning($"GunPickup: GameObject '{handImageObject.name}' tagged as 'Hand' does not have a Graphic component (like RawImage or Image).");
             }
        }
    }

    public void Pickup()
    {
        if (!isHeld && attachPointTransform != null)
        {
            isHeld = true;
            rb.isKinematic = true;
            coll.enabled = false;

            transform.SetParent(attachPointTransform);
            transform.localPosition = Vector3.zero;

            // --- USE YOUR SPECIFIED ROTATION ---
            transform.localRotation = Quaternion.Euler(270f, 0f, 180f);
            // -----------------------------------

            // HIDE HAND UI (checks if found in Start)
            if (handImageObject != null)
            {
                handImageObject.SetActive(false);
            }

            Debug.Log("Gun picked up and attached to: " + attachPointTransform.name);
        }
        else if (attachPointTransform == null)
        {
            Debug.LogError("GunPickup: Cannot pickup gun because AttachPoint was not found during Start().");
        }
    }

    void Update()
    {
        if (isHeld)
        {
            // Left mouse button for shooting
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            // Right mouse button for dropping
            if (Input.GetMouseButtonDown(1))
            {
                Drop();
            }
        }
    }

    void Shoot()
    {
        // Check again just in case
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            // Instantiate the bullet at the spawn point's position and rotation
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                // --- APPLY FORCE UPWARDS RELATIVE TO SPAWN POINT ---
                bulletRb.AddForce(bulletSpawnPoint.up * fireForce, ForceMode.Impulse);
                // ---------------------------------------------------
            }
            else
            {
                Debug.LogError("GunPickup: Bullet prefab '" + bulletPrefab.name + "' does not have a Rigidbody component!");
            }
        }
         else
        {
             Debug.LogError("GunPickup: Cannot shoot. Missing bulletPrefab or bulletSpawnPoint reference.");
        }
    }

    void Drop()
    {
        if (isHeld)
        {
            isHeld = false;

            transform.SetParent(null); // Unparent the gun
            rb.isKinematic = false;
            coll.enabled = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // SHOW HAND UI (checks if found in Start)
            if (handImageObject != null)
            {
                handImageObject.SetActive(true);
            }

            Debug.Log("Gun dropped!");
        }
    }
}