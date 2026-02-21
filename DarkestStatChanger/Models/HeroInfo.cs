using System.Collections.Generic;

namespace DarkestStatChanger.Models
{
    public class HeroInfo
    {
        public List<FileLine> Lines { get; set; } = new List<FileLine>();
        public Resistances Resistances { get; set; }
        public List<WeaponStat> Weapons { get; set; } = new List<WeaponStat>();
        public List<ArmourStat> Armours { get; set; } = new List<ArmourStat>();
        public List<CombatSkill> CombatSkills { get; set; } = new List<CombatSkill>();
    }
}
