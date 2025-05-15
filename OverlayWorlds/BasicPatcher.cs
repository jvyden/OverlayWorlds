using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader.Resonite;
using System.Reflection;
using System.Reflection.Emit;

namespace OverlayWorlds;

[HarmonyPatchCategory(nameof(BasicPatcher))]
internal class BasicPatcher : ResoniteMonkey<BasicPatcher>
{
    // protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
    // {
        // yield return new FeaturePatch<MonkeyLoader.Resonite.Features.FrooxEngine.Button>(PatchCompatibility.HookOnly);
    // }

    private static readonly MethodInfo _isUserspace = AccessTools.Method(typeof(WorldExtensions), nameof(WorldExtensions.IsUserspace));
    private static readonly MethodInfo _isDimension = AccessTools.Method(typeof(DimensionManager), nameof(DimensionManager.IsDimension));
    private static readonly MethodInfo _getWorld = AccessTools.Method(typeof(Worker), "get_World");

    [HarmonyPatch(typeof(ScreenController), nameof(ScreenController.OnAttach))]
    [HarmonyTranspiler]
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

    // I'd love to patch OnStart here but that doesn't exist on Button
    [HarmonyPatch(typeof(Button), nameof(Button.OnAwake))]
    [HarmonyPostfix]
    public static void ButtonOnAwakePostfix(Button __instance)
    {
        #if !DEBUG
        if (!__instance.World.IsUserspace())
            return;
        #endif

        __instance.World.RunInUpdates(1, () => SetupButton(__instance));
    }

    private static void SetupButton(Button button)
    {
        // ReSharper disable once VariableCanBeNotNullable
        Comment? comment = button.Slot.GetComponent<Comment>();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (comment == null)
            return;

        const string Prefix = "PocketDimension.";

        if (!comment.Text.Value.StartsWith(Prefix))
            return;

        string method = comment.Text.Value[Prefix.Length..];
        
        if(DimensionManager.RegisterButton(button, method))
            UniLog.Log($"PocketDimension successfully registered {method} button");
    }
}