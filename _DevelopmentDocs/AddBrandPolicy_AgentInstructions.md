# Task For Agent: Add SGG / SunGear Games Brand Usage Policy

## Goal

Add a short brand usage policy to the repository. The policy protects the studio/company brand names `SGG`, `SunGear Games`, `SunGearGames`, and confusingly similar variants. It does not try to protect generic product names such as `PerfMeter` by themselves.

Do not describe these names as registered trademarks unless a registered trademark certificate is later added. Use the wording "brand", "brand names", "company name", "trade name", and "brand assets".

## Files

Public-facing documentation:

```text
docs/<lang>/brand.md
```

Internal templates and maintenance notes:

```text
_DevelopmentDocs/BrandReminder.md
_DevelopmentDocs/BrandReminder_RU.md
_DevelopmentDocs/AddBrandPolicy_AgentInstructions.md
```

The reminder files are internal templates for comments, issues, or messages to projects that use SGG / SunGear Games branding in a confusing way.

## README Changes

Keep brand policy links out of the top README navigation. Link them near the license sections instead:

```md
- Brand usage policy: [English](./docs/en/brand.md), [Russian](./docs/ru/brand.md), and other localized `docs/<lang>/brand.md` files
```

For Russian docs:

```md
- Политика использования бренда: [русская](./brand.md), [английская](../en/brand.md) и другие локализованные `docs/<lang>/brand.md`
```

## License Check

Check `LICENSE.md` and `LICENSE.ru.md` for clauses saying that trademark, brand, logo, and company name rights are not granted.

If such clauses already exist, do not duplicate them. This repository already has brand/trademark restrictions in the EULA, including sections `6.1(g)` and `7.7`.

Do not replace the license with a different license. Only add a missing brand-rights clarification if needed in a future repository.

## Important Wording Constraints

- Do not claim that `SGG`, `SunGear Games`, or `SunGearGames` are registered trademarks unless registration is later confirmed.
- Do not forbid ordinary GitHub forks used for pull requests, code review, learning, or personal experiments.
- Do not forbid truthful attribution such as "Based on software by SunGear Games".
- Do not claim exclusive rights to the generic name `PerfMeter` by itself.
- Do not over-expand the policy into a broad derivative-products policy. The point is brand confusion, not banning normal use under the repository license.
- Keep public policy files in localized `docs/<lang>` folders.
- Keep friendly reminder templates in `_DevelopmentDocs`, not in public docs navigation.
