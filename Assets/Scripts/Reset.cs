using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reset"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
