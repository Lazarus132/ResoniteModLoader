using FrooxEngine;

namespace ResoniteModLoader;

public sealed class ModDataFeedGrid
    : DataFeedGroup
{
    public float CardWidth { get; init; } =
        384f;

    public float CardHeight { get; init; } =
        128f;

    public float Spacing { get; init; } =
        8f;
}