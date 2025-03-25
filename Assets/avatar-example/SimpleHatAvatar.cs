using System;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;

/// <summary>
/// This class changes enables various different gameobjects meant to represent
/// the local user's body and movements. It also handles syncing which
/// gameobject is currently active over the network.
/// </summary>
public class SimpleHatAvatar : MonoBehaviour
{
    public GameObject[] hats;
    public GameObject currentHat;
    
    private RoomClient roomClient;
    private Avatar avatar; 
    
    // Keep a record of the last hat as a string. This lets us quickly check
    // whether anything has changed when we receive the OnPeerUpdated event.
    private string lastHat;
    
    [Serializable]
    private struct SerializableHat
    {
        public int index;
    }
    
    private void Start()
    {
        avatar = GetComponentInParent<Avatar>();
        
        var networkScene = NetworkScene.Find(this); 
        roomClient = networkScene.GetComponentInChildren<RoomClient>();
        
        // Connect up a listener for whenever any peer's properties are changed. 
        roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeerUpdated);
        
        // Set an initial hat if we have none.
        if (!currentHat && hats.Length > 0)
        {
            SetHat(hats[0]);
        }
    }

    private void OnDestroy()
    {
        // Cleanup the event for new properties so it does not get called after
        // we have been destroyed.
        if (roomClient)
        {
            roomClient.OnPeerUpdated.RemoveListener(RoomClient_OnPeerUpdated);
        }
    }

    public void SetHat (GameObject hat)
    {
        if (!avatar.IsLocal)
        {
            // If we are not local, we are a remote copy, and we do not control
            // this avatar. Only the peer who controls this avatar should be
            // able to change it.
            return;
        }
        
        var index = Array.IndexOf(hats,hat);
        if (index < 0)
        {
            Debug.LogWarning("Unrecognized hat. This class can only set " +
                             "hats from a pre-determined list.");
            return;
        }
        
        // Set the hat as a property for our peer. Remote copies of this
        // avatar will be informed of the change as an event. 
        var serializedHat = JsonUtility.ToJson(new SerializableHat
        {
            index = index
        });
        ProcessHat(serializedHat);
        roomClient.Me["simple-hat-avatar"] = serializedHat;
    }
    
    private void RoomClient_OnPeerUpdated(IPeer peer)
    {
        if (peer != avatar.Peer)
        {
            // The peer who is being updated is not our peer, so we can safely
            // ignore this event.
            return;
        }
        
        ProcessHat(peer["simple-hat-avatar"]);
    }
    
    private void ProcessHat(string serializedHat)
    {
        if (String.IsNullOrEmpty(serializedHat) || serializedHat == lastHat)
        {
            return;
        }

        // Deserialize from a string into a color. 
        var index = JsonUtility.FromJson<SerializableHat>(serializedHat).index;
        if (index < 0 || index >= hats.Length)
        {
            Debug.LogWarning("Unrecognized hat received as property.");
            return;
        }
        
        // Deactivate all other hats and activate the current one.
        for (int i = 0; i < hats.Length; i++)
        {
            hats[i].SetActive(i == index);
        }
            
        lastHat = serializedHat;
        currentHat = hats[index];
    }
    
}
