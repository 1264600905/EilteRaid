using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EliteRaid
{
    // 自定义难度枚举（与EliteRaidMod中的精英难度一致）
    public enum EliteRaidDifficulty
    {
        Normal=1,
        Hard=2,
        Extreme
    }

    // 精英等级配置结构（包含压缩比例和属性）
    public class EliteLevelConfig
    {
        public int Level { get; set; }
        public float DamageFactor { get; set; } // 承伤系数（百分比，如70%=0.7）
        public float MoveSpeedFactor { get; set; } // 移速系数（百分比，如100%=1.0）
        public float ScaleFactor { get; set; } // 体型系数
        public int MaxTraits { get; set; } // 最大词条数
        public bool IsBoss { get; set; } // 是否为Boss
        public int CompressionRatio { get; set; } // 压缩比例（如2表示2人压缩成1人）

        // For Dictionary key usage, it's good practice to override Equals and GetHashCode
        // if instances are created dynamically and need to be compared.
        // However, if you only use the instances defined in Initialize() as keys,
        // reference equality (default for classes) might be sufficient.
        // For robustness if configs could be recreated with same values:
        public override bool Equals(object obj)
        {
            if (obj is EliteLevelConfig other)
            {
                return Level == other.Level &&
                       DamageFactor == other.DamageFactor &&
                       MoveSpeedFactor == other.MoveSpeedFactor &&
                       ScaleFactor == other.ScaleFactor &&
                       MaxTraits == other.MaxTraits &&
                       IsBoss == other.IsBoss &&
                       CompressionRatio == other.CompressionRatio;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 23 + Level.GetHashCode();
            hashCode = hashCode * 23 + DamageFactor.GetHashCode();
            hashCode = hashCode * 23 + MoveSpeedFactor.GetHashCode();
            // ... include all fields
            return hashCode;
        }
    }

    public class EliteLevelManager : Mod
    {
        // 存储各难度下的等级配置
        private static Dictionary<EliteRaidDifficulty, List<EliteLevelConfig>> difficultyConfigs = new Dictionary<EliteRaidDifficulty, List<EliteLevelConfig>>();
        // 存储当前生成的精英等级分布 (Config -> Count)
        private static Dictionary<EliteLevelConfig, int> currentLevelDistribution = new Dictionary<EliteLevelConfig, int>();


        // 概率表，移至静态字段
        private static readonly Dictionary<int, double> baseProbabilities = new Dictionary<int, double> {
            { 1, 0.30 }, { 2, 0.20 }, { 3, 0.15 }, { 0, 0 },
            { 4, 0.10 }, { 5, 0.10 }, { 6, 0.10 }, { 7, 0.05 }
        };

        // Stores the config of an elite that was actually part of the distribution,
        // to be used by GetRandomEliteLevel if all counts become zero.
        private static EliteLevelConfig lastResortConfig = null;

        public EliteLevelManager(ModContentPack mod):base(mod)
        {
            if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] EliteLevelManager Mod constructor called, initializing level manager data.");
            Initialize();
        }

        public static void Initialize()
        {
            // Clear previous configs if any (e.g., during mod reloads if this is called again)
            difficultyConfigs.Clear();

            difficultyConfigs[EliteRaidDifficulty.Normal] = new List<EliteLevelConfig>
{
    new EliteLevelConfig { Level=1, DamageFactor=0.78f, MoveSpeedFactor=1.0f, ScaleFactor=1.0f, MaxTraits=1, IsBoss=false, CompressionRatio=2 },
    new EliteLevelConfig { Level=2, DamageFactor=0.67f, MoveSpeedFactor=1f, ScaleFactor=1.0f, MaxTraits=2, IsBoss=false, CompressionRatio=3 },
    new EliteLevelConfig { Level=3, DamageFactor=0.53f, MoveSpeedFactor=0.7f, ScaleFactor=1.2f, MaxTraits=2, IsBoss=false, CompressionRatio=4 },
    new EliteLevelConfig { Level=4, DamageFactor=0.44f, MoveSpeedFactor=0.7f, ScaleFactor=1.35f, MaxTraits=3, IsBoss=false, CompressionRatio=5 },
    new EliteLevelConfig { Level=5, DamageFactor=0.35f, MoveSpeedFactor=0.5f, ScaleFactor=1.55f, MaxTraits=4, IsBoss=false, CompressionRatio=6 },
    new EliteLevelConfig { Level=6, DamageFactor=0.28f, MoveSpeedFactor=0.5f, ScaleFactor=1.65f, MaxTraits=4, IsBoss=false, CompressionRatio=8 },
    new EliteLevelConfig { Level=7, DamageFactor=0.20f, MoveSpeedFactor=0.5f, ScaleFactor=2.0f, MaxTraits=5, IsBoss=true, CompressionRatio=10 },
};
            difficultyConfigs[EliteRaidDifficulty.Hard] = new List<EliteLevelConfig>
{
    new EliteLevelConfig { Level=1, DamageFactor=0.74f, MoveSpeedFactor=1.0f, ScaleFactor=1.1f, MaxTraits=1, IsBoss=false, CompressionRatio=3 },
    new EliteLevelConfig { Level=2, DamageFactor=0.60f, MoveSpeedFactor=1.0f, ScaleFactor=1.15f, MaxTraits=2, IsBoss=false, CompressionRatio=4 },
    new EliteLevelConfig { Level=3, DamageFactor=0.50f, MoveSpeedFactor=0.7f, ScaleFactor=1.35f, MaxTraits=3, IsBoss=false, CompressionRatio=5 },
    new EliteLevelConfig { Level=4, DamageFactor=0.40f, MoveSpeedFactor=0.7f, ScaleFactor=1.5f, MaxTraits=4, IsBoss=false, CompressionRatio=6 },
    new EliteLevelConfig { Level=5, DamageFactor=0.30f, MoveSpeedFactor=0.5f, ScaleFactor=1.7f, MaxTraits=4, IsBoss=false, CompressionRatio=7 },
    new EliteLevelConfig { Level=6, DamageFactor=0.25f, MoveSpeedFactor=0.5f, ScaleFactor=1.8f, MaxTraits=5, IsBoss=false, CompressionRatio=9 },
    new EliteLevelConfig { Level=7, DamageFactor=0.18f, MoveSpeedFactor=0.5f, ScaleFactor=2.0f, MaxTraits=6, IsBoss=true, CompressionRatio=10 },
};
            difficultyConfigs[EliteRaidDifficulty.Extreme] = new List<EliteLevelConfig>
{
    new EliteLevelConfig { Level=1, DamageFactor=0.70f, MoveSpeedFactor=1.0f, ScaleFactor=1.2f, MaxTraits=1, IsBoss=false, CompressionRatio=2 },
    new EliteLevelConfig { Level=2, DamageFactor=0.56f, MoveSpeedFactor=1.0f, ScaleFactor=1.2f, MaxTraits=2, IsBoss=false, CompressionRatio=3 },
    new EliteLevelConfig { Level=3, DamageFactor=0.45f, MoveSpeedFactor=0.7f, ScaleFactor=1.4f, MaxTraits=3, IsBoss=false, CompressionRatio=4 },
    new EliteLevelConfig { Level=4, DamageFactor=0.37f, MoveSpeedFactor=0.7f, ScaleFactor=1.55f, MaxTraits=4, IsBoss=false, CompressionRatio=5 },
    new EliteLevelConfig { Level=5, DamageFactor=0.27f, MoveSpeedFactor=0.5f, ScaleFactor=1.75f, MaxTraits=5, IsBoss=false, CompressionRatio=6 },
    new EliteLevelConfig { Level=6, DamageFactor=0.22f, MoveSpeedFactor=0.5f, ScaleFactor=1.85f, MaxTraits=6, IsBoss=false, CompressionRatio=8 },
    new EliteLevelConfig { Level=7, DamageFactor=0.16f, MoveSpeedFactor=0.5f, ScaleFactor=2.0f, MaxTraits=7, IsBoss=true, CompressionRatio=10 },
};
            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] EliteLevelManager Initialized with {difficultyConfigs.Count} difficulty settings.");
        }

        // 添加辅助函数：判断是否应该使用简化生成策略
        private static bool ShouldUseSimplifiedGeneration(int originalCount)
        {
            // 获取当前难度配置
            if (!difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out List<EliteLevelConfig> configs))
            {
                return false;
            }

            // 获取允许的最高等级配置
            EliteLevelConfig maxLevelConfig = configs
                .Where(c => c.Level <= EliteRaidMod.maxAllowLevel)
                .OrderByDescending(c => c.Level)
                .FirstOrDefault();

            if (maxLevelConfig == null)
            {
                return false;
            }

            // 计算最大可能的精英数量
            int maxPossibleElites = General.GetenhancePawnNumber(originalCount);

            // 计算最高等级精英所需的原始人数
            int requiredOriginalCount = maxLevelConfig.CompressionRatio * maxPossibleElites;

            // 如果原始人数已经足够满足最高等级精英的最大数量，使用简化策略
            return originalCount >= requiredOriginalCount;
        }

        // 添加简化生成函数：直接生成最高等级的精英分布
        private static void GenerateSimplifiedLevelDistribution(int originalCount)
        {
            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 使用简化生成策略：直接生成最高等级精英分布。");

            currentLevelDistribution.Clear();
            lastResortConfig = null;

            // 获取当前难度配置
            if (!difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out List<EliteLevelConfig> configs))
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 找不到当前难度的配置，无法生成简化分布。");
                return;
            }

            // 获取允许的最高等级配置
            EliteLevelConfig maxLevelConfig = configs
                .Where(c => c.Level <= EliteRaidMod.maxAllowLevel)
                .OrderByDescending(c => c.Level)
                .FirstOrDefault();

            if (maxLevelConfig == null)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 找不到符合条件的最高等级配置，无法生成简化分布。");
                return;
            }

            // 计算最大可能的精英数量
            int maxPossibleElites = General.GetenhancePawnNumber(originalCount);

            // 计算实际可以生成的精英数量
            int eliteCount = Math.Min(maxPossibleElites, originalCount / maxLevelConfig.CompressionRatio);

            // 设置分布
            currentLevelDistribution[maxLevelConfig] = eliteCount;
            lastResortConfig = maxLevelConfig;

            // 计算占用的原始人数
            int slotsFilled = eliteCount * maxLevelConfig.CompressionRatio;

            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 简化生成结果：{eliteCount} 个等级 {maxLevelConfig.Level} 的精英，占用 {slotsFilled}/{originalCount} 个原始名额。");
        }

        // 检查是否只有一种有效配置
        private static bool HasSingleValidConfig()
        {
            if (!difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var configs))
                return false;

            var validConfigs = configs
                .Where(c => c.Level <= EliteRaidMod.maxAllowLevel && c.CompressionRatio > 0)
                .ToList();

            return validConfigs.Count == 1;
        }

        // 生成单一配置分布
        private static void GenerateSingleConfigDistribution(int originalCount)
        {
            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 使用单一配置分布策略。");

            currentLevelDistribution.Clear();
            lastResortConfig = null;

            if (!difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var configs))
                return;

            var validConfig = configs
                .Where(c => c.Level <= EliteRaidMod.maxAllowLevel && c.CompressionRatio > 0)
                .FirstOrDefault();

            if (validConfig == null)
                return;

            // 计算可生成的最大数量
            int maxPossibleElites = General.GetenhancePawnNumber(originalCount);
            int maxPossible = Math.Min(maxPossibleElites, originalCount / validConfig.CompressionRatio);
           
            if (maxPossible > 0)
            {
                currentLevelDistribution[validConfig] = maxPossible;
                lastResortConfig = validConfig;
            }
        }

        public static void GenerateLevelDistribution(int originalCount)
        {
            // 特殊情况1: 原始数量为0
            if (originalCount <= 0)
            {
                currentLevelDistribution.Clear();
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 原始数量为0，生成空分布");
                return;
            }

            // 特殊情况2: 使用简化生成策略（最高等级）
            if (ShouldUseSimplifiedGeneration(originalCount))
            {
                GenerateSimplifiedLevelDistribution(originalCount);
                return;
            }
            //特殊情况4：最大精英等级是0
            if (EliteRaidMod.maxAllowLevel == 0)
            {
                currentLevelDistribution.Clear();
                // 获取当前难度的配置
                if (difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var configs))
                {
                    // 获取0级配置
                    var zeroLevelConfig = configs.FirstOrDefault(c => c.Level == 0);
                    if (zeroLevelConfig != null)
                    {
                        // 计算可以生成的0级精英数量
                        int maxPossibleElites = General.GetenhancePawnNumber(originalCount);
                        currentLevelDistribution[zeroLevelConfig] = maxPossibleElites;
                        lastResortConfig = zeroLevelConfig;
                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 最大等级为0，生成 {maxPossibleElites} 个0级精英");
                    }
                }
                return;
            }

            // 特殊情况3: 只有一种有效配置
            if (HasSingleValidConfig())
            {
                GenerateSingleConfigDistribution(originalCount);
                return;
            }

            //  Log.Message($"[EliteRaid] Generating level distribution for originalCount: {originalCount}. MaxAllowedLevel: {7}, TargetMaxRaidEnemy: {20}, Difficulty: {EliteRaidMod.eliteRaidDifficulty}");
            int maxRaidEnemy = General.GetenhancePawnNumber(originalCount);
            if (EliteRaidMod.displayMessageValue) Log.Message("最大袭击人数"+maxRaidEnemy);
            const double minUtilizationThreshold = 0.80; // 80%的利用率阈值
            const int maxAttempts = 20; // 最大尝试次数
            int attempt = 1;
            Dictionary<EliteLevelConfig, int> bestDistribution = new Dictionary<EliteLevelConfig, int>();
            int bestSlotsFilled = 0;
            int bestElitesGenerated = 0;
            double bestUtilization = 0;

            while (attempt <= maxAttempts)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Attempt {attempt}/{maxAttempts}...");
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 1: Clearing current distribution");
                currentLevelDistribution.Clear();
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 2: Clearing lastResortConfig");
                lastResortConfig = null;
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 3: Checking originalCount value: {originalCount}");

                if (originalCount <= 0)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] originalCount is 0 or less, no elites will be generated.");
                    return;
                }

                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 4: Getting configs for difficulty: {EliteRaidMod.eliteRaidDifficulty}");
                if (!difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out List<EliteLevelConfig> availableConfigsForDifficulty))
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] No EliteLevelConfig found for difficulty: {EliteRaidMod.eliteRaidDifficulty}. Cannot generate distribution.");
                    return;
                }
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 5: Found {availableConfigsForDifficulty.Count} configs for current difficulty");

                // 包含Level=0的敌人（CR=1）
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 6: Starting to filter valid configs");
                List<EliteLevelConfig> validConfigs = availableConfigsForDifficulty
                    .Where(c => c.Level <= EliteRaidMod.maxAllowLevel && c.CompressionRatio > 0 && baseProbabilities.ContainsKey(c.Level))
                    .ToList();

                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 7: Found {validConfigs.Count} valid configs after filtering");
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 8: Max allowed level is {EliteRaidMod.maxAllowLevel}");

                // 计算目标精英数量范围
                int targetMaxElites = Math.Min(maxRaidEnemy, originalCount);
                int targetMinElites = Math.Max(1, (int)(targetMaxElites * 0.4)); // 保证至少有40%的精英
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 9: Target elites range: {targetMinElites}-{targetMaxElites}");

                // 调整概率
                Dictionary<EliteLevelConfig, double> adjustedProbabilities = new Dictionary<EliteLevelConfig, double>();
                double totalAdjustedProb = 0;
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 10: Starting probability adjustments");

                foreach (var config in validConfigs)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Processing config Level {config.Level} with CR {config.CompressionRatio}");
                    if (!baseProbabilities.TryGetValue(config.Level, out double baseProb))
                        continue;

                    // 根据压缩比调整概率
                    double crAdjustment = Math.Pow(0.8, config.CompressionRatio - 1);
                    double adjustedProb = baseProb * crAdjustment;

                    adjustedProbabilities[config] = adjustedProb;
                    totalAdjustedProb += adjustedProb;
                }
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 11: Completed probability adjustments, total prob: {totalAdjustedProb}");

                // 阶段1：生成初始精英
                //if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Step 12: Starting initial elite generation");
                var phase1Result = GenerateInitialElites(validConfigs, adjustedProbabilities, totalAdjustedProb, targetMaxElites, originalCount);
                int currentSlotsFilled = phase1Result.currentSlotsFilled;
                int elitesActuallyGenerated = phase1Result.elitesActuallyGenerated;
             //   Log.Message($"[EliteRaid] Phase 1 complete: {elitesActuallyGenerated} elites, consuming {currentSlotsFilled}/{originalCount} slots.");

                // 阶段2: 升级低等级精英以填满空间（改进版）
                var phase2Result = UpgradeElitesToFillSpace(validConfigs, currentLevelDistribution, currentSlotsFilled, originalCount);
                currentSlotsFilled = phase2Result.currentSlotsFilled;
                int currentElites = phase2Result.currentElites;
             //   Log.Message($"[EliteRaid] After upgrade: {currentElites} elites, {currentSlotsFilled}/{originalCount} slots.");

                // 阶段3: 降级高等级精英以调整数量（改进版）
                currentElites = DowngradeElitesToAdjustCount(validConfigs, currentLevelDistribution, currentSlotsFilled, originalCount, currentElites, targetMinElites, targetMaxElites);
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] After downgrade: {currentElites} elites, {currentSlotsFilled}/{originalCount} slots.");

                // 限制数量不超过上限
                if (currentElites > targetMaxElites)
                {
                    // 从0级开始减少数量（优先保留高等级）
                    foreach (var config in validConfigs.OrderBy(c => c.Level))
                    {
                        if (currentLevelDistribution.TryGetValue(config, out int count) && count > 0)
                        {
                            int reduceCount = Math.Min(count, currentElites - targetMaxElites);
                            currentLevelDistribution[config] -= reduceCount;
                            currentElites -= reduceCount;
                            if (currentElites <= targetMaxElites) break;
                        }
                    }
                }

                 validConfigs = availableConfigsForDifficulty
                 .Where(c => c.Level <= 7 && c.CompressionRatio > 0 && baseProbabilities.ContainsKey(c.Level))
                 .ToList();

                currentElites = currentLevelDistribution.Sum(e => e.Value);
                var finalResult = FinalOptimizeDistribution(validConfigs, currentLevelDistribution, currentSlotsFilled, originalCount, currentElites);
                currentSlotsFilled = finalResult.currentSlotsFilled;
                currentElites = finalResult.currentElites;

                // 评估本轮结果
                if (currentSlotsFilled > bestSlotsFilled)
                {
                    bestSlotsFilled = currentSlotsFilled;
                    bestDistribution = new Dictionary<EliteLevelConfig, int>(currentLevelDistribution);
                }

                // 如果达到理想利用率，提前退出
                double utilization = (double)currentSlotsFilled / originalCount;
                if (utilization >= 0.9)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Early termination: Good utilization ({utilization:P2}) achieved in attempt {attempt}.");
                    break;
                }

                // 阶段4: 用0级敌人补足剩余空间
                var zeroLevelConfig = validConfigs.FirstOrDefault(c => c.Level == 0);
                if (zeroLevelConfig != null && currentSlotsFilled < originalCount && currentElites < targetMaxElites)
                {
                    int maxPossible = Math.Min(originalCount - currentSlotsFilled, targetMaxElites - currentElites);
                    currentLevelDistribution[zeroLevelConfig] = maxPossible;
                    currentSlotsFilled += maxPossible;
                    currentElites += maxPossible;
                }

                // 新增：阶段5 - 调整分布以确保空间利用率在合理范围内
                var phase5Result = AdjustDistributionToTargetUtilization(validConfigs, currentLevelDistribution, currentSlotsFilled, originalCount);
                currentSlotsFilled = phase5Result.currentSlotsFilled;
                currentElites = phase5Result.currentElites;

                // Log.Message($"[EliteRaid] Attempt {attempt} results: {currentElites} elites, {currentSlotsFilled}/{originalCount} slots, utilization {utilization:P2}");

                // 记录最佳结果
                if (utilization > bestUtilization)
                {
                    bestUtilization = utilization;
                    bestSlotsFilled = currentSlotsFilled;
                    bestElitesGenerated = currentElites;
                    bestDistribution = new Dictionary<EliteLevelConfig, int>(currentLevelDistribution);
                }

                // 如果达到利用率阈值，退出循环
                if (utilization >= minUtilizationThreshold)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Warning($"[EliteRaid] Success: Utilization meets threshold ({utilization:P2}) after {attempt} attempts.");
                    break;
                }

                attempt++;
            }

            // 使用最佳结果作为最终分布
            currentLevelDistribution = bestDistribution;
            int finalSlotsFilled = currentLevelDistribution.Sum(e => e.Key.CompressionRatio * e.Value);
            double finalUtilization = (double)finalSlotsFilled / originalCount;
            int finalElitesGenerated = currentLevelDistribution.Sum(e => e.Value);
            if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] Final Generated Elite Distribution:");
            foreach (var entry in currentLevelDistribution.OrderBy(e => e.Key.Level))
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"  Level {entry.Key.Level}: Count={entry.Value} (CR: {entry.Key.CompressionRatio})");
            }

         //   Log.Message($"[EliteRaid] Total elites: {currentLevelDistribution.Sum(e => e.Value)}");
          //  Log.Message($"[EliteRaid] Total slots consumed: {finalSlotsFilled} / {originalCount}");
///if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Space utilization: {finalUtilization:P2}"); // 正确的利用率



            // 如果未达到阈值，输出警告
            if (finalUtilization < minUtilizationThreshold)
            {
                if (EliteRaidMod.displayMessageValue) Log.Warning($"[EliteRaid] Warning: Could not reach {minUtilizationThreshold:P0} utilization after {maxAttempts} attempts. Best result: {finalUtilization:P2}");
            }
        }

        // 阶段1：生成初始精英（优化版）
        private static (int currentSlotsFilled, int elitesActuallyGenerated) GenerateInitialElites(
            List<EliteLevelConfig> validConfigs,
            Dictionary<EliteLevelConfig, double> adjustedProbabilities,
            double totalAdjustedProb,
            int targetMaxElites,
            int originalCount)
        {
            // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Starting with targetMax={targetMaxElites}, originalCount={originalCount}");
            
            int currentSlotsFilled = 0;
            int elitesActuallyGenerated = 0;
            int maxIterations = 1000;
            int iterations = 0;

            // 动态调整的保底比例（根据原始数量变化）
            double baseRetentionRatio = originalCount < 10 ? 0.6 : 0.4;
            int minGuaranteedElites = Math.Max(1, (int)(targetMaxElites * baseRetentionRatio));
            
            // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Set minGuaranteedElites={minGuaranteedElites}");

            // 记录已生成的各等级精英数量
            Dictionary<int, int> generatedByLevel = new Dictionary<int, int>();

            while (elitesActuallyGenerated < targetMaxElites && currentSlotsFilled < originalCount)
            {
                iterations++;
                // if (EliteRaidMod.displayMessageValue && iterations % 100 == 0) Log.Message($"[EliteRaid] GenerateInitialElites: Iteration {iterations}, current elites={elitesActuallyGenerated}");

                if (totalAdjustedProb <= 0) 
                {
                    // if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GenerateInitialElites: Total probability is 0, breaking loop");
                    break;
                }

                // 关键改进：动态调整概率权重，防止高压缩比精英过多
                // if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GenerateInitialElites: Calculating dynamic probabilities");
                Dictionary<EliteLevelConfig, double> dynamicProbabilities = new Dictionary<EliteLevelConfig, double>();
                double dynamicTotalProb = 0;

                foreach (var config in validConfigs)
                {
                    double baseProb = adjustedProbabilities.ContainsKey(config) ? adjustedProbabilities[config] : 0;
                    double crFactor = 1.0;

                    // 惩罚高压缩比精英（当剩余空间不足时）
                    if (originalCount - currentSlotsFilled < config.CompressionRatio * 2)
                    {
                        crFactor = 0.2; // 大幅降低高压缩比精英的概率
                        // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Applying high CR penalty to level {config.Level}");
                    }

                    // 防止单一等级精英过多
                    int currentLevelCount = generatedByLevel.GetValueOrDefault(config.Level, 0);
                    double levelBalanceFactor = 1.0 / (currentLevelCount + 1);

                    // 新增：在高压缩率情况下，大幅降低0级敌人的生成概率
                    if (config.Level == 0 && EliteRaidMod.compressionRatio >= 5)
                    {
                        levelBalanceFactor *= 0.1; // 降低90%的0级敌人生成概率
                    }

                    double dynamicProb = baseProb * crFactor * levelBalanceFactor;
                    dynamicProbabilities[config] = dynamicProb;
                    dynamicTotalProb += dynamicProb;
                }

                // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Dynamic total probability = {dynamicTotalProb}");

                // 选择精英配置
                // if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GenerateInitialElites: Starting elite selection");
                double p = ThreadSafeRandom.NextDouble() * dynamicTotalProb;
                double cumulativeProb = 0;
                EliteLevelConfig chosenConfig = null;

                foreach (var config in validConfigs)
                {
                    if (dynamicProbabilities.ContainsKey(config))
                    {
                        cumulativeProb += dynamicProbabilities[config];
                        if (p <= cumulativeProb)
                        {
                            chosenConfig = config;
                            // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Selected config level {config.Level}");
                            break;
                        }
                    }
                }

                if (chosenConfig == null)
                {
                    // if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GenerateInitialElites: No config chosen, defaulting to lowest level");
                    chosenConfig = validConfigs.OrderBy(c => c.Level).FirstOrDefault();
                }

                if (chosenConfig == null)
                {
                    // if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GenerateInitialElites: Failed to find any valid config, breaking loop");
                    break;
                }

                // 检查是否可以添加此精英
                // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Checking if can add elite (slots needed: {chosenConfig.CompressionRatio}, available: {originalCount - currentSlotsFilled})");
                if (currentSlotsFilled + chosenConfig.CompressionRatio <= originalCount)
                {
                    if (currentLevelDistribution.ContainsKey(chosenConfig))
                        currentLevelDistribution[chosenConfig]++;
                    else
                        currentLevelDistribution[chosenConfig] = 1;

                    currentSlotsFilled += chosenConfig.CompressionRatio;
                    elitesActuallyGenerated++;
                    lastResortConfig = chosenConfig;

                    // 更新等级统计
                    generatedByLevel[chosenConfig.Level] = generatedByLevel.GetValueOrDefault(chosenConfig.Level, 0) + 1;
                    // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Added elite level {chosenConfig.Level}, total elites now {elitesActuallyGenerated}");
                }
                else
                {
                    // 从调整概率中移除无法添加的精英
                    // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Cannot add elite level {chosenConfig.Level}, removing from probability pool");
                    if (dynamicProbabilities.ContainsKey(chosenConfig))
                    {
                        totalAdjustedProb -= dynamicProbabilities[chosenConfig];
                        adjustedProbabilities.Remove(chosenConfig);
                    }
                }

                if (iterations >= maxIterations)
                {
                    // if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GenerateInitialElites: Reached max iterations ({maxIterations}), breaking loop");
                    break;
                }
            }

            // 强制补足保底数量（更智能的补足策略）
            if (elitesActuallyGenerated < minGuaranteedElites)
            {
                // 优先使用低压缩比精英补足，但避免0级敌人
                var lowCrConfigs = validConfigs
                    .Where(c => c.CompressionRatio <= 3 && c.Level > 0) // 排除0级敌人
                    .OrderBy(c => c.CompressionRatio)
                    .ToList();

                if (lowCrConfigs.Any())
                {
                    var bestConfig = lowCrConfigs.First();
                    int neededElites = minGuaranteedElites - elitesActuallyGenerated;
                    int availableSlots = originalCount - currentSlotsFilled;
                    int addCount = Math.Min(neededElites, availableSlots / bestConfig.CompressionRatio);

                    if (addCount > 0)
                    {
                        currentLevelDistribution[bestConfig] = currentLevelDistribution.GetValueOrDefault(bestConfig, 0) + addCount;
                        currentSlotsFilled += addCount * bestConfig.CompressionRatio;
                        elitesActuallyGenerated += addCount;
                    }
                }
                else
                {
                    // 如果实在没有其他选择，才使用0级敌人
                    var zeroLevelConfig = validConfigs.FirstOrDefault(c => c.Level == 0);
                    if (zeroLevelConfig != null)
                    {
                        int neededElites = minGuaranteedElites - elitesActuallyGenerated;
                        int availableSlots = originalCount - currentSlotsFilled;
                        int addCount = Math.Min(neededElites, availableSlots);

                        if (addCount > 0)
                        {
                            currentLevelDistribution[zeroLevelConfig] = currentLevelDistribution.GetValueOrDefault(zeroLevelConfig, 0) + addCount;
                            currentSlotsFilled += addCount;
                            elitesActuallyGenerated += addCount;
                        }
                    }
                }
            }

            if (iterations >= maxIterations)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Warning: Reached maximum iterations ({maxIterations}) during generation phase.");
            }

            return (currentSlotsFilled, elitesActuallyGenerated);
        }

        // 阶段2: 升级低等级精英以填满空间（改进版）
        private static (int currentSlotsFilled, int currentElites) UpgradeElitesToFillSpace(
            List<EliteLevelConfig> validConfigs,
            Dictionary<EliteLevelConfig, int> levelDistribution,
            int currentSlotsFilled,
            int originalCount)
        {
            int currentElites = levelDistribution.Sum(e => e.Value);
            if (currentSlotsFilled >= originalCount) return (currentSlotsFilled, currentElites);

            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Filling space by upgrading elites: {currentSlotsFilled}/{originalCount}.");

            // 计算每个等级的总压缩比贡献
            Dictionary<int, int> levelCrContribution = new Dictionary<int, int>();
            foreach (var config in validConfigs)
            {
                if (levelDistribution.ContainsKey(config))
                {
                    levelCrContribution[config.Level] = config.CompressionRatio * levelDistribution[config];
                }
            }

            // 阶段2.1：优先升级0级精英（但限制升级数量）
            var zeroLevelConfig = validConfigs.FirstOrDefault(c => c.Level == 0);
            if (zeroLevelConfig != null && levelDistribution.TryGetValue(zeroLevelConfig, out int zeroCount) && zeroCount > 0)
            {
                // 在高压缩率情况下，限制0级敌人的升级数量
                int maxUpgradesFromZero = EliteRaidMod.compressionRatio >= 5 ? 
                    Math.Min(zeroCount, 2) : zeroCount; // 高压缩率时最多升级2个0级敌人

                // 寻找可以升级的最低等级
                var upgradeCandidates = validConfigs
                    .Where(c => levelDistribution.ContainsKey(c) && levelDistribution[c] > 0 && c.Level < EliteRaidMod.maxAllowLevel)
                    .OrderByDescending(c => c.CompressionRatio)
                    .ToList();

                foreach (var candidate in upgradeCandidates)
                {
                    int slotsPerUpgrade = candidate.CompressionRatio - zeroLevelConfig.CompressionRatio;
                    if (slotsPerUpgrade <= 0) continue;

                    int maxUpgrades = Math.Min(maxUpgradesFromZero, (originalCount - currentSlotsFilled) / slotsPerUpgrade);
                    if (maxUpgrades <= 0) continue;

                    levelDistribution[zeroLevelConfig] -= maxUpgrades;
                    levelDistribution[candidate] = levelDistribution.GetValueOrDefault(candidate, 0) + maxUpgrades;
                    currentSlotsFilled += maxUpgrades * slotsPerUpgrade;
                    currentElites = levelDistribution.Sum(e => e.Value);

                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Upgraded {maxUpgrades} x Level0 to Level{candidate.Level}.");
                    maxUpgradesFromZero -= maxUpgrades;

                    if (currentSlotsFilled >= originalCount || maxUpgradesFromZero <= 0) break;
                }
            }

            // 阶段2.2：多级升级策略（例如从Level1→Level3）
            for (int sourceLevel = 1; sourceLevel <= 5; sourceLevel++)
            {
                var sourceConfig = validConfigs.FirstOrDefault(c => c.Level == sourceLevel);
                if (sourceConfig == null || !levelDistribution.ContainsKey(sourceConfig) || levelDistribution[sourceConfig] <= 0)
                    continue;

                // 寻找最佳升级目标（最大化空间利用）
                var possibleTargets = validConfigs
                    .Where(c => c.Level > sourceLevel && c.Level <= EliteRaidMod.maxAllowLevel)
                    .ToList();

                foreach (var targetConfig in possibleTargets)
                {
                    int slotsPerUpgrade = targetConfig.CompressionRatio - sourceConfig.CompressionRatio;
                    if (slotsPerUpgrade <= 0) continue;

                    int maxUpgrades = Math.Min(
                        levelDistribution[sourceConfig],
                        (originalCount - currentSlotsFilled) / slotsPerUpgrade
                    );

                    if (maxUpgrades <= 0) continue;

                    levelDistribution[sourceConfig] -= maxUpgrades;
                    levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + maxUpgrades;
                    currentSlotsFilled += maxUpgrades * slotsPerUpgrade;
                    currentElites = levelDistribution.Sum(e => e.Value);

                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Upgraded {maxUpgrades} x Level{sourceLevel} to Level{targetConfig.Level}.");

                    if (currentSlotsFilled >= originalCount) break;
                }

                if (currentSlotsFilled >= originalCount) break;
            }

            return (currentSlotsFilled, currentElites);
        }

        // 阶段3: 降级高等级精英以调整数量（改进版）
        private static int DowngradeElitesToAdjustCount(
            List<EliteLevelConfig> validConfigs,
            Dictionary<EliteLevelConfig, int> levelDistribution,
            int currentSlotsFilled,
            int originalCount,
            int currentElites,
            int targetMinElites,
            int targetMaxElites)
        {
            if (currentElites >= targetMinElites)
                return currentElites;

            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Elite count ({currentElites}) too low. Downgrading high-level elites.");

            // 按压缩比排序，优先降级高压缩比精英
            foreach (var sourceConfig in validConfigs.OrderByDescending(c => c.CompressionRatio).Where(c => c.Level > 0))
            {
                if (!levelDistribution.TryGetValue(sourceConfig, out int sourceCount) || sourceCount <= 0)
                    continue;

                // 寻找最佳降级目标（最大化精英数量，但避免0级）
                var possibleTargets = validConfigs
                    .Where(c => c.Level < sourceConfig.Level && c.Level > 0 && c.CompressionRatio > 0) // 不降到0级
                    .ToList();

                foreach (var targetConfig in possibleTargets)
                {
                    // 计算降级后的精英数量变化
                    int elitesPerDowngrade = sourceConfig.CompressionRatio / targetConfig.CompressionRatio;
                    if (elitesPerDowngrade <= 1) continue; // 确保降级后精英数量增加

                    // 计算需要降级的数量
                    int neededElites = targetMinElites - currentElites;
                    int requiredDowngrades = (int)Math.Ceiling((double)neededElites / (elitesPerDowngrade - 1));
                    requiredDowngrades = Math.Min(requiredDowngrades, sourceCount);

                    if (requiredDowngrades <= 0) continue;

                    // 执行降级
                    levelDistribution[sourceConfig] -= requiredDowngrades;
                    levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + (requiredDowngrades * elitesPerDowngrade);

                    // 更新统计
                    currentSlotsFilled = levelDistribution.Sum(e => e.Key.CompressionRatio * e.Value);
                    currentElites = levelDistribution.Sum(e => e.Value);

                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Downgraded {requiredDowngrades} x Level{sourceConfig.Level} to Level{targetConfig.Level} ({elitesPerDowngrade}x).");
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Current elites: {currentElites}, Slots: {currentSlotsFilled}/{originalCount}");

                    if (currentElites >= targetMinElites)
                        return currentElites; // 达到目标下限，立即返回
                }
            }

            // 如果仍然不足，且压缩率不高，才使用0级精英补足
            var zeroLevelConfig = validConfigs.FirstOrDefault(c => c.Level == 0);
            if (zeroLevelConfig != null && currentElites < targetMinElites && EliteRaidMod.compressionRatio < 5)
            {
                int neededElites = targetMinElites - currentElites;
                int availableSlots = originalCount - currentSlotsFilled;
                int addCount = Math.Min(neededElites, availableSlots);

                if (addCount > 0)
                {
                    levelDistribution[zeroLevelConfig] = levelDistribution.GetValueOrDefault(zeroLevelConfig, 0) + addCount;
                    currentSlotsFilled += addCount;
                    currentElites += addCount;
                }
            }

            return currentElites;
        }

        // 新增：阶段4 - 最终优化，提升压缩比和空间利用率
        private static (int currentSlotsFilled, int currentElites) FinalOptimizeDistribution(
            List<EliteLevelConfig> validConfigs,
            Dictionary<EliteLevelConfig, int> levelDistribution,
            int currentSlotsFilled,
            int originalCount,
            int currentElites)
        {
            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Starting final optimization: {currentElites} elites, {currentSlotsFilled}/{originalCount} slots.");

            // 计算当前利用率
            double utilization = (double)currentSlotsFilled / originalCount;

            // 如果利用率已经很高，或者空间已经填满，无需优化
            if (utilization >= 0.95 || currentSlotsFilled >= originalCount)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization skipped: high utilization ({utilization:P2}).");
                return (currentSlotsFilled, currentElites);
            }

            // 策略1: 尝试将低等级精英升级为更高等级，提高压缩比
            // 优先选择那些升级后能增加空间利用率的精英
            var upgradeCandidates = validConfigs
                .Where(c => levelDistribution.ContainsKey(c) && levelDistribution[c] > 0 && c.Level < 7) // 不升级Boss等级
                .OrderByDescending(c => c.CompressionRatio) // 优先考虑高压缩比精英
                .ToList();

            foreach (var sourceConfig in upgradeCandidates)
            {
                int currentCount = levelDistribution[sourceConfig];
                if (currentCount <= 0) continue;

                // 寻找最佳升级目标（压缩比提升最大的）
                var possibleTargets = validConfigs
                    .Where(c => c.Level > sourceConfig.Level && c.Level <= 7)
                    .OrderByDescending(c => c.CompressionRatio)
                    .ToList();

                foreach (var targetConfig in possibleTargets)
                {
                    // 计算升级后节省的空间
                    int slotsSavedPerUpgrade = sourceConfig.CompressionRatio - targetConfig.CompressionRatio;
                    if (slotsSavedPerUpgrade <= 0) continue; // 升级后空间占用增加，跳过

                    // 计算可升级的最大数量
                    int maxUpgrades = Math.Min(
                        currentCount,
                        (originalCount - currentSlotsFilled) / slotsSavedPerUpgrade
                    );

                    if (maxUpgrades <= 0) continue;

                    // 执行升级
                    levelDistribution[sourceConfig] -= maxUpgrades;
                    levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + maxUpgrades;
                    currentSlotsFilled += maxUpgrades * slotsSavedPerUpgrade;
                    currentElites = levelDistribution.Sum(e => e.Value);

                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Upgraded {maxUpgrades} x Level{sourceConfig.Level} to Level{targetConfig.Level}.");

                    // 检查是否达到满利用率
                    if (currentSlotsFilled >= originalCount)
                    {
                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Reached full utilization ({currentSlotsFilled}/{originalCount}).");
                        return (currentSlotsFilled, currentElites);
                    }

                    // 重新计算利用率，决定是否继续升级
                    utilization = (double)currentSlotsFilled / originalCount;
                    if (utilization >= 0.95)
                    {
                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: High utilization achieved ({utilization:P2}).");
                        return (currentSlotsFilled, currentElites);
                    }

                    // 如果当前配置已升级完，跳出内层循环
                    if (levelDistribution[sourceConfig] <= 0) break;
                }
            }

            // 策略2: 如果空间利用率仍然很低，尝试降级部分精英以增加总数量
            if (utilization < 0.7)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Low utilization ({utilization:P2}), attempting downgrades.");

                // 优先选择压缩比高且数量少的精英进行降级
                var downgradeCandidates = validConfigs
                    .Where(c => levelDistribution.ContainsKey(c) && levelDistribution[c] > 0 && c.Level > 0)
                    .OrderByDescending(c => c.CompressionRatio)
                    .ThenBy(c => levelDistribution[c])
                    .ToList();

                foreach (var sourceConfig in downgradeCandidates)
                {
                    int currentCount = levelDistribution[sourceConfig];
                    if (currentCount <= 0) continue;

                    // 寻找最佳降级目标（压缩比适中，能增加数量的，但避免0级）
                    var possibleTargets = validConfigs
                        .Where(c => c.Level < sourceConfig.Level && c.Level > 0) // 不降到0级
                        .OrderBy(c => c.CompressionRatio)
                        .ToList();

                    foreach (var targetConfig in possibleTargets)
                    {
                        // 计算降级后增加的精英数量
                        int elitesAddedPerDowngrade = sourceConfig.CompressionRatio / targetConfig.CompressionRatio - 1;
                        if (elitesAddedPerDowngrade <= 0) continue;

                        // 计算可降级的数量
                        int maxDowngrades = currentCount;
                        if (maxDowngrades <= 0) continue;

                        // 执行降级
                        levelDistribution[sourceConfig] -= maxDowngrades;
                        levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + (maxDowngrades * elitesAddedPerDowngrade);
                        currentElites = levelDistribution.Sum(e => e.Value);

                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Downgraded {maxDowngrades} x Level{sourceConfig.Level} to Level{targetConfig.Level}.");

                        // 重新计算利用率
                        currentSlotsFilled = levelDistribution.Sum(e => e.Key.CompressionRatio * e.Value);
                        utilization = (double)currentSlotsFilled / originalCount;

                        // 如果利用率提高到可接受范围，停止降级
                        if (utilization >= 0.8)
                        {
                            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Utilization improved to {utilization:P2}.");
                            return (currentSlotsFilled, currentElites);
                        }

                        // 如果当前配置已降级完，跳出内层循环
                        if (levelDistribution[sourceConfig] <= 0) break;
                    }
                }
            }

            // 策略3: 用0级精英填充剩余空间（但限制使用）
            if (currentSlotsFilled < originalCount)
            {
                var zeroLevelConfig = validConfigs.FirstOrDefault(c => c.Level == 0);
                if (zeroLevelConfig != null)
                {
                    // 在高压缩率情况下，限制0级敌人的使用
                    int maxZeroLevelToAdd = EliteRaidMod.compressionRatio >= 5 ? 
                        Math.Min(originalCount - currentSlotsFilled, 3) : // 高压缩率时最多添加3个0级敌人
                        originalCount - currentSlotsFilled;
                    
                    int addCount = maxZeroLevelToAdd;
                    
                    if (addCount > 0)
                    {
                        levelDistribution[zeroLevelConfig] = levelDistribution.GetValueOrDefault(zeroLevelConfig, 0) + addCount;
                        currentSlotsFilled = originalCount;
                        currentElites = levelDistribution.Sum(e => e.Value);

                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization: Added {addCount} x Level0 to fill remaining space.");
                    }
                }
            }

            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Final optimization results: {currentElites} elites, {currentSlotsFilled}/{originalCount} slots, utilization {utilization:P2}.");
            return (currentSlotsFilled, currentElites);
        }
        // 新增：阶段5 - 调整精英分布，确保空间利用率在合理范围内
        private static (int currentSlotsFilled, int currentElites) AdjustDistributionToTargetUtilization(
            List<EliteLevelConfig> validConfigs,
            Dictionary<EliteLevelConfig, int> levelDistribution,
            int currentSlotsFilled,
            int originalCount)
        {
            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Starting phase 5: Adjusting distribution to target utilization. Current slots: {currentSlotsFilled}/{originalCount}.");

            // 计算当前利用率
            double utilization = (double)currentSlotsFilled / originalCount;

            // 目标利用率范围
            const double minUtilization = 0.90;  // 90%
            const double maxUtilization = 1.10;  // 110%

            // 如果已经在目标范围内，无需调整
            if (utilization >= minUtilization && utilization <= maxUtilization)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Utilization already within target range ({utilization:P2}).");
                return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
            }

            // 如果利用率超过上限，需要降级一些精英以减少空间占用
            if (utilization > maxUtilization)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Utilization too high ({utilization:P2}). Adjusting down...");

                // 计算需要减少的空间
                int targetSlots = (int)(originalCount * maxUtilization);
                int slotsToReduce = currentSlotsFilled - targetSlots;

                if (slotsToReduce <= 0)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Calculated slots to reduce is non-positive. Skipping adjustment.");
                    return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
                }

                // 策略：优先降级高等级精英，从压缩比最高的开始
                var downgradeCandidates = validConfigs
                    .Where(c => levelDistribution.ContainsKey(c) && levelDistribution[c] > 0 && c.Level > 0)
                    .OrderByDescending(c => c.CompressionRatio)
                    .ToList();

                foreach (var sourceConfig in downgradeCandidates)
                {
                    int currentCount = levelDistribution[sourceConfig];
                    if (currentCount <= 0) continue;

                    // 寻找合适的降级目标（压缩比低于当前的）
                    var possibleTargets = validConfigs
                        .Where(c => c.Level < sourceConfig.Level && c.CompressionRatio < sourceConfig.CompressionRatio)
                        .ToList();

                    foreach (var targetConfig in possibleTargets)
                    {
                        // 计算每个降级操作可以减少的空间
                        int slotsReducedPerDowngrade = sourceConfig.CompressionRatio - targetConfig.CompressionRatio;
                        if (slotsReducedPerDowngrade <= 0) continue;

                        // 计算需要降级的数量
                        int maxDowngrades = Math.Min(currentCount, slotsToReduce / slotsReducedPerDowngrade);
                        if (maxDowngrades <= 0) continue;

                        // 执行降级
                        levelDistribution[sourceConfig] -= maxDowngrades;
                        levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + maxDowngrades;

                        // 更新统计
                        currentSlotsFilled -= maxDowngrades * slotsReducedPerDowngrade;
                        slotsToReduce -= maxDowngrades * slotsReducedPerDowngrade;

                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Downgraded {maxDowngrades} x Level{sourceConfig.Level} to Level{targetConfig.Level} to reduce space.");

                        // 如果已经达到目标，停止降级
                        if (currentSlotsFilled <= targetSlots)
                        {
                            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Reached target utilization ({(double)currentSlotsFilled / originalCount:P2}).");
                            return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
                        }

                        // 如果当前配置已降级完，跳出内层循环
                        if (levelDistribution[sourceConfig] <= 0) break;
                    }

                    // 如果已经减少了足够的空间，停止调整
                    if (slotsToReduce <= 0) break;
                }

                // 重新计算当前状态
                currentSlotsFilled = levelDistribution.Sum(e => e.Key.CompressionRatio * e.Value);
                utilization = (double)currentSlotsFilled / originalCount;

                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: After downgrades, utilization is now {utilization:P2}.");
            }
            // 如果利用率低于下限，需要升级一些精英以增加空间占用
            else if (utilization < minUtilization)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Utilization too low ({utilization:P2}). Adjusting up...");

                // 计算需要增加的空间
                int targetSlots = (int)(originalCount * minUtilization);
                int slotsToAdd = targetSlots - currentSlotsFilled;

                if (slotsToAdd <= 0)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Calculated slots to add is non-positive. Skipping adjustment.");
                    return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
                }

                // 策略：优先升级低等级精英，从压缩比最低的开始
                var upgradeCandidates = validConfigs
                    .Where(c => levelDistribution.ContainsKey(c) && levelDistribution[c] > 0 && c.Level < EliteRaidMod.maxAllowLevel)
                    .OrderBy(c => c.CompressionRatio)
                    .ToList();

                foreach (var sourceConfig in upgradeCandidates)
                {
                    int currentCount = levelDistribution[sourceConfig];
                    if (currentCount <= 0) continue;

                    // 寻找合适的升级目标（压缩比高于当前的）
                    var possibleTargets = validConfigs
                        .Where(c => c.Level > sourceConfig.Level && c.CompressionRatio > sourceConfig.CompressionRatio)
                        .ToList();

                    foreach (var targetConfig in possibleTargets)
                    {
                        // 计算每个升级操作可以增加的空间
                        int slotsAddedPerUpgrade = targetConfig.CompressionRatio - sourceConfig.CompressionRatio;
                        if (slotsAddedPerUpgrade <= 0) continue;

                        // 计算需要升级的数量
                        int maxUpgrades = Math.Min(currentCount, slotsToAdd / slotsAddedPerUpgrade);
                        if (maxUpgrades <= 0) continue;

                        // 执行升级
                        levelDistribution[sourceConfig] -= maxUpgrades;
                        levelDistribution[targetConfig] = levelDistribution.GetValueOrDefault(targetConfig, 0) + maxUpgrades;

                        // 更新统计
                        currentSlotsFilled += maxUpgrades * slotsAddedPerUpgrade;
                        slotsToAdd -= maxUpgrades * slotsAddedPerUpgrade;

                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Upgraded {maxUpgrades} x Level{sourceConfig.Level} to Level{targetConfig.Level} to increase space.");

                        // 如果已经达到目标，停止升级
                        if (currentSlotsFilled >= targetSlots)
                        {
                            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: Reached target utilization ({(double)currentSlotsFilled / originalCount:P2}).");
                            return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
                        }

                        // 如果当前配置已升级完，跳出内层循环
                        if (levelDistribution[sourceConfig] <= 0) break;
                    }

                    // 如果已经增加了足够的空间，停止调整
                    if (slotsToAdd <= 0) break;
                }

                // 重新计算当前状态
                currentSlotsFilled = levelDistribution.Sum(e => e.Key.CompressionRatio * e.Value);
                utilization = (double)currentSlotsFilled / originalCount;

                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Phase 5: After upgrades, utilization is now {utilization:P2}.");
            }

            return (currentSlotsFilled, levelDistribution.Sum(e => e.Value));
        }
        // 根据等级获取精英配置
        public static EliteLevelConfig GetEliteLevelConfigByLevel(int level)
        {
            // 检查当前难度配置
            if (difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var configs))
            {
                // 查找匹配等级的配置
                var config = configs.FirstOrDefault(c => c.Level == level);
                if (config != null)
                    return config;
            }

            // 如果没找到，尝试从任何难度配置中获取
            foreach (var difficultyConfig in difficultyConfigs.Values)
            {
                var config = difficultyConfig.FirstOrDefault(c => c.Level == level);
                if (config != null)
                    return config;
            }

            // 默认返回0级配置
            return new EliteLevelConfig
            {
                Level = 0,
                DamageFactor = 1f,
                MoveSpeedFactor = 1f,
                ScaleFactor = 1f,
                MaxTraits = 0,
                IsBoss = false,
                CompressionRatio = 1
            };
        }

        // 根据等级获取精英级别实例
        public static EliteLevel GetEliteLevelByLevel(int level)
        {
            var config = GetEliteLevelConfigByLevel(level);

            // 创建并返回EliteLevel实例
            return new EliteLevel(
                config.Level,
                config.DamageFactor,
                config.MoveSpeedFactor,
                config.ScaleFactor,
                config.MaxTraits,
                config.IsBoss
            );
        }
        public static EliteLevel GetRandomEliteLevel()
        {
            if (EliteRaidMod.maxAllowLevel == 0)
            {
                return new EliteLevel(0, 1, 1, 1, 0, false);
            }
            var availableToPick = currentLevelDistribution.Where(kvp => kvp.Value > 0).ToList();

            if (!availableToPick.Any())
            {
                if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GetRandomEliteLevel: No elites with count > 0 in distribution.");
                if (lastResortConfig != null)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Using lastResortConfig (L{lastResortConfig.Level}) to create EliteLevel.");
                    return new EliteLevel(
                        lastResortConfig.Level, lastResortConfig.DamageFactor, lastResortConfig.MoveSpeedFactor,
                        lastResortConfig.ScaleFactor, lastResortConfig.MaxTraits, lastResortConfig.IsBoss
                    );
                }

                if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] No lastResortConfig available. Falling back to absolute default L1 EliteLevel.");
                // Attempt to get a L1 config from current difficulty as a better fallback
                if (difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var currentDiffConfigs))
                {
                    var l1Config = currentDiffConfigs.FirstOrDefault(c => c.Level == 1);
                    if (l1Config != null)
                    {
                        return new EliteLevel(l1Config.Level, l1Config.DamageFactor, l1Config.MoveSpeedFactor,
                                              l1Config.ScaleFactor, l1Config.MaxTraits, l1Config.IsBoss);
                    }
                }
                // Absolute hardcoded fallback if all else fails
                return new EliteLevel(1, 0.7f, 1.0f, 1.0f, 1, false);
            }

            List<EliteLevelConfig> selectionPool = new List<EliteLevelConfig>();
            foreach (var kvp in availableToPick)
            {
                for (int i = 0; i < kvp.Value; i++) // Add one entry for each count
                {
                    selectionPool.Add(kvp.Key);
                }
            }

            // This check should ideally not be needed if availableToPick.Any() is true
            if (!selectionPool.Any())
            {
                if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] GetRandomEliteLevel: Selection pool became empty unexpectedly. This indicates a logic error. Using fallback.");
                // Repeating fallback logic from above for safety
                if (lastResortConfig != null) { /* ... same as above ... */ }
                return new EliteLevel(1, 0.7f, 1.0f, 1.0f, 1, false);
            }


            int randomIndex = ThreadSafeRandom.Next(selectionPool.Count);
            EliteLevelConfig selectedConfig = selectionPool[randomIndex];

            // Decrement the count in the original dictionary
            if (currentLevelDistribution.ContainsKey(selectedConfig))
            {
                currentLevelDistribution[selectedConfig]--;
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] Selected EliteLevel L{selectedConfig.Level}. Remaining for this level: {currentLevelDistribution[selectedConfig]}.");
            } else
            {
                // This should not happen if selectedConfig came from currentLevelDistribution
              //  if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] GetRandomEliteLevel: selectedConfig (L{selectedConfig.Level}) not found in currentLevelDistribution after selection. This is a bug.");
            }

            lastResortConfig = selectedConfig; // Update lastResortConfig to the one just picked

            EliteLevel eliteLevel = new EliteLevel(
                selectedConfig.Level, selectedConfig.DamageFactor, selectedConfig.MoveSpeedFactor,
                selectedConfig.ScaleFactor, selectedConfig.MaxTraits, selectedConfig.IsBoss
            );
            //eliteLevel.GenerateRandomTraits();
            return eliteLevel;
        }

        public static int getCurrentLevelDistributionNum()
        {
            if (EliteRaidMod.maxAllowLevel == 0)
            {
                return 0;
            }
            int totalElites = 0;
            if (currentLevelDistribution != null)
            {
                // Sum only positive counts, as 0 means "no more to spawn from this config"
                totalElites = currentLevelDistribution.Values.Where(count => count > 0).Sum();
            }
            return totalElites;
        }
        public static Dictionary<EliteLevelConfig, int> GetCurrentDistribution()
        {
            var result = new Dictionary<EliteLevelConfig, int>();
            foreach (var entry in currentLevelDistribution)
            {
                result[entry.Key] = entry.Value;
            }
            return result;
        }

        // 添加公共方法以获取当前可用的精英等级配置
        public static List<EliteLevelConfig> GetAvailableLevels()
        {
            var availableConfigs = new List<EliteLevelConfig>();

            if (difficultyConfigs.TryGetValue(EliteRaidMod.eliteRaidDifficulty, out var configs))
            {
                availableConfigs.AddRange(configs);
            }

            return availableConfigs;
        }

        public static int GetenhancePawnNumber(int baseNum)
        {
            return General.GetenhancePawnNumber(baseNum);
        }

        public static float GetcompressionRatio(int baseNum, int maxPawnNum)
        {
            if (maxPawnNum <= 0) return 1f;
            return (float)baseNum / maxPawnNum;
        }

        // 添加调试方法：验证0级敌人生成控制效果
        public static void TestZeroLevelGeneration()
        {
            if (!EliteRaidMod.displayMessageValue) return;
            
            if (EliteRaidMod.displayMessageValue) Log.Message("=== 0级敌人生成控制测试 ===");
            
            // 测试不同压缩率下的0级敌人生成
            var testCases = new[]
            {
                new { CompressionRatio = 3f, MaxRaidEnemy = 30, Description = "低压缩率" },
                new { CompressionRatio = 5f, MaxRaidEnemy = 50, Description = "中压缩率" },
                new { CompressionRatio = 8f, MaxRaidEnemy = 70, Description = "高压缩率" },
                new { CompressionRatio = 10f, MaxRaidEnemy = 100, Description = "极高压缩率" }
            };
            
            foreach (var testCase in testCases)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"--- {testCase.Description} 测试 (压缩率={testCase.CompressionRatio}, 最大袭击人数={testCase.MaxRaidEnemy}) ---");
                
                // 临时设置参数
                EliteRaidMod.compressionRatio = testCase.CompressionRatio;
                EliteRaidMod.maxRaidEnemy = testCase.MaxRaidEnemy;
                EliteRaidMod.useCompressionRatio = true;
                
                // 测试不同原始袭击人数
                int[] testNumbers = { 100, 200, 500, 1000 };
                foreach (int originalCount in testNumbers)
                {
                    // 生成分布
                    GenerateLevelDistribution(originalCount);
                    
                    // 统计0级敌人数量
                    var zeroLevelConfig = GetEliteLevelConfigByLevel(0);
                    int zeroLevelCount = currentLevelDistribution.TryGetValue(zeroLevelConfig, out int count) ? count : 0;
                    int totalElites = currentLevelDistribution.Sum(e => e.Value);
                    double zeroLevelRatio = totalElites > 0 ? (double)zeroLevelCount / totalElites : 0;
                    
                    if (EliteRaidMod.displayMessageValue) Log.Message($"  原始{originalCount}人 → 0级敌人{zeroLevelCount}/{totalElites} ({zeroLevelRatio:P1})");
                }
                if (EliteRaidMod.displayMessageValue) Log.Message("");
            }
            
            if (EliteRaidMod.displayMessageValue) Log.Message("=== 0级敌人生成控制测试结束 ===");
        }
    }

    public static class ThreadSafeRandom
    {
        private static readonly Random global = new Random();
        [ThreadStatic]
        private static Random local;

        public static int Next()
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global) seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.Next();
        }

        public static int Next(int maxValue)
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global) seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.Next(maxValue);
        }

        public static int Next(int a, int maxValue)
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global) seed = global.Next();
                local = inst = new Random(seed);
            }
            return a + inst.Next(maxValue - a);
        }

        public static double NextDouble()
        {
            Random inst = local;
            if (inst == null)
            {
                int seed;
                lock (global) seed = global.Next();
                local = inst = new Random(seed);
            }
            return inst.NextDouble();
        }

    }

    public static class DictionaryExtensions
    {
        /// <summary>
        /// 获取指定键的值（若不存在则返回默认值），适配 .NET 4.7.2。
        /// </summary>
        /// <typeparam name="TKey">键的类型</typeparam>
        /// <typeparam name="TValue">值的类型</typeparam>
        /// <param name="dictionary">目标字典</param>
        /// <param name="key">要查找的键</param>
        /// <returns>存在则返回对应值，否则返回默认值 <see cref="default(TValue)"/></returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.GetValueOrDefault(key, default(TValue));
        }

        /// <summary>
        /// 获取指定键的值（若不存在则返回自定义默认值），适配 .NET 4.7.2。
        /// </summary>
        /// <typeparam name="TKey">键的类型</typeparam>
        /// <typeparam name="TValue">值的类型</typeparam>
        /// <param name="dictionary">目标字典</param>
        /// <param name="key">要查找的键</param>
        /// <param name="defaultValue">自定义默认值</param>
        /// <returns>存在则返回对应值，否则返回 <paramref name="defaultValue"/></returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary), "Dictionary cannot be null.");
            }
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}