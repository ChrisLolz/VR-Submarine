using UnityEngine;
using UnityEngine.Events;

public class CollectObject : MonoBehaviour
{
    public LayerMask collectableLayer;
    public UnityEvent<float> CollectTreasure;

    void OnTriggerEnter(Collider other)
    {
        if ((collectableLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Treasure treasure = other.gameObject.GetComponent<Treasure>();
            if (treasure != null)
            {
                CollectTreasure?.Invoke(treasure.value);
            }

            Destroy(other.transform.parent.gameObject);
        }
    }
}
