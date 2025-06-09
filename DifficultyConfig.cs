using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteRaid
{
    // 定义难度配置类，包含所有表格参数
    public class DifficultyConfig
    {
        public string Name { get; set; }          // 难度名称
        public float DifficultyFactor { get; set; } // 难度系数
        public float RaidScale { get; set; }      // 袭击规模（%）
        public int MaxRaidPoint { get; set; }     // 袭击上限
        public int MaxEliteLevel { get; set; }    // 最大敌人等级
        public EliteRaidDifficulty EliteDifficulty { get; set; } // 精英难度
        public int CompressionRatio { get; set; } // 压缩倍率
        public string Description { get; set; }   // 描述文本

        // 在EliteRaidMod类中更新难度配置字典
        // 在EliteRaidMod类中更新难度配置字典
        public static readonly Dictionary<TotalDifficultyLevel, DifficultyConfig> myDifficultyConfig =
            new Dictionary<TotalDifficultyLevel, DifficultyConfig>
            {
        { TotalDifficultyLevel.Novice, new DifficultyConfig
            {
                Name = "Novice",
                DifficultyFactor = 0.4f,
                RaidScale = 60f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 3,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 3,
                Description = "Beginner-friendly difficulty with fewer and weaker enemies"
            }},

        { TotalDifficultyLevel.Retainer, new DifficultyConfig
            {
                Name = "Retainer",
                DifficultyFactor = 0.7f,
                RaidScale = 110f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 4,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 3,
                Description = "Basic difficulty with moderate enemy numbers"
            }},

        { TotalDifficultyLevel.Knight, new DifficultyConfig
            {
                Name = "Knight",
                DifficultyFactor = 1.0f,
                RaidScale = 220f,
                MaxRaidPoint = 15000,
                MaxEliteLevel = 6,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 3,
                Description = "Standard difficulty balancing challenge and gameplay experience"
            }},

        { TotalDifficultyLevel.Justiciar, new DifficultyConfig
            {
                Name = "Justiciar",
                DifficultyFactor = 1.3f,
                RaidScale = 220f,
                MaxRaidPoint = 20000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 3,
                Description = "Slightly higher difficulty with increased enemy numbers and strength"
            }},

        { TotalDifficultyLevel.Lord, new DifficultyConfig
            {
                Name = "Lord",
                DifficultyFactor = 1.6f,
                RaidScale = 250f,
                MaxRaidPoint = 25000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 4,
                Description = "High difficulty with frequent elite enemies"
            }},

        { TotalDifficultyLevel.Viceroy, new DifficultyConfig
            {
                Name = "Viceroy",
                DifficultyFactor = 2.0f,
                RaidScale = 280f,
                MaxRaidPoint = 30000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 4,
                Description = "Extremely difficult with significantly increased enemy numbers and elite presence"
            }},

        { TotalDifficultyLevel.Governor, new DifficultyConfig
            {
                Name = "Governor",
                DifficultyFactor = 2.4f,
                RaidScale = 320f,
                MaxRaidPoint = 35000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 5,
                Description = "Brutal difficulty with frequent boss-level enemies"
            }},

        { TotalDifficultyLevel.Duke, new DifficultyConfig
            {
                Name = "Duke",
                DifficultyFactor = 3.9f,
                RaidScale = 360f,
                MaxRaidPoint = 40000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 5,
                Description = "Beyond极限，敌人几乎不可阻挡"
            }},

        { TotalDifficultyLevel.Despot, new DifficultyConfig
            {
                Name = "Despot",
                DifficultyFactor = 6.4f,
                RaidScale = 400f,
                MaxRaidPoint = 45000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 6,
                Description = "Theoretically 'impossible' difficulty"
            }},

        { TotalDifficultyLevel.GrandDuke, new DifficultyConfig
            {
                Name = "Grand Duke",
                DifficultyFactor = 10.0f,
                RaidScale = 500f,
                MaxRaidPoint = 50000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 6,
                Description = "Developer-level challenge requiring perfect strategy and configuration"
            }},

        { TotalDifficultyLevel.Consul, new DifficultyConfig
            {
                Name = "Consul",
                DifficultyFactor = 16.0f,
                RaidScale = 600f,
                MaxRaidPoint = 55000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 7,
                Description = "Data-crushing difficulty testing game mechanics limits"
            }},

        { TotalDifficultyLevel.General, new DifficultyConfig
            {
                Name = "General",
                DifficultyFactor = 32.0f,
                RaidScale = 1000f,
                MaxRaidPoint = 60000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 7,
                Description = "Code-level stress test, may cause crashes"
            }},

        { TotalDifficultyLevel.GuardCommander, new DifficultyConfig
            {
                Name = "Guard Commander",
                DifficultyFactor = 54.0f,
                RaidScale = 1400f,
                MaxRaidPoint = 80000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 8,
                Description = "Self-inflicted difficulty beyond game design intentions"
            }},

        { TotalDifficultyLevel.GalaxyLord, new DifficultyConfig
            {
                Name = "Galaxy Lord",
                DifficultyFactor = 72.0f,
                RaidScale = 1600f,
                MaxRaidPoint = 100000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 8,
                Description = "For mod developers debugging only"
            }},

        { TotalDifficultyLevel.StarOverlord, new DifficultyConfig
            {
                Name = "Star Overlord",
                DifficultyFactor = 100.0f,
                RaidScale = 2200f,
                MaxRaidPoint = 130000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 9,
                Description = "Mathematically 'infinite' difficulty (conceptual)"
            }},

        { TotalDifficultyLevel.GalaxyEmperor, new DifficultyConfig
            {
                Name = "Galaxy Emperor",
                DifficultyFactor = 200.0f,
                RaidScale = 2500f,
                MaxRaidPoint = 150000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 10,
                Description = "Challenge the limits of the game's underlying code"
            }}
            };
    }

    public enum TotalDifficultyLevel
    {
        Novice,         // 修士（新手）
        Retainer,       // 扈从（侍从）
        Knight,         // 骑士
        Justiciar,      // 法务官（审判官）
        Lord,           // 领主
        Viceroy,        // 总督
        Governor,       // 执政总督
        Duke,           // 都督（公爵）
        Despot,         // 专制公
        GrandDuke,      // 大公
        Consul,         // 执政官
        General,        // 将军
        GuardCommander, // 近卫总长
        GalaxyLord,     // 星系主宰
        StarOverlord,   // 至高星主
        GalaxyEmperor   // 星海皇帝
    }


}
