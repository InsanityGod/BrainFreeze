using BrainFreeze.Behaviors;
using BrainFreeze.Config;
using BrainFreeze.HarmonyPatches;
using CustomTransitionLib;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BrainFreeze
{
    public class BrainFreezeModSystem : ModSystem
    {


        private const string ConfigName = "BrainFreezeConfig.json";

        public static ModConfig Config { get; private set; }

        private Harmony harmony;

        public BrainFreezeModSystem()
        {
        }

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
                harmony.PatchAll();
            }
            var registry = api.ModLoader.GetModSystem<CustomTransitionLibModSystem>();
            registry.Register(new BrainFreezeTransitionHandler());

            api.RegisterCollectibleBehaviorClass("brainfreeze:icebreakertool", typeof(IceBreakerTool));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //TODO ice freezing
            base.StartServerSide(api);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            base.Dispose();
        }

        public override void AssetsFinalize(ICoreAPI api)
        {

            //TODO
        }
    }
}
