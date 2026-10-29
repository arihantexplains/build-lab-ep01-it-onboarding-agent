# Contributing

Thanks for taking the time. This repo is the companion to a YouTube episode, so the main goal is that someone can follow the video and the docs and get the same result. Fixes that make that easier are the most welcome.

## Good contributions

- A setup step that didn't work for you, and what fixed it.
- A Graph permission that has a narrower option now (Microsoft adds these over time).
- A bug in an operation, with the steps to reproduce it in a demo tenant.
- Clearer docs or comments.
- Hardening, for example having the plugin verify the approval record before a risky operation.

Bigger changes, like new operations, are best discussed in an issue first, so we agree on the shape before you spend the time.

## Before you open a pull request

1. **Build it.** `cd src/OnboardingAgent` then `dotnet build -c Release`. It must build with no errors and no new warnings. Every public member has an XML doc comment; a missing one shows up as warning CS1591.
2. **Test it in a demo tenant** with made-up users. Never test with production users.
3. **Scrub it.** No tenant IDs, client IDs, secrets, GUIDs from your tenant, email addresses, environment URLs or real names in code, docs, screenshots or commit messages. Use placeholders like `<your-demo-domain>`.
4. **Match the style.** Small focused classes, one per operation; validate first, then change; never throw from the plugin entry point; never return or log a password.
5. **Update the docs** that your change touches (`docs/operations.md`, `docs/permissions.md`, the flow descriptions) and add a line to `CHANGELOG.md`.

## Writing style for docs

Plain, friendly English, like explaining to a colleague. Short sentences. Say "user" or "user onboarding", not "account". No em dashes.

## Security issues

Please don't open a public issue for a security problem. See [SECURITY.md](SECURITY.md).

By contributing you agree that your work is released under the [MIT License](LICENSE).
