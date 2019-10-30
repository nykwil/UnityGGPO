using UnityEngine;

[ExecuteInEditMode]
public class FaceCamera : MonoBehaviour {
    public Camera mainCamera;

    void Update() {
        if (mainCamera != null) {
            transform.forward = transform.position - mainCamera.transform.position;
        }
    }
}
