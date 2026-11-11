using System;
using UnityEngine;

/// <summary>
/// Where tutorial targets appear, in viewport coordinates: (0,0) bottom-left, (1,1) top-right, (0.5,0.5) centre.
/// Select the TutorialDirector to preview every position as a gizmo in the Scene view.
/// </summary>
[Serializable]
public sealed class TutorialLayout
{
    [Header("Aim")]
    [Tooltip("Markers the player touches with the crosshair.")]
    [SerializeField] private Vector2[] aimMarkers = { new(0.3f, 0.62f), new(0.7f, 0.66f), new(0.5f, 0.42f) };

    [Header("Shoot")]
    [Tooltip("First round: still targets.")]
    [SerializeField] private Vector2[] stillTargets = { new(0.32f, 0.58f), new(0.5f, 0.66f), new(0.68f, 0.58f) };
    [Tooltip("Second round: targets that swing back and forth.")]
    [SerializeField] private Vector2[] movingTargets = { new(0.38f, 0.5f), new(0.62f, 0.68f) };
    [Tooltip("Swing extent of moving targets, in viewport units.")]
    [SerializeField] private Vector2 movingSwing = new(0.12f, 0f);
    [SerializeField, Min(0f)] private float movingSwingSpeed = 2.4f;

    [Header("Combo")]
    [Tooltip("Positions cycled through while the player builds the combo.")]
    [SerializeField] private Vector2[] comboLanes = { new(0.3f, 0.55f), new(0.5f, 0.66f), new(0.7f, 0.55f), new(0.42f, 0.45f), new(0.58f, 0.45f) };
    [SerializeField] private Vector2 comboSwing = new(0.05f, 0.02f);
    [SerializeField, Min(0f)] private float comboSwingSpeed = 1.8f;
    [Tooltip("Targets kept on screen at once during the combo step.")]
    [SerializeField, Min(1)] private int comboActiveTargets = 2;

    public Vector2[] AimMarkers => aimMarkers;
    public Vector2[] StillTargets => stillTargets;
    public Vector2[] MovingTargets => movingTargets;
    public Vector2 MovingSwing => movingSwing;
    public float MovingSwingSpeed => movingSwingSpeed;
    public Vector2[] ComboLanes => comboLanes;
    public Vector2 ComboSwing => comboSwing;
    public float ComboSwingSpeed => comboSwingSpeed;
    public int ComboActiveTargets => comboActiveTargets;
}
