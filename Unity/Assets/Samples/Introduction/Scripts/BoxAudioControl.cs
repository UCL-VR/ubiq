using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;

public class BoxAudioControl : MonoBehaviour, IUseable
{
    private AudioSource AudioSource;

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
    }

    public void UnUse(Hand controller)
    {

    }

    public void Use(Hand controller)
    {
        if (AudioSource.isPlaying)
        {
            AudioSource.Stop();
        }
        else
        {
            AudioSource.Play();
        }
    }
}
