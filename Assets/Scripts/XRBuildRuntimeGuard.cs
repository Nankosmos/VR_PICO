using System.Collections;
using UnityEngine;

public class XRBuildRuntimeGuard : MonoBehaviour
{
    private const float CleanupDuration = 3f;
    private const float CleanupInterval = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeGuard()
    {
#if !UNITY_EDITOR
        GameObject guardObject = new GameObject("XR Build Runtime Guard");
        DontDestroyOnLoad(guardObject);
        guardObject.AddComponent<XRBuildRuntimeGuard>();
#endif
    }

    IEnumerator Start()
    {
#if !UNITY_EDITOR
        float timer = 0f;

        while (timer < CleanupDuration)
        {
            DisableEditorOnlyXRObjects();
            timer += CleanupInterval;
            yield return new WaitForSeconds(CleanupInterval);
        }
#else
        yield break;
#endif
    }

    void DisableEditorOnlyXRObjects()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject target in allObjects)
        {
            if (target == null) continue;
            if (!target.scene.IsValid()) continue;

            string objectName = target.name;

            if (objectName.Contains("XR Device Simulator")
                || objectName.Contains("Device Simulator")
                || objectName.Contains("XR Simulator"))
            {
                target.SetActive(false);
            }
        }
    }
}
