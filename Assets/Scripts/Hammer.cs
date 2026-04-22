using UnityEngine;
using UnityEngine.Events;

public class Hammer : MonoBehaviour
{
    public UnityEvent repairEvent;
    public LayerMask repairLayer;
    private AudioSource audioSource;
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & repairLayer) != 0)
        {
            audioSource.Play();
            repairEvent?.Invoke();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
