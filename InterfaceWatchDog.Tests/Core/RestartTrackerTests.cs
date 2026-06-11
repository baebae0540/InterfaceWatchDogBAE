using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.Tests.Core;

public class RestartTrackerTests
{
    [Fact]
    public void InitialState_ConsecutiveFailuresShouldBeZero()
    {
        var tracker = new RestartTracker();
        tracker.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void InitialState_ShouldNotBeInCooldown()
    {
        var tracker = new RestartTracker();
        tracker.IsInCooldown(60).Should().BeFalse();
    }

    [Fact]
    public void IncrementFailure_ShouldIncreaseCount()
    {
        var tracker = new RestartTracker();
        tracker.IncrementFailure();
        tracker.IncrementFailure();
        tracker.ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void ResetFailures_ShouldSetCountToZero()
    {
        var tracker = new RestartTracker();
        tracker.IncrementFailure();
        tracker.IncrementFailure();
        tracker.ResetFailures();
        tracker.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void SetCooldown_WithPositiveSeconds_ShouldActivateCooldown()
    {
        var tracker = new RestartTracker();
        tracker.SetCooldown(60);
        tracker.IsInCooldown(60).Should().BeTrue();
    }

    // 회귀 테스트: 기존 버그 — SetCooldown이 항상 30초 하드코딩되어
    // 0초 쿨다운 설정 시에도 즉시 해제되지 않았던 문제
    [Fact]
    public void SetCooldown_WithZeroSeconds_ShouldExpireImmediately()
    {
        var tracker = new RestartTracker();
        tracker.SetCooldown(0);
        tracker.IsInCooldown(0).Should().BeFalse();
    }

    [Fact]
    public void SetCooldown_DefaultValue_ShouldUse30Seconds()
    {
        var tracker = new RestartTracker();
        tracker.SetCooldown();  // 기본값 30초
        tracker.IsInCooldown(30).Should().BeTrue();
    }

    [Fact]
    public void IsInCooldown_AfterReset_ShouldBeFalseAgain()
    {
        var tracker = new RestartTracker();
        tracker.SetCooldown(60);
        tracker.IsInCooldown(60).Should().BeTrue();

        tracker.ResetFailures();  // 실패 카운트 초기화 (쿨다운은 별도)
        tracker.IsInCooldown(60).Should().BeTrue();  // 쿨다운은 여전히 활성
    }
}
