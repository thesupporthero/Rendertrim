using System.Collections.Generic;
using Dalamud.Configuration;

namespace RenderTrim;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public Dictionary<string, bool> EnabledTrims { get; set; } = new();

    public bool ShowDebugWindowOnLoad { get; set; } = false;

    public bool RestoreOnLoad { get; set; } = false;

    public RenderSkipMode RenderSkipMode { get; set; } = RenderSkipMode.BytePatch;

    public void Save() => DalamudApi.PluginInterface.SavePluginConfig(this);
}

public enum RenderSkipMode
{
    BytePatch = 0,
    DirectFieldWrite = 1,
}
