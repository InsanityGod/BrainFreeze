using BrainFreeze.HarmonyPatches;
using CustomTransitionLib;
using HarmonyLib;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BrainFreeze
{
    public class BrainFreezeModSystem : ModSystem
    {
        private Harmony harmony;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll();
            }
            var registry = api.ModLoader.GetModSystem<CustomTransitionLibModSystem>();
            registry.Register(new BrainFreezeTransitionHandler());
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            base.Dispose();
        }
    }
}
