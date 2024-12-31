using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BrainFreeze.Rendering
{
    //TODO see if there is some neater solution
    public class IceCubeTexPositionSource : ITexPositionSource
    {
        private readonly ItemTextureAtlasManager manager;
        private readonly Item item;

        public IceCubeTexPositionSource(ItemTextureAtlasManager manager, Item item)
        {
            this.manager = manager;
            this.item = item;
        }

        public TextureAtlasPosition this[string textureCode] => manager.GetPosition(item, item.Textures.FirstOrDefault().Key);

        public Size2i AtlasSize => manager.Size;
    }
}
