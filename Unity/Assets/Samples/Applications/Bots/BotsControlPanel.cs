using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Samples.Bots;
using UnityEngine;
using UnityEngine.UI;

public class BotsControlPanel : MonoBehaviour
{
    public Text NumberOfBotsLabel;
    public Button CreateRoomButton;
    public Button JoinRoomButton;
    public Button AddBotsButton;
    public Button ToggleCameraButton;
    public InputField JoinCodeInputField;
    public InputField NumberOfBotsInputField;

    private BotsManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<BotsManager>();

        CreateRoomButton.onClick.AddListener(() =>
        {
            manager.CreateRoom();
        });

        JoinRoomButton.onClick.AddListener(() =>
        {
            manager.JoinRoom();
        });

        AddBotsButton.onClick.AddListener(() =>
        {
            try
            {
                manager.AddBots(int.Parse(NumberOfBotsInputField.text));
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        });

        ToggleCameraButton.onClick.AddListener(() =>
        {
            manager.ToggleCamera();
        });

        JoinCodeInputField.onValueChanged.AddListener(joinCode =>
        {
            manager.JoinCode = joinCode;
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        NumberOfBotsLabel.text = manager.NumBots.ToString();
        JoinCodeInputField.text = manager.JoinCode;
    }
}
