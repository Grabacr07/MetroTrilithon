using System;
using System.Runtime.CompilerServices;
using Amethystra.Diagnostics;

namespace Amethystra.Disposables;

/// <summary>
/// Represents a boolean flag that is set to true for the lifetime of an <see cref="IDisposable"/>,
/// supports nested/enclosed scopes, and automatically resets to false when all scopes are disposed.
/// Thread-safe.
/// </summary>
public sealed class ScopedFlag : IEquatable<ScopedFlag>
{
    private readonly RefCountGate _gate = new();

    /// <summary>
    /// Returns true if <see cref="Enable"/> has been called more times than <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public bool Value
        => this._gate.IsActive;

    /// <summary>
    /// Enables the flag (sets to true) and returns an <see cref="IDisposable"/>.
    /// When disposed, it decrements the counter and resets to false if it reaches zero.
    /// </summary>
    public IDisposable Enable([CallerMemberName] string member = "", [CallerFilePath] string file = "")
        => this._gate.Acquire(Caller.GetCallerLabel(member, file));

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
