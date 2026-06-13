# agents.md

## 项目背景

本项目是一个 `Unity 2022.3 LTS` Built-in 管线开发的 2D 卡牌像素风 Roguelike 游戏，名为 **TableNine**。核心玩法为九宫格棋盘驱动的卡牌战斗。

## 技术框架

- 项目框架采用 `QFramework`。
- 项目本地代码入口在：`Assets/Scripts`，QFramework 框架代码入口在：`Assets/QFramework`。
- QFramework Architecture 唯一注册入口：`Assets/Scripts/TableNine.cs`。

## 架构导航

**每次开始编程任务前，先阅读架构总览文档**：

```
Assets/Notes/QframeworkNotes/architecture.md
```

该文档是项目的架构总览与导航中心，包含：
- 文档地图（所有分层文档的路径和职责）
- QFramework 注册表（10 Utility + 8 Model + 15 System）
- 目录结构与分层规则
- 核心数据模型速查
- 核心流程（单局循环、战斗管线、帮助卡生命周期）
- 设计不变量

**分层权威文档**（代码改动后必须同步维护）：

| 文档 | 路径 | 职责 |
|------|------|------|
| architecture.md | `Assets/Notes/QframeworkNotes/architecture.md` | 架构总览与导航 |
| commands.md | `Assets/Notes/QframeworkNotes/commands.md` | Command 总表 |
| systems.md | `Assets/Notes/QframeworkNotes/systems.md` | System 总表 |
| models.md | `Assets/Notes/QframeworkNotes/models.md` | Model 总表 |
| events.md | `Assets/Notes/QframeworkNotes/events.md` | Event 总表 |
| controllers.md | `Assets/Notes/QframeworkNotes/controllers.md` | Controller / 表现层入口 |

**文档权威优先级**：`rules.md` > `architecture.md` > 各分层文档。

## 设计文档

- 游戏设计案统一存放在 `Assets/Docs` 目录下。在进行需求理解、功能实现、数据结构设计或 UI 调整前，请优先查阅该目录中的设计资料。
- 游戏开发过程性笔记：`Assets/Notes`，根据自己需求选择性阅读。如果开发过程中用户要求落地笔记、汇报等需求，统一落地在此目录。
- QFramework 用户手册：`Assets/Notes/QframeworkNotes/QFramework_Docs_用户手册.md`（体量较大，按目录查找）。
- QFramework API 文档：`Assets/Notes/QframeworkNotes/QFramework_Docs_API.md`。

## 项目规则

- 开发本项目前，请优先阅读 `rules.md`，其中包含架构规范、渲染配置约定及文件保护规则等全局性约束。
- 当前处于原型开发期，唯一游戏场景：`Assets/Scenes/TableNineBootstrap.unity`
- 项目有 Unity MCP，在落地实现时注意了解功能并辅助使用，如果发现无法使用再回退文件操作形式开发。
- 请在完成代码落地后主动触发unity mcp 的unity 刷新功能并阅读Console，这可以暴露编译代码的错误并帮助你进行修复。
- 项目已经初始化 codegraph，在有代码查询需求、架构了解需求的情况下强烈建议利用 codegraph MCP 进行了解、查找。

## 文档维护约定

**每次代码改动落地后，必须同步维护对应的分层权威文档**：

| 改动类型 | 需更新的文档 |
|---------|------------|
| 新增/删除/修改 Command | `commands.md` — 补充/修改对应条目 |
| 新增/删除/修改 System | `systems.md` — 补充/修改对应条目 |
| 新增/删除/修改 Model | `models.md` — 补充/修改对应条目 |
| 新增/删除/修改 Event | `events.md` — 补充/修改对应条目 |
| 新增/删除/修改 Controller/UI/Query/Utility | `controllers.md` — 补充/修改对应条目 |
| 架构层面变更（注册表、分层规则、核心流程） | `architecture.md` — 更新对应章节 |
| 更新 `architecture.md` 后 | 检查导航链接是否仍然有效 |

维护要求：
1. 新增条目必须包含：类名、文件路径、职责描述、关键 API/字段、依赖关系、发送/监听事件。
2. 删除条目时必须确认无其他文档引用该条目。
3. 修改条目时必须同步更新所有交叉引用处。
4. 每条文档顶部的「最后维护」日期应更新为改动日期。

## 协作约定

- 以 `Assets/Docs` 中的游戏设计案为准，避免脱离现有设定自行扩展。如用户要求与设计案冲突，应停下并询问。
- 修改或新增功能时，优先复用项目内已有的框架、模块和资源组织方式。但是如果用户的诉求明确有破坏性、侵入性，则以在基础框架编程规范下实现用户需求为第一要务。
- 保持代码、命名和目录结构的统一性，方便后续协作与维护。
- 如果执行过程中发现领域层代码缺少必要事件、ActionKit 动作、状态变更或队列触发点，AI 应回到既有架构内补齐，不得绕过架构直接在表现层硬写逻辑。

## 已知坑点

如果你在项目里发现新的高风险坑点、反复发生的错误或容易误导后续 agent 的事实，追加到本文件。

1. **AbstractCommand / AbstractSystem / IController 中禁止裸调架构能力方法**：在 `AbstractCommand.OnExecute()`、`AbstractSystem.OnInit()`、`IController` 实现类等上下文中，访问 Model、System、Command、Event、Query 时，必须通过 `this.` 调用 QFramework 扩展方法（如 `this.GetModel<T>()`、`this.SendCommand()`、`this.SendEvent()`）。
