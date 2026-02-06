using System;
using Xunit;
using UnityEngine;
using ErenshorGlider.Tests.GameStubs;
using ErenshorGlider.Tests.Helpers;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;

namespace ErenshorGlider.Tests.Navigation;

/// <summary>
/// Unit tests for Navigation using stub implementations.
/// These tests run without requiring the actual game assemblies.
/// </summary>
public class NavigationTests : IDisposable
{
    private readonly PlayerControl? _player;
    private readonly InputController _inputController;
    private readonly PositionTracker _positionTracker;
    private readonly Navigation _navigation;

    public NavigationTests()
    {
        // Set up fresh stubs for each test
        _player = TestStubsSetup.CreateMockPlayer();
        _inputController = new InputController();
        _positionTracker = new PositionTracker();
        _navigation = new Navigation(_inputController, _positionTracker);
    }

    public void Dispose()
    {
        // Clean up after each test
        TestStubsSetup.ClearStubs();
    }

    [Fact]
    public void FaceTarget_ShouldReturnFalse_WhenNoPosition()
    {
        // Arrange
        TestStubsSetup.ClearStubs(); // No player position available

        // Act
        var result = _navigation.FaceTarget(new PlayerPosition(10f, 0f, 10f));

        // Assert
        False(result, "Should return false when player position is unavailable");
    }

    [Fact]
    public void CalculateDistance_ShouldReturnCorrectDistance()
    {
        // Arrange
        var pos1 = new PlayerPosition(0f, 0f, 0f);
        var pos2 = new PlayerPosition(3f, 4f, 0f);

        // Act
        var distance = Navigation.CalculateDistance(pos1, pos2);

        // Assert
        Equal(5f, distance, 0.01f); // 3-4-5 triangle
    }

    [Fact]
    public void CalculateDistanceSquared_ShouldReturnCorrectSquaredDistance()
    {
        // Arrange
        var pos1 = new PlayerPosition(0f, 0f, 0f);
        var pos2 = new PlayerPosition(3f, 4f, 0f);

        // Act
        var distanceSquared = Navigation.CalculateDistanceSquared(pos1, pos2);

        // Assert
        Equal(25f, distanceSquared, 0.01f); // 5^2 = 25
    }

    [Fact]
    public void CalculateDirection_ShouldReturnForward_WhenTargetNorth()
    {
        // Arrange
        var from = new PlayerPosition(0f, 0f, 0f);
        var to = new PlayerPosition(0f, 0f, 10f); // North in game coords

        // Act
        var direction = Navigation.CalculateDirection(from, to);

        // Assert
        Equal(NavigationDirection.Forward, direction);
    }

    [Fact]
    public void CalculateDirection_ShouldReturnRight_WhenTargetEast()
    {
        // Arrange
        var from = new PlayerPosition(0f, 0f, 0f);
        var to = new PlayerPosition(10f, 0f, 0f); // East in game coords

        // Act
        var direction = Navigation.CalculateDirection(from, to);

        // Assert
        Equal(NavigationDirection.Right, direction);
    }

    [Fact]
    public void StoppingDistance_ShouldBeRespected()
    {
        // Arrange
        _navigation.StoppingDistance = 5f;
        var target = new PlayerPosition(3f, 0f, 0f);
        _player!.transform.position = new Vector3(0f, 0f, 0f);

        // Act
        var reached = _navigation.HasReached(in target);

        // Assert
        True(reached, "Should be considered reached when within stopping distance");
    }

    [Fact]
    public void HasReached_ShouldReturnFalse_WhenOutsideStoppingDistance()
    {
        // Arrange
        _navigation.StoppingDistance = 1f;
        var target = new PlayerPosition(10f, 0f, 0f);
        _player!.transform.position = new Vector3(0f, 0f, 0f);

        // Act
        var reached = _navigation.HasReached(in target);

        // Assert
        False(reached, "Should not be reached when outside stopping distance");
    }

    [Fact]
    public void GetDistanceTo_ShouldReturnMaxValue_WhenNoPosition()
    {
        // Arrange
        TestStubsSetup.ClearStubs();
        var target = new PlayerPosition(10f, 0f, 0f);

        // Act
        var distance = _navigation.GetDistanceTo(in target);

        // Assert
        Equal(float.MaxValue, distance);
    }

    [Fact]
    public void MoveTo_ShouldReturnFalse_WhenAlreadyAtTarget()
    {
        // Arrange
        _navigation.StoppingDistance = 2f;
        var target = new PlayerPosition(1f, 0f, 0f);
        _player!.transform.position = new Vector3(0f, 0f, 0f);

        // Act
        var started = _navigation.MoveTo(in target);

        // Assert
        False(started, "Should return false when already at target");
    }

    [Fact]
    public void MoveTo_ShouldReturnTrue_WhenNotAtTarget()
    {
        // Arrange
        _navigation.StoppingDistance = 1f;
        var target = new PlayerPosition(10f, 0f, 0f);
        _player!.transform.position = new Vector3(0f, 0f, 0f);

        // Act
        var started = _navigation.MoveTo(in target);

        // Assert
        True(started, "Should return true when not at target");
    }

    [Fact]
    public void IsFacing_ShouldReturnFalse_WhenNoPosition()
    {
        // Arrange
        TestStubsSetup.ClearStubs();
        var target = new PlayerPosition(10f, 0f, 0f);

        // Act
        var facing = _navigation.IsFacing(in target);

        // Assert
        False(facing, "Should return false when player position is unavailable");
    }
}
