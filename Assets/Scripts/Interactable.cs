using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class Interactable : MonoBehaviour
{
    // Used for controls we typically grip (e.g. dial, lever, slider)
    public virtual void OnGripBegin(OVRController ctrl) { }
    public virtual void OnGripEnd(OVRController ctrl) { }

    // Used for controls we just touch or push (e.g. button, switch)
    public virtual void OnTouchEnter(OVRController ctrl) { }
    public virtual void OnTouchExit(OVRController ctrl) { }
    public virtual void OnTouchStay(OVRController ctrl) { }
}
