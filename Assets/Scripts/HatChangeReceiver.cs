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

        // Ensure the peer has a unique ID
        if (string.IsNullOrEmpty(roomClient.Me["uuid"]))
        {
            roomClient.Me["uuid"] = System.Guid.NewGuid().ToString();
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<HatChangeMessage>();

        // Only act if the message is for this player
        if (msg.targetPeerId == roomClient.Me["uuid"] && avatar.IsLocal)
        {
            if (hatAvatar && msg.hatIndex >= 0 && msg.hatIndex < hatAvatar.hats.Length)
            {
                hatAvatar.SetHat(hatAvatar.hats[msg.hatIndex]);
            }
        }
    }
}
