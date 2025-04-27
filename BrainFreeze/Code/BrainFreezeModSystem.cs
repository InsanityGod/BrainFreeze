using BrainFreeze.Code.Compatibility;
using BrainFreeze.Code.HarmonyPatches.DynamicRegistry;
using InsanityLib.Attributes.Auto;
using Vintagestory.API.Common;

[assembly: AutoPatcher("brainfreeze")]
[assembly: AutoRegistry("brainfreeze")]
namespace BrainFreeze.Code
{
    public class BrainFreezeModSystem : ModSystem
    {
        public override double ExecuteOrder() => 0.11;

        //TODO if items are in barrel while liquid is at certain level and freezes, the slot should become locked

        //TODO maybe some code for removing no longer existing frozen variants from a world?

        //TODO spill over into other slots if going over slot stacksize

        //TODO have snow be collectible similar to rainwater

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

            if (api.ModLoader.IsModEnabled("hydrateordiedrate"))
            {
                HydrateOrDiedrateCompatibility.FixSnow(api);
                //TODO: see if I can change the snowball model to reflect that you have multiple items on the stack
                //TODO: Collecting snow should be slower
                //TODO: Moving snow with shovel
                //TODO: Allowing to grab snow with buckets
            }

            base.AssetsFinalize(api);
        }
    }
}