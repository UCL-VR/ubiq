using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Samples;
using Ubiq.Messaging;
using Ubiq.Networking;
using UnityEngine.UI;
public class RecorderReplayerMenu : MonoBehaviour
{
    public NetworkScene scene;
    public Sprite playSprite;
    public Sprite pauseSprite;

    private RecorderReplayer recRep;
    private bool recording;
    private bool replaying;



    // Start is called before the first frame update
    void Start()
    {
        recRep = scene.GetComponent<RecorderReplayer>();
    }

    public void ToggleRecord(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();
        
        if (recRep.IsRecording()) // if recording stop it
        {
            img.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            recRep.recording = false;
        }
        else // start recording
        {
            img.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            recRep.recording = true;
        }
        
    }

    public void ToggleReplay(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();

        if (recRep.replaying) // if replaying stop it
        {
            img.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            recRep.replaying = false;
            // panel with slider gets set in PanelSwitcher
        }
        else // start replaying
        {
            img.color = new Color(0.0f, 0.8f, 0.2f, 1.0f);
            recRep.replaying = true;
        }
    }

    public void PlayPauseReplay(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();

        if (recRep.play) // if playing pause it
        {
            img.sprite = playSprite;
            recRep.play = false;
        }
        else // resume
        {
            img.sprite = pauseSprite;
            recRep.play = true;
        }

    }



}
