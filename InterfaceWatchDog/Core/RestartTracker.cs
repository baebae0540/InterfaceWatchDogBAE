namespace InterfaceWatchDog.Core;

internal class RestartTracker
{
    public int ConsecutiveFailures { get; private set; }
    private DateTime _cooldownUntil = DateTime.MinValue;

    public bool IsInCooldown(int cooldownSeconds)
        => DateTime.Now < _cooldownUntil;

    public void IncrementFailure() => ConsecutiveFailures++;
    public void ResetFailures()    => ConsecutiveFailures = 0;

    // cooldownSeconds를 실제로 적용 (기존: 항상 30초 하드코딩)
    public void SetCooldown(int cooldownSeconds = 30)
        => _cooldownUntil = DateTime.Now.AddSeconds(cooldownSeconds);
}
