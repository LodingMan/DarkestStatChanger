using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    public static class EffectsFileWriter
    {
        /// <summary>
        /// Saves modified EffectInfo entries back to their source files using RawParams.
        /// Creates a .bak backup per changed file. Returns the number of files written.
        /// </summary>
        public static int Save(IEnumerable<EffectInfo> effects)
        {
            var byFile = effects
                .Where(e => !string.IsNullOrEmpty(e.SourceFile) && File.Exists(e.SourceFile))
                .GroupBy(e => e.SourceFile, StringComparer.OrdinalIgnoreCase);

            int filesSaved = 0;
            foreach (var group in byFile)
            {
                var filePath = group.Key;
                var effectMap = group.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

                var encoding = DetectEncoding(filePath);
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath, encoding);
                }
                catch (Exception ex)
                {
                    throw new DscException("E301", $"Effects file could not be read: {filePath}", ex);
                }
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var trimmed = lines[i].Trim();
                    if (!trimmed.StartsWith("effect:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var nameMatch = Regex.Match(trimmed,
                        @"\.name\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    if (!nameMatch.Success) continue;

                    if (!effectMap.TryGetValue(nameMatch.Groups[1].Value, out var eff)) continue;

                    string updated = lines[i];

                    foreach (var param in eff.RawParams)
                    {
                        // Skip the name key (it's the identifier, never changed)
                        if (param.Key.Equals("name", StringComparison.OrdinalIgnoreCase)) continue;
                        // Skip params with no original value (standalone flags)
                        if (string.IsNullOrEmpty(param.OriginalValue)) continue;
                        // Skip if not actually changed
                        if (param.Value == param.OriginalValue) continue;

                        string escapedKey      = Regex.Escape(param.Key);
                        string escapedOriginal = Regex.Escape(param.OriginalValue);

                        string result = Regex.Replace(
                            updated,
                            $@"(\.{escapedKey}\s+){escapedOriginal}",
                            m => m.Groups[1].Value + param.Value,
                            RegexOptions.IgnoreCase);

                        if (result != updated)
                        {
                            updated = result;
                            // Update OriginalValue so subsequent saves use the new value
                            param.OriginalValue = param.Value;
                        }
                    }

                    if (updated != lines[i]) { lines[i] = updated; changed = true; }
                }

                if (changed)
                {
                    try
                    {
                        File.Copy(filePath, filePath + ".bak", overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        throw new DscException("E302", $"Failed to create backup before saving effects file: {filePath}.bak", ex);
                    }
                    try
                    {
                        File.WriteAllLines(filePath, lines, encoding);
                    }
                    catch (Exception ex)
                    {
                        throw new DscException("E303", $"Failed to write effects file: {filePath}", ex);
                    }
                    filesSaved++;
                }
            }
            return filesSaved;
        }

        private static Encoding DetectEncoding(string path)
        {
            var bom = new byte[3];
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                fs.Read(bom, 0, 3);
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return new UTF8Encoding(true);
            return new UTF8Encoding(false);
        }
    }
}
