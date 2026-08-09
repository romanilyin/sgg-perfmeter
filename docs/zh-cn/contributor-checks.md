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

## Performance CI

`.github/workflows/performance-ci.yml` 会在同一 repository 的 pull request、推送到 `main` 以及 manual run 中，使用 Unity `6000.4.12f1` 和 `6000.5.6f1` 运行完整 EditMode correctness suite 与 isolated performance tests。GitHub 不会向 fork pull request 提供 Unity license secrets，因此该 job 会跳过。CI 只在 ephemeral checkout 中注入 `com.unity.test-framework.performance` `3.5.0`，package 不保留 hard dependency。versioned threshold 位于 `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`；CI 会上传 raw NUnit XML、converted JUnit XML、performance JSON 和 logs。

同一 workflow 会在 repository variable `PERFMETER_UNITY_CI_ENABLED` 为 `true` 且配置了 GameCI-compatible Unity credentials 时，针对两个 Unity version 运行单独的 full PlayMode lifecycle job。`Tests/PlayMode/**` 下的更改会 trigger workflow；credentials 未启用或 fork PR 会得到明确的 skipped job。
