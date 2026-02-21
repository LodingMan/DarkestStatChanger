using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    public static class EffectsParser
    {
        /// <summary>
        /// Scans every *.effects.darkest file in <paramref name="effectsDir"/>
        /// and returns a dictionary keyed by effect name (case-insensitive).
        /// </summary>
        public static Dictionary<string, EffectInfo> LoadAll(string effectsDir)
        {
            var dict = new Dictionary<string, EffectInfo>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(effectsDir)) return dict;

            foreach (var file in Directory.GetFiles(effectsDir, "*.effects.darkest", SearchOption.AllDirectories))
                LoadFile(file, dict);

            return dict;
        }

        /// <summary>
        /// Loads a single *.effects.darkest file and merges its entries into <paramref name="dict"/>.
        /// Existing entries with the same name are overwritten.
        /// </summary>
        public static void LoadFile(string filePath, Dictionary<string, EffectInfo> dict)
        {
            if (!File.Exists(filePath)) return;
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("effect:", StringComparison.OrdinalIgnoreCase)) continue;

                var info = ParseEffectLine(trimmed);
                if (info != null && !string.IsNullOrEmpty(info.Name))
                {
                    info.SourceFile = filePath;
                    dict[info.Name] = info;
                }
            }
        }

        private static EffectInfo ParseEffectLine(string line)
        {
            var info = new EffectInfo();

            info.Name     = ExtractStringValue(line, "name");
            info.Target   = ExtractStringValue(line, "target");
            info.Chance   = ExtractDouble(line, "chance");
            info.DotPoison = ExtractInt(line, "dotPoison");
            info.DotBleed  = ExtractInt(line, "dotBleed");
            info.Duration  = ExtractInt(line, "duration");
            info.IsStun    = Regex.IsMatch(line, @"\.stun\s+1\b", RegexOptions.IgnoreCase);

            // Debuff fields
            info.DebuffIds        = ExtractUnquotedToken(line, "buff_ids");
            info.SpeedRatingAdd   = ExtractSignedDouble(line, "speed_rating_add");
            info.AttackRatingAdd  = ExtractSignedDouble(line, "attack_rating_add");
            info.DefenseRatingAdd = ExtractSignedDouble(line, "defense_rating_add");
            info.DmgLowMultiply   = ExtractSignedDouble(line, "damage_low_multiply");
            info.DmgHighMultiply  = ExtractSignedDouble(line, "damage_high_multiply");
            info.CritChanceAdd    = ExtractSignedDouble(line, "crit_chance_add");

            bool hasDebuffEnemy = Regex.IsMatch(line, @"\.debuffenemy\s+1\b", RegexOptions.IgnoreCase);
            bool hasBuff_ids    = !string.IsNullOrEmpty(info.DebuffIds);
            bool hasNegStatMod  = info.SpeedRatingAdd < 0 || info.AttackRatingAdd < 0 ||
                                  info.DefenseRatingAdd < 0 || info.DmgLowMultiply < 0 ||
                                  info.DmgHighMultiply < 0;

            info.IsDebuff = !info.IsStun &&
                            info.DotPoison == 0 && info.DotBleed == 0 &&
                            (hasDebuffEnemy || hasBuff_ids || hasNegStatMod);

            info.RawParams = ParseAllParams(line);

            return info;
        }

        /// <summary>
        /// Generically extracts every .key value pair from an effect line.
        /// The 'name' key is included so callers can identify the effect.
        /// </summary>
        private static List<Models.RawParam> ParseAllParams(string line)
        {
            var result = new List<Models.RawParam>();
            var keyMatches = Regex.Matches(line, @"\.\w+").Cast<Match>().ToList();

            for (int i = 0; i < keyMatches.Count; i++)
            {
                string key = keyMatches[i].Value.TrimStart('.');
                int valueStart = keyMatches[i].Index + keyMatches[i].Length;
                int valueEnd   = (i + 1 < keyMatches.Count)
                                 ? keyMatches[i + 1].Index
                                 : line.Length;
                string value = line.Substring(valueStart, valueEnd - valueStart).Trim();

                result.Add(new Models.RawParam { Key = key, OriginalValue = value, Value = value });
            }
            return result;
        }

        // Extracts .key "value" (quoted string)
        private static string ExtractStringValue(string line, string key)
        {
            var m = Regex.Match(line, $@"\.{key}\s+""([^""]+)""", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        // Extracts .key N (integer)
        private static int ExtractInt(string line, string key)
        {
            var m = Regex.Match(line, $@"\.{key}\s+(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        // Extracts .key N% (percentage as double, strips %)
        private static double ExtractDouble(string line, string key)
        {
            var m = Regex.Match(line, $@"\.{key}\s+([\d.]+)%?", RegexOptions.IgnoreCase);
            if (!m.Success) return 0;
            double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v);
            return v;
        }

        // Extracts .key -N or .key -N% (handles negative values)
        private static double ExtractSignedDouble(string line, string key)
        {
            var m = Regex.Match(line, $@"\.{key}\s+(-?[\d.]+)%?", RegexOptions.IgnoreCase);
            if (!m.Success) return 0;
            double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v);
            return v;
        }

        // Extracts .key unquotedtoken (e.g. .buff_ids blight_debuff_1)
        private static string ExtractUnquotedToken(string line, string key)
        {
            var m = Regex.Match(line, $@"\.{key}\s+(\S+)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
