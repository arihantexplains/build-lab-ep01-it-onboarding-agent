# Setup in a demo tenant

Build bottom-up: identity first, then Dataverse, then the plugin, the flow and the agent last. Test each layer before you add the next one, so when something breaks you know which layer it is.

Every `<...>` value below is yours to fill in. Keep them out of Git.

## 1. The demo tenant

- A Microsoft 365 developer or trial tenant with a neutral name.
- Licences: a Microsoft 365 E3/E5 trial or Business Premium for test users, Entra ID P1 (P2 optional), a Copilot Studio trial.
- A Power Platform environment with Dataverse.
- An Azure subscription for Key Vault.

## 2. The fake organisation

- 30 to 100 made-up users, all with display names starting with `Demo-` (or your own prefix).
- Departments such as Finance, Sales, HR, IT and Operations, with job titles and a manager chain a few levels deep.
- Security groups that also start with `Demo-`, for example `Demo-VPN-Users` and `Demo-Finance-Team`. Add one dynamic group and one role-assignable group if you want to see the skip rules work.
- An administrative unit, for example `Demo-Employees`, containing the normal staff. Keep your admin users **outside** it.
- A security group for the people allowed to use the agent, for example `Demo-IT-Admin-Agent-Users`.
- Optional: a shared mailbox to send from, and a SharePoint site for reports.

## 3. App registration

1. **Entra admin center > App registrations > New registration.** Name it something like `onboarding-agent-graph`, single tenant, no redirect URI.
2. Note the **Application (client) ID** and **Directory (tenant) ID**. They go into environment variables, not code.
3. **Certificates & secrets:** add a client secret with a short expiry and store it in Key Vault straight away. (A certificate is better outside a demo.)
4. **API permissions > Microsoft Graph > Application permissions:** add the 15 in [permissions.md](permissions.md), then **Grant admin consent**.
5. **Roles & administrators:** assign **User Administrator** to the app's service principal, scoped to the administrative unit.
6. Lock `Mail.Send` to the sender mailbox (Exchange Online RBAC for Applications or an application access policy).
7. Grant the app write access on the reports site only (`Sites.Selected`).
8. Quick proof before any code: get an app-only token and call `GET /users?$top=5`. If this fails, nothing after it will work.

## 4. Solution and environment variables

1. In Power Apps, create a solution (for example "Onboarding Agent") with a publisher prefix. The code uses `ab_`; if you choose another prefix, change the names in `Config.cs` and the flow too.
2. Create the environment variables from [`appsettings.sample.json`](../appsettings.sample.json):

| Schema name | Type | What to put in it |
|---|---|---|
| `ab_GraphTenantId` | Text | Tenant ID |
| `ab_GraphClientId` | Text | App registration client ID |
| `ab_GraphClientSecret` | Secret (Azure Key Vault) | Reference to the Key Vault secret |
| `ab_SenderMailbox` | Text | The mailbox that sends email |
| `ab_ReportsSiteId` | Text | Graph site ID of the reports site (`hostname,siteId,webId`) |
| `ab_AdminUnitId` | Text | Object ID of the administrative unit |
| `ab_UpnDomain` | Text | Domain for new user principal names |
| `ab_RequiredPrefix` | Text | Display-name prefix of the managed scope, for example `Demo-` |
| `ab_AssignableRoles` | Text or JSON | Roles `AssignRole` may grant, for example `["Reports Reader"]` |
| `ab_ProtectedGroups` | Text or JSON | Groups the agent may never change or clone |
| `ab_ApproverEmail` | Text | The IT lead who approves risky actions (used by the flow) |

3. For the secret: create the Key Vault secret, give the Dataverse service access as described on Microsoft Learn for secret environment variables, then link the variable to the vault and secret name.

## 5. Audit table and Custom API

Create the `ab_adminactionlog` table and the `ab_ExecuteAdminAction` Custom API exactly as listed in [src/OnboardingAgent/README.md](../src/OnboardingAgent/README.md):

- Custom API: unbound, not a function. Request parameters `ActionName`, `ParametersJson`, `RequestedBy` (required) and `ApprovalId` (optional). Response properties `Success`, `Message`, `ResultJson`, `CorrelationId`.
- Set an **Execute Privilege Name** and give it only to the identity the flow's Dataverse connection runs as (or a small IT group).

## 6. Plugin

```bash
cd src/OnboardingAgent
sn -k OnboardingAgent.snk
dotnet build -c Release
```

1. Open the Plugin Registration Tool (`pac tool prt`) and connect to the environment.
2. Register `bin/Release/net462/OnboardingAgent.dll` (sandbox, database storage).
3. On the Custom API, set the plugin type to `OnboardingAgent.ExecuteAdminAction`.
4. Add the assembly to your solution.
5. Test from the Dataverse Web API or the flow designer: `ActionName = CountByDepartment`, `ParametersJson = {}`. You should get `Success = true` and a row in the audit table.

## 7. Agent flow

Build "Run admin action" with [flow/README.md](../flow/README.md). Test one read operation and one approval (approve once, reject once) before you touch Copilot Studio.

## 8. Copilot Studio agent

Follow [agent/README.md](../agent/README.md): create "IT Admin Assistant", paste the instructions, add the flow as its only tool, and share it with the IT admin group only.

## 9. Test

Run [agent/test-prompts.md](../agent/test-prompts.md), including every prompt that must be refused. Then open the audit table and check every call is there.

## When you're done

- Rotate or delete the client secret.
- Keep the demo tenant and fake organisation if you want to build the next episodes.
