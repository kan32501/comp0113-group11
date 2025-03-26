using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Avatars;

public class MatChangeReceiver : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    private Ubiq.Avatars.Avatar avatar;
    private SimpleMaterialAvatar materialAvatar;

    private struct MaterialChangeMessage
    {
        public string targetPeerId;
        public int materialIndex;
    }

    void Start()
    {
        context = NetworkScene.Register(this);
        roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
        avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        materialAvatar = GetComponentInChildren<SimpleMaterialAvatar>();
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<MaterialChangeMessage>();
        if (msg.targetPeerId == avatar?.Peer["uuid"])
        {
            if (materialAvatar != null && msg.materialIndex >= 0 && msg.materialIndex < materialAvatar.materials.Length)
            {   
                materialAvatar.SetMaterial(materialAvatar.materials[msg.materialIndex]); 
            }
        }
    }
}
