# 蜱虫快乐模组

[English Guide](README.md)

_一个用于 [Casualties Unknown](https://store.steampowered.com/app/3624440/Casualties_Unknown_Demo/)
多人模式的模组封禁/玩家踢出工具。_

_灵感来源于 Snownee 和 forestbat121 的 Minecraft 模组 [蝙蝠快乐模组](https://www.curseforge.com/minecraft/mc-mods/bat-happy-mod)。_

---

## 功能

| 功能           | 说明                                         |
|--------------|--------------------------------------------|
| **封禁 Mod**   | 配置被封禁的 Mod GUID 列表。带有这些 Mod 的玩家加入时会被服务器踢出。 |
| **要求安装 THM** | 可选强制所有客户端安装 Tick Happy Mod。未安装的玩家超时后被踢出。   |
| **单人自检**     | 单人模式下，本地检测到被封禁 Mod 时立即退出游戏。                |
| **多人通知**     | 被踢出的玩家会在 KrokoshaMP 弹窗中看到原因。               |

## 安装

1. 为 Casualties Unknown 安装 [BepInEx 5.x](https://github.com/BepInEx/BepInEx)。
2. 安装 [KrokoshaCasualtiesMP](https://github.com/Krokosha/CasualtiesMP)（多人模式必需）。
3. 将 `TickHappyMod.dll` 放入 `BepInEx/plugins/Tick Happy Mod/`。
4. 配置 `BepInEx/config/org.cncumc.tickhappymod.cfg`。

## 配置

```ini
[Tick Happy Mod]

## 被封禁的 Mod GUID 列表，支持逗号/空格/分号等分隔符。
# 设置类型: String
# 默认值: （空）
ban_mods =

## 启用后，服务器会踢出所有未安装 THM 的玩家。
# 设置类型: Boolean
# 默认值: false
require_thm = false

## 等待客户端上报 Mod 列表的超时秒数。
# 设置类型: Single
# 默认值: 15
report_timeout = 15
```

### 示例

```ini
ban_mods = net.cucorelib, CUAdvancedBossBar
require_thm = true
```

此配置下，加入服务器的玩家必须安装 Tick Happy Mod，且不能安装 `net.cucorelib` 或 `CUAdvancedBossBar`。

## 工作原理

### 单人模式

- `Awake()` 将 `Chainloader.PluginInfos` 与 `BanModsList` 比对。
- 发现被封禁 Mod → 日志记录 → `Application.Quit()`。

### 多人模式（服务端）

1. 通过反射将消息处理器注入到 KrokoshaMP 的 `SERVER_MESSAGE_HANDLERS` 字典。
2. 客户端加入时（`NetPlayer.OnPlayerJoined`），服务端启动超时计时。
3. 客户端（若安装了 THM）通过 `Net.Client_Send` 发送其完整 Mod 列表。
4. 服务端比对客户端 Mod 列表与 `BanModsList`。
5. 发现被封禁 Mod → `Server_DoAlertSingle` 弹窗提示 + `Net.Server_Kick` 踢出。
6. 若 `require_thm = true` 且超时内未收到报告 → 内置 KrokoshaMP 弹窗提醒 + 踢出。

### 多人模式（客户端）

- `OnPlayerJoined` 触发时（`is_local`），将 `Chainloader.PluginInfos.Keys` 上报给服务端。
- 客户端无需安装 THM 也可加入（仅限 `require_thm = false` 时），只是不会上报 Mod 列表。

## 开发

- **目标框架：** .NET Framework 4.8
- **依赖：** BepInEx 5, HarmonyLib, KrokoshaCasualtiesMP, LiteNetLib
- **构建：** `dotnet build`

### 项目结构

```
Tick-Happy-Mod/
├── Plugin.cs                  # 主插件逻辑
├── TickHappyMod.csproj        # 项目文件
├── Directory.Build.props      # 共享构建属性（游戏路径）
├── README.md / README_ZH.md   # 文档
└── CHANGELOG.md / CHANGELOG_ZH.md
```

## 许可

[GPL v3](LICENSE.md)
