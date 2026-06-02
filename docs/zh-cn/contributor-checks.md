# Contributor Checks

使用与变更相匹配的最轻量检查。Unity compile 和 Test Runner checks 成本较高，因此它们适用于 runtime/editor behavior changes，而不是每次 documentation-only edit。

## Documentation Or Metadata Only

```bash
git diff --check
```

同时验证受影响 links，并在同时影响多语言文档时保持对应文档同步。

## Runtime Or Editor Code Changes

为目标项目运行 Unity compile check，并在 pull request 中包含命令。相关时运行 EditMode 和/或 PlayMode Test Runner checks。

对于 maintainer-only release gates 或 device smoke tests，使用当前 project-maintainer checklist，并在 pull request 中说明 command 或 environment。

## Before Opening A Pull Request

- 检查 `git status`，只 stage 预期文件。
- 不要提交生成的 Unity state，例如 `Library/`、`Logs/`、`Temp/`、`Obj/` 或 local build outputs。
- 不要提交 secrets、`.env` files、device dumps、private logs 或无关 screenshots。
- 如果 runtime profiler behavior 发生变化，在同一个 PR 中更新 tests 和 user-facing docs。
