using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Ubiq.Messaging;
using UnityEngine.UI;
using Ubiq.Rooms;
using Ubiq.Samples;

public class RecorderReplayerMenu : MonoBehaviour
{
    public NetworkScene scene;
    private RoomClient roomClient;
    public Sprite playSprite;
    public Sprite pauseSprite;
    public GameObject buttonPrefab;
    public GameObject recordBtn;
    public GameObject replayBtn;

    private Image recordImage;
    private Image replayImage;

    private PanelSwitcher panelSwitcher;

    private RecorderReplayer recRep;
    private GameObject sliderPanel;
    private Slider slider;
    private Text sliderText;
    private GameObject filePanel;
    private GameObject scrollView;
    private Text fileText;
    private DirectoryInfo dir;

    private EventTrigger trigger;

    private List<string> recordings;
    private List<string> newRecordings;

    private bool infoSet = false;
    private bool loaded = false;
    private bool needsUpdate = true;
    private bool resetReplayImage = false; // in case we loaded an invalid file path

    public void OnPeerAdded(IPeer peer)
    {
        Debug.Log("Menu: OnPeerAdded");
        OnPeerUpdated(peer);

    }

    public void OnJoinedRoom(IRoom room)
    {
        if (roomClient.Me["creator"] == "1")
        {
            GetReplayFiles();
        }
    }

    public void OnPeerUpdated(IPeer peer)
    {
        if (peer.UUID == roomClient.Me.UUID) // check this otherwise we also update wrong peer and hide menu accidentally
        {
            UpdateMenu(peer);
        }
    }

    private void UpdateMenu(IPeer peer)
    {
        if (peer["creator"] == "1")
        {
            Debug.Log("Menu: creator");
            recordBtn.SetActive(true);
            replayBtn.SetActive(true);
            // set color of record/replay button back to gray in case of ongoing recording/replaying
            if (!recRep.recording)
            {
                recordImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            }
            if (!recRep.replaying)
            {
                filePanel.SetActive(true);
                replayImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            }
        }
        else
        {
            Debug.Log("Menu: NOT creator");
            recordBtn.SetActive(false);
            replayBtn.SetActive(false);
            filePanel.SetActive(false);
        }
    }

    public void OnPanelSwitch()
    {
        if (recRep.recording)
        {
            Debug.Log("OnPanelSwitch: End ongoing recording (replay)");
            EndRecordingAndCleanup();
        }
        if (recRep.replaying)
        {
            Debug.Log("OnPanelSwitch: End ongoing replay");
            EndReplayAndCleanup();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        panelSwitcher = GetComponent<PanelSwitcher>();
        panelSwitcher.OnPanelSwitch.AddListener(OnPanelSwitch);

        recordings = new List<string>();
        newRecordings = new List<string>();
        replayImage = replayBtn.transform.Find("Icon").gameObject.GetComponent<Image>();
        recordImage = recordBtn.transform.Find("Icon").gameObject.GetComponent<Image>();

        // this is not ideal if menu hierarchy changes forgive me but it does the job for now
        filePanel = gameObject.transform.GetChild(0).GetChild(2).GetChild(1).gameObject;
        scrollView = filePanel.transform.GetChild(0).gameObject;
        fileText = filePanel.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Text>();
        fileText.text = "Replay: " + "none";

        Debug.Log("Set RecorderReplayer in Menu");
        recRep = scene.GetComponent<RecorderReplayer>();

        sliderPanel = gameObject.transform.Find("Slider Panel").gameObject;
        slider = sliderPanel.GetComponentInChildren<Slider>();
        sliderText = sliderPanel.GetComponentInChildren<Text>();
        trigger = slider.gameObject.GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => { OnPointerUpDelegate((PointerEventData)data); });
        trigger.triggers.Add(entry);
        //EventTrigger.Entry entry = new EventTrigger.Entry();
        //entry.eventID = EventTriggerType.EndDrag;
        //entry.callback.AddListener((data) => { OnEndDrag((PointerEventData)data); });
        //trigger.triggers.Add(entry);

        roomClient = scene.GetComponent<RoomClient>();
        roomClient.OnPeerUpdated.AddListener(OnPeerUpdated);
        roomClient.OnPeerAdded.AddListener(OnPeerAdded);
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);



        GetReplayFiles();
    }

    public void OnPointerUpDelegate(PointerEventData data)
    {
        SetReplayFrame();
    }

    private void GetReplayFiles()
    {
        recordings.Clear();
        try
        {
            dir = new DirectoryInfo(recRep.path); // path to recordings
            if (dir.Exists)
            {
                foreach (var file in dir.EnumerateFiles("r*"))
                {
                    recordings.Add(Path.GetFileNameWithoutExtension(file.Name));
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

    // called when "select replay" is clicked
    public void ShowReplayFiles(GameObject content)
    {
        Debug.Log("Show replay files");
        scrollView.SetActive(!scrollView.activeSelf);
        if(!loaded)
        {
            Debug.Log("Add all files");
            AddFiles(recordings, content);
            loaded = true;
            needsUpdate = false;
            newRecordings.Clear();
        }
        else if (needsUpdate)
        {
            Debug.Log("Add new!");
            AddFiles(newRecordings, content);
            needsUpdate = false;
            newRecordings.Clear();
        }
    
    }

    private void CloseFileWindow(GameObject content)
    {
        content.transform.parent.parent.gameObject.SetActive(false);
        scrollView.SetActive(false);
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

    public void ToggleRecord()
    {
        //Image img = icon.GetComponent<Image>();
        
        if (recRep.recording) // if recording stop it
        {
            EndRecordingAndCleanup();
        }
        else // start recording
        {
            Debug.Log("Toggle Record");
            recordImage.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            recRep.recording = true;
        }
        
    }

    private void EndRecordingAndCleanup()
    {
        // add new recording to dropdown and set it as current replay file
        string rec = Path.GetFileNameWithoutExtension(recRep.recordFile);
        Debug.Log(rec);
        recordings.Add(rec);
        newRecordings.Add(rec);
        needsUpdate = true;

        recordImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
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

    public void ToggleReplay()
    {
        //Image img = icon.GetComponent<Image>();
        //replayImage = img;

        if (recRep.replaying) // if replaying stop it
        {
            EndReplayAndCleanup();
        }
        else // start replaying
        {
            sliderPanel.SetActive(true);
            filePanel.SetActive(false);
            replayImage.color = new Color(0.0f, 0.8f, 0.2f, 1.0f);
            recRep.replaying = true;
            resetReplayImage = true;
            slider.minValue = 0;
        }
    }

    private void EndReplayAndCleanup()
    {
        sliderPanel.SetActive(false);
        filePanel.SetActive(true);
        replayImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        recRep.replaying = false;
        resetReplayImage = false;
        if (!recRep.play)
        {
            // when replay is initialised again the correct sprite is visible in the slider panel
            var setToPause = sliderPanel.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Image>();
            setToPause.sprite = pauseSprite;
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
                sliderPanel.SetActive(false);
                filePanel.SetActive(true);
            }
        }
    }

    //public void OnEndDrag(PointerEventData eventData)
    //{
    //    Debug.Log("OnEndDrag");
    //}
}
