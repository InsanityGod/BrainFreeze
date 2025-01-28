using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BrainFreeze.Code.LakeFreezing
{
    public class LakeFreezingManager
    {
        public readonly ICoreServerAPI CoreServerAPI;
        private int StillWaterId;
        private int LakeIceId;
        public bool IsDoingStuff { get; private set; }

        public LakeFreezingManager(ICoreServerAPI coreServerAPI) => CoreServerAPI = coreServerAPI;

        internal void Initialize()
        {
            CoreServerAPI.Event.ChunkDirty += HandleChunkDirty;
            StillWaterId = CoreServerAPI.World.GetItem(new AssetLocation("water-still-7")).Id;
            LakeIceId = CoreServerAPI.World.GetItem(new AssetLocation("lakeice")).Id;
        }

        private void HandleChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
        {
            if (!IsDoingStuff)
            {
                //chunk.Unpack();
                //chunk.Data.GetFluid();

                IsDoingStuff = true;
                Task.Run(DoStuff);
            }
        }

        private async Task DoStuff()
        {
            var accessor = CoreServerAPI.World.GetBlockAccessorPrefetch(false, false);
            
            //do work

            IsDoingStuff = false;
        }
    }
}
