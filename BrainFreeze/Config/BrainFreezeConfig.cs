using InsanityLib.Attributes.Auto.Config;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BrainFreeze.Config;

public class BrainFreezeConfig
{
    [AutoConfig("BrainFreezeConfig.json", ServerSync = true)]
    public static BrainFreezeConfig Instance { get; private set; }

    /// <summary>
    /// Mapping of itemcode to freezing temperature (only checks on base code so you can't do per variant registration right now).
    /// </summary>
    public Dictionary<string, float> AutoRegFrozenVariants { get; set; } = new Dictionary<string, float>()
    {
            {"waterportion",         -0f },
            {"distilledwaterportion",-0f},
            {"rainwaterportion",     -0f},
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
            {"weaktanninportion",   -1f},
            {"strongtanninportion", -3f},
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
    };

    /// <summary>
    /// How much damage is taken every time you take damage from freezing.
    /// Base game value is 0.2
    /// </summary>
    [Category("Cold Penalties")]
    [Range(0, float.PositiveInfinity)]
    [DefaultValue(1f)]
    public float FreezingDamage { get; set; } = 1f;

    /// <summary>
    /// How much you can be slowed down when you are frozen.
    /// </summary>
    [Category("Cold Penalties")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0, 1)]
    [DefaultValue(0.3f)]
    public float FreezingMaxSpeedPenalty { get; set; } = 0.3f;
}