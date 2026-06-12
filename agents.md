# agents.md

## 项目背景(必需) 

本项目是一个 `Unity 2022.3 lts 版本` bulit-in 管线 开发的2D卡牌像素风游戏。

## 技术框架(必需) 

- 项目框架采用 `QFramework`。
- 项目本地代码入口在：Assets/Scripts，QFramework框架代码入口在：Assets/QFramework。

## 设计文档(按需) 

- 游戏设计案统一存放在 `Assets/Docs` 目录下。在进行需求理解、功能实现、数据结构设计或 UI 调整前，请优先查阅该目录中的设计资料。  
- 游戏开发过程性笔记：Assets/Notes，根据自己需求选择性阅读。如果开发过程中用户要求落地笔记、汇报等需求，统一落地在此目录。
- 当前Notes中几个值得关注的笔记:
1. 游戏开发核心架构设计案：Assets/Notes/ArchitectureDesign.md。
2. Assets/Notes/QframeworkNotes/QFramework_Docs_用户手册.md，在QFramework范式开发过程中遇到问题可以参考，注意手册体量较大，根据目录查找即可。
3. 在Assets/Notes/QframeworkNotes/QFramework_Docs_API.md 有QFramework API文档。

## 项目规则(必需) 

- 开发本项目前，请优先阅读 `rules.md`，其中包含架构规范、渲染配置约定及文件保护规则等全局性约束。
- 当前处于原型开发期，唯一游戏场景：Assets/Scenes/TableNineBootstrap.unity
- 项目有unity mcp，在落地实现时注意了解功能并辅助使用，如果发现无法使用再回退文件操作形式开发。
- 项目已经初始化codegraph，在有代码查询需求，架构了解需求的情况下可以利用codegraph mcp进行了解。

## 协作约定(建议) 

- 以 `Assets/Docs` 中的游戏设计案为准，避免脱离现有设定自行扩展。
- 修改或新增功能时，优先复用项目内已有的框架、模块和资源组织方式。
- 保持代码、命名和目录结构的统一性，方便后续协作与维护。

## 维护要求(建议) 

- 如果你在项目里发现新的高风险坑点、反复发生的错误或容易误导后续 agent 的事实，追加到本文件。

1. AbstractCommand / AbstractSystem / IController 中禁止裸调架构能力方法：
在 AbstractCommand.OnExecute()、AbstractSystem.OnInit()、IController 实现类等上下文中，访问 Model、System、Command、Event、Query 时，必须通过 this. 调用 QFramework 扩展方法。