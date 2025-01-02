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

        public BrainFreezeModSystem()
        {
            DynamicFrozenVariant.AllowedCodes ??= new()
            {
                "waterportion",         // -0c
                "distilledwaterportion",// -0c
                "rainwaterportion",     // -0c
                //TODO wellwaterportion // -0c
                "juiceportion",         // -2c
                "vegetablejuiceportion",// -2c
                "yogurt",               // -3c
                "vinegarportion",       // -2c
                "ciderportion",         // -4c
                "finewineportion",      // -5c
                "strongwineportion",    // -8c
                "milkportion",          // -0.5c
                "curdledmilkportion",   // -0.5c
                "dilutedalum",          // -1.5c
                "dilutedborax",         // -1.5c
                "dilutedcassiterite",   // -1.5c
                "dilutedchromite",      // -1.5c
                "dye",                  // -2c
                "weaktanningportion",   // -1c
                "strongtanningportion", // -3c
                "eggwhiteportion",      // -0.5c
                "eggwhiteportion",      // -0.5c
                "eggyolkportion",       // -0.5c
                "eggyolkfullportion",   // -0.5c
                "brothportion",         // -2c
                "clarifiedbrothportion",// -2c
                "fishsauce",            // -6c
                "dressing",             // -2.5c
                //"soymilk",              // -2c //TODO find out why this turns ucontents into a single TreeAttribute instead of a TreeAttributeArray
                "soysauce",             // -8c
                "yeastwaterportion",    // -3c
                "breadstarter",         // -3c
                "bloodportion",         // -2c
                //foodoilportion        // depends on oil type
                //Syrup maybe?          // -11c
                //brine                 // -1c (remember to mess with pickling as well :3)
                //TODO if items are in barrel while liquid freezes the slot should become locked
            };
            //TODO disable mixing recipes for frozen variants :3
            //TODO freeze point temperature
            //TODO maybe some code for removing non existant frozen variants from a world?
            //TODO Water ice cubes have no hydration?
            //TODO frozen liquid is solid so you can move it arround in item slots which is big no no
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
            DynamicFrozenVariant.AllowedCodes = null;
            base.Dispose();
        }

        //TODO BlockLiquidContainerTopOpened.CanDrinkFrom (for auto added stuff)
        //TODO slush
        //TODO barrels?
        //TODO make it so you can't get it out of bottles if it's ice :p
    }
}