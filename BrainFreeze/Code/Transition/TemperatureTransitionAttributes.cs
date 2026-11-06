using InsanityLib.Extended.Transitions;

namespace BrainFreeze.Code.Transition;

public class TemperatureTransitionAttributes : TransitionAttributes
{
    /// <summary>
    /// If set to to true the transition wil happen at higher temperature then "freezePoint" instad of lower temperature
    /// </summary>
    public bool Invert { get; set; }
}
