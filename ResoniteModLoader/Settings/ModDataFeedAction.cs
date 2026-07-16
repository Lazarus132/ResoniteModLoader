using FrooxEngine;
using Elements.Core;

namespace ResoniteModLoader;

public sealed class ModDataFeedAction
    : DataFeedValueAction<string>
{
    public float TitleSize { get; init; } = 26f;
}