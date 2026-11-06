using TMPro;
using UnityEngine;

public sealed class SlotReelView : MonoBehaviour
{
    [SerializeField] private float speed = 16f;
    [SerializeField] private float curvature = 0.45f;
    [SerializeField] private float edgeSoftness = 0.3f;
    private readonly TextMeshProUGUI[] digits = new TextMeshProUGUI[7];
    private readonly Material[] materials = new Material[7];
    [SerializeField, Min(0.01f)] private float snapDuration = 0.22f;
    [SerializeField, Range(0f, 0.4f)] private float snapOvershoot = 0.12f;
    private float visualOvershoot;
    private int targetDigit;
    private float position;
    private float elapsed;
    private float duration;
    private float distance;
    private int previousStep;
    public event System.Action<SlotReelView> Stopped;
    public bool IsSpinning { get; private set; }

    public void Initialize(TMP_FontAsset font)
    {
        for (int i = 0; i < digits.Length; i++)
        {
            var go = new GameObject("Digit", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 112;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.09f, 0.12f, 0.18f);
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(180, 140);
            text.text = Random.Range(1, 10).ToString();
            materials[i] = new Material(text.fontSharedMaterial);
            text.fontSharedMaterial = materials[i];
            digits[i] = text;
        }
        Render();
    }

    public void ConfigureSnap(float seconds, float overshoot)
    {
        snapDuration = Mathf.Max(0.01f, seconds);
        snapOvershoot = Mathf.Clamp(overshoot, 0f, 0.4f);
    }

    public void Spin(float seconds, int target)
    {
        if (IsSpinning) return;
        targetDigit = target;
        duration = Mathf.Max(0.02f, seconds);
        visualOvershoot = 0f;
        distance = Mathf.Max(4f, Mathf.Round(speed * duration * 0.6f));
        position = elapsed = 0;
        previousStep = 0;
        IsSpinning = true;
    }

    private void Update()
    {
        if (!IsSpinning) return;
        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, duration);
        float snapTime = Mathf.Min(snapDuration, duration * 0.3f);
        float travelTime = duration - snapTime;
        float t = Mathf.Clamp01(elapsed / travelTime);
        // Integral of a speed curve with smooth acceleration and deceleration.
        position = (distance - 1f) * (t - Mathf.Sin(2 * Mathf.PI * t) / (2 * Mathf.PI));
        visualOvershoot = 0f;
        if (elapsed >= travelTime)
        {
            float snap = Mathf.Clamp01((elapsed - travelTime) / snapTime);
            // Reach the final row quickly, overshoot, then lock without recycling backwards.
            float progress = snap < 0.55f
                ? Mathf.Lerp(0f, 1f + snapOvershoot, 1f - Mathf.Pow(1f - snap / 0.55f, 3f))
                : Mathf.Lerp(1f + snapOvershoot, 1f, Mathf.SmoothStep(0f, 1f, (snap - 0.55f) / 0.45f));
            position = distance - 1f + Mathf.Min(progress, 1f);
            visualOvershoot = Mathf.Max(0f, progress - 1f);
        }
        int step = Mathf.FloorToInt(position);
        while (previousStep < step)
        {
            var last = digits[6];
            var material = materials[6];
            for (int i = 6; i > 0; i--) { digits[i] = digits[i - 1]; materials[i] = materials[i - 1]; }
            digits[0] = last;
            materials[0] = material;
            last.text = previousStep + 1 == (int)distance - 3
                ? targetDigit.ToString() : Random.Range(1, 10).ToString();
            previousStep++;
        }
        if (elapsed >= duration) { position = distance; visualOvershoot = 0f; IsSpinning = false; }
        Render();
        if (!IsSpinning) Stopped?.Invoke(this);
    }

    private void Render()
    {
        float fraction = position - Mathf.Floor(position) + visualOvershoot;
        for (int i = 0; i < digits.Length; i++)
        {
            float row = 3 - i - fraction;
            float edge = Mathf.Clamp01(Mathf.Abs(row) / 1.5f);
            digits[i].rectTransform.anchoredPosition = new Vector2(0, row * 112);
            digits[i].rectTransform.localScale = new Vector3(1 - edge * 0.08f, 1 - edge * curvature, 1);
            materials[i].SetFloat("_OutlineSoftness", edge * edgeSoftness);
        }
    }

    private void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
