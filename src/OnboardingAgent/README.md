# OnboardingAgent

A Dataverse plug-in for the `ab_ExecuteAdminAction` Custom API. It runs IT admin operations through Microsoft Graph with an app-only token.

## Build and register

1. Create a signing key once: `sn -k OnboardingAgent.snk`. The project signs the assembly automatically when the key file exists.
2. Run `dotnet build -c Release`. The build produces a single `OnboardingAgent.dll` that depends on nothing beyond the .NET Framework and the Dataverse SDK.
3. Register the assembly in the sandbox, then create the Custom API:
   - Unique name `ab_ExecuteAdminAction`, unbound, not a function, plug-in type `OnboardingAgent.ExecuteAdminAction`.
   - Request parameters: `ActionName`, `ParametersJson` and `RequestedBy` (String, required) and `ApprovalId` (String, optional).
   - Response properties: `Success` (Boolean), `Message`, `ResultJson` and `CorrelationId` (String).
   - Set an Execute Privilege Name so that only IT staff can call it.

## Environment variables

| Schema name | Type | Value |
|---|---|---|
| `ab_GraphTenantId` | Text | Tenant ID |
| `ab_GraphClientId` | Text | App registration client ID |
| `ab_GraphClientSecret` | Secret (Key Vault), or Text where no Key Vault is set up | Client secret |
| `ab_SenderMailbox` | Text | Mailbox that sends email |
| `ab_ReportsSiteId` | Text | Graph site ID (`host,siteGuid,webGuid`), used only by BuildExcelReport |
| `ab_AdminUnitId` | Text | Object ID of the administrative unit |
| `ab_UpnDomain` | Text | Domain for new UPNs, which is also the only domain SendEmail sends to |
| `ab_RequiredPrefix` | Text | Display-name prefix that defines the managed scope (case-sensitive) |
| `ab_AssignableRoles` | Text | Comma-separated list or JSON array of role names or template IDs |
| `ab_ProtectedGroups` | Text | Comma-separated list or JSON array of group IDs or names |

Values are cached for 5 minutes per organization.

## Audit table `ab_adminactionlog`

| Column | Type |
|---|---|
| `ab_name` | Text (200), primary name |
| `ab_actionname` | Text (100) |
| `ab_requestedby` | Text (200) |
| `ab_approvalid` | Text (100) |
| `ab_correlationid` | Text (100) |
| `ab_success` | Yes/No |
| `ab_message` | Multiline text (4,000) |
| `ab_target` | Text (300) |
| `ab_parametersjson` | Multiline text (4,000) |
| `ab_durationms` | Whole number |

The plug-in never throws. Every outcome, including a failure, goes back through the output parameters, so the audit row is never rolled back. If the audit write itself fails, the platform may still roll back the call.

## Graph application permissions

The 15 granted to the app registration, nothing more:

- `User.Create`, `User.ReadUpdate.All`, `User.ReadWrite.All`, `User.Read.All`, `Directory.Read.All`
- `User-PasswordProfile.ReadWrite.All`, `User.EnableDisableAccount.All`, `User.RevokeSessions.All`
- `GroupMember.ReadWrite.All`, `RoleManagement.ReadWrite.Directory`, `LicenseAssignment.ReadWrite.All`
- `UserAuthenticationMethod.Read.All`, `AuditLog.Read.All`
- `Mail.Send`, limited to the sender mailbox; `Sites.Selected`, with write access on the reports site only

Not granted on purpose: `AdministrativeUnit.ReadWrite.All`. New users are created with `POST /users`; adding them to the
administrative unit is reported as a failed step until an administrator adds them (or an AU-scoped role is assigned, which needs Entra ID P1).

## Behaviour notes

- **Scope.** Every user and group must have the prefix. Writes also require the user to be in the administrative unit and to hold no active, eligible or group-based directory role. The admin check fails closed. The one exception: when a tenant has no Entra ID P2 licence, the PIM eligibility check is skipped, because eligible assignments cannot exist without P2.
- **Passwords.** Temporary passwords go to the user's manager by email, and the email is not saved to Sent Items. They are never returned, logged or traced. CreateUser without `managerId` discards the password.
- **Roles.** AssignRole scopes the assignment to the administrative unit. Once a user holds a role, the admin check blocks further changes to that user.
- **Preview.** OnboardUser and CloneAccess default to `previewOnly: true`.
- **Time budget.** Each call has a 90-second budget. Each request times out after at most 25 seconds. Retries and paging stop early rather than run past the budget.
