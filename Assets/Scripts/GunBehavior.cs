using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.InputSystem;

public class GunBehavior : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform gunMuzzle;
    public float maxRange = 100f;

    private NetworkContext context;
    private bool isOwner = false;

    // NEW: Input detection
    private InputAction triggerAction;

    private struct RayMessage
    {
        public Vector3 start;
        public Vector3 end;
    }

    void Start()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        context = NetworkScene.Register(this);

        //Bind right trigger to override
        triggerAction = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/trigger");
        triggerAction.Enable();
    }

    void Update()
    {
        if (!isOwner) return;

        // Manually check trigger value
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
        Debug.Log("FIRE TRIGGERED");
    }

    private void FireGun()
    {
        Vector3 start = gunMuzzle.position;
        Vector3 direction = gunMuzzle.forward;
        Vector3 end = start + direction * maxRange;

        if (Physics.Raycast(start, direction, out var hit, maxRange))
        {
            end = hit.point;

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log($"Hit {hit.collider.name}");
                //Send a hat-change RPC here!
            }
        }

        DrawRay(start, end);
        context.SendJson(new RayMessage { start = start, end = end });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var ray = message.FromJson<RayMessage>();
        DrawRay(ray.start, ray.end);
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
