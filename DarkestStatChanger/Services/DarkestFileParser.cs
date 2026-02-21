using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    public static class DarkestFileParser
    {
        public static HeroInfo Parse(string filePath)
        {
            var heroInfo = new HeroInfo();
            var lines = File.ReadAllLines(filePath);

            int weaponIdx = 0, armourIdx = 0, skillIdx = 0;

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.TrimStart();

                if (trimmed.StartsWith("resistances:"))
                {
                    heroInfo.Resistances = ParseResistances(trimmed);
                    heroInfo.Lines.Add(new FileLine
                    {
                        OriginalText = rawLine, IsParsed = true,
                        ParsedType = "resistances", DataIndex = 0
                    });
                }
                else if (trimmed.StartsWith("weapon:"))
                {
                    heroInfo.Weapons.Add(ParseWeapon(trimmed));
                    heroInfo.Lines.Add(new FileLine
                    {
                        OriginalText = rawLine, IsParsed = true,
                        ParsedType = "weapon", DataIndex = weaponIdx++
                    });
                }
                else if (trimmed.StartsWith("armour:"))
                {
                    heroInfo.Armours.Add(ParseArmour(trimmed));
                    heroInfo.Lines.Add(new FileLine
                    {
                        OriginalText = rawLine, IsParsed = true,
                        ParsedType = "armour", DataIndex = armourIdx++
                    });
                }
                else if (trimmed.StartsWith("combat_skill:") ||
                         trimmed.StartsWith("riposte_skill:") ||
                         trimmed.StartsWith("combat_move_skill:"))
                {
                    heroInfo.CombatSkills.Add(ParseCombatSkill(trimmed));
                    heroInfo.Lines.Add(new FileLine
                    {
                        OriginalText = rawLine, IsParsed = true,
                        ParsedType = "combat_skill", DataIndex = skillIdx++
                    });
                }
                else
                {
                    heroInfo.Lines.Add(new FileLine
                    {
                        OriginalText = rawLine, IsParsed = false
                    });
                }
            }

            return heroInfo;
        }

        private static List<PropertyEntry> ExtractProperties(string content)
        {
            var props = new List<PropertyEntry>();
            var matches = Regex.Matches(content, @"\.([a-zA-Z_]\w*)");

            for (int i = 0; i < matches.Count; i++)
            {
                string key = matches[i].Groups[1].Value;
                int valueStart = matches[i].Index + matches[i].Length;
                int valueEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : content.Length;

                string rawValue = content.Substring(valueStart, valueEnd - valueStart).Trim();
                props.Add(new PropertyEntry(key, rawValue));
            }

            return props;
        }

        private static Dictionary<string, string> PropsToDict(List<PropertyEntry> props)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in props)
            {
                dict[p.Key] = p.Value;
            }
            return dict;
        }

        private static int ParsePercent(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            val = val.Replace("%", "").Trim();
            int.TryParse(val, out int result);
            return result;
        }

        private static string StripQuotes(string val)
        {
            if (val == null) return "";
            return val.Trim().Trim('"');
        }

        private static Resistances ParseResistances(string line)
        {
            var dict = PropsToDict(ExtractProperties(line));
            return new Resistances
            {
                Stun = ParsePercent(dict.GetValueOrDefault("stun")),
                Poison = ParsePercent(dict.GetValueOrDefault("poison")),
                Bleed = ParsePercent(dict.GetValueOrDefault("bleed")),
                Disease = ParsePercent(dict.GetValueOrDefault("disease")),
                Move = ParsePercent(dict.GetValueOrDefault("move")),
                Debuff = ParsePercent(dict.GetValueOrDefault("debuff")),
                DeathBlow = ParsePercent(dict.GetValueOrDefault("death_blow")),
                Trap = ParsePercent(dict.GetValueOrDefault("trap")),
            };
        }

        private static WeaponStat ParseWeapon(string line)
        {
            var dict = PropsToDict(ExtractProperties(line));
            var dmgParts = (dict.GetValueOrDefault("dmg") ?? "0 0").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            return new WeaponStat
            {
                Name = StripQuotes(dict.GetValueOrDefault("name")),
                Atk = ParsePercent(dict.GetValueOrDefault("atk")),
                DmgMin = int.TryParse(dmgParts.ElementAtOrDefault(0), out int mn) ? mn : 0,
                DmgMax = int.TryParse(dmgParts.ElementAtOrDefault(1), out int mx) ? mx : 0,
                Crit = ParsePercent(dict.GetValueOrDefault("crit")),
                Spd = int.TryParse(dict.GetValueOrDefault("spd"), out int spd) ? spd : 0,
                UpgradeRequirementCode = dict.GetValueOrDefault("upgradeRequirementCode"),
            };
        }

        private static ArmourStat ParseArmour(string line)
        {
            var dict = PropsToDict(ExtractProperties(line));
            return new ArmourStat
            {
                Name = StripQuotes(dict.GetValueOrDefault("name")),
                Def = ParsePercent(dict.GetValueOrDefault("def")),
                Prot = int.TryParse(dict.GetValueOrDefault("prot"), out int prot) ? prot : 0,
                Hp = int.TryParse(dict.GetValueOrDefault("hp"), out int hp) ? hp : 0,
                Spd = int.TryParse(dict.GetValueOrDefault("spd"), out int spd) ? spd : 0,
                UpgradeRequirementCode = dict.GetValueOrDefault("upgradeRequirementCode"),
            };
        }

        private static CombatSkill ParseCombatSkill(string line)
        {
            string skillType = line.Substring(0, line.IndexOf(':')).Trim();
            var allProps = ExtractProperties(line);
            var dict = PropsToDict(allProps);

            return new CombatSkill
            {
                SkillType = skillType,
                Id = StripQuotes(dict.GetValueOrDefault("id")),
                Level = int.TryParse(dict.GetValueOrDefault("level"), out int lv) ? lv : 0,
                Atk = dict.GetValueOrDefault("atk") ?? "",
                Dmg = dict.GetValueOrDefault("dmg") ?? "",
                Crit = dict.GetValueOrDefault("crit") ?? "",
                Launch = dict.GetValueOrDefault("launch") ?? "",
                Target = dict.GetValueOrDefault("target") ?? "",
                Type = StripQuotes(dict.GetValueOrDefault("type") ?? ""),
                Properties = allProps,
            };
        }

        private static string GetValueOrDefault(this Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var val) ? val : null;
        }
    }
}
