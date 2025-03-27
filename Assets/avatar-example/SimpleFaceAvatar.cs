using System;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;

/// <summary>
/// This class changes the face material of a specific mesh renderer in the avatar.
/// It also handles syncing the selected face over the network.
/// </summary>
public class SimpleFaceAvatar : MonoBehaviour
{
    public Material[] faces; // List of face materials to choose from
    public Material currentFace;
    public string targetFaceMeshName = "FaceMesh"; // Name of the target face mesh renderer

    private Renderer targetRenderer; // The specific renderer we want to change
    private RoomClient roomClient;
    private Avatar avatar; 
    
    // Keep a record of the last face as a string for syncing across peers.
    private string lastFace;
    
    [Serializable]
    private struct SerializableFace
    {
        public int index;
    }
    
    private void Start()
    {
        avatar = GetComponentInParent<Avatar>();
        
        // Find the specific mesh renderer by name (you can also use other criteria like tag, etc.)
        targetRenderer = FindRendererByName(targetFaceMeshName);
        
        if (targetRenderer == null)
        {
            Debug.LogWarning("No face mesh renderer found with the name: " + targetFaceMeshName);
        }
        
        var networkScene = NetworkScene.Find(this); 
        roomClient = networkScene.GetComponentInChildren<RoomClient>();
        
        // Connect a listener for when any peer's properties change.
        roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeerUpdated);
        
        // Set an initial face if none is set.
        if (faces.Length > 0 && !currentFace)
        {
            SetFace(faces[0]);
        }
    }

    private void OnDestroy()
    {
        // Cleanup the event so it does not get called after we have been destroyed.
        if (roomClient)
        {
            roomClient.OnPeerUpdated.RemoveListener(RoomClient_OnPeerUpdated);
        }
    }

    public void SetFace(Material face)
    {
        if (!avatar.IsLocal)
        {
            // If we are not local, we are a remote copy, and we do not control
            // this avatar. Only the peer who controls this avatar should be
            // able to change it.
            return;
        }
        
        int index = Array.IndexOf(faces, face);
        if (index < 0)
        {
            Debug.LogWarning("Unrecognized face. This class can only set " +
                             "faces from a pre-determined list.");
            return;
        }
        
        // Set the face as a property for our peer. Remote copies of this
        // avatar will be informed of the change as an event.
        var serializedFace = JsonUtility.ToJson(new SerializableFace
        {
            index = index
        });
        ProcessFace(serializedFace);
        roomClient.Me["simple-face-avatar"] = serializedFace;
    }
    
    private void RoomClient_OnPeerUpdated(IPeer peer)
    {
        if (peer != avatar.Peer)
        {
            // The peer being updated is not our peer, so we can safely ignore this event.
            return;
        }
        
        ProcessFace(peer["simple-face-avatar"]);
    }
    
    private void ProcessFace(string serializedFace)
    {
        if (String.IsNullOrEmpty(serializedFace) || serializedFace == lastFace)
        {
            return;
        }

        // Deserialize from a string into an index. 
        int index = JsonUtility.FromJson<SerializableFace>(serializedFace).index;
        if (index < 0 || index >= faces.Length)
        {
            Debug.LogWarning("Unrecognized face received as property.");
            return;
        }
        
        // Apply the new face to the target renderer
        if (targetRenderer != null)
        {
            Material[] materials = targetRenderer.materials;
            materials[1] = faces[index]; // Assuming face is at index 1 in the materials array
            targetRenderer.materials = materials;
            currentFace = faces[index];
        }
        else
        {
            Debug.LogWarning("Target face renderer not found.");
        }
            
        lastFace = serializedFace;
    }

    // Helper function to find the renderer by its name
    private Renderer FindRendererByName(string meshName)
    {
        // Customize search with other conditions if necessary (e.g., by tag, by index, etc.)
        var renderers = GetComponentsInChildren<Renderer>(true); // Include inactive if necessary
        foreach (var renderer in renderers)
        {
            if (renderer.name == meshName)
            {
                return renderer;
            }
        }
        return null;
    }
}
