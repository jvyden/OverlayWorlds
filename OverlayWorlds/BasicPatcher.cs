using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace OverlayWorlds;

[HarmonyPatchCategory(nameof(BasicPatcher))]
internal class BasicPatcher : ResoniteMonkey<BasicPatcher>
{
    // The options for these should be provided by your game's game pack.
    protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
    {
        yield return new FeaturePatch<ProtofluxTool>(PatchCompatibility.HookOnly);
    }

    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnAttach))]
    [HarmonyPostfix]
    private static void ProtoFluxToolOnAttachPostfix()
    {
        // TODO: replace with dynamic impulse-based starting from userspace
        Logger.Info(() => "Postfix for ProtoFluxTool.OnAttach()!");
        Task.Run(DimensionManager.StartDimensionAsync);
    }

    private static readonly MethodInfo _isUserspace = AccessTools.Method(typeof(WorldExtensions), nameof(WorldExtensions.IsUserspace));
    private static readonly MethodInfo _isDimension = AccessTools.Method(typeof(DimensionManager), nameof(DimensionManager.IsDimension));
    private static readonly MethodInfo _getWorld = AccessTools.Method(typeof(Worker), "get_World");

    [HarmonyPatch(typeof(ScreenController), nameof(ScreenController.OnAttach))]
    [HarmonyTranspiler, HarmonyDebug]
    private static IEnumerable<CodeInstruction> ScreenControllerOnAttachTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();

        for (int i = 0; i < code.Count; i++)
        {
            CodeInstruction instruction = code[i];

            if (instruction.Calls(_isUserspace))
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, _getWorld);
                yield return new CodeInstruction(OpCodes.Call, _isDimension);
                yield return new CodeInstruction(OpCodes.Or);
                continue;
            }
            
            yield return instruction;
        }
    }
}