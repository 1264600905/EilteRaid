// 精英等级配置类
using System.Collections.Generic;
using System;
using System.Linq;
using Verse;
using System.Threading;

namespace EliteRaid
{
    public class EliteLevel
    {
        public int Level { get; }
        public float DamageFactor { get; }    // 承伤系数
        public float MoveSpeedFactor { get; } // 移速系数
        public float ScaleFactor { get; }    // 体型系数

        public int MaxTraits { get; }        // 最大词条数
        public bool IsBoss { get; }          // 是否为Boss

        //精英化词条相关开关
        //植入体强化
        public bool addBioncis = false;
        //药物强化
        public bool addDrug = false;
        //装备强化
        public bool refineGear = false;
        // 是否允许护甲增强
        public bool armorEnhance = false;
        //允许温度抗性强化
        public bool temperatureResist = false;
        //近战强化
        public bool meleeEnhance = false;
        //远程强化
        public bool rangedSpeedEnhance = false;
        //移速强化
        public bool moveSpeedEnhance = false;
        //巨人化
        public bool giantEnhance = false;
        //身体部件强化
        public bool bodyPartEnhance = false;
        //无痛强化
        public bool painEnhance = false;
        //移速降低抵抗
        public bool moveSpeedResist = false;
        //意识强化
        public bool consciousnessEnhance = false;

        // 声明线程安全的随机数生成器（静态且线程本地）
        private static readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(
            () => new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId)
        );

        // 使用线程本地的 Random 实例
        private Random myRandom => threadLocalRandom.Value;

        // 构造函数中初始化词条开关（示例：Boss强制开启部分词条）
        public EliteLevel(int level, float damageFactor, float moveSpeedFactor,
                          float scaleFactor, int maxTraits,
                          bool isBoss = false)
        {
            Level = level;
            DamageFactor = damageFactor;
            MoveSpeedFactor = moveSpeedFactor;
            ScaleFactor = scaleFactor;
            MaxTraits = maxTraits;
            IsBoss = isBoss;

            GenerateRandomTraits(); // 在构造函数中调用生成随机词条方法
        }

        /// <summary>
        /// 根据全局配置随机生成精英词条组合
        /// </summary>
        public Dictionary<string, bool> GenerateRandomTraits()
        {
            // 直接列出全局配置映射（避免字符串匹配）
            var globalTraitMap = new Dictionary<string, Func<bool>>
    {
        { nameof(addBioncis), () => EliteRaidMod.AllowAddBioncis },    
        { nameof(addDrug), () => EliteRaidMod.AllowAddDrug },
        { nameof(refineGear), () => true },
        { nameof(armorEnhance), () => EliteRaidMod.AllowArmorEnhance },
        { nameof(temperatureResist), () => EliteRaidMod.AllowTemperatureResist },
        { nameof(meleeEnhance), () => EliteRaidMod.AllowMeleeEnhance },
        { nameof(rangedSpeedEnhance), () => EliteRaidMod.AllowRangedSpeedEnhance },
        { nameof(moveSpeedEnhance), () => EliteRaidMod.AllowMoveSpeedEnhance },
        { nameof(giantEnhance), () => EliteRaidMod.AllowGiantEnhance },
        { nameof(bodyPartEnhance), () => EliteRaidMod.AllowBodyPartEnhance },
        { nameof(painEnhance), () => EliteRaidMod.AllowPainEnhance },
        { nameof(moveSpeedResist), () => EliteRaidMod.AllowMoveSpeedResist },
        { nameof(consciousnessEnhance), () => EliteRaidMod.AllowConsciousnessEnhance }
    };

            // 筛选全局允许的词条
            List<string> allowedTraits = new List<string>();
            foreach (var trait in globalTraitMap)
            {
                if (trait.Value())
                {
                    allowedTraits.Add(trait.Key);
                }
            }

            // 结合本地限制（如Boss强制词条）
            List<string> validTraits = new List<string>();
            foreach (string trait in allowedTraits)
            {
                // 这里应添加实际的本地限制逻辑，当前代码未实现
                validTraits.Add(trait);
            }

            // 确保至少有一个特性可用
            if (!validTraits.Any())
            {
                Console.WriteLine($"警告: 精英等级 {Level} 没有可用的特性！使用默认特性。");
                validTraits = new List<string> { nameof(addBioncis) };
            }

            // 计算要生成的特性数量（直接使用MaxTraits，但不超过可用特性数量）
            int traitCount = Math.Min(MaxTraits, validTraits.Count);

            // 随机选择特性
            List<string> shuffledTraits = new List<string>(validTraits);
            ShuffleList(shuffledTraits);
            List<string> selectedTraits = shuffledTraits.Take(traitCount).ToList();

            // 生成最终词条字典
            Dictionary<string, bool> traits = new Dictionary<string, bool>();
            foreach (string trait in globalTraitMap.Keys)
            {
                traits.Add(trait, false);
            }

            foreach (string trait in selectedTraits)
            {
                traits[trait] = true;
            }

            foreach (string trait in selectedTraits)
            {
                switch (trait)
                {
                    case nameof(addBioncis):
                        addBioncis = true;
                        break;
                    case nameof(addDrug):
                        addDrug = true;
                        break;
                    case nameof(refineGear):
                        refineGear = true;
                        break;
                    case nameof(armorEnhance):
                        armorEnhance = true;
                        break;
                    case nameof(temperatureResist):
                        temperatureResist = true;
                        break;
                    case nameof(meleeEnhance):
                        meleeEnhance = true;
                        break;
                    case nameof(rangedSpeedEnhance):
                        rangedSpeedEnhance = true;
                        break;
                    case nameof(moveSpeedEnhance):
                        moveSpeedEnhance = true;
                        break;
                    case nameof(giantEnhance):
                        giantEnhance = true;
                        break;
                    case nameof(bodyPartEnhance):
                        bodyPartEnhance = true;
                        break;
                    case nameof(painEnhance):
                        painEnhance = true;
                        break;
                    case nameof(moveSpeedResist):
                        moveSpeedResist = true;
                        break;
                    case nameof(consciousnessEnhance):
                        consciousnessEnhance = true;
                        break;
                    default:
                        Log.Error($"[EliteRaid] 未知词条: {trait}");
                        break;
                }
            }

            // 调试输出
            //if (EliteRaidMod.displayMessageValue) 
            //Log.Message($"[EliteRaid] 为精英等级 {Level} 生成了 {selectedTraits.Count} 个词条（最大: {MaxTraits}，可用: {validTraits.Count}）");

            return traits;
        }
        /// <summary>
        /// Fisher-Yates 洗牌算法
        /// </summary>
        private void ShuffleList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = myRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}