# agents.md

## 项目背景

本项目是一个 `Unity` 开发的卡牌像素风游戏。

## 设计文档

游戏设计案统一存放在 `Assets/Docs` 目录下。  
在进行需求理解、功能实现、数据结构设计或 UI 调整前，请优先查阅该目录中的设计资料。

## 技术框架

项目框架采用 `QFramework`。项目本地代码入口在：Assets/Scripts，QFramework框架代码入口在：Assets/QFramework。
开发qframework代码前请看：rules.md。

## 协作约定

- 以 `Assets/Docs` 中的游戏设计案为准，避免脱离现有设定自行扩展。
- 修改或新增功能时，优先复用项目内已有的框架、模块和资源组织方式。
- 保持代码、命名和目录结构的统一性，方便后续协作与维护。
- 项目有unity mcp，在落地实现时注意了解功能并辅助使用，如果发现无法使用再回退你认为合适的开发形式。
- 严禁在任何情况以任何理由改动.unity后缀文件，相关改动需求可以考虑用unity mcp帮助落地。
- 项目已经初始化codegraph，在有代码查询需求，架构了解需求的情况下可以利用codegraph mcp进行了解。

## 维护要求

- 如果你在项目里发现新的高风险坑点、反复发生的错误或容易误导后续 agent 的事实，追加到本文件。

1. AbstractCommand / AbstractSystem / IController 中禁止裸调架构能力方法：
在 AbstractCommand.OnExecute()、AbstractSystem.OnInit()、IController 实现类等上下文中，访问 Model、System、Command、Event、Query 时，必须通过 this. 调用 QFramework 扩展方法。