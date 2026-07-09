# 更新日志

本文件记录本项目所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/)，本项目遵循 [语义化版本控制](https://semver.org/)。

---

## v1.0.0

### 新增

- **封禁 Mod 系统** — 服务端通过可配置的 GUID 列表封禁 Mod（`ban_mods`）
- **要求安装 THM** — 可选服务端设置（`require_thm`），踢出未安装 Tick Happy Mod 的玩家
- **多人 Mod 检测协议** — 安装了 THM 的客户端在加入时向服务端上报完整 Mod 列表
    - 自定义网络消息（msgId `33993`），通过反射直接注入 `SERVER_MESSAGE_HANDLERS`
    - 服务端比对客户端 Mod 列表与 `BanModsList`，匹配则踢出
- **踢出原因通知** — 踢出前使用 `Server_DoAlertSingle`（对 THM 客户端）和 KrokoshaMP 内置弹窗 msgId `30006`（对无 THM 客户端）
- **超时机制** — `report_timeout`（默认 15 秒），超时未上报 Mod 列表的玩家被踢出
- **单人自检** — 本地检测到被封禁 Mod 时退出游戏
- **KrokoshaCasualtiesMP 集成** — `[BepInDependency("KrokoshaCasualtiesMP", SoftDependency)]`
    - 订阅 `NetPlayer.OnPlayerJoined` 事件检测玩家加入
    - 通过反射注入 `Net.SERVER_MESSAGE_HANDLERS` 实现无 Harmony 的服务端消息注册
- **多分隔符拆分** — `BanModsList` 支持 `,` `;` `|` `、` `，` 空格 制表符 换行

### 变更

- 目标框架从 `net472` 升级至 `net48` 以兼容 KrokoshaMP

### 技术说明

- KrokoshaMP 版本差异在运行时通过反射处理
- 跳过对 `Net.InvokeServerMessage` 的 Harmony 补丁（该方法在 KrokoshaMP v4.0.1 中被 DMD 包装）
