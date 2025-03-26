using UnityEngine;
using UnityEngine.InputSystem;
using Ubiq.Messaging;
using Ubiq.Avatars;

public class MatGunBehavior : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform gunMuzzle;
    public float maxRange = 100f;

    private NetworkContext context;
    private bool isOwner = false;

    private InputAction triggerAction;

    private struct RayMessage
    {
        public Vector3 start;
        public Vector3 end;
    }

    private struct MaterialChangeMessage
    {
        public string targetPeerId;
        public int materialIndex;
    }

    void Start()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        context = NetworkScene.Register(this);

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
        Vector3 start = gunMuzzle.position;
        Vector3 direction = gunMuzzle.forward;
        Vector3 end = start + direction * maxRange;

        if (Physics.Raycast(start, direction, out var hit, maxRange))
        {
            end = hit.point;

            if (hit.collider.CompareTag("Player"))
            {
                var avatar = hit.collider.GetComponentInParent<Ubiq.Avatars.Avatar>();
                var matAvatar = hit.collider.GetComponentInChildren<SimpleMaterialAvatar>();

                if (avatar != null && avatar.Peer != null && matAvatar != null)
                {
                    int materialCount = matAvatar.materials.Length;

                    if (materialCount > 1)
                    {
                        int newIndex = Random.Range(0, materialCount); // Random mat index each fire

                        var msg = new MaterialChangeMessage
                        {
                            targetPeerId = avatar.Peer["uuid"],
                            materialIndex = newIndex
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
    {
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
