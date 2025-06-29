# EilteRaid
环世界Mod   精英化袭击

## 功能说明

EliteRaid Mod 是一个为 RimWorld 游戏添加精英化袭击功能的模组。它能够压缩大量敌人袭击，并将压缩后的敌人增强为精英单位。

## 主要功能

### 袭击压缩
- 支持人类袭击压缩
- 支持机械族袭击压缩
- 支持虫族袭击压缩
- 支持动物袭击压缩
- 支持实体群袭击压缩
- **新增：支持蹒跚怪袭击压缩**

### 蹒跚怪袭击支持

本模组现在完全支持以下蹒跚怪袭击类型的压缩和增强：

1. **IncidentWorker_ShamblerAssault** - 人类类型蹒跚怪袭击
   - 继承自 `IncidentWorker_RaidEnemy`
   - 生成人类类型的蹒跚怪单位
   - 支持数量压缩和属性增强

2. **IncidentWorker_ShamblerSwarm** - 实体群类型蹒跚怪袭击
   - 继承自 `IncidentWorker_EntitySwarm`
   - 生成实体群类型的蹒跚怪
   - 支持数量压缩和属性增强

3. **IncidentWorker_ShamblerSwarmAnimals** - 动物类型蹒跚怪袭击
   - 继承自 `IncidentWorker_ShamblerSwarm`
   - 生成动物类型的蹒跚怪（包括奇美拉）
   - 支持数量压缩和属性增强

### 补丁实现

为支持蹒跚怪袭击，添加了以下补丁：

- `ShamblerAssault_Patch` - 处理人类类型蹒跚怪袭击
- `ShamblerSwarmAnimals_Patch` - 处理动物类型蹒跚怪袭击
- 增强的 `EntitySwarmIncidentUtility_Patch` - 处理实体群类型蹒跚怪袭击

### 配置选项

在模组设置中可以配置：
- 是否启用实体袭击压缩 (`allowEntitySwarmValue`)
- 最大袭击敌人数量 (`maxRaidEnemy`)
- 压缩倍率 (`compressionRatio`)
- 是否显示调试消息 (`displayMessageValue`)

## 安装说明

1. 下载模组文件
2. 将文件放入 RimWorld 的 Mods 文件夹
3. 在游戏启动器中启用模组
4. 在游戏内调整模组设置

## 兼容性

本模组与大多数其他模组兼容，但可能与以下类型的模组产生冲突：
- 修改袭击生成逻辑的模组
- 修改敌人属性的模组
- 修改Harmony补丁的模组

## 更新日志

### v1.3.4
- 新增蹒跚怪袭击压缩支持
- 修复EntitySwarm补丁中的baseNum计算错误
- 增强蹒跚怪类型识别逻辑
- 添加详细的调试日志支持
