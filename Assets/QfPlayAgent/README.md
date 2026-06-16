# QfPlayAgent

可移植的 **QFramework Command ↔ Unity MCP** 适配包，让外部 AI 通过 `qf_play_agent` 自定义工具执行「玩家意图级」Command，并配合截图、日志、快照做探索式验证。

## 目录

```text
Assets/QfPlayAgent/
  Runtime/          # 与游戏无关的核心：目录、工厂、调用器、会话日志
  Editor/           # unity-mcp 自定义工具 qf_play_agent
  Samples/TableNine/ # 本项目示例适配（可复制改名为你的项目）
```

## 移植到新项目

1. 复制整个 `Assets/QfPlayAgent/` 文件夹（可删除 `Samples/TableNine`）。
2. 确保项目已安装：
   - `com.coplaydev.unity-mcp`
   - QFramework
3. 新建 `Samples/YourGame/`（Editor asmdef）并实现：
   - `IPlayAgentArchitectureProvider` — 返回你的 `IArchitecture`
   - `IPlayAgentSnapshotProvider`（可选）— 评估用结构化快照
   - `PlayAgentCatalogConfig` 或代码工厂 — **显式 allowlist 玩家 Command**
4. 在 `[InitializeOnLoad]` 中调用：
   ```csharp
   PlayAgentBootstrap.RegisterArchitectureProvider(...);
   PlayAgentBootstrap.RegisterSnapshotProvider(...); // optional
   PlayAgentBootstrap.RegisterCatalog(...);
   ```
5. **重连 MCP 客户端**（或重启 Cursor）以发现 `qf_play_agent` 工具。
6. 建议使用 **HTTP transport** 连接 unity-mcp（stdio 下 custom tools 可能不可见）。

## MCP 工具：`qf_play_agent`

通过 `execute_custom_tool` 调用，`parameters.action` 支持：

| action | 说明 |
|--------|------|
| `list_commands` | 列出已暴露 Command 及参数 schema |
| `invoke_command` | 执行 Command（`command` + `args`） |
| `observe` | 抓场景相机截图到 `Assets/QfPlayAgent/AgentRuns/{session}/` |
| `wait` | 等待若干 Editor 帧 |
| `get_command_log` | 本会话 AI 调用日志 |
| `reset` | 停止 Play Mode + 清空会话 |
| `get_runtime_snapshot` | 结构化快照（由项目 SnapshotProvider 提供） |
| `read_logs` | 读取 Unity Console 最近条目 |

### 示例

```json
{
  "tool_name": "qf_play_agent",
  "parameters": {
    "action": "invoke_command",
    "command": "DebugPingCommand",
    "args": {},
    "wait_frames": 5
  }
}
```

## 设计原则

- **Command 管理独立**：哪些 Command 暴露给 AI 由项目侧 Catalog / `[QfAiAction]` 决定，核心包不硬编码游戏逻辑。
- **玩家意图优先**：默认 `RequireExplicitAllowlist = true`，避免 AI 直接调用内部/作弊 Command。
- **外部 AI 主循环**：工具不做「一口气跑完整局」；由 MCP 客户端看图 → 选 Command → wait → 再看图。
- **证据链**：截图 + command log + console + snapshot，便于回放与回归汇报。

## 可选：Command 元数据

在 Command 类上标记（需 Runtime 引用 `QfPlayAgent.Runtime`）：

```csharp
[QfAiAction(Description = "释放技能", Risk = QfAiRisk.PlayerInput)]
public class UseSkillCommand : AbstractCommand { ... }
```

也可用 `PlayAgentCatalogConfig` ScriptableObject 维护 allowlist（推荐，零侵入游戏 Command 文件）。

## 已知限制

- 直接 `SendCommand` 会绕过 UI Raycast / InputSystem，需抽样做真实点击交叉验证。
- `observe` 依赖场景中存在 `Camera.main` 或任意 Camera。
- 复杂 Command 参数（如 `CardUid`）需项目侧提供简化包装 Command 或扩展类型转换。
