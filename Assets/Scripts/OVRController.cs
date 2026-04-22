using UnityEngine;

public class OVRController : MonoBehaviour
{
    public enum Hand { Left, Right }
    public Hand hand = Hand.Right;

    [Header("Input")]
    public OVRInput.Button gripButton = OVRInput.Button.PrimaryHandTrigger;

    // 
    public Interactable touchedItem;
    public Interactable grippedItem;
    OVRInput.Controller Ctrl =>
        (hand == Hand.Left) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

    bool gripping;

    void Update()
    {
        // Release gripped item on grip release
        var ctrl = (hand == Hand.Left) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                
        if (grippedItem != null && OVRInput.GetUp(gripButton, ctrl))
        {
            Debug.Log("Release grip");
            grippedItem.OnGripEnd(this);
            grippedItem = null;
            gripping = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        var interactable = other.attachedRigidbody.GetComponent<Interactable>();
        if (interactable == null || !interactable.enabled) return;
       
        //Debug.Log("Call Dial on Touch Enter");
        interactable.OnTouchEnter(this);
    }

    // Gripping setup is placed here as user might touch component then press grip trigger
    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        var interactable = other.attachedRigidbody.GetComponent<Interactable>();
        if (interactable == null || !interactable.enabled) return;
        // Already gripping it
        if (grippedItem == interactable) return;

        var ctrl = (hand == Hand.Left) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        bool grip = OVRInput.Get(gripButton, ctrl);
      
        // If grip trigger held and we haven't already set up grip  
        if (grip && !gripping)
        {
            gripping = true;
            grippedItem = interactable;
            grippedItem.OnGripBegin(this);
        }
        
        interactable.OnTouchStay(this);
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        var interactable = other.attachedRigidbody.GetComponent<Interactable>();
        if (interactable == null || !interactable.enabled) return;
        
        // if (interactable == grippedItem)
        // {
        //     grippedItem.OnGripEnd(this);
        //     gripping = false;
        // }
        interactable.OnTouchExit(this);
    }

    // Simple haptics helpers
    public void HapticTick(float amplitude = 0.18f, float duration = 0.015f) => HapticPulse(amplitude, duration);
    public void HapticClick(float amplitude = 0.40f, float duration = 0.035f) => HapticPulse(amplitude, duration);

    void HapticPulse(float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(1f, Mathf.Clamp01(amplitude), Ctrl);
        CancelInvoke(nameof(StopHaptics));
        Invoke(nameof(StopHaptics), duration);
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, Ctrl);
    }
}
