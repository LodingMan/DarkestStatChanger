using System.Collections.Generic;

namespace DarkestStatChanger.Models
{
    public class CampingSkill
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public int Cost { get; set; }
        public int UseLimit { get; set; }
        public List<CampingEffect> Effects { get; set; } = new List<CampingEffect>();
        public List<string> HeroClasses { get; set; } = new List<string>();
    }

    public class CampingEffect
    {
        public string Selection { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public double Amount { get; set; }
    }
}
