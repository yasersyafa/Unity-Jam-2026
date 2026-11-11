using System.Collections.Generic;
using UnityEngine;

/// <summary>Sweep the crosshair over floating markers. Shooting is allowed so the crosshair moves, but nothing to hit yet.</summary>
public sealed class AimStep : TutorialStep
{
    private static readonly Vector2[] Points = { new(0.3f, 0.62f), new(0.7f, 0.66f), new(0.5f, 0.42f) };
    private readonly List<TutorialTarget> markers = new();

    public override string Title => "AIM!";
    public override string Instruction => "Move the mouse and touch all 3 targets with your crosshair.";
    public override string Hint => "Slide the crosshair right onto the bullseye!";
    public override bool TeasesMisses => false;

    public override void Enter()
    {
        markers.Clear();
        foreach (var point in Points)
        {
            var marker = Director.SpawnTarget(point, shootable: false);
            if (marker != null) markers.Add(marker);
        }
    }

    public override bool Tick()
    {
        bool remaining = false;
        foreach (var marker in markers)
        {
            if (marker == null || marker.IsCleared) continue;
            if (Director.IsAimOver(marker))
            {
                marker.Touch();
                Director.Progress("PING!");
            }
            else remaining = true;
        }
        return !remaining;
    }
}

/// <summary>Shoot three still targets, then two swinging ones.</summary>
public sealed class ShootStep : TutorialStep
{
    private static readonly Vector2[] Still = { new(0.32f, 0.58f), new(0.5f, 0.66f), new(0.68f, 0.58f) };
    private static readonly Vector2[] Moving = { new(0.38f, 0.5f), new(0.62f, 0.68f) };
    private int round;

    public override string Title => "SHOOT!";
    public override string Instruction => "Left click to shoot. Smash every target!";
    public override string Hint => "Aim, then left click. Moving targets: wait for them to cross your crosshair.";

    public override void Enter()
    {
        round = 0;
        foreach (var point in Still) Director.SpawnTarget(point, shootable: true);
    }

    public override bool Tick()
    {
        // A limited starting weapon may run dry here; the slot lesson comes next anyway.
        if (Director.Shooting != null && !Director.Shooting.HasAmmo) return true;
        if (Director.ActiveTargetCount > 0) return false;
        if (round > 0) return true;
        round++;
        Director.Coach("Too easy? Now they MOVE!");
        foreach (var point in Moving) Director.SpawnTarget(point, shootable: true, new Vector2(0.12f, 0f), 2.4f);
        return false;
    }
}

/// <summary>Open the slot machine, spin it and take the weapon it gives.</summary>
public sealed class SlotStep : TutorialStep
{
    private int equippedAtStart;

    public override string Title => "SPIN!";
    public override string Instruction => "Press E to open the SLOT, then spin for a new weapon!";
    public override string Hint => "Press E! The slot only opens once its cooldown is over.";
    public override bool AllowSlot => true;
    public override bool TeasesMisses => false;

    public override void Enter() => equippedAtStart = Director.WeaponsEquipped;

    public override bool Tick()
    {
        if (Director.Slots == null) return true;
        return Director.WeaponsEquipped > equippedAtStart && !Director.Slots.IsOpen;
    }

    public override void Exit()
    {
        if (Director.Shooting != null && Director.Shooting.CurrentWeapon != null)
            Director.Coach($"{Director.Shooting.CurrentWeapon.name}! Ammo is limited. Run dry? Spin again.");
    }
}

/// <summary>Chain hits without missing until the style meter reaches rank B.</summary>
public sealed class StyleStep : TutorialStep
{
    private const int TargetRank = 1; // B
    private const int FallbackHits = 8;
    private static readonly Vector2[] Lanes = { new(0.3f, 0.55f), new(0.5f, 0.66f), new(0.7f, 0.55f), new(0.42f, 0.45f), new(0.58f, 0.45f) };
    private int lane;
    private int hitsAtStart;
    private int lastRank = -1;
    private bool warnedAmmo;

    public override string Title => "COMBO!";
    public override string Instruction => "Keep hitting without missing until rank B!";
    public override string Hint => "Misses drain the gauge, and so does waiting. Keep firing!";
    public override bool AllowSlot => true;

    public override void Enter()
    {
        lane = 0;
        lastRank = Director.Style != null ? Director.Style.RankIndex : -1;
        hitsAtStart = Director.HitCount;
        warnedAmmo = false;
    }

    public override bool Tick()
    {
        while (Director.ActiveTargetCount < 2)
        {
            Director.SpawnTarget(Lanes[lane], shootable: true, new Vector2(0.05f, 0.02f), 1.8f);
            lane = (lane + 1) % Lanes.Length;
        }
        if (Director.Shooting != null && !Director.Shooting.HasAmmo && !warnedAmmo)
        {
            warnedAmmo = true;
            Director.Coach("Out of ammo! Press E and spin again!");
        }
        if (Director.Style == null) return Director.HitCount - hitsAtStart >= FallbackHits;
        int rank = Director.Style.RankIndex;
        if (rank > lastRank && Director.Style.HasRank) Director.Progress(Director.Style.CurrentRank.label + "!");
        lastRank = rank;
        return rank >= TargetRank;
    }
}

/// <summary>A real enemy flies in and fires back; take it down.</summary>
public sealed class SurviveStep : TutorialStep
{
    private Enemy enemy;
    private bool defeated;
    private bool coachedDamage;

    public override string Title => "SURVIVE!";
    public override string Instruction => "A real enemy! Shoot it before it attacks you!";
    public override string Hint => "Aim at the robot and hold fire. Watch your HP in the corner!";
    public override bool AllowSlot => true;

    public override void Enter()
    {
        defeated = coachedDamage = false;
        enemy = Director.SpawnEnemy();
        if (enemy != null) enemy.Died += OnDied;
        Director.PlayerDamaged += OnPlayerDamaged;
    }

    public override bool Tick() => defeated || enemy == null;

    public override void Exit()
    {
        if (enemy != null) enemy.Died -= OnDied;
        Director.PlayerDamaged -= OnPlayerDamaged;
    }

    private void OnDied()
    {
        defeated = true;
        Director.Progress("KO!");
    }

    private void OnPlayerDamaged()
    {
        if (coachedDamage) return;
        coachedDamage = true;
        Director.Coach("OUCH! You lost HP. Take it down before it strikes again!");
    }
}

/// <summary>Final stamp before the real waves begin.</summary>
public sealed class FinishStep : TutorialStep
{
    private const float Duration = 1.6f;
    private float startedAt;

    public override string Title => "READY!";
    public override string Instruction => "You're ready. Showtime!";
    public override bool AllowSlot => true;
    public override bool TeasesMisses => false;

    public override void Enter()
    {
        startedAt = Time.unscaledTime;
        Director.Progress("READY!");
    }

    public override bool Tick() => Time.unscaledTime - startedAt >= Duration;
}
