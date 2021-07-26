﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;
using UnityEngine.UIElements;

public class RecorderReplayerMenu : MonoBehaviour
{
    public NetworkScene scene;
    public Sprite playSprite;
    public Sprite pauseSprite;
    public GameObject buttonPrefab;

    private RecorderReplayer recRep;
    private bool recording;
    private bool replaying;
    private GameObject sliderPanel;
    private Slider slider;
    private Text sliderText;
    private GameObject filePanel;
    private Text fileText;
    private DirectoryInfo dir;
    private Image replayImage;
    private GameObject selectReplayBtn;

    private List<string> recordings;
    private List<string> newRecordings;

    private bool infoSet = false;
    private bool loaded = false;
    private bool needsUpdate = true;
    private bool resetReplayImage = false; // in case we loaded an invalid file path

    // Start is called before the first frame update
    void Start()
    {
        sliderPanel = gameObject.transform.Find("Slider Panel").gameObject;
        filePanel = gameObject.transform.Find("Replay File Panel").gameObject;
        fileText = filePanel.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Text>();

        //var verticalLayoutGroup = filePanel.GetComponentsInChildren<HorizontalLayoutGroup>(); // second one is scroll view content
        //content = verticalLayoutGroup[1].gameObject;

        slider = sliderPanel.GetComponentInChildren<Slider>();
        sliderText = sliderPanel.GetComponentInChildren<Text>();

        recordings = new List<string>();
        newRecordings = new List<string>();
        recRep = scene.GetComponent<RecorderReplayer>();
        //fileText.text = "Replay: " + recRep.replayFile;
        fileText.text = "Replay: " + "none";

        try
        {
            dir = new DirectoryInfo(recRep.path); // path to recordings
            if (dir.Exists)
            {
                foreach(var file in dir.EnumerateFiles("r*"))
                {
                    recordings.Add(Path.GetFileNameWithoutExtension(file.Name));
                    //GameObject go = Instantiate(buttonPrefab, content.transform);
                    //Text t = go.GetComponentInChildren<Text>();
                    //t.text = file.Name;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private void SelectReplayFile(string file)
    {
        recRep.replayFile = file;
        fileText.text = "Replay: " + file;
    }

    public void ShowReplayFiles(GameObject content)
    {
        Debug.Log("Show replay files");
        if(!loaded)
        {
            AddFiles(recordings, content);
            loaded = true;
        }
        if (needsUpdate)
        {
            AddFiles(newRecordings, content);
            needsUpdate = false;
            newRecordings.Clear();
        }
    
    }

    private void CloseFileWindow(GameObject content)
    {
        content.transform.parent.parent.gameObject.SetActive(false);
    }

    private void AddFiles(List<string> recordings, GameObject content)
    {
        foreach (var file in recordings)
        {
            GameObject go = Instantiate(buttonPrefab, content.transform);
            go.GetComponent<Button>().onClick.AddListener(delegate { SelectReplayFile(file); } );
            go.GetComponent<Button>().onClick.AddListener(delegate { CloseFileWindow(content); });

            Text t = go.GetComponentInChildren<Text>();
            t.text = file;
        }
    }

    public void ToggleRecord(GameObject icon)
    {
        Image img = icon.GetComponent<Image>();
        
        if (recRep.recording) // if recording stop it
        {
            // add new recording to dropdown and set it as current replay file
            string rec = Path.GetFileNameWithoutExtension(recRep.recordFile);
            recordings.Add(rec);
            newRecordings.Add(rec);
            needsUpdate = true;

            img.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            if (recRep.replaying)
            {
                filePanel.SetActive(true);
                sliderPanel.SetActive(false);
            }
            recRep.recording = false;
            recRep.replayFile = rec; // is probably set twice (in RecorderReplayer SetReplayFile() too)
            fileText.text = "Replay: " + rec;
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
        replayImage = img;

        if (recRep.replaying) // if replaying stop it
        {
            img.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            recRep.replaying = false;
            resetReplayImage = false;
            // panel with slider gets set in PanelSwitcher
        }
        else // start replaying
        {
            img.color = new Color(0.0f, 0.8f, 0.2f, 1.0f);
            recRep.replaying = true;
            resetReplayImage = true;
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
        try
        {
            recRep.sliderFrame = (int)slider.value;
            //sliderText.text = slider.value.ToString();

        }
        catch (System.Exception e)
        {

            Debug.Log("Really uncool... " + e.ToString());
           
        }
    }

    void Update()
    {
        if(recRep.replaying)
        {
            if(!infoSet && recRep.replayer.recInfo != null)
            {
                infoSet = true;
                slider.maxValue = recRep.replayer.recInfo.frames-1;
            }


            if (!recRep.play) // only display frame number during pause otherwise it looks confusing because text changes so fast
            {
                sliderText.text = slider.value.ToString();
            }
            else
            {
                sliderText.text = ""; 
                slider.value = recRep.currentReplayFrame;
            }
        }
        else
        {
            infoSet = false;
            if(resetReplayImage)
            {
                replayImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                resetReplayImage = false;
            }
        }
    }



}