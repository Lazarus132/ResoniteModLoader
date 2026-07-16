using System.Reflection;
using HarmonyLib;

namespace ResoniteModLoader;

// this class does all the harmony-related RML work.
// this is needed to avoid importing harmony in ExecutionHook,
// where it may not be loaded yet.
internal sealed class HarmonyWorker
{
    internal static void Init()
    {
        Harmony harmony =
            new("com.resonitemodloader.ResoniteModLoader");

        Assembly? dataFeedPatchAssembly =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "ModDataFeedGridPatch",
                            StringComparison.Ordinal));

        if (dataFeedPatchAssembly != null)
        {
            harmony.PatchAll(
                dataFeedPatchAssembly);

            Logger.MsgInternal(
                "Loaded DataFeed grid patches.");
        }
        else
        {
            Logger.WarnInternal(
                "ResoniteModLoader.DataFeedPatches.dll was not loaded.");
        }

        ModLoader.LoadMods();

        ModConfiguration.RegisterShutdownHook(
            harmony);
    }
}