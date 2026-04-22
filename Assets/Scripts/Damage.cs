using UnityEngine;
using UnityEngine.Events;

public class Damage : MonoBehaviour
{
    public UnityEvent<bool> damageEvent;
    public LayerMask damageLayer;
    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & damageLayer) != 0)
        {
            damageEvent?.Invoke(true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & damageLayer) != 0)
        {
            damageEvent?.Invoke(false);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
