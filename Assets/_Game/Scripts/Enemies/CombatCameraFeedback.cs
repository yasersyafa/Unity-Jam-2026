using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatCameraFeedback : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float shakeScale = 1f;
    [SerializeField, Min(0f)] private float maximumOffset = 0.12f;
    private Vector3 offset;
    private float strength;
    private float remaining;
    private float duration;
    public void Play(float amplitude, float seconds)
    {
        strength = Mathf.Max(strength, amplitude);
        duration = Mathf.Max(0.01f, seconds);
        remaining = duration;
    }
    private void LateUpdate()
    {
        transform.localPosition -= offset;
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        float decay = remaining / Mathf.Max(0.01f, duration);
        offset = remaining > 0f ? Vector3.ClampMagnitude((Vector3)Random.insideUnitCircle * strength * decay * shakeScale, maximumOffset) : Vector3.zero;
        transform.localPosition += offset;
        if (remaining <= 0f) strength = 0f;
    }
    private void OnDisable()
    {
        transform.localPosition -= offset;
        offset = Vector3.zero;
        remaining = strength = 0f;
    }
}
