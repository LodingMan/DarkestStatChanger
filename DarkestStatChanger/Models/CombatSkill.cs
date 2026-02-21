using System.Collections.Generic;

namespace DarkestStatChanger.Models
{
    public class CombatSkill
    {
        public string SkillType { get; set; }
        public string Id { get; set; }
        public int Level { get; set; }
        public string Atk { get; set; }
        public string Dmg { get; set; }
        public string Crit { get; set; }
        public string Launch { get; set; }
        public string Target { get; set; }
        public string Type { get; set; }
        public List<PropertyEntry> Properties { get; set; } = new List<PropertyEntry>();
    }

    public class PropertyEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public PropertyEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
