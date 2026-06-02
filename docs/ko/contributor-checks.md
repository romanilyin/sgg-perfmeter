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
