using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform targetCamera;
    public Vector3 cameraLocalOffset = new Vector3(0f, 0f, 1.2f);
    public float followSmooth = 18f;
    public bool lockWorldUp = true;

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            targetCamera = mainCamera.transform;
        }

        Vector3 forward = targetCamera.forward;

        if (lockWorldUp)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = targetCamera.forward;
            }
        }

        forward.Normalize();

        Vector3 right = targetCamera.right;

        if (lockWorldUp)
        {
            right.y = 0f;
            if (right.sqrMagnitude < 0.001f)
            {
                right = targetCamera.right;
            }
        }

        right.Normalize();

        Vector3 up = lockWorldUp ? Vector3.up : targetCamera.up;

        Vector3 targetPosition = targetCamera.position
            + right * cameraLocalOffset.x
            + up * cameraLocalOffset.y
            + forward * cameraLocalOffset.z;

        float lerp = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, lerp);

        Vector3 lookDirection = transform.position - targetCamera.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, up);
        }
    }
}
