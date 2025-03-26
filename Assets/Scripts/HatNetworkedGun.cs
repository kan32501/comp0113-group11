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

    void Start()
    {
        gunBehavior = GetComponent<HatGunBehavior>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        context = NetworkScene.Register(this); // Register w Ubiq

        // Initially disable gun shooting until grabbed
        gunBehavior.enabled = false;

        // Subscribe to grab and activation events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnActivate);
        grabInteractable.deactivated.AddListener(OnDeactivate);
    }
    Vector3 lastPosition;

    void Update() // ADDED
    {
        // Regularly send the current state to ensure synch
        context.SendJson(new GunMessage() { isGrabbed = isGrabbed, isFiring = isFiring });

        if(lastPosition != transform.localPosition && isGrabbed)
        {
            lastPosition = transform.localPosition;
            context.SendJson(new GunMessage()
            {
                position = transform.localPosition
            });
        }

    
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Enable gun behavior when grabbed
        gunBehavior.enabled = true;
        isGrabbed = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // Disable gun behavior when released
        gunBehavior.enabled = false;
        isGrabbed = false;
    }

    void OnActivate(ActivateEventArgs args)
    {
        // Trigger gun firing when activated 
        gunBehavior.Fire();
        isFiring = true;
    }

    void OnDeactivate(DeactivateEventArgs args)
    {
        // Handle stop firing
        isFiring = false;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    // Syncs gun state - should i only send messages with a state change?*
    {
        var gunMessage = message.FromJson<GunMessage>();

        // Handle incoming messages to synchronize state
        if (gunMessage.isGrabbed)
        {
            gunBehavior.enabled = gunMessage.isGrabbed;
            isGrabbed = gunMessage.isGrabbed;
        }
        if (gunMessage.isFiring)
        {
            isFiring = gunMessage.isFiring;
            if (isFiring)
            {
                gunBehavior.Fire();
            }
        }

        transform.localPosition = gunMessage.position;

        // Make sure the logic in Update doesn't trigger as a result of this message
        lastPosition = transform.localPosition;
    }

    private struct GunMessage
    {
        public bool isGrabbed;
        public bool isFiring;
        public Vector3 position;
    }
}
