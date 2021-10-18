using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Rooms.Messages;
using Ubiq.XR;
using System;

public class Ball : RoomObject, IUseable, IBall
{
    private Hand follow;
    public float throwStrength = 1f;

    [Serializable]
    public class BallState: IRoomObjectState 
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public bool grasped;
    }
    [Serializable]
    public class BallStateMessage
    {
        public RoomObjectInfo objectInfo;
        public BallState state;
    }

    BallState previousState;


    void Awake()
    {
        Debug.Log("Ball: Awake");
        base.Awake();
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Ball: Start");
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
        if (follow != null)
        {
            owner = true;
            rb.isKinematic = false;
            transform.position = follow.transform.position;
            transform.rotation = follow.transform.rotation;
        }

        if (owner && stateChanged())
        {
            UpdateObjectState();
        }
    }

    void UpdateObjectState()
    {
        SetObjectProperties();
        
        previousState = new BallState {
                            position = transform.position,
                            rotation = transform.rotation,
                            velocity = currentVelocity,
                            grasped = true
                        };
        Debug.Log("UpdateObjectState: previousState: " + previousState.position + ", " + previousState.velocity);
        SendToServer("UpdateObjectState", new BallStateMessage {
            objectInfo = Me.GetRoomObjectInfo(),
            state = previousState
        });
    }

    bool stateChanged()
    {
        if(previousState != null && (previousState.position == transform.position && previousState.rotation == transform.rotation))
            return false;
        return true;
    }

    public void Attach(Hand hand)
    {
        Debug.Log("Ball: Attach");
        follow = hand;
        owner = true;
    }

    public void Grasp(Hand controller)
    {
        follow = controller;
        if(!owner)
        {
            owner = true;
            UpdateOwner(Me.GetRoomObjectInfo().MyPeer.uuid);
        }
        
    }

    public void Release(Hand controller)
    {
        follow = null;
        if(rb != null)
        {
            rb.AddForce(-rb.velocity, ForceMode.VelocityChange);
            rb.AddForce(controller.velocity * throwStrength, ForceMode.VelocityChange);
        }
        
    }

    public void UnUse(Hand controller)
    {
    }

    public void Use(Hand controller)
    {
        Debug.Log("Ball: Use");
        if(OwnerUUID == "")
        {
            owner = true;
            UpdateOwner(MyPeerUUID);
        }
        follow = null;
        rb.isKinematic = false;
        rb.AddForce(controller.transform.forward * throwStrength, ForceMode.Impulse);
    }

    override public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        base.ProcessMessage(message);
        if(messageProcessed)
        {
            return;
        }
        // var state = message.FromJson<RoomObjectInfoMessage>();
        // owner = state.ownerPeerUUID == Me.GetRoomObjectInfo().MyPeer.UUID;
        
        if(owner)
        {
            rb.isKinematic = false;
            return;
        }
        var container = JsonUtility.FromJson<Message>(message.ToString());
        switch (container.type)
        {
            case "ObjectStateUpdated":
            {
                var state = JsonUtility.FromJson<BallState>(container.args);
                transform.position = state.position;
                transform.rotation = state.rotation;
                rb.velocity = state.velocity;
            }
            break;
        }

        rb.isKinematic = true;
    }

 
    public override void ObjectSpawned(RoomInfo room, string jsonObjectInfo)
    {
        Debug.Log("Ball: ObjectSpawned: " + jsonObjectInfo);
        RoomDictionaryInfoMessage<BallState> objectInfo = JsonUtility.FromJson<RoomDictionaryInfoMessage<BallState>>(jsonObjectInfo);
        SetRoomObjectInfo(objectInfo);
        
        Me.SetRoom(room);
        RoomName = room.Name;
    }

    override protected void SetObjectProperties(bool setVelocityZero = false)
    {
        RoomObjectInfo info = Me.GetRoomObjectInfo();

        var objectState = new BallState(){
            position = this.transform.position,
            rotation = this.transform.rotation,
            velocity = setVelocityZero ? Vector3.zero : this.currentVelocity,
            grasped = (follow != null)
        };
  

        Me[info.RoomDictionaryKey] = JsonUtility.ToJson( new RoomDictionaryInfoMessage<BallState>(){
            catalogueIndex = this.catalogueIndex,
            networkId = this.Id,
            persistencyLevel = info.PersistencyLevel,
            state = objectState
        });
    }

    protected override void SetRoomObjectInfo<T>(RoomDictionaryInfoMessage<T> msg)
    {
        Debug.Log("Ball: SetRoomObjectInfo");
        var info = msg; 
        if(info.state is BallState)
        {
            Debug.Log("Ball: Is ball state");
            var state = info.state as BallState;
            this.transform.position = state.position;
            this.transform.rotation = state.rotation;
            
            if(rb != null)
            {
                rb.velocity = state.velocity;
            }
            this.currentVelocity = state.velocity;
            this.catalogueIndex = msg.catalogueIndex;
            Id = msg.networkId;
            
            this.persistencyLevel = msg.persistencyLevel.StringToEnum();
            Me.SetPersistencyLevel(this.persistencyLevel);
        }
    }
}
