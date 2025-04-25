using UnityEngine;

public class DitherManAnim : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    public void PauseAnim()
    {
        animator.speed = 0;
    }

    public void PlayAnim()
    {
        animator.speed = 1;
    }
}
