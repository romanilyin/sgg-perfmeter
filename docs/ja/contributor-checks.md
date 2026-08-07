# Contributor Checks

変更に合う最も軽い check を使用してください。Unity compile と Test Runner checks は高コストなため、runtime/editor behavior changes では期待されますが、すべての documentation-only edit で必要ではありません。

## Documentation Or Metadata Only

```bash
git diff --check
```

affected links も確認し、複数言語に影響する場合は docs の同期を保ってください。

## Runtime Or Editor Code Changes

target project の Unity compile check を実行し、pull request に command を含めます。tests が関連する場合は、EditMode または PlayMode Test Runner checks を実行します。

maintainer-only release gates または device smoke tests については、現在の project-maintainer checklist を使用し、pull request に command または environment を記載します。

## Pull Request を開く前に

- `git status` を確認し、意図した files のみを stage します。
- `Library/`、`Logs/`、`Temp/`、`Obj/`、local build outputs などの generated Unity state を commit しないでください。
- secrets、`.env` files、device dumps、private logs、unrelated screenshots を commit しないでください。
- runtime profiler behavior が変わる場合は、同じ PR で tests と user-facing docs を更新します。

## Performance CI

`.github/workflows/performance-ci.yml` は、同一 repository の pull request、`main` への push、manual run で、Unity `6000.4.12f1` と `6000.5.6f1` の full EditMode correctness suite と isolated performance tests を実行します。GitHub は fork pull request に Unity license secrets を公開しないため、その job は skip されます。CI は ephemeral checkout にのみ `com.unity.test-framework.performance` `3.5.0` を追加し、package に hard dependency は残しません。versioned threshold は `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json` にあり、CI は raw NUnit XML、converted JUnit XML、performance JSON、logs を upload します。
