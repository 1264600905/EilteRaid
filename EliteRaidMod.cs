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
using static UnityEngine.Scripting.GarbageCollector;
using static Verse.Widgets;


namespace EliteRaid
{
  
    public class EliteRaidMod : Mod
    {
        //显示部分日志
        public static bool displayMessageValue = false;
        public static bool showRaidMessages = true; // 修改：显示袭击提示选项，默认为true

        public static bool modEnabled = true; // 是否启用Mod
        public static bool allowRaidFriendlyValue = true; // 是否允许友好突袭精英化
        public static bool allowDropPodRaidValue = false; // 新增：是否允许精英化空投袭击
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
        public static int silverDropPerLevel=30;
        public static int maxRaidPoint = 10000; // 新增：最大袭击点数上限（默认10000）
        public static bool showDetailConfig = false;
        public static float raidScale = 1.0f;  //游戏内袭击倍率
        public static TotalDifficultyLevel CurrentDifficulty { get; set; } = TotalDifficultyLevel.Consul;
        // 在 EliteRaidMod 类顶部添加（与其他静态字段并列）
        public static bool AllowModBionicsAndDrugs = true; // 新增：允许模组药物和仿生体
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
        public static int startCompressNum = 20; // 新增：开始压缩人数

        private int lastSelectedDifficultyIndex = -1;

        private static List<string> DifficultyOptions = new List<string>
{
    "<color=#39FF14>修士</color> 难度系数0.4",
    "<color=#FFA500>扈从</color> 难度系数0.7",
    "<color=#B0E2FF>骑士</color> 难度系数1",
    "<color=#00FFFF>法务官</color> 难度系数1.3",

    "<color=#FF6EC7>领主</color> 难度系数1.6",
    "<color=#FF3030>总督</color> 难度系数2",
    "<color=#1E90FF>执政总督</color> 难度系数2.4",
    "<color=#7FFF00>都督</color> 难度系数3.9",

    "<color=#FFD700>专制公</color> 难度系数6.4",
    "<color=#FF6347>大公</color> 难度系数10",
    "<color=#39FF14>执政官</color> 难度系数16",
    "<color=#FFA500>将军</color> 难度系数32",

    "<color=#B0E2FF>近卫总长</color> 难度系数54",
    "<color=#00FFFF>星系主宰</color> 难度系数72",
    "<color=#FF6EC7>至高星主</color> 难度系数100",
    "<color=#FF3030>星海皇帝</color> 难度系数200"
};

        public static bool AllowCompress(Pawn pawn, bool isDropPodRaid = false)
        {
            if(pawn == null) return false;
            if(pawn.Faction==Faction.OfPlayer) return false;
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
            
            // 修改：更准确地识别实体群类型，包括蹒跚怪
            bool isEntitySwarm = pawn?.RaceProps.FleshType == FleshTypeDefOf.Fleshbeast ||
                                 pawn?.RaceProps.FleshType == FleshTypeDefOf.EntityMechanical ||
                                 pawn?.RaceProps.FleshType == FleshTypeDefOf.EntityFlesh ||
                                 pawn?.Faction == Faction.OfEntities; // 新增：蹒跚怪属于OfEntities派系

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
            if (parms.faction == Faction.OfPlayer) return false;
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
            if (parms.faction == Faction.OfPlayer) {
                return false;
            } 
           
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

        private const float LABEL_WIDTH = 270f;
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
            return "EliteRaidSettingsCategory".Translate()+"(v1.5.1)";
        }
        public int timer = 0;
        public void Tick()
        {
            if (timer > 360) {
                timer = 0;
                SyncSettingsToStaticFields();
            } else
            {
                timer++;
            }
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
                height += ROW_HEIGHT + GAP_SMALL; // 新增：开始压缩人数
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
            Rect viewRect = new Rect(0, 0, inRect.width - 40f, contentHeight+600);
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

            // 隐藏袭击提示开关
            DrawCheckbox(ref y, viewRect.width, "showRaidMessages", "showRaidMessages".Translate(),
                ref settings.showRaidMessages, "showRaidMessagesDesc".Translate());

            y += GAP_LARGE;

            // ====== 袭击类型允许 ======
            DrawSectionTitle(ref y, viewRect.width, "AllowedPawnTypes".Translate());
            y += GAP_LARGE;

            // 友好突袭开关
            DrawCheckbox(ref y, viewRect.width, "allowRaidFriendlyValue", "allowRaidFriendlyValue".Translate(),
                ref settings.allowRaidFriendlyValue, "allowRaidFriendlyValueDesc".Translate());

            //// 新增：允许精英化空投袭击
            //DrawCheckbox(ref y, viewRect.width, "allowDropPodRaidValue", "allowDropPodRaidValue".Translate(),
            //    ref settings.allowDropPodRaidValue, "allowDropPodRaidValueDesc".Translate());

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
           
            y += GAP_SMALL; // 间隔

            // 新增：允许模组药物和仿生体
            DrawCheckbox(ref y, viewRect.width, "allowModBionicsAndDrugs", "AllowModBionicsAndDrugs".Translate(),
                ref settings.allowModBionicsAndDrugs, "AllowModBionicsAndDrugsDesc".Translate()); // 需添加翻译键


            float labelWidth = 120f;
            float doubleRowHeight = ROW_HEIGHT * 2; // 双行高度
            Rect labelRect = new Rect(0, y, labelWidth, doubleRowHeight);
            Widgets.Label(labelRect, "TotalDifficulty".Translate()); // 显示标签

            Rect dropdownRect = new Rect(labelRect.x + labelWidth + GAP_SMALL, y, viewRect.width - labelWidth - GAP_SMALL, ROW_HEIGHT);
            // 总难度下拉框
            var translatedOptions = GetTranslatedDifficultyOptions();
            string currentLabel = translatedOptions[settings.currentDifficultyIndex];

            // 确保索引有效
            int currentIndex = settings.currentDifficultyIndex;
            if (currentIndex < 0 || currentIndex >= translatedOptions.Count)
            {
                settings.currentDifficultyIndex = currentIndex = 0;
                currentLabel = translatedOptions[currentIndex];
            }

            Widgets.Dropdown(
                dropdownRect,
                currentIndex,
                index => (TotalDifficultyLevel)index,
                index => GenerateDifficultyMenu(index),
                buttonLabel: currentLabel,
                buttonIcon: null,
                dragLabel: "DragToSelectDifficulty".Translate()
            );
            y += ROW_HEIGHT + GAP_LARGE;
            // 新增：难度描述文本框
            Rect descRect = new Rect(labelRect.x, y, viewRect.width - GAP_SMALL, ROW_HEIGHT * 2); // 两行高度
            string difficultyDesc = GetDifficultyDescription(CurrentDifficulty); // 获取描述文本
            Widgets.Label(descRect, difficultyDesc);
            Widgets.DrawBox(descRect, 1); // 绘制边框
            y += ROW_HEIGHT * 2 + GAP_LARGE; // 预留间距


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
                    ref maxLevelFloat, "maxAllowLevelDesc".Translate(), 0, 7);
                settings.maxAllowLevel = (int)maxLevelFloat;

                // 取消压缩倍率切换开关，始终显示两个滑块
                float ratioFloat = settings.compressionRatio;
                DrawSlider(ref y, viewRect.width, "compressionRatio", "compressionRatio".Translate(),
                    ref ratioFloat, "compressionRatioDesc".Translate(), 1.0f, 10.0f);
                settings.compressionRatio = (float)Math.Round(ratioFloat, 1);

                float maxEnemyFloat = settings.maxRaidEnemy;
                DrawSlider(ref y, viewRect.width, "maxRaidEnemy", "maxRaidEnemy".Translate(),
                    ref maxEnemyFloat, "maxRaidEnemyDesc".Translate(), 1, 1000);
                settings.maxRaidEnemy = (int)maxEnemyFloat;

                // 新增：开始压缩人数
                int maxEnemy = settings.maxRaidEnemy > 0 ? settings.maxRaidEnemy : 1;
                float startCompressNumFloat = settings.startCompressNum;
                DrawSlider(ref y, viewRect.width, "startCompressNum", "StartCompressNum".Translate(),
                    ref startCompressNumFloat, "StartCompressNumDesc".Translate(), 0, maxEnemy);
                settings.startCompressNum = (int)Math.Min(Math.Max(0, startCompressNumFloat), maxEnemy);

                // 最大袭击点数
                float maxPointsFloat = settings.maxRaidPoint;
                DrawSlider(ref y, viewRect.width, "maxRaidPoint", "MaxRaidPoint".Translate(),
                    ref maxPointsFloat, "MaxRaidPointDesc".Translate(), 5000, 400000);
                settings.maxRaidPoint = (int)maxPointsFloat;

                // 袭击缩放倍率
                float scaleFloat = settings.raidScale;
                DrawSlider(ref y, viewRect.width, "raidScale", "RaidScale".Translate(),
                    ref scaleFloat, "RaidScaleDesc".Translate(), 0.5f, 40.0f, true); // 添加true参数
                settings.raidScale = (float)Math.Round(scaleFloat, 2);


            }



            y += GAP_LARGE;

            // ====== 重置按钮 ======
            Rect resetButtonRect = new Rect(viewRect.x, y, viewRect.width, ROW_HEIGHT);
            if (Widgets.ButtonText(resetButtonRect, "ResetToDefault".Translate()))
            {
                settings.Reset();
                SyncSettingsToStaticFields();
                settings.Write();
            }

            Widgets.EndScrollView();

            // 同步设置到静态字段（移到UI绘制完成后）
            SyncSettingsToStaticFields();
           
        }

        private string GetDifficultyDescription(TotalDifficultyLevel level)
        {
            // 根据难度级别返回对应的描述文本（需在XML中添加翻译键）
            switch (level)
            {
                case TotalDifficultyLevel.Novice:
                    return "Difficulty_Novice_Desc".Translate("0.3"); // 示例："难度系数0.4，适合新手，敌人强度较低"
                case TotalDifficultyLevel.Retainer:
                    return "Difficulty_Retainer_Desc".Translate("0.6");
                case TotalDifficultyLevel.Knight:
                    return "Difficulty_Knight_Desc".Translate("1.0"); // 默认难度，平衡体验
                case TotalDifficultyLevel.Justiciar:
                    return "Difficulty_Justiciar_Desc".Translate("1.3");
                case TotalDifficultyLevel.Lord:
                    return "Difficulty_Lord_Desc".Translate("1.6");
                case TotalDifficultyLevel.Viceroy:
                    return "Difficulty_Viceroy_Desc".Translate("2.0");
                case TotalDifficultyLevel.Governor:
                    return "Difficulty_Governor_Desc".Translate("3");
                case TotalDifficultyLevel.Duke:
                    return "Difficulty_Duke_Desc".Translate("3.9");
                case TotalDifficultyLevel.Despot:
                    return "Difficulty_Despot_Desc".Translate("6.4");
                case TotalDifficultyLevel.GrandDuke:
                    return "Difficulty_GrandDuke_Desc".Translate("10.0");
                case TotalDifficultyLevel.Consul:
                    return "Difficulty_Consul_Desc".Translate("16.0");
                case TotalDifficultyLevel.General:
                    return "Difficulty_General_Desc".Translate("1.0");
                case TotalDifficultyLevel.GuardCommander:
                    return "Difficulty_GuardCommander_Desc".Translate("2.0");
                case TotalDifficultyLevel.GalaxyLord:
                    return "Difficulty_GalaxyLord_Desc".Translate("3.0");
                case TotalDifficultyLevel.StarOverlord:
                    return "Difficulty_StarOverlord_Desc".Translate("0.3");
                case TotalDifficultyLevel.GalaxyEmperor:
                    return "Difficulty_GalaxyEmperor_Desc".Translate("2.0");
                default:
                    return "Difficulty_Unknown_Desc".Translate(); // 未知难度
            }
        }

        // 在EliteRaidMod类中添加以下方法
        // 修改GenerateDifficultyMenu方法，确保选项文本正确
        // 修改 GenerateDifficultyMenu 方法，只生成存在配置的菜单项
        private static IEnumerable<DropdownMenuElement<TotalDifficultyLevel>> GenerateDifficultyMenu(int currentIndex)
        {
            var mod = (EliteRaidMod)LoadedModManager.GetMod<EliteRaidMod>();
            var translatedOptions = mod.GetTranslatedDifficultyOptions();

            // 只生成存在配置的难度级别
            foreach (var kvp in DifficultyConfig.myDifficultyConfig)
            {
                TotalDifficultyLevel level = kvp.Key;
                int index = (int)level;

                // 确保索引在有效范围内
                if (index >= 0 && index < translatedOptions.Count)
                {
                    yield return new DropdownMenuElement<TotalDifficultyLevel>
                    {
                        payload = level,
                        option = new FloatMenuOption(
                            translatedOptions[index], // 使用翻译后的文本
                            () => SetDifficultyLevel(index),
                            null,
                            null,
                            true
                        )
                    };
                } else
                {
                    Log.Error($"[EliteRaid] 难度级别 {level} 的索引 {index} 超出范围");
                }
            }
        }



        private List<string> GetTranslatedDifficultyOptions()
        {
            return new List<string>
    {
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Novice, "Difficulty_Novice", "DifficultyFactor_0_4"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Retainer, "Difficulty_Retainer", "DifficultyFactor_0_7"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Knight, "Difficulty_Knight", "DifficultyFactor_1"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Justiciar, "Difficulty_Justiciar", "DifficultyFactor_1_3"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Lord, "Difficulty_Lord", "DifficultyFactor_1_6"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Viceroy, "Difficulty_Viceroy", "DifficultyFactor_2"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Governor, "Difficulty_Governor", "DifficultyFactor_2_4"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Duke, "Difficulty_Duke", "DifficultyFactor_3_9"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Despot, "Difficulty_Despot", "DifficultyFactor_6_4"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.GrandDuke, "Difficulty_GrandDuke", "DifficultyFactor_10"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.Consul, "Difficulty_Consul", "DifficultyFactor_16"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.General, "Difficulty_General", "DifficultyFactor_32"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.GuardCommander, "Difficulty_GuardCommander", "DifficultyFactor_54"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.GalaxyLord, "Difficulty_GalaxyLord", "DifficultyFactor_72"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.StarOverlord, "Difficulty_StarOverlord", "DifficultyFactor_100"),
        GetDifficultyLabelWithColor(TotalDifficultyLevel.GalaxyEmperor, "Difficulty_GalaxyEmperor", "DifficultyFactor_200"),
    };
        }

        // 辅助方法：获取带颜色标签的难度选项文本
        private string GetDifficultyLabelWithColor(TotalDifficultyLevel level, string nameKey, string factorKey)
        {
            string colorTag = GetDifficultyColorTag(level);
            return $"{colorTag}{nameKey.Translate()}</color> {factorKey.Translate()}";
        }

        // 辅助方法：根据难度级别获取颜色标签
        private static string GetDifficultyColorTag(TotalDifficultyLevel level)
        {
            switch (level)
            {
                case TotalDifficultyLevel.Novice: return "<color=#39FF14>";       // 浅绿色
                case TotalDifficultyLevel.Retainer: return "<color=#FFA500>";      // 橙色
                case TotalDifficultyLevel.Knight: return "<color=#B0E2FF>";        // 浅蓝色
                case TotalDifficultyLevel.Justiciar: return "<color=#00FFFF>";     // 青色
                case TotalDifficultyLevel.Lord: return "<color=#FF6EC7>";          // 粉色
                case TotalDifficultyLevel.Viceroy: return "<color=#FF3030>";       // 亮红色
                case TotalDifficultyLevel.Governor: return "<color=#1E90FF>";      // 天蓝色
                case TotalDifficultyLevel.Duke: return "<color=#7FFF00>";          // 黄绿色
                case TotalDifficultyLevel.Despot: return "<color=#FFD700>";        // 金色
                case TotalDifficultyLevel.GrandDuke: return "<color=#FF6347>";     // 珊瑚色
                case TotalDifficultyLevel.Consul: return "<color=#39FF14>";        // 浅绿色（与新手区分）
                case TotalDifficultyLevel.General: return "<color=#FFA500>";       // 橙色（与扈从区分）
                case TotalDifficultyLevel.GuardCommander: return "<color=#B0E2FF>"; // 浅蓝色（与骑士区分）
                case TotalDifficultyLevel.GalaxyLord: return "<color=#00FFFF>";    // 青色（与法务官区分）
                case TotalDifficultyLevel.StarOverlord: return "<color=#FF6EC7>";  // 粉色（与领主区分）
                case TotalDifficultyLevel.GalaxyEmperor: return "<color=#FF3030>"; // 亮红色（与总督区分）
                default: return "<color=white>";
            }
        }

        private static void SetDifficultyLevel(int index)
        {
            var mod = (EliteRaidMod)LoadedModManager.GetMod<EliteRaidMod>();

            // 只有当选择的索引与上次不同时才应用难度
            if (index != mod.lastSelectedDifficultyIndex)
            {
                mod.lastSelectedDifficultyIndex = index;
                mod.settings.currentDifficultyIndex = index;
                mod.ApplySelectedDifficulty();
                mod.settings.Write();
            }
        }
        // 修改ApplySelectedDifficulty方法，确保只应用存在的配置
        private void ApplySelectedDifficulty()
        {
            int index = settings.currentDifficultyIndex;
            
            // 确保索引有效
            if (index < 0 || index >= DifficultyOptions.Count)
            {
                Log.Error($"[EliteRaid] 无效的难度索引 {index}，重置为0");
                settings.currentDifficultyIndex = index = 0;
                settings.Write();
                return;
            }

            TotalDifficultyLevel level = (TotalDifficultyLevel)index;

            // 检查配置是否存在
            if (DifficultyConfig.myDifficultyConfig.ContainsKey(level))
            {
                DifficultyConfig config = DifficultyConfig.myDifficultyConfig[level];

                // 应用难度配置到游戏设置
                eliteRaidDifficulty = config.EliteDifficulty;
                compressionRatio = config.CompressionRatio;
                maxRaidPoint = config.MaxRaidPoint;
                raidScale = config.RaidScale/100f;
                maxAllowLevel = config.MaxEliteLevel;
                useCompressionRatio = true;
                maxRaidEnemy = config.MaxRaidEnemy;  // 使用配置中的最大袭击人数
                
                // 同步配置值到设置对象
                settings.eliteRaidDifficulty = eliteRaidDifficulty;
                settings.compressionRatio = compressionRatio;
                settings.maxRaidPoint = maxRaidPoint;
                settings.raidScale = raidScale;
                settings.maxAllowLevel = maxAllowLevel;
                settings.useCompressionRatio = true;
                settings.maxRaidEnemy = config.MaxRaidEnemy;  // 同步到设置
                startCompressNum=StaticVariables.DEFAULT_START_COMPRESS_NUM;
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 应用难度配置: {config.Name} (系数: {config.DifficultyFactor}, 最大袭击人数: {config.MaxRaidEnemy})");
                }
            } else
            {
                Log.Error($"[EliteRaid] 缺少难度级别 {level} 的配置，使用默认配置");

                // 使用默认配置或第一个配置作为备选
                if (DifficultyConfig.myDifficultyConfig.Count > 0)
                {
                    var firstConfig = DifficultyConfig.myDifficultyConfig.First().Value;
                    eliteRaidDifficulty = firstConfig.EliteDifficulty;
                    compressionRatio = firstConfig.CompressionRatio;
                    maxRaidPoint = firstConfig.MaxRaidPoint;
                    raidScale = firstConfig.RaidScale;
                    maxAllowLevel = firstConfig.MaxEliteLevel;
                    maxRaidEnemy = firstConfig.MaxRaidEnemy;
                } else
                {
                    Log.Error($"[EliteRaid] 没有可用的难度配置!");
                }
            }

            // 更新当前难度
            CurrentDifficulty = level;
        }

        

        // 新增：跟踪当前难度索引
        private int currentDifficultyIndex = 0;
        public static bool needApplyRaidScale = false;

        private void ApplyRaidScaleIfNeeded()
        {
            if (EliteRaidMod.modEnabled && Find.Storyteller != null)
            {
                Find.Storyteller.difficulty.threatScale = EliteRaidMod.raidScale;
             //   Log.Message($"[EliteRaid] 应用袭击缩放倍率: {EliteRaidMod.raidScale}");
            }
        }
        private static bool lastAllowModBionicsAndDrug = EliteRaidMod.AllowModBionicsAndDrugs;
        private void SyncSettingsToStaticFields()
        {
            // 先同步用户设置到静态字段
            EliteRaidMod.modEnabled = settings.modEnabled;
            EliteRaidMod.displayMessageValue = settings.displayMessageValue;
            EliteRaidMod.showRaidMessages = settings.showRaidMessages; // 新增：同步隐藏袭击提示设置
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
            EliteRaidMod.AllowModBionicsAndDrugs = settings.allowModBionicsAndDrugs;
            EliteRaidMod.raidScale = settings.raidScale;
            EliteRaidMod.startCompressNum = Math.Min(Math.Max(0, settings.startCompressNum), EliteRaidMod.maxRaidEnemy); // 新增同步
            
            //// 添加调试日志
            //if (EliteRaidMod.displayMessageValue)
            //{
            //    Log.Message($"[EliteRaid] 设置同步: maxRaidEnemy={EliteRaidMod.maxRaidEnemy}, useCompressionRatio={EliteRaidMod.useCompressionRatio}, compressionRatio={EliteRaidMod.compressionRatio}");
                
                
            //    // 测试0级敌人生成控制
            //    EliteLevelManager.TestZeroLevelGeneration();
            //}
            
            ApplyRaidScaleIfNeeded();
            // 验证难度索引
            int index = settings.currentDifficultyIndex; // 不要减1！
            int maxIndex = DifficultyOptions.Count - 1; // 修正最大索引计算
            if (index < 0 || index > maxIndex)
            {
                Log.Error($"[EliteRaid] 无效的难度索引 {index}，范围应为 0-{maxIndex}");
                settings.currentDifficultyIndex = index = 0;
            }
            TotalDifficultyLevel level = (TotalDifficultyLevel)index;
            if (!DifficultyConfig.myDifficultyConfig.ContainsKey(level))
            {
                Log.Error($"[EliteRaid] 索引 {index} 对应的难度级别 {level} 没有配置");

                // 尝试找到第一个有配置的级别
                foreach (var kvp in DifficultyConfig.myDifficultyConfig)
                {
                    int validIndex = (int)kvp.Key;
                    if (validIndex >= 0 && validIndex <= maxIndex)
                    {
                        settings.currentDifficultyIndex = index = validIndex;
                        settings.Write();
                        break;
                    }
                }
            }

            // 更新当前难度
            CurrentDifficulty = (TotalDifficultyLevel)index;


            // 保存当前难度索引，用于检测变化
            int previousDifficultyIndex = currentDifficultyIndex;
            currentDifficultyIndex = index;

            // 只有当用户首次加载、重置设置时，才应用难度配置
            // 移除 previousDifficultyIndex 比较，避免重复应用
            if (settings.isFirstLoad || settings.wasReset)
            {
                if (DifficultyConfig.myDifficultyConfig.TryGetValue(CurrentDifficulty, out var config))
                {
                    eliteRaidDifficulty = config.EliteDifficulty;
                    compressionRatio = config.CompressionRatio;
                    maxRaidPoint = config.MaxRaidPoint;
                    raidScale = config.RaidScale / 100f;
                    maxAllowLevel = config.MaxEliteLevel;
                    maxRaidEnemy = config.MaxRaidEnemy;  // 使用配置中的最大袭击人数
                    useCompressionRatio = true;
                    
                    // 同步到设置文件
                    settings.eliteRaidDifficulty = config.EliteDifficulty;
                    settings.compressionRatio = config.CompressionRatio;
                    settings.maxRaidPoint = config.MaxRaidPoint;
                    settings.raidScale = config.RaidScale / 100f;
                    settings.maxAllowLevel = config.MaxEliteLevel;
                    settings.maxRaidEnemy = config.MaxRaidEnemy;  // 同步最大袭击人数
                    settings.useCompressionRatio = useCompressionRatio;
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

        // 添加这个方法到EliteRaidMod类中
        public override void WriteSettings()
        {
            base.WriteSettings();
            // 确保在设置保存时应用袭击缩放倍率
            ApplyRaidScaleIfNeeded();
        }

        private void DrawDifficultySlider(ref float y, float width, string label,
    ref EliteRaidDifficulty difficulty, string tooltip)
        {
            Rect labelRect = new Rect(0, y, LABEL_WIDTH, ROW_HEIGHT);
            Widgets.Label(labelRect, label);

            float sliderAvailableWidth = width - LABEL_WIDTH - GAP_SMALL - 40f;
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
        // 修改DrawSlider方法，当修改raidScale时标记需要应用更改
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

            float sliderAvailableWidth = width - LABEL_WIDTH - GAP_SMALL - 40f;
            Rect sliderRect = new Rect(
                LABEL_WIDTH + GAP_SMALL,
                y,
                sliderAvailableWidth,
                28
            );

            // 保存原始值用于比较
            float originalValue = value;

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

            // 如果是raidScale且值发生了变化，标记需要应用
            if (key == "raidScale" && Math.Abs(value - originalValue) > 0.01f)
            {
                needApplyRaidScale = true;
            }

            if (!string.IsNullOrEmpty(tooltip))
                TooltipHandler.TipRegion(sliderRect, tooltip);

            y += ROW_HEIGHT + GAP_SMALL;
        }

    }

    public class EliteRaidMapComponent : MapComponent
    {
        private bool raidScaleApplied = false;
        private int tickTime = 0;
        public EliteRaidMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            tickTime++;
            // 每200个tick检查一次，而不是每个tick都检查
            if (tickTime>200||raidScaleApplied)
            {
                TryApplyRaidScale();
                tickTime = 0;
            }
        }

        private void TryApplyRaidScale()
        {
            // 确保这是玩家的主地图
            if (map.IsPlayerHome && !raidScaleApplied && EliteRaidMod.modEnabled)
            {
                if (EliteRaidMod.modEnabled && Find.Storyteller != null)
                {
                    Find.Storyteller.difficulty.threatScale = EliteRaidMod.raidScale;
                  //  Log.Message($"[EliteRaid] 在加载地图时应用袭击规模: {EliteRaidMod.raidScale}");
                }
                raidScaleApplied = true;
            }
        }

        // 当游戏切换到新地图时重置标志
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref raidScaleApplied, "raidScaleApplied", false);

            // 如果是新游戏或加载存档，重置标志以便再次应用
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                raidScaleApplied = false;
            }
        }
    }

}

