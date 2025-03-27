using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Rooms;
using Ubiq.Messaging;


public class HatTarget : MonoBehaviour
{
    private SimpleHatAvatar hatAvatar;
    private Ubiq.Avatars.Avatar avatar;
    private RoomClient roomClient;

    private void Start()
    {
        hatAvatar = GetComponent<SimpleHatAvatar>();
        avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
    }

    public void RegisterHit()
    {
        if (avatar == null || !avatar.IsLocal)
        {
            Debug.Log("Hit registered on remote avatar — ignoring.");
            return;
        }

        if (hatAvatar == null || hatAvatar.hats.Length == 0)
        {
            Debug.LogWarning("⚠️ No hats available on this avatar.");
            return;
        }

        int index = Random.Range(0, hatAvatar.hats.Length);
        Debug.Log($"🎩 I was hit — switching to hat index {index}: {hatAvatar.hats[index].name}");
        hatAvatar.SetHat(hatAvatar.hats[index]); // This auto-syncs via Ubiq
    }
}
