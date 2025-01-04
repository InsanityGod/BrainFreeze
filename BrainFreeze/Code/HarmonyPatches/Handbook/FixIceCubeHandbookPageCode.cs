using BrainFreeze.Code.Items;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BrainFreeze.Code.HarmonyPatches.Handbook
{
    [HarmonyPatch(typeof(GuiHandbookItemStackPage))]
    public static class FixIceCubeHandbookPageCode
    {
        [HarmonyPatch(nameof(GuiHandbookItemStackPage.PageCodeForStack))]
        [HarmonyPrefix]
        public static bool PageCodeForStackPrefix(ItemStack stack, ref string __result)
        {
            if(stack.Collectible is IceCube iceCube)
            {
                var content = iceCube.GetContent(stack);

                if(content != null)
                {
                    __result =  $"{stack.Class.Name()}-{iceCube.Code.ToShortString()}-{content.Id}";
                    return false;
                }
            }

            return true;
        }
    }
}
