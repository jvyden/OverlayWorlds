using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using FrooxEngine.UIX;
using SkyFrost.Base;

namespace OverlayWorlds;

/// <summary>
/// Management utility for a pocket dimension.
/// </summary>
public static class DimensionManager
{
    private static World? _world;
    
    private static void SetupWorld(World world)
    {
        // Slot ground = world.AddSlot("Ground");
        // ground.AttachCylinder<PBS_Metallic>(15f, 0.2f).Sides.Value = 128;
        // ground.GetComponent<CylinderCollider>().SetCharacterCollider();
        // ground.GlobalPosition = float3.Down * 0.1f;
        
        CommonAvatarBuilder builder = world.AddSlot("Avatar Builder").AttachComponent<CommonAvatarBuilder>();
        builder.SetupServerVoice.Value = false;
        builder.SetupClientVoice.Value = false;
        builder.FillEmptySlots.Value = false;
        
        builder.SetupLocomotion.Value = true;
        builder.AllowLocomotion.Value = false;
        
        Slot slot = world.AddSlot("UserRoot", false);
        UserRoot userRoot = slot.AttachComponent<UserRoot>();
        world.LocalUser.Root = userRoot;
        
        builder.BuildDevices(world.LocalUser, userRoot, slot, out Slot _, out List<InteractionHandler> interactions);
        foreach (InteractionHandler handler in interactions)
        {
            UserspacePointer pointer = world.AddSlot($"{handler.Side} {nameof(UserspacePointer)}", false).AttachComponent<UserspacePointer>();
            handler.Equip(pointer, true);
            pointer.Slot.SetIdentityTransform();
            handler.EquippingEnabled.Value = false;
            handler.UserScalingEnabled.Value = false;
            handler.VisualEnabled.Value = false;
        }

        AvatarManager manager = slot.AttachComponent<AvatarManager>();
        manager.AutoAddNameBadge.Value = false;
        manager.AutoAddIconBadge.Value = false;
        manager.AutoAddLiveIndicator.Value = false;
        manager.EmptySlotHandler.Target = null;
        manager.FillEmptySlots();
    }

    /// <summary>
    /// Starts the pocket dimension.
    /// </summary>
    /// <remarks>Does nothing if the world is already running.</remarks>
    public static async Task StartDimensionAsync()
    {
        if (_world != null && !_world.IsDisposed)
            return;
        
        World world = await Userspace.OpenWorld(new WorldStartSettings
        {
            InitWorld = SetupWorld,
            AutoFocus = false,
            HideFromListing = true,
            GetExisting = true,
            DefaultAccessLevel = SessionAccessLevel.Private,
            FetchedWorldName = "Pocket Dimension",
            CreateLoadIndicator = false,
        });
        
        // world.Focus = World.WorldFocus.Focused;
        world.Name = "Pocket Dimension";

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(() => world.Coroutines.StartTask(async w =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        {
            await new NextUpdate();
            w.RunSynchronously(() =>
            {
                Slot? inspector = w.RootSlot.OpenInspectorForTarget();
                if (inspector == null)
                    return;

                inspector.PositionInFrontOfUser(float3.Backward, float3.Forward * 2);
            });
        }, world));

        Engine.Current.WorldManager.OverlayWorld(world);

        _world = world;
    }
    
    public static bool IsDimension(World w) => _world == w;

    /// <summary>
    /// Stops the pocket dimension.
    /// </summary>
    /// <remarks>Does nothing if the world is already stopped.</remarks>
    public static async Task StopDimensionAsync()
    {
        if (_world == null)
            return;

        if (_world.IsDisposed)
        {
            _world = null;
            return;
        }

        await Userspace.ExitWorld(_world);
    }

    public static bool RegisterButton(Button button, string method)
    {
        switch (method)
        {
            case "Start":
                button.LocalPressed += (_, _) => Task.Run(StartDimensionAsync);
                break;
            case "Stop":
                button.LocalPressed += (_, _) => Task.Run(StopDimensionAsync);
                break;
            default:
                UniLog.Warning("Unknown dimension button method " + method);
                return false;
        }

        return true;
    }
}