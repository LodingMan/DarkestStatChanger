namespace DarkestStatChanger.Models
{
    public class WeaponStat
    {
        public string Name { get; set; }
        public int Atk { get; set; }
        public int DmgMin { get; set; }
        public int DmgMax { get; set; }
        public int Crit { get; set; }
        public int Spd { get; set; }
        public string UpgradeRequirementCode { get; set; }
    }
}
