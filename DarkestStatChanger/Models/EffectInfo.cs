namespace DarkestStatChanger.Models
{
    /// <summary>
    /// Represents one parsed "effect:" line from a *.effects.darkest file.
    /// Only fields relevant to simulation are stored.
    /// </summary>
    public class EffectInfo
    {
        public string Name { get; set; }

        // DoT
        public int DotPoison { get; set; }   // .dotPoison N  — damage per turn (blight)
        public int DotBleed  { get; set; }   // .dotBleed  N  — damage per turn (bleed)
        public int Duration  { get; set; }   // .duration  N  — number of turns

        // Application chance (base, before resistance)
        public double Chance { get; set; }   // e.g. 100 for 100%

        // Stun
        public bool IsStun { get; set; }     // .stun 1

        // Debuff
        public bool IsDebuff { get; set; }
        public string DebuffIds { get; set; }           // .buff_ids token (e.g. blight_debuff_1)
        public double SpeedRatingAdd { get; set; }      // .speed_rating_add N
        public double AttackRatingAdd { get; set; }     // .attack_rating_add N%
        public double DefenseRatingAdd { get; set; }    // .defense_rating_add N%
        public double DmgLowMultiply { get; set; }      // .damage_low_multiply N%
        public double DmgHighMultiply { get; set; }     // .damage_high_multiply N%
        public double CritChanceAdd { get; set; }       // .crit_chance_add N%

        // Target (performer / target / global)
        public string Target { get; set; }

        // Source tracking (for saving back to the correct file)
        public string SourceFile { get; set; }

        // All raw key-value pairs parsed from the effect line (for dynamic display & save)
        public System.Collections.Generic.List<RawParam> RawParams { get; set; }
            = new System.Collections.Generic.List<RawParam>();
    }

    /// <summary>
    /// One key-value pair from an effect line, e.g. key="chance" value="100%".
    /// OriginalValue is the value as it appeared in the file (used to locate it for saving).
    /// </summary>
    public class RawParam
    {
        public string Key           { get; set; }
        public string OriginalValue { get; set; }  // value when last read/saved
        public string Value         { get; set; }  // current (possibly edited) value
    }

    /// <summary>
    /// Tracks an active DoT on the monster during simulation.
    /// </summary>
    public class ActiveDot
    {
        public string EffectName { get; set; }
        public string Kind { get; set; }    // "Blight" or "Bleed"
        public int DmgPerTurn { get; set; }
        public int TurnsLeft  { get; set; }
    }

    /// <summary>
    /// Tracks an active debuff on the monster during simulation.
    /// </summary>
    public class ActiveDebuff
    {
        public string EffectName { get; set; }
        public string Description { get; set; }
        public int TurnsLeft { get; set; }
    }
}
