using BrainFreeze.Code.Behaviors;
using BrainFreeze.Code.HarmonyPatches.DynamicRegistry;
using BrainFreeze.Code.Items;
using BrainFreeze.Code.Transition;
using BrainFreeze.Config;
using CustomTransitionLib;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BrainFreeze.Code
{
    public class BrainFreezeModSystem : ModSystem
    {
        public override double ExecuteOrder() => 0.11;

        private const string ConfigName = "BrainFreezeConfig.json";

        public static ModConfig Config { get; private set; }

        private Harmony harmony;

        //TODO if items are in barrel while liquid is at certain level and freezes, the slot should become locked
        //TODO maybe ensure mixing recipes can't be created with frozen variants

        //TODO maybe some code for removing no longer existing frozen variants from a world?

        //TODO spill over into other slots if going over slot stacksize

        //TODO snow liquid that will be collected when it is snowing instead of rainwater :p
        //TODO also why can't eat snow?

        public override void StartPre(ICoreAPI api) => LoadConfig(api);

        private static void LoadConfig(ICoreAPI api)
        {
            try
            {
                Config ??= api.LoadModConfig<ModConfig>(ConfigName) ?? new();
                api.StoreModConfig(Config, ConfigName);
            }
            catch (Exception ex)
            {
                api.Logger.Error(ex);
                api.Logger.Warning("Failed to load config, using default values instead");
                Config = new();
            }
        }

        #region HarmonyWorkAround
        private static ICoreAPI apiCache;

        public static IEnumerable<Assembly> ModAssembliesForHarmonyScan => apiCache.ModLoader.Mods.Select(mod => mod.Systems.FirstOrDefault())
            .Where(modSystem => modSystem != null)
            .Select(modSystem => modSystem.GetType().Assembly);

        public static IEnumerable<Type> ModTypesForHarmonyScan => ModAssembliesForHarmonyScan.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                try
                {
                    apiCache.Logger.Warning($"Could not get types from assembly '{assembly.FullName}', WearAndTear Harmony Patches might not have applied propperly for this mod");
                }
                catch { }
                return Enumerable.Empty<Type>();
            }
        });
        #endregion HarmonyWorkAround

        public override void Start(ICoreAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                apiCache = api;
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAllUncategorized();

                if (api.ModLoader.IsModEnabled("hydrateordiedrate"))
                {
                    harmony.PatchCategory("hydrateordiedrate");
                }
                apiCache = null;
            }

            var registry = api.ModLoader.GetModSystem<CustomTransitionLibModSystem>();
            registry.Register<BrainFreezeTransitionHandler, EnumBrainFreezeTransitionType>();

            api.RegisterItemClass("brainfreeze:Ice", typeof(Ice));

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
            Config = null;
            base.Dispose();
        }
    }
}