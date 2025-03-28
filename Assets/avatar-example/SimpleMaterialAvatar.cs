using System;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;

/// <summary>
/// This class changes the material of specific mesh renderers in the avatar.
/// It also handles syncing the selected material over the network.
/// </summary>
public class SimpleMaterialAvatar : MonoBehaviour
{
    public Material[] materials; // List of materials to choose from
    public Material currentMaterial;
    public string[] targetMeshNames = { "Small Body", "Left Hand", "Right Hand" }; // Mesh names to update

    private List<Renderer> targetRenderers = new List<Renderer>(); // All target renderers to change
    private RoomClient roomClient;
    private Avatar avatar;
    private string lastMaterial;

    [Serializable]
    private struct SerializableMaterial
    {
        public int index;
    }

    private void Start()
    {
        avatar = GetComponentInParent<Avatar>();

        // Find all specified mesh renderers
        foreach (var meshName in targetMeshNames)
        {
            var renderer = FindRendererByName(meshName);
            if (renderer != null)
            {
                targetRenderers.Add(renderer);
            }
            else
            {
                Debug.LogWarning("No mesh renderer found with the name: " + meshName);
            }
        }

        var networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponentInChildren<RoomClient>();
        roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeerUpdated);

        if (materials.Length > 0 && !currentMaterial)
        {
            SetMaterial(materials[0]);
        }
    }

    private void OnDestroy()
    {
        if (roomClient)
        {
            roomClient.OnPeerUpdated.RemoveListener(RoomClient_OnPeerUpdated);
        }
    }

    public void SetMaterial(Material material)
    {
        if (!avatar.IsLocal) return;

        int index = Array.IndexOf(materials, material);
        if (index < 0)
        {
            Debug.LogWarning("Material not in list.");
            return;
        }

        var serializedMaterial = JsonUtility.ToJson(new SerializableMaterial { index = index });
        ProcessMaterial(serializedMaterial);
        roomClient.Me["simple-material-avatar"] = serializedMaterial;
    }

    private void RoomClient_OnPeerUpdated(IPeer peer)
    {
        if (peer != avatar.Peer) return;
        ProcessMaterial(peer["simple-material-avatar"]);
    }

    private void ProcessMaterial(string serializedMaterial)
    {
        if (string.IsNullOrEmpty(serializedMaterial) || serializedMaterial == lastMaterial) return;

        int index = JsonUtility.FromJson<SerializableMaterial>(serializedMaterial).index;
        if (index < 0 || index >= materials.Length)
        {
            Debug.LogWarning("Invalid material index.");
            return;
        }

        foreach (var renderer in targetRenderers)
        {
            if (renderer != null)
            {
                renderer.material = materials[index];
            }
        }

        currentMaterial = materials[index];
        lastMaterial = serializedMaterial;
    }

    private Renderer FindRendererByName(string meshName)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
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