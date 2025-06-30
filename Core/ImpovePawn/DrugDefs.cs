using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EliteRaid
{
    //[StaticConstructorOnStartup]
    //public class MustNegativeEffectDef : Def
    //{
    //    //weak reference by defName string
    //    public List<string> drugs;
    //}
    //[StaticConstructorOnStartup]
    //public class ExcludeDrugDef : Def
    //{
    //    //weak reference by defName string
    //    public List<string> drugs;
    //}

    /// <summary>
    /// 定义允许的PawnGroupKindWorker类型
    /// 功能：存储和管理允许的PawnGroupKindWorker类型名称列表
    /// 输入：通过XML配置文件加载workerTypeNames
    /// 输出：提供workerTypeNames列表供其他系统使用
    /// </summary>
    [StaticConstructorOnStartup]
    public class AllowPawnGroupKindWorkerTypeDef : Def
    {
        public List<string> workerTypeNames;
    }
}
