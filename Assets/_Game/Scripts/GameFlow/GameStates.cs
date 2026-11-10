using UnityEngine;

/// <summary>Waits until the scene transition has fully revealed the scene, then a short beat.</summary>
public sealed class IntroState : GameState
{
    private float elapsed;

    public override void Enter()
    {
        elapsed = 0f;
        Manager.SetPlayerControl(shooting: false, manualSlot: false);
    }

    public override void Tick()
    {
        if (SceneTransition.IsTransitioning) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Manager.IntroDelay) Manager.ChangeState<WeaponSelectState>();
    }
}

/// <summary>Opens the slot machine for the first weapon and waits for it to close.</summary>
public sealed class WeaponSelectState : GameState
{
    public override void Enter()
    {
        if (Manager.Slots != null) Manager.Slots.Open();
    }

    public override void Tick()
    {
        if (Manager.Slots == null || !Manager.Slots.IsOpen) Manager.ChangeState<CombatState>();
    }
}

/// <summary>Regular waves. The slot can be reopened by the player once its cooldown ends.</summary>
public sealed class CombatState : GameState
{
    public override void Enter()
    {
        Manager.SetPlayerControl(shooting: true, manualSlot: true);
        if (Manager.Spawner != null) Manager.Spawner.Begin();
    }
}

/// <summary>Boss fight; the manager moves to victory when the boss is defeated.</summary>
public sealed class BossState : GameState { }

public sealed class VictoryState : GameState
{
    public override bool IsTerminal => true;
    public override void Enter() => Manager.SetPlayerControl(shooting: false, manualSlot: false);
}

public sealed class GameOverState : GameState
{
    public override bool IsTerminal => true;
    public override void Enter() => Manager.SetPlayerControl(shooting: false, manualSlot: false);
}
