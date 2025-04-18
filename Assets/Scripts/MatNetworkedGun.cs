using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;

public class MatNetworkedGun : MonoBehaviour
{
    private MatGunBehavior gunBehavior;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private NetworkContext context;
    private bool isGrabbed = false;
    private bool isFiring = false;

    void Start()
    {
        gunBehavior = GetComponent<MatGunBehavior>();
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

    void Update()
    {
        // ADDED: Regularly send the current state to ensure synch
        context.SendJson(new GunMessage { isGrabbed = isGrabbed, isFiring = isFiring });
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
    {
        var gunMessage = message.FromJson<GunMessage>();

        // Handle incoming messages to synchronize state
        if (gunMessage.isGrabbed.HasValue)
        {
            gunBehavior.enabled = gunMessage.isGrabbed.Value;
            isGrabbed = gunMessage.isGrabbed.Value;
        }
        if (gunMessage.isFiring.HasValue)
        {
            isFiring = gunMessage.isFiring.Value;
            if (isFiring)
            {
                gunBehavior.Fire();
            }
        }
    }

    private struct GunMessage
    {
        public bool? isGrabbed;
        public bool? isFiring;
    }
}
