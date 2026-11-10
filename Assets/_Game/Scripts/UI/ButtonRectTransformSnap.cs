using UnityEngine;

/// <summary>Attach to the target RectTransform and bind Play to the source Button's On Click.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ButtonRectTransformSnap : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button button;

    [Header("Initial Pose")]
    [SerializeField] private Vector3 initialPosition = new Vector3(648.8f, 116.2f, 0f);
    [SerializeField] private Vector3 initialRotation = Vector3.zero;

    [Header("Clicked Pose")]
    [SerializeField] private Vector3 clickedPosition = new Vector3(648.8f, 137f, 0f);
    [SerializeField] private Vector3 clickedRotation = new Vector3(0f, 180f, -180f);
    [Tooltip("Seconds in real time before returning to the initial pose. Repeated clicks restart the timer.")]
    [SerializeField, Min(0f)] private float returnDelay = 0.5f;

    private RectTransform target;
    private bool waitingToReturn;
    private double returnTime;

    private void Awake()
    {
        target = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        RestoreInitialPose();
    }

    public void Play()
    {
        if (!isActiveAndEnabled || button == null || !button.isActiveAndEnabled || !button.IsInteractable())
            return;

        target.anchoredPosition3D = clickedPosition;
        target.localRotation = Quaternion.Euler(clickedRotation);
        returnTime = Time.unscaledTimeAsDouble + Mathf.Max(0f, returnDelay);
        waitingToReturn = true;

        if (returnDelay <= 0f)
            RestoreInitialPose();
    }

    private void Update()
    {
        if (waitingToReturn && Time.unscaledTimeAsDouble >= returnTime)
            RestoreInitialPose();
    }

    private void OnDisable()
    {
        RestoreInitialPose();
    }

    private void RestoreInitialPose()
    {
        waitingToReturn = false;
        if (target == null)
            return;

        target.anchoredPosition3D = initialPosition;
        target.localRotation = Quaternion.Euler(initialRotation);
    }
}
