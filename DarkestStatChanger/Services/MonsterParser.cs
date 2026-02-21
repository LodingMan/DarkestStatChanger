using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    public static class MonsterParser
    {
        /// <summary>
        /// Scans the monsters directory and parses all monster variants.
        /// </summary>
        public static List<MonsterInfo> LoadAll(string monstersDir)
        {
            var monsters = new List<MonsterInfo>();
            if (!Directory.Exists(monstersDir)) return monsters;

            foreach (var monsterDir in Directory.GetDirectories(monstersDir))
            {
                var baseName = Path.GetFileName(monsterDir);

                // Find attack image
                string attackImage = FindAttackImage(monsterDir);

                // Each sub-folder (e.g. skeleton_common_A) is a variant
                foreach (var variantDir in Directory.GetDirectories(monsterDir))
                {
                    var variantName = Path.GetFileName(variantDir);
                    var infoFiles = Directory.GetFiles(variantDir, "*.info.darkest");
                    if (infoFiles.Length == 0) continue;

                    try
                    {
                        var monster = ParseMonsterFile(infoFiles[0]);
                        monster.Id = baseName;
                        monster.DisplayName = FormatName(baseName);

                        // Extract variant suffix (last part after underscore: A, B, C, etc.)
                        var suffix = variantName.Substring(baseName.Length).TrimStart('_');
                        monster.VariantSuffix = string.IsNullOrEmpty(suffix) ? "A" : suffix;

                        monster.ImagePath = attackImage;
                        monsters.Add(monster);
                    }
                    catch { /* skip unparseable monsters */ }
                }
            }

            return monsters.OrderBy(m => m.DisplayName).ThenBy(m => m.VariantSuffix).ToList();
        }

        private static MonsterInfo ParseMonsterFile(string filePath)
        {
            var monster = new MonsterInfo();
            var lines = File.ReadAllLines(filePath);

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();

                if (trimmed.StartsWith("stats:"))
                {
                    var props = ExtractProps(trimmed);
                    monster.Hp = GetInt(props, "hp");
                    monster.Dodge = GetPercent(props, "def");
                    monster.Prot = GetDouble(props, "prot");
                    monster.Spd = GetInt(props, "spd");
                    monster.StunResist = GetPercent(props, "stun_resist");
                    monster.PoisonResist = GetPercent(props, "poison_resist");
                    monster.BleedResist = GetPercent(props, "bleed_resist");
                    monster.DebuffResist = GetPercent(props, "debuff_resist");
                    monster.MoveResist = GetPercent(props, "move_resist");
                }
                else if (trimmed.StartsWith("skill:"))
                {
                    var props = ExtractProps(trimmed);
                    var dmgParts = GetValue(props, "dmg")?.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    var skill = new MonsterSkill
                    {
                        Id = StripQuotes(GetValue(props, "id")),
                        Type = StripQuotes(GetValue(props, "type") ?? "melee"),
                        Atk = GetPercent(props, "atk"),
                        DmgMin = dmgParts != null && dmgParts.Length > 0 ? ParseInt(dmgParts[0]) : 0,
                        DmgMax = dmgParts != null && dmgParts.Length > 1 ? ParseInt(dmgParts[1]) : 0,
                        Crit = GetPercent(props, "crit"),
                        Launch = GetValue(props, "launch") ?? "",
                        Target = GetValue(props, "target") ?? "",
                    };

                    // Parse effects
                    var effectVal = GetValue(props, "effect");
                    if (!string.IsNullOrEmpty(effectVal))
                    {
                        var effectMatches = Regex.Matches(effectVal, "\"([^\"]+)\"|([^\\s\"]+)");
                        foreach (Match m in effectMatches)
                        {
                            var eff = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                            if (!string.IsNullOrWhiteSpace(eff))
                                skill.Effects.Add(eff);
                        }
                    }

                    monster.Skills.Add(skill);
                }
                else if (trimmed.StartsWith("display:"))
                {
                    var props = ExtractProps(trimmed);
                    monster.Size = GetInt(props, "size", 1);
                }
                else if (trimmed.StartsWith("enemy_type:"))
                {
                    var props = ExtractProps(trimmed);
                    monster.EnemyType = StripQuotes(GetValue(props, "id") ?? "");
                }
            }

            return monster;
        }

        private static string FindAttackImage(string monsterDir)
        {
            var animDir = Path.Combine(monsterDir, "anim");
            if (!Directory.Exists(animDir)) return null;

            var attackPngs = Directory.GetFiles(animDir, "*attack*.png")
                .OrderBy(f => f)
                .ToArray();

            return attackPngs.Length > 0 ? attackPngs[0] : null;
        }

        private static string FormatName(string rawName)
        {
            return string.Join(" ", rawName.Split('_')
                .Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1) : w));
        }

        #region Property Parsing Helpers

        private static Dictionary<string, string> ExtractProps(string line)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var matches = Regex.Matches(line, @"\.([a-zA-Z_]\w*)");

            for (int i = 0; i < matches.Count; i++)
            {
                string key = matches[i].Groups[1].Value;
                int valueStart = matches[i].Index + matches[i].Length;
                int valueEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;
                string val = line.Substring(valueStart, valueEnd - valueStart).Trim();
                dict[key] = val;
            }

            return dict;
        }

        private static string GetValue(Dictionary<string, string> props, string key)
        {
            return props.TryGetValue(key, out var v) ? v : null;
        }

        private static double GetPercent(Dictionary<string, string> props, string key)
        {
            var val = GetValue(props, key);
            if (val == null) return 0;
            val = val.Replace("%", "").Trim();
            double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result);
            return result;
        }

        private static double GetDouble(Dictionary<string, string> props, string key)
        {
            var val = GetValue(props, key);
            if (val == null) return 0;
            double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result);
            return result;
        }

        private static int GetInt(Dictionary<string, string> props, string key, int fallback = 0)
        {
            var val = GetValue(props, key);
            if (val == null) return fallback;
            return ParseInt(val, fallback);
        }

        private static int ParseInt(string val, int fallback = 0)
        {
            val = val.Replace("%", "").Trim();
            return int.TryParse(val, out int r) ? r : fallback;
        }

        private static string StripQuotes(string val)
        {
            return val?.Trim().Trim('"') ?? "";
        }

        #endregion
    }
}
