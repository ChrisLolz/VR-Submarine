using UnityEngine;

public class CubeCollisionState : MonoBehaviour
{
    public bool IsColliding { get; private set; }

    void OnCollisionEnter(Collision collision)
    {
        IsColliding = true;
    }

    void OnCollisionStay(Collision collision)
    {
        IsColliding = true;
    }

    void OnCollisionExit(Collision collision)
    {
        IsColliding = false;
    }

    void OnTriggerEnter(Collider other)
    {
        IsColliding = true;
    }

    void OnTriggerStay(Collider other)
    {
        IsColliding = true;
    }

    void OnTriggerExit(Collider other)
    {
        IsColliding = false;
    }
}
