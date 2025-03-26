using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;

public class HatNetworkedGun : MonoBehaviour
{
    private HatGunBehavior gunBehavior;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private NetworkContext context;
    private bool isGrabbed = false;
    private bool isFiring = false;
    private bool isOwner = false;

    private Vector3 lastPosition;

    void Start()
    {
        gunBehavior = GetComponent<HatGunBehavior>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        context = NetworkScene.Register(this);

        gunBehavior.enabled = false;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnActivate);
        grabInteractable.deactivated.AddListener(OnDeactivate);
    }

    void Update()
    {
        // Only the owner sends sync updates
        if (!isOwner) return;

        context.SendJson(new GunMessage()
        {
            isGrabbed = isGrabbed,
            isFiring = isFiring
        });

        if (lastPosition != transform.localPosition)
        {
            lastPosition = transform.localPosition;
            context.SendJson(new GunMessage()
            {
                isGrabbed = isGrabbed,
                isFiring = isFiring,
                position = transform.localPosition
            });
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        gunBehavior.enabled = true;
        isGrabbed = true;
        isOwner = true;
        gunBehavior.SetOwnership(true);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        gunBehavior.enabled = false;
        isGrabbed = false;
        isOwner = false;
        gunBehavior.SetOwnership(false);
    }

    void OnActivate(ActivateEventArgs args)
    {
        gunBehavior.Fire();
        isFiring = true;
    }

    void OnDeactivate(DeactivateEventArgs args)
    {
        isFiring = false;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // Skip if this client is currently the owner
        if (isOwner) return;

        var gunMessage = message.FromJson<GunMessage>();

        transform.localPosition = gunMessage.position;
        lastPosition = gunMessage.position;

        gunBehavior.enabled = gunMessage.isGrabbed;
        isGrabbed = gunMessage.isGrabbed;

        if (gunMessage.isFiring && !isFiring)
        {
            gunBehavior.Fire();
        }

        isFiring = gunMessage.isFiring;
    }

    private struct GunMessage
    {
        public bool isGrabbed;
        public bool isFiring;
        public Vector3 position;
    }
}
