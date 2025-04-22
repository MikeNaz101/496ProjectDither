using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Animator taskListAnimator; // We'll try to find this at runtime
    public string animatorHolderName = "HandWithPaper"; // Name of the object with the Animator
    public string boolParameterName = "IsTaskListVisible"; // Name of the boolean parameter

    private bool isTaskListVisible = false;

    void Start()
    {
        // Try to find the Animator at start
        FindTaskListAnimator();

        if (taskListAnimator == null)
        {
            Debug.LogError($"Animator on object '{animatorHolderName}' not found in the scene!");
        }
    }

    void FindTaskListAnimator()
    {
        // Find the object with the Animator
        GameObject animatorHolder = GameObject.Find(animatorHolderName);
        if (animatorHolder != null)
        {
            // Get the Animator component from it
            taskListAnimator = animatorHolder.GetComponent<Animator>();
        }
    }

    public void ToggleTaskListAnimation()
    {
        if (taskListAnimator != null)
        {
            isTaskListVisible = !isTaskListVisible;
            taskListAnimator.SetBool(boolParameterName, isTaskListVisible);
        }
        else
        {
            Debug.LogError($"Animator on object '{animatorHolderName}' not found, cannot toggle animation!");
        }
    }
}