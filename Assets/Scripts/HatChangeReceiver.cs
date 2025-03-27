using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Avatars;

public class HatChangeReceiver : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    private Ubiq.Avatars.Avatar avatar;
    private SimpleHatAvatar hatAvatar;

    //private static readonly NetworkId HatGunNetworkId = new NetworkId(12345);

    private struct HatChangeMessage
    {
        public string targetPeerId;
        public int hatIndex;
    }

    void Start()
    {
        context = NetworkScene.Register(this);
        roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
        avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        hatAvatar = GetComponentInChildren<SimpleHatAvatar>();

        // Ensure the peer has a unique ID when they join 
        if (string.IsNullOrEmpty(roomClient.Me["uuid"]))
        {
            roomClient.Me["uuid"] = System.Guid.NewGuid().ToString();
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<HatChangeMessage>();

        Debug.Log($" HatChangeReceiver received message: {msg.targetPeerId}, index: {msg.hatIndex}");

        if (msg.targetPeerId == roomClient.Me["uuid"] && avatar.IsLocal)
        {
            Debug.Log("This hat change is for me!");

            if (hatAvatar && msg.hatIndex >= 0 && msg.hatIndex < hatAvatar.hats.Length)
            {
                Debug.Log("Found hat system — changing hat to index " + msg.hatIndex);
                hatAvatar.SetHat(hatAvatar.hats[msg.hatIndex]);
            }
            else
            {
                Debug.LogWarning("⚠️ Hat system not found or invalid index.");
            }
        }
        else
        {
            Debug.Log("📬 Message not for me or not local avatar.");
        }
    }

}


