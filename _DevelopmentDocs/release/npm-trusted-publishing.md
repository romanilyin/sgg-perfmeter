# npm Trusted Publishing

`com.sungeargames.perfmeter` publishes from GitHub Actions through npm Trusted Publishing and OpenID Connect. The release workflow must not use `NPM_TOKEN`, `NODE_AUTH_TOKEN`, an npm auth secret or a repository `.npmrc` auth entry.

## One-Time npm Configuration

Trusted-publisher configuration is available through npm CLI `11.15.0+`, but creating it requires interactive npm account authentication and 2FA. Use either the CLI or the equivalent npmjs.com form.

After this workflow is merged to the default branch, an authenticated package owner can run:

```bash
npx --yes npm@latest trust github com.sungeargames.perfmeter --repo romanilyin/sgg-perfmeter --file publish-npm.yml --env npm --allow-publish --yes
```

If the CLI session is not authenticated, configure the same relationship on npmjs.com:

1. Sign in to npmjs.com with an owner of `com.sungeargames.perfmeter`.
2. Open the package settings and the **Trusted Publisher** section.
3. Select **GitHub Actions**.
4. Enter these exact values:

| Field | Value |
| --- | --- |
| Organization or user | `romanilyin` |
| Repository | `sgg-perfmeter` |
| Workflow filename | `publish-npm.yml` |
| Environment name | `npm` |
| Allowed actions | `npm publish` |

Use only the workflow filename, not `.github/workflows/publish-npm.yml`. All values are case-sensitive. The workflow must already exist on the default branch before either configuration method is used.

After saving the trusted publisher, open **Publishing access**, select **Require two-factor authentication and disallow tokens**, and save the package settings. This blocks traditional write tokens without blocking trusted OIDC publishing.

## Automated Release

1. Complete the release gates and merge the candidate to `main`.
2. Create a tag that exactly matches `package.json`, for example `2026.8.5-2`.
3. Publish a normal GitHub Release for that tag.
4. GitHub Actions runs `.github/workflows/publish-npm.yml` on the GitHub-hosted runner and publishes from `Assets/Scripts/SGG.PerfMeter`.
5. Verify the published npm version and its provenance attestation before updating public install pins.

The workflow uses Node 24 and npm 11.5.1 or later, requests `id-token: write`, and receives a short-lived OIDC credential directly from npm. Trusted publishing adds provenance automatically for this public repository and public package.

`npm whoami` and `npm publish --dry-run` do not validate OIDC authentication. OIDC credential exchange occurs only during `npm publish` or `npm stage publish` inside the configured workflow.

## GitHub Controls

- The workflow uses the protected `npm` GitHub Environment.
- The environment accepts only release tags matching the configured version-tag policy.
- The release tag must exactly match the package version or the workflow fails before publishing.
- Do not add an npm write token as a fallback; fix the trusted-publisher, workflow filename, environment or OIDC permission instead.

Official reference: <https://docs.npmjs.com/trusted-publishers/>.
