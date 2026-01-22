using UnityEngine;

public class RandomStartAnimation : MonoBehaviour
{
    void Start()
    {
        Animator anim = GetComponent<Animator>();
        anim.Play(0, -1, Random.value);
    }
}