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

    private static readonly NetworkId HatGunNetworkId = new NetworkId(123456); // Shared ID

    void Start()
    {
        context = NetworkScene.Register(this, HatGunNetworkId);
        roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
        avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        hatAvatar = GetComponent<SimpleHatAvatar>();

        if (string.IsNullOrEmpty(roomClient.Me["uuid"]))
        {
            roomClient.Me["uuid"] = System.Guid.NewGuid().ToString();
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<HatChangeMessage>();

        if (msg.targetPeerId == roomClient.Me["uuid"] && avatar.IsLocal)
        {
            if (hatAvatar != null && msg.hatIndex >= 0 && msg.hatIndex < hatAvatar.hats.Length)
            {
                hatAvatar.SetHat(hatAvatar.hats[msg.hatIndex]);
            }
            else
            {
                Debug.LogWarning("⚠️ Invalid hat index or missing hat system.");
            }
        }
    }
}

public struct HatChangeMessage
{
    public string targetPeerId;
    public int hatIndex;
}
