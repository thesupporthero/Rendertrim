using System;
using Dalamud.Memory;

namespace RenderTrim.Util;

internal sealed class MemoryReplacement : IDisposable
{
    private readonly nint _address;
    private readonly byte[] _replacement;
    private byte[]? _original;

    public bool IsApplied => _original is not null;

    public MemoryReplacement(nint address, byte[] replacement)
    {
        _address = address;
        _replacement = replacement;
    }

    public void Enable()
    {
        if (_original is not null) return;
        _original = ReplaceRaw(_address, _replacement);
    }

    public void Disable()
    {
        if (_original is null) return;
        ReplaceRaw(_address, _original);
        _original = null;
    }

    public void Dispose() => Disable();

    private static byte[] ReplaceRaw(nint address, byte[] data)
    {
        var existing = MemoryHelper.ReadRaw(address, data.Length);
        MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite, out var prev);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, prev);
        return existing;
    }
}
