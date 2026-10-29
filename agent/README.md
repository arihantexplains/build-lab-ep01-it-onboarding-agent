# Copilot Studio agent: IT Admin Assistant

| Setting | Value |
|---|---|
| Name | IT Admin Assistant |
| Description | Helps IT admins and helpdesk staff with user onboarding, user management, group management and Entra ID administration. |
| Orchestration | Generative orchestration on |
| Instructions | [instructions.md](instructions.md) |
| Tools | One: [Run admin action](tool-run-admin-action.md) |
| Knowledge | None. Every answer comes from the tool, so the agent never guesses about people or access. |
| Topics | No custom topics. The default system topics (greeting, escalate, fallback and so on) stay as they are; the instructions and the tool description do the routing. |
| Authentication | Microsoft Entra ID (authenticate with Microsoft) |
| Channels | Teams and Microsoft 365 Copilot |

## Who can use it

Share the agent only with a security group such as `Demo-IT-Admin-Agent-Users`, and put that group in `ab_ProtectedGroups` so the agent can never add people to it. Add Conditional Access (MFA, compliant device) for the members if your demo tenant allows it.

## Build order

Build the plugin, then the flow, then the agent. The agent is the last and simplest piece: if a flow test run works, the agent will too.

Try it with the prompts in [test-prompts.md](test-prompts.md).
