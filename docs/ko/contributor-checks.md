# Contributor Checks

변경에 맞는 가장 가벼운 check를 사용합니다. Unity compile 및 Test Runner check는 비용이 크므로 모든 documentation-only edit가 아니라 runtime/editor behavior change에 필요합니다.

## Documentation 또는 Metadata Only

```bash
git diff --check
```

영향을 받는 link도 확인하고, 둘 이상의 language가 영향을 받는 경우 해당 언어 버전의 동기화를 유지합니다.

## Runtime 또는 Editor Code Changes

target project에 대해 Unity compile check를 실행하고 pull request에 command를 포함합니다. test가 관련 있으면 EditMode 및/또는 PlayMode Test Runner check를 실행합니다.

maintainer-only release gate 또는 device smoke test에는 현재 project-maintainer checklist를 사용하고 pull request에 command 또는 environment를 언급합니다.

## Pull Request를 열기 전

- `git status`를 확인하고 의도한 file만 stage합니다.
- `Library/`, `Logs/`, `Temp/`, `Obj/`, local build output 같은 generated Unity state를 commit하지 않습니다.
- secret, `.env` file, device dump, private log, unrelated screenshot을 commit하지 않습니다.
- runtime profiler behavior가 변경되면 같은 PR에서 test와 user-facing docs를 update합니다.

## Performance CI

`.github/workflows/performance-ci.yml`은 동일 repository의 pull request, `main` push 및 manual run에서 Unity `6000.4.12f1`과 `6000.5.6f1`의 full EditMode correctness suite와 isolated performance test를 실행합니다. GitHub가 fork pull request에 Unity license secret을 제공하지 않으므로 해당 job은 skip됩니다. CI는 ephemeral checkout에만 `com.unity.test-framework.performance` `3.5.0`을 추가하며 package에는 hard dependency가 남지 않습니다. versioned threshold는 `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`에 있고 CI는 raw NUnit XML, converted JUnit XML, performance JSON과 log를 upload합니다.

같은 workflow는 repository variable `PERFMETER_UNITY_CI_ENABLED`가 `true`이고 GameCI-compatible Unity credentials가 구성된 경우 두 Unity version에서 별도의 full PlayMode lifecycle job을 실행합니다. `Tests/PlayMode/**` 변경은 workflow를 trigger하며, credentials가 비활성화되거나 fork PR인 경우 명시적으로 skipped job이 됩니다.
