# The 33 operations

Each operation is one class in [`src/OnboardingAgent/Functions`](../src/OnboardingAgent/Functions) and one entry in `ActionRouter.cs`. People and groups can be given by name; the plugin finds them and refuses anything outside the managed scope.

Operations marked **approval** only run with an `ApprovalId`, which the flow supplies after the IT lead approves.

## User onboarding

| Operation | What it does | Parameters |
|---|---|---|
| `OnboardUser` | New joiner in one call: create, set manager, licences, groups (listed or cloned), temporary password to the manager. Preview by default. | `displayName`, `givenName`, `surname`, `department`, `jobTitle`, `managerId`, `cloneFromUserId` or `groups`, `skus`, `previewOnly` |
| `CreateUser` | Creates the account only. With `managerId` the manager gets the temporary password; without it the password is discarded. | `displayName`, `givenName`, `surname`, `department`, `jobTitle`, `managerId` |
| `CloneAccess` | Copies direct group memberships from one user to another, skipping dynamic, role-assignable and protected groups. Preview by default. | `sourceUserId`, `targetUserId`, `previewOnly` |
| `AssignLicence` | Assigns licences by SKU part number or ID. | `userId`, `skus` |
| `SendEmail` | Sends from the configured mailbox to internal recipients only, with a footer naming the requester. | `to`, `subject`, `body` |

## User management

| Operation | What it does | Parameters |
|---|---|---|
| `GetUser` | Profile details for one user. | `userId` |
| `SearchUsers` | Finds managed users by part of the name, department or job title. | `name`, `department`, `jobTitle` |
| `UpdateUser` | Changes profile fields. | `userId`, `jobTitle`, `department`, `officeLocation`, `mobilePhone` |
| `SetManager` | Sets the manager. | `userId`, `managerId` |
| `RemoveManager` | Clears the manager. | `userId` |
| `GetUserLicences` | Licences, and whether each is direct or from a group. | `userId` |
| `RemoveLicence` | Removes direct licences. Group-based ones are reported, not removed. | `userId`, `skus` |
| `EnableUser` | Allows sign-in again. Groups and licences are not restored. | `userId` |
| `ResetPassword` | **Approval.** New temporary password, emailed to the manager. Never returned or logged. | `userId` |
| `RevokeSessions` | **Approval.** Signs the user out everywhere. | `userId` |
| `DisableUser` | **Approval.** Blocks sign-in, optionally revokes sessions too. | `userId`, `revokeSessions` |
| `OffboardUser` | **Approval.** Leaver in one call: block sign-in, revoke sessions, remove changeable groups and direct licences. Nothing is deleted. | `userId` |

## Group management

| Operation | What it does | Parameters |
|---|---|---|
| `ListGroups` | Managed groups, optionally filtered by name. | `nameStartsWith` |
| `ListGroupMembers` | Users in a managed group. Members outside the scope are counted, not named. | `group` |
| `GetGroupMemberships` | A user's groups. Out-of-scope groups are counted, not named. | `userId` |
| `AddToGroups` | Adds a user to groups. One ineligible group refuses the whole request. | `userId`, `groups` |
| `RemoveFromGroups` | Removes a user from groups, with the same all-or-nothing check. | `userId`, `groups` |

## Entra ID administration

| Operation | What it does | Parameters |
|---|---|---|
| `ListAssignableRoles` | The allow-list of roles `AssignRole` may grant. | none |
| `GetUserRoles` | Directory roles a user holds, with the scope of each. | `userId` |
| `AssignRole` | **Approval.** Grants an allow-listed role, scoped to the administrative unit. | `userId`, `role` |
| `RemoveRole` | **Approval.** Removes an allow-listed role. | `userId`, `role` |
| `GetSignInMethods` | Registered authentication methods, with phone numbers and emails masked. | `userId` |
| `GetSignInStatus` | Whether sign-in is allowed and when the user last signed in. | `userId` |

## Questions and reports

| Operation | What it does | Parameters |
|---|---|---|
| `GetManagerChain` | Walks up the manager chain, stopping at the top, a loop, or the first manager outside the scope. | `userId` |
| `GetDirectReports` | Direct reports. Out-of-scope reports are counted, not named. | `userId` |
| `CountByDepartment` | Headcount per department, for in-scope users. | `department` (optional) |
| `AuditQuery` | Entra ID audit events for a user, or this tool's own action log. | `userId` or `source: "log"`, `days` |
| `BuildExcelReport` | An `.xlsx` of users or a group's members, saved to the reports site. | `reportType` (`Users` or `GroupMembers`), `department` or `group` |

The parameter classes in [`Functions/Parameters`](../src/OnboardingAgent/Functions/Parameters) are the source of truth if anything here drifts.
