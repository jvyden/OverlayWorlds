using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.Features.FrooxEngine;
using SkyFrost.Base;

namespace OverlayWorlds
{
    [HarmonyPatchCategory(nameof(BasicPatcher))]
    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnAttach))]
    internal class BasicPatcher : ResoniteMonkey<BasicPatcher>
    {
        // The options for these should be provided by your game's game pack.
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches()
        {
            yield return new FeaturePatch<ProtofluxTool>(PatchCompatibility.HookOnly);
        }

        private static void SetupWorld(World w)
        {
            Slot ground = w.AddSlot("Ground");
            ground.AttachCylinder<PBS_Metallic>(15f, 0.2f).Sides.Value = 128;
            ground.GetComponent<CylinderCollider>().SetCharacterCollider();
            ground.GlobalPosition = float3.Down * 0.1f;
        }

        private static void Postfix()
        {
            Logger.Info(() => "Postfix for ProtoFluxTool.OnAttach()!");
            Task.Run(async () =>
            {
                World world = await Userspace.OpenWorld(new WorldStartSettings
                {
                    InitWorld = SetupWorld,
                    AutoFocus = false,
                    HideFromListing = true,
                    GetExisting = false,
                    DefaultAccessLevel = SessionAccessLevel.Private,
                });
                world.Focus = World.WorldFocus.Overlay;
            });
        }
    }
}