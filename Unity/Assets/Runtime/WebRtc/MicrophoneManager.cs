using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneManager : MonoBehaviour
{
    private AudioSource audiosource;

    public string microphone;

    private void Awake()
    {
        audiosource = GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (var item in Microphone.devices)
        {
            Debug.Log("Found Microphone " + item);
        }

        if (!Microphone.devices.Contains(microphone))
        {
            microphone = Microphone.devices[0];
        }

        audiosource.clip = Microphone.Start(microphone, true, 1, 44100);
        audiosource.loop = true;
        while (!(Microphone.GetPosition(microphone) > 0))
        { 
            // wait here until we have some audio
        }
        audiosource.Play();
    }

}
