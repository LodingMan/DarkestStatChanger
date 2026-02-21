using System.Collections.Generic;

namespace DarkestStatChanger.Models
{
    public class MonsterInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string VariantSuffix { get; set; } // A, B, C
        public string EnemyType { get; set; }
        public int Size { get; set; } = 1;

        // Stats
        public int Hp { get; set; }
        public double Dodge { get; set; }
        public double Prot { get; set; }
        public int Spd { get; set; }
        public double StunResist { get; set; }
        public double PoisonResist { get; set; }
        public double BleedResist { get; set; }
        public double DebuffResist { get; set; }
        public double MoveResist { get; set; }

        // Skills
        public List<MonsterSkill> Skills { get; set; } = new List<MonsterSkill>();

        // Image
        public string ImagePath { get; set; }

        public override string ToString()
        {
            return $"{DisplayName} ({VariantSuffix})";
        }
    }

    public class MonsterSkill
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public double Atk { get; set; }
        public int DmgMin { get; set; }
        public int DmgMax { get; set; }
        public double Crit { get; set; }
        public string Launch { get; set; }
        public string Target { get; set; }
        public List<string> Effects { get; set; } = new List<string>();
    }
}
