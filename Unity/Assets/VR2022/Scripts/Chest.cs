using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;

public class Chest : MonoBehaviour, IUseable
{

    Animator animator;

    bool open = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Use(Hand controller)
    {
        open = !open;
        animator.SetBool("OpenChest", open);
    }

    public void UnUse(Hand controller)
    {
        
    }
}
