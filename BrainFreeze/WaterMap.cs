using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace BrainFreeze
{
    public class WaterMap
    {
        private ushort[] waterMap;

        private int waterCount = 0;
        
        public bool ContainsWater => waterCount > 0;

        public ushort this[int index]
        {
            get
            {
                if(waterMap == null) return 0;
                return waterMap[index];
            }
            set
            {

                waterMap ??= new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize];
                
                if (waterMap[index] != value)
                {
                    waterMap[index] = value;
                    if(value == 0)
                    {
                        waterCount--;
                    }
                    else
                    {
                        waterCount++;
                    }
                }
            }
        }

        public void DoFreezing(BrainFreezeModSystem system, IBulkBlockAccessor block, IServerChunk chunk)
        {
            var totalDays = system.serverAPI.World.Calendar.TotalDays;
            //TODO use totalDays and chunk to see how much time has passed
            //TODO further refine this logic
            for(int i = 0; i < waterMap.Length; i++) 
            {
                if (waterMap[i] == 0) continue;

                double chance = 0d;

                if(system.serverAPI.World.Rand.NextDouble() < chance)
                {
                    //system.serverAPI.World
                    //var temperature = block.GetClimateAt(chunk.MapChunk.)
                }
            }
        }
    }
}
