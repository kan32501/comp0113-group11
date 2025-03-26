using UnityEngine;
using UnityEngine.InputSystem;
using Ubiq.Messaging;
using Ubiq.Avatars;
using Ubiq.Rooms;

//
public class HatGunBehavior : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform gunMuzzle;
    public float maxRange = 100f;

    private NetworkContext context;
    private RoomClient roomClient;
    private bool isOwner = false;

    private InputAction triggerAction;

    private struct RayMessage
    {
        public Vector3 start;
        public Vector3 end;
    }

    private struct HatChangeMessage
    {
        public string targetPeerId;
        public int hatIndex;
    }

    void Start()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        context = NetworkScene.Register(this);
        roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();

        // Bind right trigger to override
        triggerAction = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/trigger");
        triggerAction.Enable();
    }

    void Update()
    {
        if (!isOwner) return;

        if (triggerAction.ReadValue<float>() > 0.1f)
        {
            Fire();
        }
    }

    public void SetOwnership(bool owner)
    {
        isOwner = owner;
    }

    public void Fire()
    {
        FireGun();
    }

    
    private void FireGun()
    // Casts ray/firing visuals
    // Detects player hits and sends message to change the hat
    {
        Vector3 start = gunMuzzle.position;
        Vector3 direction = gunMuzzle.forward;
        Vector3 end = start + direction * maxRange;

        if (Physics.Raycast(start, direction, out var hit, maxRange))
        {
            end = hit.point;

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log($"Hit player: {hit.collider.name}"); // if max tests

                var avatar = hit.collider.GetComponentInParent<Ubiq.Avatars.Avatar>();
                if (avatar != null && avatar.Peer != null)
                {
                    int hatCount = 0;
                    var hatAvatar = avatar.GetComponentInChildren<SimpleHatAvatar>();
                    if (hatAvatar != null)
                    {
                        hatCount = hatAvatar.hats.Length;
                    }

                    if (hatCount > 1)
                    {
                        int newHatIndex = Random.Range(0, hatCount); // New Hat each time

                        var msg = new HatChangeMessage
                        {
                            targetPeerId = avatar.Peer["uuid"], // Meta data stored in strings for ubiq
                            hatIndex = newHatIndex
                        };

                        context.SendJson(msg);
                    }
                }
            }
        }

        DrawRay(start, end);
        context.SendJson(new RayMessage { start = start, end = end });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    // Only messages related to firing
    // HatChangeReceiver handles the hat change message
    {
        // Check which type of message it is
        var json = message.ToString();
        if (json.Contains("start") && json.Contains("end"))
        {
            var ray = message.FromJson<RayMessage>();
            DrawRay(ray.start, ray.end);
        }
    }

    private void DrawRay(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        CancelInvoke(nameof(HideRay));
        Invoke(nameof(HideRay), 0.1f);
    }

    private void HideRay()
    {
        lineRenderer.enabled = false;
    }
}
