using Ubiq.Messaging;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;
using System.IO;
using System;

public class FPSMonitor : MonoBehaviour
{
    private EventLogger logger;
    int fps;

    private string id;
    NetworkScene scene;
    public RoomClient client;

    int FPSLowLimit = 30;
    float TimeBeforeQuit = 10;
    float remaining;
    bool timerStarted = false;
    float timerStartTime;

    public float updateInterval = 0.5F;
    private double lastInterval;
    private int frames;


    private StreamWriter stream;

    void Start()
    {
        if(client == null)
        {
            client = this.transform.parent.GetComponentInChildren<RoomClient>();
        } 
        
        if(client != null)
        {
            id = client.Me.UUID;
        }
        Application.targetFrameRate = 60;
        // QualitySettings.vSyncCount = 1;
        // logger = new UserEventLogger(this);
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;

        stream = new StreamWriter(OpenNewFile());
    }
    // Update is called once per frame
    void Update()
    {
        ++frames;
        float timeNow = Time.realtimeSinceStartup;
        if (timeNow > lastInterval + updateInterval)
        {
            fps = (int)(frames / (timeNow - lastInterval));
            // logger.Log("FPS", Time.realtimeSinceStartup, fps > 0 ? fps : 0, id);
            try
            {
                stream.WriteLine($"{Time.realtimeSinceStartup}, {fps}, {id}");
            }
            catch(Exception)
            {
                return;
            }
            
            frames = 0;
            lastInterval = timeNow;
        }


    }

    Stream OpenNewFile()
    {
        string filename = Filepath();

        return File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    private string Filepath()
    {
        return Path.Combine(Application.persistentDataPath, $"FPS_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{id}.csv");
    }

    private void OnDestroy()
    {
        stream.Close();
    }
}
