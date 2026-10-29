# Security

This is a teaching sample from a YouTube episode, not a supported product. Please read this before you run it anywhere.

## Use a demo tenant

- Build and test it only in a demo or developer tenant with made-up users.
- Never point it at production users or real people's data.
- When you finish testing, rotate or delete the app's client secret.

## Keep your settings out of Git

The repo contains placeholders only. Your tenant ID, client ID, secret, mailbox, site ID and administrative unit ID belong in Dataverse environment variables (the secret in Azure Key Vault), never in code, commits or issues. The `.gitignore` blocks signing keys, certificates, real settings files and solution exports, but check `git status` before every commit anyway.

If you open an issue, remove IDs, domains and names from logs and screenshots first.

## What the sample does to stay safe

The guardrails are described in [docs/security.md](docs/security.md): least-privilege Graph permissions, a display-name prefix and administrative unit as the fence, no actions on administrators, an approval step for risky operations, passwords that never reach the chat, and an audit row for every call.

## Reporting a problem

If you find a security problem in the code, please don't open a public issue with exploit details. Use GitHub's **Report a vulnerability** button on the Security tab of this repository (private vulnerability reporting), or open an issue that just says you found something and would like a private way to share it.

There is no bug bounty and no guaranteed response time, but I read every report and will fix real problems in the sample.
