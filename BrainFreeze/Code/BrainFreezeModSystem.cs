using BrainFreeze.Code.Behaviors;
using BrainFreeze.Code.HarmonyPatches.DynamicRegistry;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BrainFreeze.Code;

public partial class BrainFreezeModSystem : ModSystem
{
    public override double ExecuteOrder() => 0.11;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        AutoSetup(api);
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        AutoAssetsLoaded(api);
    }

    //TODO if items are in barrel while liquid is at certain level and freezes, the slot should become locked

    //TODO maybe some code for removing no longer existing frozen variants from a world?

    //TODO spill over into other slots if going over slot stacksize

    //TODO have snow be collectible similar to rainwater

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if (api.Side == EnumAppSide.Client) return;

        foreach (var item in api.World.Items)
        {
            if(item is ItemChisel)
            {
                var beh = new IceBreakerTool(item);
                beh.Initialize(new JsonObject(new JObject()));

                item.CollectibleBehaviors = item.CollectibleBehaviors.Append(beh);
            }
            if (item.Variant["brainfreeze"] is null) continue;

            DynamicFrozenVariant.TryFinalizeFrozenCollectible(api, item);
        }

        DynamicFrozenVariant.FinalizeIceCube(api);

        //TODO: see if I can change the snowball model to reflect that you have multiple items on the stack
        //TODO: Collecting snow should be slower
        //TODO: Moving snow with shovel
        //TODO: Allowing to grab snow with buckets
        base.AssetsFinalize(api);
    }

    public override void Dispose()
    {
        base.Dispose();
        AutoDispose();
    }
}