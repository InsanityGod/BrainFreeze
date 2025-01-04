﻿using System.Collections.Generic;

namespace BrainFreeze.Config
{
    public class ModConfig
    {
        public Dictionary<string, float> AutoRegFrozenVariants { get; set; } = new Dictionary<string, float>()
        {
                {"waterportion",         -0f },
                {"distilledwaterportion",-0f},
                {"rainwaterportion",     -0f},
                {"wellwaterportion",     -0f},
                {"juiceportion",         -2f},
                {"vegetablejuiceportion",-2f},
                {"yogurt",               -3f},
                {"vinegarportion",       -2f},
                {"ciderportion",         -4f},
                {"finewineportion",      -5f},
                {"strongwineportion",    -8f},
                {"milkportion",          -0.5f},
                {"curdledmilkportion",   -0.5f},
                {"dilutedalum",          -1.5f},
                {"dilutedborax",         -1.5f},
                {"dilutedcassiterite",   -1.5f},
                {"dilutedchromite",      -1.5f},
                {"dye",                  -2f},
                {"weaktanningportion",   -1f},
                {"strongtanningportion", -3f},
                {"eggwhiteportion",      -0.5f},
                {"eggyolkportion",       -0.5f},
                {"eggyolkfullportion",   -0.5f},
                {"brothportion",         -2f},
                {"clarifiedbrothportion",-2f},
                {"fishsauce",            -6f},
                {"dressing",             -2.5f},
                {"soymilk",              -2f},
                {"soysauce",             -8f},
                {"yeastwaterportion",    -3f},
                {"breadstarter",         -3f},
                {"bloodportion",         -2f},
                //foodoilportion        // depends on oil type
                //Syrup maybe?          // -11c
                //brine                 // -1c (remember to mess with pickling as well :3)
        };

        /// <summary>
        /// Limits? what are those?
        /// </summary>
        public bool SuperBrainFreeze { get; set; } = false;
    }
}