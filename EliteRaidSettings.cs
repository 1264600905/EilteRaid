// ========== 设置数据类 ==========
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EliteRaid { 
public class EliteRaidSettings : ModSettings
{
    public Vector2 scrollPosition = Vector2.zero;

    // 与Mod类中的字段完全对应
    public bool modEnabled = StaticVariables.DEFAULT_MOD_ENABLED;
    public bool displayMessageValue = StaticVariables.DEFAULT_DISPLAY_MESSAGES;
    public bool showRaidMessages = StaticVariables.DEFAULT_SHOW_RAID_MESSAGES; // 修改：显示袭击提示
    public bool allowRaidFriendlyValue = StaticVariables.DEFAULT_ALLOW_FRIENDLY_RAID;
    public EliteRaidDifficulty eliteRaidDifficulty = StaticVariables.DEFAULT_DIFFICULTY;
    public bool allowMechanoidsValue = StaticVariables.DEFAULT_ALLOW_MECHANODS;
    public bool allowInsectoidsValue = StaticVariables.DEFAULT_ALLOW_INSECTOIDS;
    public bool allowEntitySwarmValue = StaticVariables.DEFAULT_ALLOW_ENTITY_SWARM;
    public bool allowAnimalsValue = StaticVariables.DEFAULT_ALLOW_ANIMALS;
    public int maxAllowLevel = StaticVariables.DEFAULT_MAX_LEVEL;
    public int maxRaidEnemy = StaticVariables.DEFAULT_MAX_ENEMY;
    public bool mapGeneratedEnemyEnhanced = StaticVariables.DEFAULT_MAP_ENHANCED;
        public int maxRaidPoint = StaticVariables.DEFAULT_MAX_RAID_POINT; // 新增
        public bool showDetailConfig=StaticVariables.DEFAULT_SHOW_DETAIL_CONFIG; // 是否显示详细配置
        // 总体难度等级
        public TotalDifficultyLevel totalDifficulty = TotalDifficultyLevel.Knight;
        public bool useCompressionRatio = StaticVariables.DEFAULT_USE_COMPRESSION_RATIO; // 使用压缩倍率替换数量
        public float compressionRatio = StaticVariables.DEFAULT_COMPRESSION_RATIO; // 压缩倍率（1-10）
        public int currentDifficultyIndex = 0;
        public bool enableSilverDrop = StaticVariables.DEFAULT_ENABLE_SILVER_DROP; // 敌人掉落白银
        public int silverDropPerLevel = StaticVariables.DEFAULT_SILVER_DROP_PER_LEVEL;  // 每级掉落数量(20-300)
        public bool allowDropPodRaidValue = StaticVariables.DEFAULT_ALLOW_DROP_POD_RAID; // 新增
        public float raidScale = StaticVariables.DEFAULT_RAID_SCALE; // 新增默认值
        public bool isFirstLoad = true;
        public bool wasReset = false;
        public bool allowModBionicsAndDrugs = StaticVariables.DEFAULT_ALLOW_MOD_BIONICS_AND_DRUGS; // 新增
        public override void ExposeData()
    {
        base.ExposeData();
            Scribe_Values.Look(ref enableSilverDrop, "enableSilverDrop", StaticVariables.DEFAULT_ENABLE_SILVER_DROP);
            Scribe_Values.Look(ref silverDropPerLevel, "silverDropPerLevel", StaticVariables.DEFAULT_SILVER_DROP_PER_LEVEL);
            Scribe_Values.Look(ref useCompressionRatio, "useCompressionRatio", StaticVariables.DEFAULT_USE_COMPRESSION_RATIO);
            Scribe_Values.Look(ref compressionRatio, "compressionRatio", StaticVariables.DEFAULT_COMPRESSION_RATIO);
            Scribe_Values.Look(ref modEnabled, "modEnabled", StaticVariables.DEFAULT_MOD_ENABLED);
        Scribe_Values.Look(ref displayMessageValue, "displayMessageValue", StaticVariables.DEFAULT_DISPLAY_MESSAGES);
        Scribe_Values.Look(ref showRaidMessages, "showRaidMessages", StaticVariables.DEFAULT_SHOW_RAID_MESSAGES); // 修改：保存显示袭击提示设置
        Scribe_Values.Look(ref allowRaidFriendlyValue, "allowRaidFriendlyValue", StaticVariables.DEFAULT_ALLOW_FRIENDLY_RAID);
        Scribe_Values.Look(ref eliteRaidDifficulty, "eliteRaidDifficulty", StaticVariables.DEFAULT_DIFFICULTY);
        Scribe_Values.Look(ref allowMechanoidsValue, "allowMechanoidsValue", StaticVariables.DEFAULT_ALLOW_MECHANODS);
        Scribe_Values.Look(ref allowInsectoidsValue, "allowInsectoidsValue", StaticVariables.DEFAULT_ALLOW_INSECTOIDS);
        Scribe_Values.Look(ref allowEntitySwarmValue, "allowEntitySwarmValue", StaticVariables.DEFAULT_ALLOW_ENTITY_SWARM);
        Scribe_Values.Look(ref allowAnimalsValue, "allowAnimalsValue", StaticVariables.DEFAULT_ALLOW_ANIMALS);
        Scribe_Values.Look(ref maxAllowLevel, "maxAllowLevel", StaticVariables.DEFAULT_MAX_LEVEL);
        Scribe_Values.Look(ref maxRaidEnemy, "maxRaidEnemy", StaticVariables.DEFAULT_MAX_ENEMY);
        Scribe_Values.Look(ref mapGeneratedEnemyEnhanced, "mapGeneratedEnemyEnhanced", StaticVariables.DEFAULT_MAP_ENHANCED);
            Scribe_Values.Look(ref maxRaidPoint, "maxRaidPoint", StaticVariables.DEFAULT_MAX_RAID_POINT);
            Scribe_Values.Look(ref totalDifficulty, "totalDifficulty", StaticVariables.DEFAULT_TOTAL_DIFFICULTY);
            Scribe_Values.Look(ref showDetailConfig, "showDetailConfig", StaticVariables.DEFAULT_SHOW_DETAIL_CONFIG);
            Scribe_Values.Look(ref currentDifficultyIndex, "currentDifficultyIndex", 0);
            Scribe_Values.Look(ref allowDropPodRaidValue, "allowDropPodRaidValue", StaticVariables.DEFAULT_ALLOW_DROP_POD_RAID); // 新增
            Scribe_Values.Look(ref isFirstLoad, "isFirstLoad", true);
            Scribe_Values.Look(ref wasReset, "wasReset", false);
            Scribe_Values.Look(ref raidScale, "raidScale", StaticVariables.DEFAULT_RAID_SCALE);
            // 在 ExposeData 方法中添加序列化逻辑（找到 Scribe_Values 部分）
            Scribe_Values.Look(ref allowModBionicsAndDrugs, "allowModBionicsAndDrugs", StaticVariables.DEFAULT_ALLOW_MOD_BIONICS_AND_DRUGS); // 新增
        }

    public void Reset()
    {
        modEnabled = StaticVariables.DEFAULT_MOD_ENABLED;
        displayMessageValue = StaticVariables.DEFAULT_DISPLAY_MESSAGES;
        showRaidMessages = StaticVariables.DEFAULT_SHOW_RAID_MESSAGES; // 修改：重置显示袭击提示设置
        allowRaidFriendlyValue = StaticVariables.DEFAULT_ALLOW_FRIENDLY_RAID;
        eliteRaidDifficulty = StaticVariables.DEFAULT_DIFFICULTY;
        allowMechanoidsValue = StaticVariables.DEFAULT_ALLOW_MECHANODS;
        allowInsectoidsValue = StaticVariables.DEFAULT_ALLOW_INSECTOIDS;
        allowEntitySwarmValue = StaticVariables.DEFAULT_ALLOW_ENTITY_SWARM;
        allowAnimalsValue = StaticVariables.DEFAULT_ALLOW_ANIMALS;
        maxAllowLevel = StaticVariables.DEFAULT_MAX_LEVEL;
        maxRaidEnemy = StaticVariables.DEFAULT_MAX_ENEMY;
        mapGeneratedEnemyEnhanced = StaticVariables.DEFAULT_MAP_ENHANCED;
            useCompressionRatio = StaticVariables.DEFAULT_USE_COMPRESSION_RATIO;
            compressionRatio = StaticVariables.DEFAULT_COMPRESSION_RATIO;
            enableSilverDrop = StaticVariables.DEFAULT_ENABLE_SILVER_DROP;
            silverDropPerLevel = StaticVariables.DEFAULT_SILVER_DROP_PER_LEVEL;
            maxRaidPoint = StaticVariables.DEFAULT_MAX_RAID_POINT;
            totalDifficulty = StaticVariables.DEFAULT_TOTAL_DIFFICULTY; // 重置总体难度等级
           
            allowDropPodRaidValue = StaticVariables.DEFAULT_ALLOW_DROP_POD_RAID; // 新增
            raidScale = StaticVariables.DEFAULT_RAID_SCALE; // 重置袭击缩放倍率
            wasReset = true;
            allowModBionicsAndDrugs = StaticVariables.DEFAULT_ALLOW_MOD_BIONICS_AND_DRUGS;

            // 总难度下拉框索引重置
            currentDifficultyIndex = (int)StaticVariables.DEFAULT_TOTAL_DIFFICULTY;
        }
}


// ========== 静态默认值 ==========
public static class StaticVariables
{
        // 在 StaticVariables 类中添加（与其他默认值并列）
        public const bool DEFAULT_ALLOW_MOD_BIONICS_AND_DRUGS = true; // 新增
        public const bool DEFAULT_USE_COMPRESSION_RATIO = true; // 新增默认值
        public const float DEFAULT_COMPRESSION_RATIO = 3.0f;
        public const bool DEFAULT_ENABLE_SILVER_DROP = true;
        public const int DEFAULT_SILVER_DROP_PER_LEVEL = 30;
        public const bool DEFAULT_MOD_ENABLED = true;
    public const bool DEFAULT_DISPLAY_MESSAGES = false;
    public const bool DEFAULT_ALLOW_FRIENDLY_RAID = true;
    public const EliteRaidDifficulty DEFAULT_DIFFICULTY = EliteRaidDifficulty.Normal;
    public const bool DEFAULT_ALLOW_MECHANODS = true;
    public const bool DEFAULT_ALLOW_INSECTOIDS = true;
    public const bool DEFAULT_ALLOW_ENTITY_SWARM = true;
    public const bool DEFAULT_ALLOW_ANIMALS = true;
    public const int DEFAULT_MAX_LEVEL = 0;
    public const int DEFAULT_MAX_ENEMY = 20;
    public const bool DEFAULT_MAP_ENHANCED = false;
        public const int DEFAULT_MAX_RAID_POINT = 10000; // 新增默认值
        public const TotalDifficultyLevel DEFAULT_TOTAL_DIFFICULTY = TotalDifficultyLevel.Retainer; // 新增默认值
        public const bool DEFAULT_SHOW_DETAIL_CONFIG = false; // 新增默认值
        public const bool DEFAULT_ALLOW_DROP_POD_RAID = false; // 新增默认值
        public const float DEFAULT_RAID_SCALE=1f; // 新增：袭击缩放倍率
        public const bool DEFAULT_SHOW_RAID_MESSAGES = false; // 新增：显示袭击提示
    }
}