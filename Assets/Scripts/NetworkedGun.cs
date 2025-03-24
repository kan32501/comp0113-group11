using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;

public class NetworkedGun : MonoBehaviour
{
    private GunBehavior gunBehavior;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private NetworkContext context;

    void Start()
    {
        gunBehavior = GetComponent<GunBehavior>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        context = NetworkScene.Register(this);

        // Initially disable gun shooting until grabbed
        gunBehavior.enabled = false;

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnActivate);
        grabInteractable.deactivated.AddListener(OnDeactivate);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Enable gun behavior when grabbed
        gunBehavior.enabled = true;
        // Notify others that this gun has been grabbed
        context.SendJson(new GunMessage { isGrabbed = true });
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // Disable gun behavior when released
        gunBehavior.enabled = false;
        // Notify others that this gun has been released
        context.SendJson(new GunMessage { isGrabbed = false });
    }

    void OnActivate(ActivateEventArgs args)
    {
        // Trigger gun firing when activated (e.g., trigger pulled)
        gunBehavior.Fire();
        // Notify others about the firing action
        context.SendJson(new GunMessage { isFiring = true });
    }

    void OnDeactivate(DeactivateEventArgs args)
    {
        // Handle deactivation if needed (e.g., stop firing)
        // Notify others that firing has stopped
        context.SendJson(new GunMessage { isFiring = false });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var gunMessage = message.FromJson<GunMessage>();

        // Handle incoming messages to synchronize state
        if (gunMessage.isGrabbed.HasValue)
        {
            gunBehavior.enabled = gunMessage.isGrabbed.Value;
        }
        if (gunMessage.isFiring.HasValue && gunMessage.isFiring.Value)
        {
            gunBehavior.Fire();
        }
    }

    private struct GunMessage
    {
        public bool? isGrabbed;
        public bool? isFiring;
    }
}
