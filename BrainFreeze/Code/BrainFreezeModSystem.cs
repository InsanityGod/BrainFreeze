using BrainFreeze.Code.Behaviors;
using BrainFreeze.Code.HarmonyPatches.DynamicRegistry;
using BrainFreeze.Code.Items;
using BrainFreeze.Code.Transition;
using BrainFreeze.Config;
using CustomTransitionLib;
using HarmonyLib;
using System;
using Vintagestory.API.Common;

namespace BrainFreeze.Code
{
    public class BrainFreezeModSystem : ModSystem
    {
        private const string ConfigName = "BrainFreezeConfig.json";

        public static ModConfig Config { get; private set; }

        private Harmony harmony;

        public override void StartPre(ICoreAPI api) => LoadConfig(api);

        private static void LoadConfig(ICoreAPI api)
        {
            try
            {
                Config = api.LoadModConfig<ModConfig>(ConfigName) ?? new();
                api.StoreModConfig(Config, ConfigName);
            }
            catch (Exception ex)
            {
                api.Logger.Error(ex);
                api.Logger.Warning("Failed to load config, using default values instead");
                Config = new();
            }
        }

        public override void Start(ICoreAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAllUncategorized();

                if (api.ModLoader.IsModEnabled("hydrateordiedrate"))
                {
                    harmony.PatchCategory("hydrateordiedrate");
                }
            }

            var registry = api.ModLoader.GetModSystem<CustomTransitionLibModSystem>();
            registry.Register<BrainFreezeTransitionHandler, EnumBrainFreezeTransitionType>();

            api.RegisterItemClass("brainfreeze:IceCube", typeof(IceCube));

            api.RegisterCollectibleBehaviorClass("brainfreeze:icebreakertool", typeof(IceBreakerTool));
            api.RegisterCollectibleBehaviorClass("brainfreeze:frozennameprefix", typeof(FrozenNamePrefix));
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if(api.Side == EnumAppSide.Client) return;

            foreach (var item in api.World.Items)
            {
                if (item.Variant["brainfreeze"] != null)
                {
                    DynamicFrozenVariant.FinalizeFrozenCollectible(api, item);
                }
            }

            DynamicFrozenVariant.FinalizeIceCube(api);

            base.AssetsFinalize(api);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            base.Dispose();
        }

        //TODO BlockLiquidContainerTopOpened.CanDrinkFrom (for auto added stuff)
        //TODO slush
        //TODO barrels?
        //TODO make it so you can't get it out of bottles if it's ice :p
    }
}