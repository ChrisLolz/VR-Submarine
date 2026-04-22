using UnityEngine;
using System.Collections.Generic;

public class Sonar : MonoBehaviour
{
    public Transform submarine; 
    public LayerMask iconLayer; 
    public float borderPadding = 0.05f;
    public float cameraHeight = 100f;

    private Camera minimapCam;

    void Start() {
        minimapCam = GetComponent<Camera>();
    }

    void LateUpdate() {
        if (submarine == null) return;

        transform.position = new Vector3(submarine.position.x, cameraHeight, submarine.position.z);
        transform.rotation = Quaternion.Euler(90, submarine.eulerAngles.y, -90);

        int layerIndex = (int)Mathf.Log(iconLayer.value, 2);
        GameObject[] goArray = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> icons = new List<GameObject>();
        for (int i = 0; i < goArray.Length; i++) {
            if (goArray[i].layer == layerIndex) icons.Add(goArray[i]);
        }

        foreach (GameObject icon in icons) {
            Transform parent = icon.transform.parent;
            if (parent == null) continue;

            Vector3 viewportPos = minimapCam.WorldToViewportPoint(parent.position);

            viewportPos.x = Mathf.Clamp(viewportPos.x, borderPadding, 1f - borderPadding);
            viewportPos.y = Mathf.Clamp(viewportPos.y, borderPadding, 1f - borderPadding);
            viewportPos.z = minimapCam.nearClipPlane + 1f; 

            icon.transform.position = minimapCam.ViewportToWorldPoint(viewportPos);
        }
    }
}
