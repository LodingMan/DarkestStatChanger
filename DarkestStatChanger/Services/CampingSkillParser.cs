using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using DarkestStatChanger.Models;

namespace DarkestStatChanger.Services
{
    public static class CampingSkillParser
    {
        /// <summary>
        /// Searches for a camping skills JSON file relative to the .darkest file path,
        /// looking in ../../raid/camping/ for *.camping_skills.json
        /// </summary>
        public static CampingSkillData Parse(string darkestFilePath)
        {
            var result = new CampingSkillData();

            // Navigate: heroes/{name}/{name}.info.darkest -> ../../raid/camping/
            var heroDir = Path.GetDirectoryName(darkestFilePath);
            if (heroDir == null) return result;

            var heroesDir = Path.GetDirectoryName(heroDir);
            if (heroesDir == null) return result;

            var modRoot = Path.GetDirectoryName(heroesDir);
            if (modRoot == null) return result;

            var campingDir = Path.Combine(modRoot, "raid", "camping");
            if (!Directory.Exists(campingDir)) return result;

            // Find camping skills JSON
            var jsonFiles = Directory.GetFiles(campingDir, "*.camping_skills.json");
            if (jsonFiles.Length == 0) return result;

            result.JsonFilePath = jsonFiles[0];
            result.Skills = ParseJson(result.JsonFilePath);

            // Find camping skill icons
            var iconDir = Path.Combine(campingDir, "skill_icons");
            if (Directory.Exists(iconDir))
            {
                result.IconDirectory = iconDir;
                foreach (var iconFile in Directory.GetFiles(iconDir, "*.png"))
                {
                    // Icon filename: camp_skill_{id}.png
                    var baseName = Path.GetFileNameWithoutExtension(iconFile);
                    if (baseName.StartsWith("camp_skill_"))
                    {
                        var skillId = baseName.Substring("camp_skill_".Length);
                        result.IconPaths[skillId] = iconFile;
                    }
                }
            }

            return result;
        }

        private static List<CampingSkill> ParseJson(string jsonPath)
        {
            var skills = new List<CampingSkill>();
            var json = File.ReadAllText(jsonPath);
            var root = JObject.Parse(json);
            var skillsArray = root["skills"] as JArray;
            if (skillsArray == null) return skills;

            foreach (var item in skillsArray)
            {
                var skill = new CampingSkill
                {
                    Id = item["id"]?.ToString() ?? "",
                    Level = item["level"]?.Value<int>() ?? 0,
                    Cost = item["cost"]?.Value<int>() ?? 0,
                    UseLimit = item["use_limit"]?.Value<int>() ?? 0,
                };

                // Parse effects
                var effectsArr = item["effects"] as JArray;
                if (effectsArr != null)
                {
                    foreach (var eff in effectsArr)
                    {
                        skill.Effects.Add(new CampingEffect
                        {
                            Selection = eff["selection"]?.ToString() ?? "",
                            Type = eff["type"]?.ToString() ?? "",
                            SubType = eff["sub_type"]?.ToString() ?? "",
                            Amount = eff["amount"]?.Value<double>() ?? 0,
                        });
                    }
                }

                // Parse hero_classes
                var heroClasses = item["hero_classes"] as JArray;
                if (heroClasses != null)
                {
                    skill.HeroClasses = heroClasses.Select(h => h.ToString()).ToList();
                }

                skills.Add(skill);
            }

            return skills;
        }

        public static void Save(CampingSkillData data)
        {
            if (string.IsNullOrEmpty(data.JsonFilePath) || !File.Exists(data.JsonFilePath))
                return;

            // Backup
            var bakPath = data.JsonFilePath + ".bak";
            File.Copy(data.JsonFilePath, bakPath, true);

            var json = File.ReadAllText(data.JsonFilePath);
            var root = JObject.Parse(json);
            var skillsArray = root["skills"] as JArray;
            if (skillsArray == null) return;

            // Update values from model
            int idx = 0;
            foreach (var item in skillsArray)
            {
                if (idx >= data.Skills.Count) break;
                var skill = data.Skills[idx];

                item["cost"] = skill.Cost;
                item["use_limit"] = skill.UseLimit;

                // Update effects
                var effectsArr = item["effects"] as JArray;
                if (effectsArr != null)
                {
                    for (int e = 0; e < effectsArr.Count && e < skill.Effects.Count; e++)
                    {
                        effectsArr[e]["amount"] = skill.Effects[e].Amount;
                    }
                }

                idx++;
            }

            File.WriteAllText(data.JsonFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CampingSkillData
    {
        public string JsonFilePath { get; set; }
        public string IconDirectory { get; set; }
        public List<CampingSkill> Skills { get; set; } = new List<CampingSkill>();
        public Dictionary<string, string> IconPaths { get; set; } = new Dictionary<string, string>();
    }
}
