using System;
using Xunit;
using UnityEngine;
using ErenshorGlider.Tests.GameStubs;
using ErenshorGlider.Tests.Helpers;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Tests.GameState;

/// <summary>
/// Unit tests for PositionTracker using stub implementations.
/// These tests run without requiring the actual game assemblies.
/// </summary>
public class PositionTrackerTests : IDisposable
{
    private PlayerControl? _player;

    public PositionTrackerTests()
    {
        // Set up fresh stubs for each test
        _player = TestStubsSetup.CreateMockPlayer();
    }

    public void Dispose()
    {
        // Clean up after each test
        TestStubsSetup.ClearStubs();
    }

    [Fact]
    public void GameStateReader_ShouldReturnPlayerPosition()
    {
        // Arrange
        var expectedPosition = new Vector3(100f, 0f, 200f);
        _player!.transform.position = expectedPosition;

        var reader = new GameStateReader();

        // Act
        var position = reader.GetPlayerPosition();

        // Assert
        Assert.NotNull(position);
        Assert.Equal(100f, position.Value.X);
        Assert.Equal(0f, position.Value.Y);
        Assert.Equal(200f, position.Value.Z);
    }

    [Fact]
    public void GameStateReader_ShouldReturnNull_WhenPlayerNotAvailable()
    {
        // Arrange
        TestStubsSetup.ClearStubs();
        var reader = new GameStateReader();

        // Act
        var position = reader.GetPlayerPosition();

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void GameStateReader_ShouldReturnPlayerVitals()
    {
        // Arrange
        var character = _player!.Myself!;
        character.MyStats!.CurrentHP = 75f;
        character.MyStats!.MaxHP = 100f;
        character.MyStats!.CurrentMP = 40f;
        character.MyStats!.MaxMP = 50f;
        character.MyStats!.Level = 5;

        var reader = new GameStateReader();

        // Act
        var vitals = reader.GetPlayerVitals();

        // Assert
        Assert.NotNull(vitals);
        Assert.Equal(75f, vitals.Value.CurrentHealth);
        Assert.Equal(100f, vitals.Value.MaxHealth);
        Assert.Equal(40f, vitals.Value.CurrentMana);
        Assert.Equal(50f, vitals.Value.MaxMana);
        Assert.Equal(5, vitals.Value.Level);
    }

    [Fact]
    public void GameStateReader_ShouldReturnCombatState()
    {
        // Arrange
        var combat = TestStubsSetup.CreateMockPlayerCombat();
        combat.InCombat = true;

        var reader = new GameStateReader();

        // Act
        var combatState = reader.GetCombatState();

        // Assert
        Assert.NotNull(combatState);
        Assert.True(combatState.Value.InCombat);
        Assert.True(combatState.Value.IsAlive); // Player is alive by default
    }

    [Fact]
    public void GameStateReader_ShouldDetectDeadPlayer()
    {
        // Arrange
        _player!.Myself!.Dead = true;

        var reader = new GameStateReader();

        // Act
        var combatState = reader.GetCombatState();

        // Assert
        Assert.NotNull(combatState);
        Assert.False(combatState.Value.IsAlive);
    }

    [Fact]
    public void GameStateReader_ShouldReturnTargetInfo()
    {
        // Arrange
        var target = TestStubsSetup.CreateMockTarget("Goblin", 10f, 0f, 15f, level: 3);
        target.MyStats!.CurrentHP = 35f;
        target.MyStats!.MaxHP = 50f;
        TestStubsSetup.SetPlayerTarget(target);

        var reader = new GameStateReader();

        // Act
        var targetInfo = reader.GetTargetInfo();

        // Assert
        Assert.True(targetInfo.HasTarget);
        Assert.Equal("Goblin", targetInfo.Name);
        Assert.Equal(3, targetInfo.Level);
        Assert.Equal(35f, targetInfo.CurrentHealth);
        Assert.Equal(50f, targetInfo.MaxHealth);
        Assert.Equal(TargetHostility.Hostile, targetInfo.Hostility);
        Assert.False(targetInfo.IsDead);
    }

    [Fact]
    public void GameStateReader_ShouldReturnNoTarget_WhenNoTargetSet()
    {
        // Arrange
        TestStubsSetup.SetPlayerTarget(null);
        var reader = new GameStateReader();

        // Act
        var targetInfo = reader.GetTargetInfo();

        // Assert
        Assert.False(targetInfo.HasTarget);
    }

    [Fact]
    public void PlayerPosition_ShouldCalculateDistance()
    {
        // Arrange
        var pos1 = new PlayerPosition(0f, 0f, 0f);
        var pos2 = new PlayerPosition(3f, 4f, 0f);

        // Act
        var distance = pos1.DistanceTo(pos2);

        // Assert
        // Distance should be 5 (3-4-5 triangle)
        Assert.Equal(5f, distance, 0.01f);
    }

    [Fact]
    public void PlayerPosition_ShouldCalculateHorizontalDistance()
    {
        // Arrange
        var pos1 = new PlayerPosition(0f, 0f, 0f);
        var pos2 = new PlayerPosition(3f, 10f, 4f); // Height should be ignored

        // Act
        var distance = pos1.HorizontalDistanceTo(pos2);

        // Assert
        // Horizontal distance should be 5 (3-4-5 triangle on XZ plane)
        Assert.Equal(5f, distance, 0.01f);
    }

    [Fact]
    public void PlayerPosition_ShouldConvertToVector3()
    {
        // Arrange
        var position = new PlayerPosition(10f, 20f, 30f);

        // Act
        var vector3 = position.ToVector3();

        // Assert
        Assert.Equal(10f, vector3.x);
        Assert.Equal(20f, vector3.y);
        Assert.Equal(30f, vector3.z);
    }

    [Fact]
    public void PlayerVitals_ShouldCalculateHealthPercent()
    {
        // Arrange
        var vitals = new PlayerVitals(50f, 100f, 30f, 50f, 1, 0f, 1000f);

        // Act
        var healthPercent = vitals.HealthPercent;

        // Assert
        Assert.Equal(50f, healthPercent);
    }

    [Fact]
    public void PlayerVitals_ShouldCalculateManaPercent()
    {
        // Arrange
        var vitals = new PlayerVitals(50f, 100f, 25f, 50f, 1, 0f, 1000f);

        // Act
        var manaPercent = vitals.ManaPercent;

        // Assert
        Assert.Equal(50f, manaPercent);
    }

    [Fact]
    public void CombatState_ShouldIndicateCanAct()
    {
        // Arrange
        var character = _player!.Myself!;
        character.Dead = false;
        _player.PlayerSpells!.Casting = false;

        var reader = new GameStateReader();

        // Act
        var combatState = reader.GetCombatState();

        // Assert
        Assert.NotNull(combatState);
        Assert.True(combatState.Value.CanAct);
    }

    [Fact]
    public void CombatState_ShouldIndicateCannotAct_WhenCasting()
    {
        // Arrange
        _player!.PlayerSpells!.Casting = true;

        var reader = new GameStateReader();

        // Act
        var combatState = reader.GetCombatState();

        // Assert
        Assert.NotNull(combatState);
        Assert.False(combatState.Value.CanAct);
    }

    [Fact]
    public void CombatState_ShouldIndicateCannotAct_WhenDead()
    {
        // Arrange
        _player!.Myself!.Dead = true;
        _player.PlayerSpells!.Casting = false;

        var reader = new GameStateReader();

        // Act
        var combatState = reader.GetCombatState();

        // Assert
        Assert.NotNull(combatState);
        Assert.False(combatState.Value.CanAct);
    }
}
