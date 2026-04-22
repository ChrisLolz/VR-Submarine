using System.Security.Cryptography;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Rigidbody player;
    public float speed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        Vector2 primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector3 movement = transform.right * primaryThumbstick.x + transform.forward * primaryThumbstick.y;
        player.linearVelocity = new Vector3(movement.x * speed, player.linearVelocity.y, movement.z * speed); 
    }
}
