using UnityEngine;

public class YourGunPickupTask : Task
{
    public GameObject gunToPickup; // Assign the gun in the Inspector

    public override void Activate()
    {
        base.Activate();
        if (gunToPickup != null)
        {
            GunPickup gunPickupScript = gunToPickup.GetComponent<GunPickup>();
            if (gunPickupScript != null)
            {
                gunPickupScript.Pickup();
                Complete(); // Call the Complete method when the task is done
            }
            else
            {
                Debug.LogError("YourGunPickupTask: Gun object does not have a GunPickup script!");
            }
        }
        else
        {
            Debug.LogError("YourGunPickupTask: gunToPickup not assigned!");
        }
    }

    // This is the implementation of the abstract Complete() method from the Task class
    public override void Complete()
    {
        Debug.Log($"Task '{taskName}' (Gun Pickup) is now complete.");
        TaskCompleted(); // Call the TaskCompleted method to remove it from the TaskManager
    }
}