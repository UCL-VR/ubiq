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
    private GameObject sliderPanel;
    private Slider slider;
    private Text sliderText;
    private bool infoSet = false;



    // Start is called before the first frame update
    void Start()
    {
        recRep = scene.GetComponent<RecorderReplayer>();
        sliderPanel = gameObject.transform.Find("Slider Panel").gameObject;
        slider = sliderPanel.GetComponentInChildren<Slider>();
        sliderText = sliderPanel.GetComponentInChildren<Text>();
    }

    public void ToggleRecord(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();
        
        if (recRep.IsRecording()) // if recording stop it
        {
            img.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            recRep.recording = false;
            infoSet = false;
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
            slider.minValue = 0;
        }
    }

    public void PlayPauseReplay(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();

        if (recRep.play) // if playing pause it
        {
            img.sprite = playSprite;
            recRep.play = false;
            slider.interactable = true;
        }
        else // resume
        {
            img.sprite = pauseSprite;
            recRep.play = true;
            slider.interactable = false;
        }

    }

    public void SetReplayFrame()
    {
        Debug.Log("Set slider value");
        recRep.currentReplayFrame = (int)slider.value;
        slider.value = recRep.currentReplayFrame;
        sliderText.text = slider.value.ToString();
    }

    void Update()
    {
        if(recRep.replaying)
        {
            if(!infoSet && recRep.replayer.recInfo != null)
            {
                infoSet = true;
                slider.maxValue = recRep.replayer.recInfo.frames;
            }

            slider.value = recRep.currentReplayFrame;

            if (!recRep.play) // only display frame number during pause otherwise it looks confusing because text changes so fast
            {
                sliderText.text = slider.value.ToString();
            }
        }
    }



}
