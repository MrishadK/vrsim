using UnityEngine;

public class RandomAnimationOffset : MonoBehaviour
{
    void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            anim.Play(state.fullPathHash, 0, Random.Range(0f, 1f));
        }
    }
}