# The tool: Run admin action

The agent has exactly **one tool**: the "Run admin action" agent flow. One well-described tool with a clear list of operations turned out to be more reliable than six separate flows, because the orchestrator only has to choose an operation name, not a tool.

## How to add it

1. In the agent, open **Tools > Add a tool > Flow** and pick **Run admin action** (it must be in the same solution and environment).
2. Name: `Run admin action`.
3. Description: paste the text below. The orchestrator reads this to decide when and how to call the tool.
4. Inputs:
   - `ActionName` and `ParametersJson`: filled by the agent ("Dynamically fill with AI"). Their descriptions come from the flow trigger, see [../flow/README.md](../flow/README.md).
   - `RequestedBy` and `ApprovalId`: set to **not prompt the user**. The flow records the signed-in user itself, and approval IDs only ever come from the approval inside the flow.
5. Outputs: `Message`, `ResultJson`, `Success`.
6. Authentication: run the flow with the connection of the person chatting (**Invoker**), so the audit log shows who asked.

## Description

```text
Runs any IT admin operation in Microsoft Entra ID through the Onboarding Agent plugin: 33 operations for user onboarding, user management, group management, Entra ID administration, and questions and reports. Use it for every request about people, groups, licences, roles, sign-in or the audit log: new joiners, leavers, password resets, group changes, role changes, look-ups, headcounts and reports. Set ActionName to exactly one operation name from its list, and ParametersJson to a JSON object with that operation's parameters, using names as the user wrote them. For OnboardUser and CloneAccess, call first with previewOnly true, show the summary, and call again with previewOnly false only after the user confirms. Risky operations (ResetPassword, AssignRole, RemoveRole, RevokeSessions, DisableUser, OffboardUser) go to the IT lead for approval: confirm with the user first, then say it was sent to the IT lead and nothing has changed yet. It never returns passwords.
```
