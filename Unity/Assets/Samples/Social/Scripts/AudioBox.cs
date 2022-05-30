using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;

public class AudioBox : MonoBehaviour, IUseable, IGraspable
{
    private AudioSource audioSource;
    private Hand follow;
    private Rigidbody body;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        body = GetComponent<Rigidbody>();
    }

    public void Grasp(Hand controller)
    {
        follow = controller;
    }

    public void Release(Hand controller)
    {
        follow = null;
    }

    public void UnUse(Hand controller)
    {

    }

    public void Use(Hand controller)
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        else
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (follow != null)
        {
            transform.position = follow.transform.position;
            transform.rotation = follow.transform.rotation;
            body.isKinematic = true;
        }
        else
        {
            body.isKinematic = false;
        }
    }
}
