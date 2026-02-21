using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    /// <summary>
    /// Saves hero data by performing in-place regex replacements on the
    /// ORIGINAL line text, so all whitespace and unmodified fields are
    /// preserved byte-for-byte.
    /// </summary>
    public static class DarkestFileWriter
    {
        public static void Save(HeroInfo heroInfo, string filePath)
        {
            // Create backup
            if (File.Exists(filePath))
            {
                string bakPath = filePath + ".bak";
                File.Copy(filePath, bakPath, true);
            }

            // Detect original BOM to preserve encoding exactly
            bool hasBom = false;
            if (File.Exists(filePath))
            {
                var rawBytes = File.ReadAllBytes(filePath);
                hasBom = rawBytes.Length >= 3
                         && rawBytes[0] == 0xEF
                         && rawBytes[1] == 0xBB
                         && rawBytes[2] == 0xBF;
            }

            var sb = new StringBuilder();

            foreach (var line in heroInfo.Lines)
            {
                if (!line.IsParsed)
                {
                    sb.Append(line.OriginalText);
                    sb.Append("\r\n");
                    continue;
                }

                string result = line.OriginalText;

                switch (line.ParsedType)
                {
                    case "resistances":
                        result = PatchResistances(result, heroInfo.Resistances);
                        break;
                    case "weapon":
                        result = PatchWeapon(result, heroInfo.Weapons[line.DataIndex]);
                        break;
                    case "armour":
                        result = PatchArmour(result, heroInfo.Armours[line.DataIndex]);
                        break;
                    case "combat_skill":
                        result = PatchCombatSkill(result, heroInfo.CombatSkills[line.DataIndex]);
                        break;
                }

                sb.Append(result);
                sb.Append("\r\n");
            }

            // Remove trailing extra newline if original didn't have one
            var content = sb.ToString();
            if (content.EndsWith("\r\n\r\n"))
                content = content.Substring(0, content.Length - 2);

            // Write with same encoding as original (with or without BOM)
            var encoding = new UTF8Encoding(hasBom);
            File.WriteAllText(filePath, content, encoding);
        }

        // Replace .key VALUE in-place, preserving surrounding whitespace
        private static string ReplaceField(string line, string key, string newValue)
        {
            // Word boundary (?!\w) prevents .hp from matching inside .death_blow
            var pattern = $@"(\.{Regex.Escape(key)}(?!\w)\s+)(\S+)";
            return Regex.Replace(line, pattern, $"${{1}}{newValue}", RegexOptions.IgnoreCase);
        }

        // Replace .key "quoted value" in-place
        private static string ReplaceQuotedField(string line, string key, string newValue)
        {
            var pattern = $@"(\.{Regex.Escape(key)}\s+)""[^""]*""";
            return Regex.Replace(line, pattern, $"${{1}}\"{newValue}\"", RegexOptions.IgnoreCase);
        }

        // Replace .dmg MIN MAX (two numbers after .dmg)
        private static string ReplaceDmgField(string line, int min, int max)
        {
            var pattern = @"(\.dmg\s+)-?\d+\s+-?\d+";
            return Regex.Replace(line, pattern, $"${{1}}{min} {max}", RegexOptions.IgnoreCase);
        }

        private static string PatchResistances(string original, Resistances r)
        {
            var result = original;
            result = ReplaceField(result, "stun", $"{r.Stun}%");
            result = ReplaceField(result, "poison", $"{r.Poison}%");
            result = ReplaceField(result, "bleed", $"{r.Bleed}%");
            result = ReplaceField(result, "disease", $"{r.Disease}%");
            result = ReplaceField(result, "move", $"{r.Move}%");
            result = ReplaceField(result, "debuff", $"{r.Debuff}%");
            result = ReplaceField(result, "death_blow", $"{r.DeathBlow}%");
            result = ReplaceField(result, "trap", $"{r.Trap}%");
            return result;
        }

        private static string PatchWeapon(string original, WeaponStat w)
        {
            var result = original;
            result = ReplaceField(result, "atk", $"{w.Atk}%");
            result = ReplaceDmgField(result, w.DmgMin, w.DmgMax);
            result = ReplaceField(result, "crit", $"{w.Crit}%");
            result = ReplaceField(result, "spd", $"{w.Spd}");
            return result;
        }

        private static string PatchArmour(string original, ArmourStat a)
        {
            var result = original;
            result = ReplaceField(result, "def", $"{a.Def}%");
            result = ReplaceField(result, "prot", $"{a.Prot}");
            result = ReplaceField(result, "hp", $"{a.Hp}");
            result = ReplaceField(result, "spd", $"{a.Spd}");
            return result;
        }

        private static string PatchCombatSkill(string original, CombatSkill skill)
        {
            var result = original;
            // Only patch the editable fields - atk, dmg, crit
            if (!string.IsNullOrEmpty(skill.Atk))
                result = ReplaceField(result, "atk", skill.Atk);
            if (!string.IsNullOrEmpty(skill.Dmg))
                result = ReplaceField(result, "dmg", skill.Dmg);
            if (!string.IsNullOrEmpty(skill.Crit))
                result = ReplaceField(result, "crit", skill.Crit);
            return result;
        }
    }
}
