using UnityEngine;

 [RequireComponent(typeof(Rigidbody))]
 [RequireComponent(typeof(Collider))]
 public class DogFood: MonoBehaviour
 {
     private Rigidbody rb;
     private Collider coll;
     private Transform originalParent;
     private bool isHeld = false;
     private Transform holderAttachPoint = null;

     void Start()
     {
         rb = GetComponent<Rigidbody>();
         coll = GetComponent<Collider>();
         originalParent = transform.parent; // Remember original parent if needed

         if (rb == null || coll == null)
         {
             Debug.LogError($"FoodItem on '{gameObject.name}' is missing Rigidbody or Collider!");
             enabled = false;
         }
     }

     // Call this from your player interaction script when interacting with "FoodItem" tagged object
     public void Pickup(Transform attachPoint)
     {
         if (isHeld || attachPoint == null) return; // Already held or no point to attach to

         // Check if attach point already holds something (optional, simple check)
         if (attachPoint.childCount > 0) {
              Debug.Log("Attach point is already holding something!");
              // Optionally drop the other item first, or prevent pickup
              // For now, just prevent pickup if something else is there.
              // You could also check if the held item is ALSO food and swap/combine etc.
              // A more robust inventory system would handle this better.
              return;
         }


         isHeld = true;
         holderAttachPoint = attachPoint; // Store who is holding us

         rb.isKinematic = true;  // Disable physics simulation while held
         coll.enabled = false;   // Disable collision while held

         // Parent the food to the attach point
         transform.SetParent(attachPoint);

         // Reset position and rotation relative to the attach point
         transform.localPosition = Vector3.zero; // Adjust as needed for visual offset
         transform.localRotation = Quaternion.identity; // Adjust as needed

         Debug.Log($"FoodItem '{gameObject.name}' picked up by holder with attach point '{attachPoint.name}'.");
     }

     // Call this if you implement a drop action (e.g., pressing a key)
     public void Drop()
     {
         if (!isHeld) return;

         Debug.Log($"Dropping FoodItem '{gameObject.name}'.");
         isHeld = false;
         holderAttachPoint = null;

         // Detach from the parent (the attach point)
         transform.SetParent(originalParent); // Or set to null to drop in world root

         // Re-enable physics and collision
         rb.isKinematic = false;
         coll.enabled = true;

         // Reset velocities
         rb.linearVelocity = Vector3.zero;
         rb.angularVelocity = Vector3.zero;

         // Optional: Add a small force/torque when dropping
         // rb.AddForce(transform.forward * 0.5f, ForceMode.Impulse);
     }

     // Optional: Called automatically when the object is destroyed
     void OnDestroy() {
         // If held when destroyed (e.g. task completed), ensure state is clean
         // This prevents issues if the drop logic wasn't called first
         if (isHeld) {
             // Technically already destroyed, but good practice if logic could change
             isHeld = false;
             holderAttachPoint = null;
         }
     }
 }