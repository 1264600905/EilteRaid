
using EliteRaid;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.Widgets;


namespace EliteRaid
{
    // 新增：总难度枚举（建议放在EliteRaidDifficulty同级）
    public enum TotalDifficultyLevel
    {
        Knight,         // 骑士
        Justiciar,     // 法务官
        Lord,          // 领主
        Governor,      // 执政总督（原索引4，现索引3）
        Viceroy,       // 总督（原索引3，现索引4）
        Duke,          // 都督
        Despot,        // 专制公
        GrandDuke,     // 大公
        General,       // 将军
        GalaxyDominator// 星系主宰
    }

    public class EliteRaidMod : Mod
    {
        //显示部分日志
        public static bool displayMessageValue = false;

        public static bool modEnabled = true; // 是否启用Mod
        public static bool allowRaidFriendlyValue = true; // 是否允许友好突袭精英化
        public static bool allowDropPodRaidValue = true; // 新增：是否允许精英化空投袭击
        public static bool allowMechanoidsValue = true;
        public static bool allowInsectoidsValue = true;
        public static bool allowEntitySwarmValue = true;
        public static bool allowAnimalsValue = true; // 是否允许动物精英化
        public static int  maxAllowLevel = 7; // 最大允许精英化等级
        public static int maxRaidEnemy = 20; // 最大允许突袭敌人数量
        public static EliteRaidDifficulty eliteRaidDifficulty =EliteRaidDifficulty.Normal; // 总难度等级
        public static bool mapGeneratedEnemyEnhanced=false; // 是否允许地图生成敌人增强
        public static bool useCompressionRatio=true;
        public static float compressionRatio=3f;
        public static bool enableSilverDrop=true;
        public static int silverDropPerLevel=50;
        public static int maxRaidPoint = 10000; // 新增：最大袭击点数上限（默认10000）
        public static bool showDetailConfig = false;
        public static float raidScale = 1.0f;  //游戏内袭击倍率
        //植入体强化
        // 新增：全局词条开关（与EliteLevel中的开关对应）
        public static bool AllowAddBioncis = true;   
        public static bool AllowAddDrug = true;
        public static bool AllowRefineGear = true;
        public static bool AllowArmorEnhance = true;
        public static bool AllowTemperatureResist = true;
        public static bool AllowMeleeEnhance = false;
        public static bool AllowRangedSpeedEnhance = true;
        public static bool AllowMoveSpeedEnhance = true;
        public static bool AllowGiantEnhance = false;
        public static bool AllowBodyPartEnhance = false;
        public static bool AllowPainEnhance = false;
        public static bool AllowMoveSpeedResist = true;
        public static bool AllowConsciousnessEnhance = true;


        // 新增：难度配置数据（建议在EliteRaidMod类中声明为静态）
        private static readonly Dictionary<int, (float compressionRatio, int maxRaidPoint, EliteRaidDifficulty difficulty)> DifficultyConfig =
            new Dictionary<int, (float, int, EliteRaidDifficulty)>
            {
        {0, (3f, 10000, EliteRaidDifficulty.Normal)},
        {1, (3f, 10000, EliteRaidDifficulty.Hard)},
        {2, (3f, 10000, EliteRaidDifficulty.Extreme)},
        {3, (3f, 20000, EliteRaidDifficulty.Normal)},
        {4, (3f, 15000, EliteRaidDifficulty.Extreme)},
        {5, (3f, 30000, EliteRaidDifficulty.Hard)},
        {6, (4f, 40000, EliteRaidDifficulty.Extreme)},
        {7, (6f, 60000, EliteRaidDifficulty.Extreme)},
        {8, (8f, 80000, EliteRaidDifficulty.Extreme)},
        {9, (10f, 100000, EliteRaidDifficulty.Extreme)}
            };

        private static List<string> DifficultyOptions= new List<string>
{
    "<color=#39FF14>骑士</color> 难度系数1",
    "<color=#FFA500>法务官</color> 难度系数1.3",
    "<color=#B0E2FF>领主</color> 难度系数1.6",
    "<color=#FF6EC7>执政总督</color> 难度系数2", // 原索引4，现索引3
    "<color=#00FFFF>总督</color> 难度系数2.4", // 原索引3，现索引4
    "<color=#FF3030>都督</color> 难度系数3.9",
    "<color=#1E90FF>专制公</color> 难度系数6.4",
    "<color=#7FFF00>大公</color> 难度系数10",
    "<color=#FFD700>将军</color> 难度系数13",
    "<color=#FF6347>星系主宰</color> 难度系数16" 
};

        // 更新难度选项列表生成方法
        private static List<string> GenerateDifficultyOptions()
        {
            return new List<string>
    {
        "<color=#39FF14>" + "Difficulty_Knight".Translate() + "</color> " + "DifficultyFactor_1".Translate(),
        "<color=#FFA500>" + "Difficulty_Justiciar".Translate() + "</color> " + "DifficultyFactor_1_3".Translate(),
        "<color=#B0E2FF>" + "Difficulty_Lord".Translate() + "</color> " + "DifficultyFactor_1_6".Translate(),
        "<color=#00FFFF>" + "Difficulty_Viceroy".Translate() + "</color> " + "DifficultyFactor_2_4".Translate(),
        "<color=#FF6EC7>" + "Difficulty_Governor".Translate() + "</color> " + "DifficultyFactor_2".Translate(),
        "<color=#FF3030>" + "Difficulty_Duke".Translate() + "</color> " + "DifficultyFactor_3_9".Translate(),
        "<color=#1E90FF>" + "Difficulty_Despot".Translate() + "</color> " + "DifficultyFactor_6_4".Translate(),
        "<color=#7FFF00>" + "Difficulty_GrandDuke".Translate() + "</color> " + "DifficultyFactor_10".Translate(),
        "<color=#FFD700>" + "Difficulty_General".Translate() + "</color> " + "DifficultyFactor_13".Translate(),
        "<color=#FF6347>" + "Difficulty_GalaxyDominator".Translate() + "</color> " + "DifficultyFactor_16".Translate()
    };
        }


        public static bool AllowCompress(Pawn pawn, bool isDropPodRaid = false)
        {
            if (!allowMechanoidsValue)
            {
                bool isMechanoid = pawn?.RaceProps?.IsMechanoid ?? false;
                if (isMechanoid)
                {
                    return false;
                }
            }
            if (!allowInsectoidsValue)
            {
                bool isInsectoid = pawn?.RaceProps?.FleshType == FleshTypeDefOf.Insectoid;
                if (isInsectoid)
                {
                    return false;
                }
            }
            bool isEntitySwarm = pawn?.RaceProps.FleshType == FleshTypeDefOf.Fleshbeast ||
                                 pawn?.RaceProps.FleshType == FleshTypeDefOf.EntityMechanical ||
                                 pawn?.RaceProps.FleshType == FleshTypeDefOf.EntityFlesh;

            if (!allowEntitySwarmValue && isEntitySwarm)
            {
                return false;
            }

            // 检查是否允许精英化空投袭击
            if (isDropPodRaid && !allowDropPodRaidValue)
            {
              //  Log.Message($"[EliteRaid] 不允许精英化空投袭击，跳过 Pawn: {pawn.Name}");
                return false;
            }

            // 检查友军压缩设置
            if (!pawn.HostileTo(Faction.OfPlayer) && !allowRaidFriendlyValue)
            {
              //  Log.Message($"[EliteRaid] 不允许压缩友军，跳过 Pawn: {pawn.Name}");
                return false;
            }
            return true;
        }

        public static bool AllowCompress(IncidentParms parms)
        {
            bool hostileRaid = FactionUtility.HostileTo(Faction.OfPlayer, parms.faction);
            if (hostileRaid)
            {
                return true;
            } else
            {
                if (EliteRaidMod.allowRaidFriendlyValue)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AllowCompress(PawnGroupMakerParms parms, out bool raidFriendly)
        {
            raidFriendly = false;
            bool hostileRaid = FactionUtility.HostileTo(Faction.OfPlayer, parms.faction);
            if (hostileRaid)
            {
                return true;
            } else
            {
                if (!EliteRaidMod.allowRaidFriendlyValue)
                {
                    return false;
                }
                bool isRaidFriendly = parms.groupKind == PawnGroupKindDefOf.Combat;
                if (isRaidFriendly)
                {
                    raidFriendly = true;
                    return true;
                }
            }
            return false;
        }


        //UI部分
        public EliteRaidSettings settings;

        private const float LABEL_WIDTH = 180f;
        private const float GAP_SMALL = 12f;
        private const float GAP_LARGE = 24f;
        private const float ROW_HEIGHT = 30f;
        public EliteRaidMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<EliteRaidSettings>();

            SyncSettingsToStaticFields();
        }

        public override string SettingsCategory()
        {
            return "EliteRaidSettingsCategory".Translate();
        }


        private float CalculateContentHeight(float width)
        {
            float height = 15f; // 起始偏移量

            // 基础设置部分
            height += ROW_HEIGHT + GAP_LARGE; // 标题
            height += (ROW_HEIGHT + GAP_SMALL) * 2; // 两个开关
            height += GAP_LARGE;

            // 袭击类型允许部分
            height += ROW_HEIGHT + GAP_LARGE; // 标题
            height += (ROW_HEIGHT + GAP_SMALL) * 6; // 六个开关
            height += GAP_LARGE;

            // 数值限制部分
            height += ROW_HEIGHT + GAP_LARGE; // 标题
            height += ROW_HEIGHT + GAP_SMALL; // 白银开关

            // 白银数量滑块（仅当启用时）
            if (settings.enableSilverDrop)
            {
                height += ROW_HEIGHT + GAP_SMALL;
            }

            // 总难度下拉框
            height += ROW_HEIGHT + GAP_LARGE;

            // 显示详细配置开关
            height += ROW_HEIGHT + GAP_SMALL;

            // 详细配置内容（仅当启用时）
            if (settings.showDetailConfig)
            {
                height += ROW_HEIGHT + GAP_LARGE; // 难度滑块
                height += ROW_HEIGHT + GAP_SMALL; // 最大精英等级
                height += ROW_HEIGHT + GAP_SMALL; // 压缩倍率/最大敌人数量
                height += ROW_HEIGHT + GAP_SMALL; // 最大袭击点数
            }

            height += GAP_LARGE; // 底部间距
            height += ROW_HEIGHT; // 重置按钮高度

            return height;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 计算内容高度
            float contentHeight = CalculateContentHeight(inRect.width);

            // 使用计算出的内容高度创建滚动视图
            Rect viewRect = new Rect(0, 0, inRect.width - 40f, contentHeight+200);
            Widgets.BeginScrollView(inRect, ref settings.scrollPosition, viewRect);

            float y = 15f;

            // ====== 基础设置 ======
            DrawSectionTitle(ref y, viewRect.width, "BasicSettings".Translate());
            y += GAP_LARGE;

            // 启用Mod开关
            DrawCheckbox(ref y, viewRect.width, "modEnabled", "modEnabled".Translate(),
                ref settings.modEnabled, "modEnabledDesc".Translate());

            // 显示日志开关
            DrawCheckbox(ref y, viewRect.width, "displayMessageValue", "displayMessageValue".Translate(),
                ref settings.displayMessageValue, "displayMessageValueDesc".Translate());

            y += GAP_LARGE;

            // ====== 袭击类型允许 ======
            DrawSectionTitle(ref y, viewRect.width, "AllowedPawnTypes".Translate());
            y += GAP_LARGE;

            // 友好突袭开关
            DrawCheckbox(ref y, viewRect.width, "allowRaidFriendlyValue", "allowRaidFriendlyValue".Translate(),
                ref settings.allowRaidFriendlyValue, "allowRaidFriendlyValueDesc".Translate());

            // 新增：允许精英化空投袭击
            DrawCheckbox(ref y, viewRect.width, "allowDropPodRaidValue", "allowDropPodRaidValue".Translate(),
                ref settings.allowDropPodRaidValue, "allowDropPodRaidValueDesc".Translate());

            // 机械体
            DrawCheckbox(ref y, viewRect.width, "allowMechanoidsValue", "allowMechanoidsValue".Translate(),
                ref settings.allowMechanoidsValue, "allowMechanoidsValueDesc".Translate());

            // 昆虫
            DrawCheckbox(ref y, viewRect.width, "allowInsectoidsValue", "allowInsectoidsValue".Translate(),
                ref settings.allowInsectoidsValue, "allowInsectoidsValueDesc".Translate());

            // 群体实体
            DrawCheckbox(ref y, viewRect.width, "allowEntitySwarmValue", "allowEntitySwarmValue".Translate(),
                ref settings.allowEntitySwarmValue, "allowEntitySwarmValueDesc".Translate());

            // 动物
            DrawCheckbox(ref y, viewRect.width, "allowAnimalsValue", "allowAnimalsValue".Translate(),
                ref settings.allowAnimalsValue, "allowAnimalsValueDesc".Translate());

            y += GAP_LARGE;

            // ====== 数值限制 ======
            DrawSectionTitle(ref y, viewRect.width, "ValueLimits".Translate());
            y += GAP_LARGE;

            // 敌人掉落白银
            DrawCheckbox(ref y, viewRect.width, "enableSilverDrop", "enableSilverDrop".Translate(),
                ref settings.enableSilverDrop, "enableSilverDropDesc".Translate());

            // 每级掉落数量
            if (settings.enableSilverDrop)
            {
                float silverDropFloat = settings.silverDropPerLevel;
                DrawSlider(ref y, viewRect.width, "silverDropPerLevel", "silverDropPerLevel".Translate(),
                    ref silverDropFloat, "silverDropPerLevelDesc".Translate(), 20, 100);
                settings.silverDropPerLevel = (int)silverDropFloat;
            }

            // 新增：总难度（标签+下拉框 同行显示）
            float labelWidth = 120f;
            Rect labelRect = new Rect(0, y, labelWidth, ROW_HEIGHT);
            Widgets.Label(labelRect, "TotalDifficulty".Translate());

            Rect dropdownRect = new Rect(labelRect.x + labelWidth + GAP_SMALL, y, viewRect.width - labelWidth - GAP_SMALL, ROW_HEIGHT);
            string currentLabel = DifficultyOptions[settings.currentDifficultyIndex];

            // 确保索引有效
            int currentIndex = settings.currentDifficultyIndex;
            if (currentIndex < 0 || currentIndex >= DifficultyOptions.Count)
            {
                settings.currentDifficultyIndex = currentIndex = 0;
                currentLabel = DifficultyOptions[currentIndex];
            }

            Widgets.Dropdown(
                dropdownRect,
                currentIndex,
                index => DifficultyConfig[index],
                index => GenerateDifficultyMenu(index),
                buttonLabel: currentLabel,
                buttonIcon: null,
                dragLabel: "DragToSelectDifficulty".Translate()
            );

            y += ROW_HEIGHT + GAP_LARGE;

            // 新增：根据选择的总难度更新相关设置
            DrawCheckbox(ref y, viewRect.width, "showDetailConfig", "ShowDetailConfig".Translate(),
                ref settings.showDetailConfig, "ShowDetailConfigDesc".Translate());

            if (settings.showDetailConfig)
            {
                // 基础设置中的难度滑动条
                DrawDifficultySlider(ref y, viewRect.width, "eliteRaidDifficulty".Translate(),
                    ref settings.eliteRaidDifficulty,
                    "eliteRaidDifficultyDesc".Translate());

                // 最大精英等级（滑动条）
                float maxLevelFloat = settings.maxAllowLevel;
                DrawSlider(ref y, viewRect.width, "maxAllowLevel", "maxAllowLevel".Translate(),
                    ref maxLevelFloat, "maxAllowLevelDesc".Translate(), 1, 7);
                settings.maxAllowLevel = (int)maxLevelFloat;

                // 压缩倍率切换开关
                //DrawCheckbox(ref y, viewRect.width, "useCompressionRatio", "useCompressionRatio".Translate(),
                //    ref settings.useCompressionRatio, "useCompressionRatioDesc".Translate());

                // 根据勾选状态显示不同控件
                if (settings.useCompressionRatio)
                {
                    float ratioFloat = settings.compressionRatio;
                    DrawSlider(ref y, viewRect.width, "compressionRatio", "compressionRatio".Translate(),
                        ref ratioFloat, "compressionRatioDesc".Translate(), 1.0f, 10.0f);
                    settings.compressionRatio = (float)Math.Round(ratioFloat, 1);
                } else
                {
                    float maxEnemyFloat = settings.maxRaidEnemy;
                    DrawSlider(ref y, viewRect.width, "maxRaidEnemy", "maxRaidEnemy".Translate(),
                        ref maxEnemyFloat, "maxRaidEnemyDesc".Translate(), 20, 100);
                    settings.maxRaidEnemy = (int)maxEnemyFloat;
                }

                // 最大袭击点数
                float maxPointsFloat = settings.maxRaidPoint;
                DrawSlider(ref y, viewRect.width, "maxRaidPoint", "MaxRaidPoint".Translate(),
                    ref maxPointsFloat, "MaxRaidPointDesc".Translate(), 5000, 200000);
                settings.maxRaidPoint = (int)maxPointsFloat;

                // 袭击缩放倍率
                // 袭击缩放倍率
                float scaleFloat = settings.raidScale;
                DrawSlider(ref y, viewRect.width, "raidScale", "RaidScale".Translate(),
                    ref scaleFloat, "RaidScaleDesc".Translate(), 0.5f, 20.0f, true); // 添加true参数
                settings.raidScale = (float)Math.Round(scaleFloat, 2);
            }



            y += GAP_LARGE;

            // ====== 重置按钮 ======
            Rect resetButtonRect = new Rect(viewRect.x, y, viewRect.width, ROW_HEIGHT);
            if (Widgets.ButtonText(resetButtonRect, "ResetToDefault".Translate()))
            {
                settings.Reset();
                settings.Write();
               
            }

            Widgets.EndScrollView();

            // 同步设置到静态字段（移到UI绘制完成后）
            SyncSettingsToStaticFields();
           
        }


        // 新增：跟踪当前难度索引
        private int currentDifficultyIndex = 0;
        public static bool needApplyRaidScale = false;

        private void ApplyRaidScaleIfNeeded()
        {
            if (needApplyRaidScale && EliteRaidMod.modEnabled && Find.Storyteller != null)
            {
                Find.Storyteller.difficulty.threatScale = EliteRaidMod.raidScale;
                needApplyRaidScale = false;
                Log.Message($"[EliteRaid] 应用袭击缩放倍率: {EliteRaidMod.raidScale}");
            }
        }
        private void SyncSettingsToStaticFields()
        {
            // 先同步用户设置到静态字段
            EliteRaidMod.modEnabled = settings.modEnabled;
            EliteRaidMod.displayMessageValue = settings.displayMessageValue;
            EliteRaidMod.allowRaidFriendlyValue = settings.allowRaidFriendlyValue;
            EliteRaidMod.eliteRaidDifficulty = settings.eliteRaidDifficulty;
            EliteRaidMod.allowMechanoidsValue = settings.allowMechanoidsValue;
            EliteRaidMod.allowInsectoidsValue = settings.allowInsectoidsValue;
            EliteRaidMod.allowEntitySwarmValue = settings.allowEntitySwarmValue;
            EliteRaidMod.allowAnimalsValue = settings.allowAnimalsValue;
            EliteRaidMod.maxAllowLevel = settings.maxAllowLevel;
            EliteRaidMod.maxRaidEnemy = settings.maxRaidEnemy;
            EliteRaidMod.mapGeneratedEnemyEnhanced = settings.mapGeneratedEnemyEnhanced;

            // 新增设置
            EliteRaidMod.useCompressionRatio = settings.useCompressionRatio;
            EliteRaidMod.compressionRatio = settings.compressionRatio;
            EliteRaidMod.enableSilverDrop = settings.enableSilverDrop;
            EliteRaidMod.silverDropPerLevel = settings.silverDropPerLevel;
            EliteRaidMod.maxRaidPoint = settings.maxRaidPoint;
            EliteRaidMod.allowDropPodRaidValue = settings.allowDropPodRaidValue;
            showDetailConfig = settings.showDetailConfig;
            raidScale= settings.raidScale;
            ApplyRaidScaleIfNeeded();
            // 验证难度索引
            int index = settings.currentDifficultyIndex;
            int maxIndex = DifficultyOptions.Count - 1;
            if (index < 0 || index > maxIndex)
            {
                Log.Error($"[EliteRaid] 无效的难度索引 {index}，范围应为 0-{maxIndex}");
                settings.currentDifficultyIndex = index = 0;
            }



            // 保存当前难度索引，用于检测变化
            int previousDifficultyIndex = currentDifficultyIndex;
            currentDifficultyIndex = index;

            // 只有当用户首次加载、重置设置或更改总难度时，才应用难度配置
            if (settings.isFirstLoad || settings.wasReset || previousDifficultyIndex != index)
            {
                if (DifficultyConfig.TryGetValue(index, out var config))
                {
                    useCompressionRatio = true;
                    compressionRatio = config.compressionRatio;
                    maxRaidPoint = config.maxRaidPoint;
                    eliteRaidDifficulty = config.difficulty;

                    // 同步到settings字段
                    settings.compressionRatio = compressionRatio;
                    settings.maxRaidPoint = maxRaidPoint;
                    settings.eliteRaidDifficulty = eliteRaidDifficulty;
                   
                  //  Log.Message($"[EliteRaid] 应用难度配置: 压缩比={compressionRatio}, 最大袭击点数={maxRaidPoint}, 难度={eliteRaidDifficulty}");
                }

                // 重置标志
                settings.isFirstLoad = false;
                settings.wasReset = false;
            }
            needApplyRaidScale = true;
        }
        // ========== 布局辅助方法 ==========
        private void DrawSectionTitle(ref float y, float width, string title)
        {
            Widgets.Label(new Rect(0, y, width, ROW_HEIGHT), title);
            Widgets.DrawLineHorizontal(0, y + ROW_HEIGHT + GAP_SMALL / 2, width);
            y += ROW_HEIGHT + GAP_LARGE;
        }

        private void DrawCheckbox(ref float y, float width, string key, string label,
            ref bool value, string tooltip)
        {
            Rect rect = new Rect(0, y, width, ROW_HEIGHT);
            Widgets.CheckboxLabeled(rect, label, ref value);
            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(rect, tooltip);
            y += ROW_HEIGHT + GAP_SMALL;
        }

        private static IEnumerable<DropdownMenuElement<(float, int, EliteRaidDifficulty)>> GenerateDifficultyMenu(int currentIndex)
        {
            for (int i = 0; i < DifficultyOptions.Count; i++)
            {
                string optionLabel = DifficultyOptions[i];
                var config = DifficultyConfig[i];

                // 捕获当前循环的索引，避免闭包问题
                int captureIndex = i;

                yield return new DropdownMenuElement<(float, int, EliteRaidDifficulty)>
                {
                    payload = config,
                    option = new FloatMenuOption(
                        optionLabel,
                        () => SetDifficultyIndex(currentIndex, captureIndex),
                        null,
                        null,
                        true
                    )
                };
            }
        }

        private static void SetDifficultyIndex(int oldIndex, int newIndex)
        {
            if (oldIndex != newIndex && newIndex >= 0 && newIndex < DifficultyOptions.Count)
            {
                var mod = (EliteRaidMod)LoadedModManager.GetMod<EliteRaidMod>();
                mod.settings.currentDifficultyIndex = newIndex;
                mod.SyncSettingsToStaticFields(); // 同步配置
            } 
        }


        private void DrawDifficultySlider(ref float y, float width, string label,
    ref EliteRaidDifficulty difficulty, string tooltip)
        {
            Rect labelRect = new Rect(0, y, LABEL_WIDTH, ROW_HEIGHT);
            Widgets.Label(labelRect, label);

            float sliderAvailableWidth = width - LABEL_WIDTH - GAP_SMALL - 80f;
            Rect sliderRect = new Rect(
                LABEL_WIDTH + GAP_SMALL,
                y,  // 从顶部开始
                sliderAvailableWidth,
                28  // 增加高度至28像素
            );

            float sliderValue = (float)difficulty;
            sliderValue = Widgets.HorizontalSlider(
                sliderRect,
                sliderValue,
                1f,
                3f,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: "Difficulty_Normal".Translate(),
                rightAlignedLabel: "Difficulty_Extreme".Translate(),
                roundTo: 1f
            );

            difficulty = (EliteRaidDifficulty)Mathf.RoundToInt(sliderValue);
            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(sliderRect, tooltip);

            y += ROW_HEIGHT + GAP_LARGE;
        }

        // 为其他滑动条添加相同的逻辑
        // 修改后的DrawSlider方法，支持百分比显示
        private void DrawSlider(ref float y, float width, string key, string label,
            ref float value, string tooltip, float min, float max, bool isPercentage = false)
        {
            Rect labelRect = new Rect(0, y, LABEL_WIDTH, ROW_HEIGHT);

            // 根据是否为百分比显示调整格式
            string displayValue = isPercentage
                ? $"{label}: {(int)(value * 100)}%"
                : (max - min) % 1 == 0
                    ? $"{label}: {(int)value}"
                    : $"{label}: {value:F1}";

            Widgets.Label(labelRect, displayValue);

            float sliderAvailableWidth = width - LABEL_WIDTH - GAP_SMALL - 60f;
            Rect sliderRect = new Rect(
                LABEL_WIDTH + GAP_SMALL,
                y,
                sliderAvailableWidth,
                28
            );

            value = Widgets.HorizontalSlider(
                sliderRect,
                value,
                min,
                max,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: isPercentage ? $"{min * 100}%" : min.ToString("F1"),
                rightAlignedLabel: isPercentage ? $"{max * 100}%" : max.ToString("F1"),
                roundTo: 0.1f
            );

            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(sliderRect, tooltip);

            y += ROW_HEIGHT + GAP_SMALL;
        }

    }


}

     