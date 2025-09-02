using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BrainFreeze.Code.Rendering;

public class IceTexPositionSource(ItemTextureAtlasManager manager, Item item) : ITexPositionSource
{
    public TextureAtlasPosition this[string textureCode] => manager.GetPosition(item, item.Textures.FirstOrDefault().Key);

    public Size2i AtlasSize => manager.Size;
}