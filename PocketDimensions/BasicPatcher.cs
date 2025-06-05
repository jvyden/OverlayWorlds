using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader.Resonite;
using System.Reflection;
using System.Reflection.Emit;

namespace PocketDimensions;

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
    private static readonly MethodInfo _getUserspaceWorld = AccessTools.Method(typeof(Userspace), "get_UserspaceWorld");

    [HarmonyPatch(typeof(ScreenController), nameof(ScreenController.OnAttach))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> ScreenControllerOnAttach(IEnumerable<CodeInstruction> instructions)
        => ExtensionAlsoCheckForDimensionTranspiler(instructions);
    
    [HarmonyPatch(typeof(PointerInteractionController), nameof(PointerInteractionController.OnAttach))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> PointerInteractionControllerOnAttach(IEnumerable<CodeInstruction> instructions)
        => ExtensionAlsoCheckForDimensionTranspiler(instructions);
    
    [HarmonyPatch(typeof(PointerInteractionController), nameof(PointerInteractionController.UpdatePointer))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> PointerInteractionControllerUpdatePointer(IEnumerable<CodeInstruction> instructions)
        => ExtensionAlsoCheckForDimensionTranspiler(instructions);
    
    
    [HarmonyPatch(typeof(OverlayLayer), nameof(OverlayLayer.CheckCanUse))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> OverlayLayerCheckCanUse(IEnumerable<CodeInstruction> instructions)
        => EqualityAlsoCheckForDimensionTranspiler(instructions);
    
    [HarmonyPatch(typeof(PointerInteractionController), nameof(PointerInteractionController.GetTouchable))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> PointerInteractionControllerGetTouchable(IEnumerable<CodeInstruction> instructions)
        => EqualityAlsoCheckForDimensionTranspiler(instructions);
    
    [HarmonyPatch(typeof(PointerInteractionController), nameof(PointerInteractionController.BeforeInputUpdate))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> PointerInteractionControllerBeforeInputUpdate(IEnumerable<CodeInstruction> instructions)
        => EqualityAlsoCheckForDimensionTranspiler(instructions);

    /// <summary>
    /// When a method is checking if a world is userspace, also check if the world is a dimension
    /// </summary>
    private static IEnumerable<CodeInstruction> ExtensionAlsoCheckForDimensionTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();

        for (int i = 0; i < code.Count; i++)
        {
            CodeInstruction instruction = code[i];

            if (instruction.Calls(_isUserspace))
            {
                // _isUserspace() || _isDimension()
                
                yield return instruction;                                                     // IsUserspace()
                yield return new CodeInstruction(OpCodes.Ldarg_0);                    // this.
                yield return new CodeInstruction(OpCodes.Call, _getWorld);    // World.
                yield return new CodeInstruction(OpCodes.Call, _isDimension); // IsDimension()
                yield return new CodeInstruction(OpCodes.Or);                         // ||
                continue;
            }
            
            yield return instruction;
        }
    }
    
    /// <summary>
    /// When a method is checking if a world is userspace, also check if the world is a dimension
    /// </summary>
    private static IEnumerable<CodeInstruction> EqualityAlsoCheckForDimensionTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();

        for (int i = 0; i < code.Count; i++)
        {
            CodeInstruction instruction = code[i];

            if (instruction.opcode == OpCodes.Beq_S && code[i - 1].Calls(_getUserspaceWorld))
            {
                // this.World == Userspace.UserspaceWorld || this.World.IsDimension()
                
                yield return new CodeInstruction(OpCodes.Ceq);                        // ==
                yield return new CodeInstruction(OpCodes.Ldarg_0);                    // this.
                yield return new CodeInstruction(OpCodes.Call, _getWorld);    // World.
                yield return new CodeInstruction(OpCodes.Call, _isDimension); // IsDimension()
                yield return new CodeInstruction(OpCodes.Or);                         // ||
                yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
                continue;
            }
            
            yield return instruction;
        }
    }
    
    /// <summary>
    /// When a world is checking if we're userspace, also check if the world is a dimension
    /// </summary>
    [HarmonyPatch(typeof(World), nameof(World.RunWorldEvents))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> WorldAlsoCheckForDimensionTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();

        for (int i = 0; i < code.Count; i++)
        {
            CodeInstruction instruction = code[i];

            if (instruction.Calls(_isUserspace))
            {
                // _isUserspace() || _isDimension()
                
                yield return instruction;                                                     // IsUserspace()
                yield return new CodeInstruction(OpCodes.Ldarg_0);                    // this.
                yield return new CodeInstruction(OpCodes.Call, _isDimension); // IsDimension()
                yield return new CodeInstruction(OpCodes.Or);                         // ||
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

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (!(comment?.Text?.Value?.StartsWith(Prefix) ?? false))
            return;

        string method = comment.Text.Value[Prefix.Length..];
        
        if(DimensionManager.RegisterButton(button, method))
            UniLog.Log($"PocketDimension successfully registered {method} button");
    }
}