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
        public int MaxRaidEnemy { get; set; }     // 最大袭击人数
        public string Description { get; set; }   // 描述文本

        // 在EliteRaidMod类中更新难度配置字典
        // 在EliteRaidMod类中更新难度配置字典
        public static readonly Dictionary<TotalDifficultyLevel, DifficultyConfig> myDifficultyConfig =
            new Dictionary<TotalDifficultyLevel, DifficultyConfig>
            {
        { TotalDifficultyLevel.Novice, new DifficultyConfig
            {  //难度：修士，难度系数{0.3} 对应原版孤星探险难度，敌人数量1~20，最大等级不超过三级,假如你是一个新手，这是一个相当平衡的难度
                Name = "Novice",
                DifficultyFactor = 0.3f,
                RaidScale = 60f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 3,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 2,
                MaxRaidEnemy = 30,  // 修士
                Description = "Difficulty level: Novice, with difficulty factor {0.3}. Corresponds to vanilla Lost Colony difficulty, enemy count 1-20, maximum level not exceeding 3. If you're a newcomer, this is a well-balanced difficulty"
            }},

        { TotalDifficultyLevel.Retainer, new DifficultyConfig
            { //难度：扈从，难度系数{0.6}默认难度 比原版荒野求生难度稍微难一点，敌人数量1~25，最大等级不超过四级，适合喜欢刺激的老玩家。
                Name = "Retainer",
                DifficultyFactor = 0.6f,
                RaidScale = 110f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 4,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 3,
                MaxRaidEnemy = 30,  // 扈从
                Description = "Difficulty level: Retainer, with difficulty factor {0.6}. Default difficulty slightly harder than vanilla Strive to Survive, enemy count 1-25, maximum level not exceeding 4. Suitable for experienced players who enjoy a challenge"
            }},

        { TotalDifficultyLevel.Knight, new DifficultyConfig
            {//难度：骑士，难度系数{1}  ,原版的冷酷难度，敌人数量1~30，最大等级不超过五级,大部分敌人精英等级在3级，适合挑战极限的老玩家。
                Name = "Knight",
                DifficultyFactor = 1.0f,
                RaidScale = 220f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 5,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 3,
                MaxRaidEnemy = 30,  // 骑士
                Description = "Difficulty level: Knight, with difficulty factor {1.0}. Equivalent to vanilla Blood and Dust difficulty, enemy count 1-30, maximum level not exceeding 5. Most enemies are at elite level 3. Suitable for veteran players seeking a challenge"
            }},

        { TotalDifficultyLevel.Justiciar, new DifficultyConfig
            {//难度：法务官，难度系数{1.3} ，冷酷难度+，敌人数量1~30，最大等级不超过六级，大部分敌人精英等级在3级,适合挑战极限的老玩家。
                Name = "Justiciar",
                DifficultyFactor = 1.3f,
                RaidScale = 290f,
                MaxRaidPoint = 15000,
                MaxEliteLevel = 6,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 3,
                MaxRaidEnemy = 30,  // 法务官
                Description = "Difficulty level: Justiciar, with difficulty factor {1.3}. Blood and Dust+, enemy count 1-30, maximum level not exceeding 6. Most enemies are at elite level 3. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Lord, new DifficultyConfig
            {//难度：领主，难度系数{1.6} ，冷酷难度++，敌人数量1~35，最大等级不超过七级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在3,4级,适合挑战极限的老玩家。
                Name = "Lord",
                DifficultyFactor = 1.6f,
                RaidScale = 350f,
                MaxRaidPoint = 20000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 4,
                MaxRaidEnemy = 35,  // 领主
                Description = "Difficulty level: Lord, with difficulty factor {1.6}. Blood and Dust++, enemy count 1-35, maximum level not exceeding 7. May encounter psychic immune boss enemies. Most enemies are at elite level 3-4. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Viceroy, new DifficultyConfig
            {//难度：总督，难度系数{2} ，冷酷500%难度 ，敌人数量1~40，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在3,4级,适合挑战极限的老玩家。
                Name = "Viceroy",
                DifficultyFactor = 2.0f,
                RaidScale = 450f,
                MaxRaidPoint = 20000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 4,
                MaxRaidEnemy = 40,  // 总督
                Description = "Difficulty level: Viceroy, with difficulty factor {2.0}. 500% Blood and Dust difficulty, enemy count 1-40, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 3-4. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Governor, new DifficultyConfig
            {//难度：执政总督，难度系数{3} ，冷酷难度500%难度+ ，敌人数量1~45，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在4,5级,适合挑战极限的老玩家。
                Name = "Governor",
                DifficultyFactor = 3f,
                RaidScale = 660f,
                MaxRaidPoint = 30000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 5,
                MaxRaidEnemy = 45,  // 都督
                Description = "Difficulty level: Governor, with difficulty factor {3.0}. 500%+ Blood and Dust difficulty, enemy count 1-45, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 4-5. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Duke, new DifficultyConfig
            {//难度：都督，难度系数{3.9} ，冷酷难度1000%难度 ，敌人数量1~50，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在4,5级,适合挑战极限的老玩家。
                Name = "Duke",
                DifficultyFactor = 3.9f,
                RaidScale = 900f,
                MaxRaidPoint = 40000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 5,
                MaxRaidEnemy = 50,  // 专制公
                Description = "Difficulty level: Duke, with difficulty factor {3.9}. 1000% Blood and Dust difficulty, enemy count 1-50, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 4-5. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Despot, new DifficultyConfig
            {//难度：专制公，难度系数{6.4} ，袭击倍率1400% ，敌人数量1~60，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在5,6级,适合挑战极限的老玩家。
                Name = "Despot",
                DifficultyFactor = 6.4f,
                RaidScale = 1400f,
                MaxRaidPoint = 60000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 6,
                MaxRaidEnemy = 60,  // 大公
                Description = "Difficulty level: Despot, with difficulty factor {6.4}. 1400% raid scale, enemy count 1-60, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 5-6. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.GrandDuke, new DifficultyConfig
            {//难度：大公，难度系数{10} ，袭击倍率2000% ，敌人数量1~70，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在5,6级,适合挑战极限的老玩家。
                Name = "Grand Duke",
                DifficultyFactor = 10.0f,
                RaidScale = 2000f,
                MaxRaidPoint = 100000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 6,
                MaxRaidEnemy = 70,  // 执政官
                Description = "Difficulty level: Grand Duke, with difficulty factor {10.0}. 2000% raid scale, enemy count 1-70, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 5-6. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.Consul, new DifficultyConfig
            {//难度：执政官，难度系数{16} ，袭击倍率3500% ，敌人数量1~80，拥有全部的精英等级，可能出现心灵免疫的boss敌人，大部分敌人精英等级在6,7级,适合挑战极限的老玩家。
                Name = "Consul",
                DifficultyFactor = 16.0f,
                RaidScale = 3500f,
                MaxRaidPoint = 160000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 7,
                MaxRaidEnemy = 80,  // 将军
                Description = "Difficulty level: Consul, with difficulty factor {16.0}. 3500% raid scale, enemy count 1-80, with all elite levels available. May encounter psychic immune boss enemies. Most enemies are at elite level 6-7. Suitable for veteran players seeking an extreme challenge"
            }},

        { TotalDifficultyLevel.General, new DifficultyConfig
            {//特别难度：压缩率3,难度系数1，敌人数量1~200，利用压缩率来限制来袭的敌人数量，只使用压缩率会让来袭敌人数量变成原来的1/3，不限制敌人数量，会让游戏更加平衡.
                Name = "General",
                DifficultyFactor = 1.0f,
                RaidScale = 220f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 5,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 3,
                MaxRaidEnemy = 200,  // 近卫总长
                Description = "Special difficulty: Compression ratio 3, difficulty factor 1.0. Enemy count 1-200. Uses compression ratio to limit raid enemy count, making it 1/3 of the original. Not limiting enemy count allows for more balanced gameplay"
            }},

        { TotalDifficultyLevel.GuardCommander, new DifficultyConfig
            {//特别难度：压缩率4,难度系数2，敌人数量1~200，利用压缩率来限制来袭的敌人数量，只使用压缩率会让来袭敌人数量变成原来的1/4，不限制敌人数量，会让游戏更加平衡.
                Name = "Guard Commander",
                DifficultyFactor = 2.0f,
                RaidScale = 450f,
                MaxRaidPoint = 20000,
                MaxEliteLevel = 6,
                EliteDifficulty = EliteRaidDifficulty.Hard,
                CompressionRatio = 4,
                MaxRaidEnemy = 200,  // 星系主宰
                Description = "Special difficulty: Compression ratio 4, difficulty factor 2.0. Enemy count 1-200. Uses compression ratio to limit raid enemy count, making it 1/4 of the original. Not limiting enemy count allows for more balanced gameplay"
            }},

        { TotalDifficultyLevel.GalaxyLord, new DifficultyConfig
            {//特别难度：压缩率5,难度系数3，敌人数量1~200，利用压缩率来限制来袭的敌人数量，只使用压缩率会让来袭敌人数量变成原来的1/5，不限制敌人数量，会让游戏更加平衡.
                Name = "Galaxy Lord",
                DifficultyFactor = 3.0f,
                RaidScale = 660f,
                MaxRaidPoint = 30000,
                MaxEliteLevel = 7,
                EliteDifficulty = EliteRaidDifficulty.Extreme,
                CompressionRatio = 5,
                MaxRaidEnemy = 200,  // 至高星主
                Description = "Special difficulty: Compression ratio 5, difficulty factor 3.0. Enemy count 1-200. Uses compression ratio to limit raid enemy count, making it 1/5 of the original. Not limiting enemy count allows for more balanced gameplay"
            }},

        { TotalDifficultyLevel.StarOverlord, new DifficultyConfig
            {//设置示例：限制敌人数量,难度系数0.3,这是一个难度示例，表明你可以通过设置，单纯限制敌人的数量，来优化游戏战斗帧率.这个示例把敌人数量限制到了50人,并且没有精英强化.
                Name = "Star Overlord",
                DifficultyFactor = 0.3f,
                RaidScale = 220f,
                MaxRaidPoint = 10000,
                MaxEliteLevel = 0,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 4,
                MaxRaidEnemy = 50,  // 星海皇帝
                Description = "Setting example: Enemy count limit with difficulty factor 0.3. This is an example showing how you can optimize game combat framerate by simply limiting enemy count. This example limits enemies to 50 and has no elite enhancements"
            }},

        { TotalDifficultyLevel.GalaxyEmperor, new DifficultyConfig
            {//设置示例: 只使用袭击规模，难度系数2,这是一个示例，你可以通过设置，单纯调整游戏的袭击规模和袭击点数上限，没有精英等级和压缩。
                Name = "Galaxy Emperor",
                DifficultyFactor = 2.0f,
                RaidScale = 500f,
                MaxRaidPoint = 150000,
                MaxEliteLevel = 0,
                EliteDifficulty = EliteRaidDifficulty.Normal,
                CompressionRatio = 1,
                MaxRaidEnemy = 200,  // 星海皇帝
                Description = "Setting example: Using only raid scale with difficulty factor 2.0. This example shows how you can adjust the game by only modifying raid scale and raid point limit, without elite levels or compression"
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
