# Security notes

An agent that can create users and reset passwords is a serious tool. These are the guardrails, and where each one lives. The rule I follow: every guardrail the agent has, the plugin enforces again on its own. The agent is the friendly front door, not the lock.

## Demo tenant only

- Build and test this in a demo or developer tenant. **Never point it at production users**, and never use real people's names or data while testing.
- Use a separate browser profile for the demo tenant.
- When you're done, rotate or delete the client secret.

## Least privilege

- 15 narrow Graph application permissions, listed in [permissions.md](permissions.md). No `Directory.ReadWrite.All`, no `AdministrativeUnit.ReadWrite.All`.
- The User Administrator role on the app is scoped to the administrative unit, not the tenant.
- `Mail.Send` is locked to one mailbox; `Sites.Selected` to one site.

## The fence

Enforced in `Validator.cs` for every operation:

- Every user and group must have the display-name prefix (`ab_RequiredPrefix`, case-sensitive).
- Writes also need the user to be a member of the administrative unit.
- Writes are refused for anyone holding an active, eligible or group-based directory role. If that check can't be completed, it fails closed.
- Dynamic groups, role-assignable groups and the groups in `ab_ProtectedGroups` are never changed or cloned.
- `AssignRole` only grants roles on the `ab_AssignableRoles` allow-list, scoped to the administrative unit. Anything else is refused before Graph is called.

## Approvals

`ResetPassword`, `AssignRole`, `RemoveRole`, `RevokeSessions`, `DisableUser` and `OffboardUser` need an approval ID. The flow only supplies one after the IT lead approves, and the plugin refuses these operations without it, so the agent can't skip the approval. The plugin checks that an approval ID is present and well-formed, but it doesn't look it up in the Approvals service. So someone who holds the Custom API's Execute Privilege and calls the Dataverse Web API directly could pass a made-up ID. Keep that privilege to the smallest group you can (ideally only the identity the flow's Dataverse connection runs as). If you need more than that, have the plugin look up the approval record before it acts. That's a good first contribution.

## Passwords

- Temporary passwords are generated in the plugin and emailed to the user's manager. They are not saved to Sent Items.
- They are never returned to the flow or the agent, never logged and never traced.
- The audit logger masks any parameter whose name contains password, secret, token or credential.
- The approved steps in the flow have secure inputs and outputs turned on.

## Who can call it

- The agent is shared only with an IT security group, with Entra ID authentication.
- The Custom API has an Execute Privilege Name, so only the people or identity you give that privilege can call it. Keep that list as small as you can.

## Secrets

- No tenant ID, client ID, secret, mailbox or site is in the code. Everything comes from Dataverse environment variables.
- The client secret lives in Azure Key Vault and is read through `RetrieveEnvironmentVariableSecretValue`.
- `.gitignore` blocks signing keys, certificates, real settings files and solution zips.

## Audit

Every call writes a row to `ab_adminactionlog`: who asked, which operation, the target, the approval ID, success, duration and a correlation ID. The same changes also show in the Entra ID audit logs.

## What a regulated organisation would add

- PIM for role assignment instead of letting the app assign roles at all.
- A certificate or a managed identity instead of a client secret.
- Separate service principals per action family.
- Conditional Access (MFA, compliant device) for the people allowed to use the agent.
