using System;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;

/// <summary>
/// This class changes the material of a specific mesh renderer in the avatar.
/// It also handles syncing the selected material over the network.
/// </summary>
public class SimpleMaterialAvatar : MonoBehaviour
{
    public Material[] materials; // List of materials to choose from
    public Material currentMaterial;
    public string targetMeshName = "MeshName"; // Name of the target mesh renderer to change material for

    private Renderer targetRenderer; // The specific renderer we want to change
    private RoomClient roomClient;
    private Avatar avatar; 
    
    // Keep a record of the last material as a string. This lets us quickly check
    // whether anything has changed when we receive the OnPeerUpdated event.
    private string lastMaterial;
    
    [Serializable]
    private struct SerializableMaterial
    {
        public int index;
    }
    
    private void Start()
    {
        avatar = GetComponentInParent<Avatar>();
        
        // Find the specific mesh renderer by name (you can also use other criteria like tag, etc.)
        targetRenderer = FindRendererByName(targetMeshName);
        
        if (targetRenderer == null)
        {
            Debug.LogWarning("No mesh renderer found with the name: " + targetMeshName);
        }
        
        var networkScene = NetworkScene.Find(this); 
        roomClient = networkScene.GetComponentInChildren<RoomClient>();
        
        // Connect a listener for whenever any peer's properties are changed.
        roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeerUpdated);
        
        // Set an initial material if we have none.
        if (materials.Length > 0 && !currentMaterial)
        {
            SetMaterial(materials[0]);
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

    public void SetMaterial(Material material)
    {
        if (!avatar.IsLocal)
        {
            // If we are not local, we are a remote copy, and we do not control
            // this avatar. Only the peer who controls this avatar should be
            // able to change it.
            return;
        }
        
        int index = Array.IndexOf(materials, material);
        if (index < 0)
        {
            Debug.LogWarning("Unrecognized material. This class can only set " +
                             "materials from a pre-determined list.");
            return;
        }
        
        // Set the material as a property for our peer. Remote copies of this
        // avatar will be informed of the change as an event. 
        var serializedMaterial = JsonUtility.ToJson(new SerializableMaterial
        {
            index = index
        });
        ProcessMaterial(serializedMaterial);
        roomClient.Me["simple-material-avatar"] = serializedMaterial;
    }
    
    private void RoomClient_OnPeerUpdated(IPeer peer)
    {
        if (peer != avatar.Peer)
        {
            // The peer who is being updated is not our peer, so we can safely
            // ignore this event.
            return;
        }
        
        ProcessMaterial(peer["simple-material-avatar"]);
    }
    
    private void ProcessMaterial(string serializedMaterial)
    {
        if (String.IsNullOrEmpty(serializedMaterial) || serializedMaterial == lastMaterial)
        {
            return;
        }

        // Deserialize from a string into an index. 
        int index = JsonUtility.FromJson<SerializableMaterial>(serializedMaterial).index;
        if (index < 0 || index >= materials.Length)
        {
            Debug.LogWarning("Unrecognized material received as property.");
            return;
        }
        
        // Apply the new material to the target renderer
        if (targetRenderer != null)
        {
            targetRenderer.material = materials[index];
            currentMaterial = materials[index];
        }
        else
        {
            Debug.LogWarning("Target renderer not found.");
        }
            
        lastMaterial = serializedMaterial;
    }

    // Helper function to find the renderer by its name
    private Renderer FindRendererByName(string meshName)
    {
        // You can customize this search with other conditions (e.g., by tag, by index, etc.)
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