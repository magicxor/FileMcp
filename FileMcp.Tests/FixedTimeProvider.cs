namespace FileMcp.Tests;

/// <summary>
/// A minimal <see cref="TimeProvider"/> that always returns a fixed "now", so age-based
/// behavior can be tested deterministically without touching the wall clock.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}
