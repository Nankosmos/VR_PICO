// 文件名建议：HoldStartTrigger.cs

using UnityEngine;

public class HoldStartTrigger : MonoBehaviour
{
    public HoldNote parentHoldNote;

    private void OnTriggerEnter(Collider other)
    {
        if (parentHoldNote != null)
        {
            parentHoldNote.TryStartHold(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (parentHoldNote != null)
        {
            parentHoldNote.TryStartHold(other);
        }
    }
}