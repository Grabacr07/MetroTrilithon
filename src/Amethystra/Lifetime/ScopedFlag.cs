using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Amethystra.Lifetime;

/// <summary>
/// Represents a boolean flag that is set to true for the lifetime of an <see cref="IDisposable"/>,
/// supports nested/enclosed scopes, and automatically resets to false when all scopes are disposed.
/// Thread-safe.
/// </summary>
public sealed class ScopedFlag : IEquatable<ScopedFlag>
{
    // スコープの参照カウント
    private int _count;

    /// <summary>
    /// Returns true if <see cref="Enable"/> has been called more times than <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public bool Value
        => Volatile.Read(ref this._count) > 0;

    /// <summary>
    /// Enables the flag (sets to true) and returns an <see cref="IDisposable"/>.
    /// When disposed, it decrements the counter and resets to false if it reaches zero.
    /// </summary>
    public IDisposable Enable()
    {
        Interlocked.Increment(ref this._count);
        return Disposable.Create(() => Interlocked.Decrement(ref this._count));
    }

    public bool Equals(ScopedFlag? other)
        => this.Value == other?.Value;

    public override bool Equals(object? obj)
        => obj is ScopedFlag other && this.Equals(other);

    public override int GetHashCode()
        => this.Value.GetHashCode();

    public static bool operator ==(ScopedFlag left, bool right)
        => left.Value == right;

    public static bool operator !=(ScopedFlag left, bool right)
        => left.Value != right;

    public static bool operator ==(bool left, ScopedFlag right)
        => left == right.Value;

    public static bool operator !=(bool left, ScopedFlag right)
        => left != right.Value;

    public static implicit operator bool(ScopedFlag flag)
        => flag.Value;
}
