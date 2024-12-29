using BrainFreeze.Behaviors;
using BrainFreeze.Config;
using BrainFreeze.HarmonyPatches;
using BrainFreeze.HarmonyPatches.DynamicRegistry;
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
using Vintagestory.Server;

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
            api.RegisterCollectibleBehaviorClass("brainfreeze:frozennameprefix", typeof(FrozenNamePrefix));
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if(api.Side == EnumAppSide.Client) return;

            waterBlockId = api.World.GetBlock(AssetLocation.Create("water-still-7")).BlockId;
            iceBlockId = api.World.GetBlock(AssetLocation.Create("lakeice")).BlockId;
            foreach(var item in api.World.Items)
            {
                if (item.Variant["brainfreeze"] != null)
                {
                    DynamicFrozenVariant.FinalizeFrozenCollectible(api, item);
                }
            }
            base.AssetsFinalize(api);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            base.Dispose();
        }

        internal int waterBlockId = -1;
        internal int iceBlockId = -1;


        internal ICoreServerAPI serverAPI;
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            serverAPI = api;
            if(!GlobalConstants.MeltingFreezingEnabled) return;
            api.Event.RegisterGameTickListener(DoWaterStuff, 1000);
            api.Event.ChunkDirty += OnChunkDirty;
        }

        private void DoWaterStuff(float lastCalled)
        {
            var block = serverAPI.World.GetBlockAccessorBulkUpdate(true, false);
            
            //serverAPI.WorldManager.Load
            //TODO performance
            var loadedChunks = serverAPI.WorldManager.AllLoadedChunks;

            bool anyFreezing = false;
            foreach((var key, var chunk) in loadedChunks)
            {
                if(chunk.LiveModData.TryGetValue("BrainFreeze:WaterMap", out object value) && value is WaterMap waterMap && waterMap.ContainsWater)
                {
                    waterMap.DoFreezing(this, block, chunk);
                    anyFreezing = true;
                }
            }

            if (anyFreezing)
            {
                block.Commit();
            }
        }

        private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
        {
            if(chunk.Data == null) return;
            //TODO optimize
            var waterMap = (WaterMap) (
                chunk.LiveModData.TryGetValue("BrainFreeze:WaterMap", out object value) ? 
                value :
                chunk.LiveModData["BrainFreeze:WaterMap"] = new WaterMap()); //32 ^ 2

            for (int z = 0; z < MagicNum.ServerChunkSize; z++)
            {
                int zOffset = z * MagicNum.ServerChunkSize; // Precompute the Z contribution

                for (int x = 0; x < MagicNum.ServerChunkSize; x++)
                {
                    int xzOffset = zOffset + x; // Precompute the ZX contribution

                    bool found = false;
                    for (ushort y = chunk.MapChunk.YMax; y > 0; y--)
                    {
                        int index3D = xzOffset * MagicNum.ServerChunkSize + y; // Add Y contribution

                        var fluidId = chunk.Data.GetFluid(index3D);
                        if (fluidId == waterBlockId || fluidId == iceBlockId)
                        {
                            waterMap[xzOffset] = y;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        waterMap[xzOffset] = 0;
                    }
                }
            }

            if(reason == EnumChunkDirtyReason.NewlyLoaded || reason == EnumChunkDirtyReason.NewlyCreated)
            {
                //TODO see about freezing afterwards to a realistic point
            }
        }

        private void OnChunkLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            throw new NotImplementedException();
        }



        //TODO BlockLiquidContainerTopOpened.CanDrinkFrom (for auto added stuff)
        //TODO slush
        //TODO barrels?
        //TODO make it so you can't get it out of bottles if it's ice :p
    }
}
