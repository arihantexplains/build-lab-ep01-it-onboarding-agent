# Microsoft Graph permissions

The app registration gets **15 application permissions**, nothing more. Each one is the narrowest permission Microsoft Learn lists for the Graph call the plugin makes.

| Permission | Why the plugin needs it |
|---|---|
| `User.Create` | Create new users (`POST /users`) |
| `User.ReadUpdate.All` | Update profile fields such as department and job title |
| `User.ReadWrite.All` | Set or remove a manager (the narrowest permission listed for `manager/$ref`) |
| `User.Read.All` | Read users, search, count by department, walk the manager chain |
| `Directory.Read.All` | Read another user's group memberships and directory roles |
| `User-PasswordProfile.ReadWrite.All` | Reset a password (also needs a directory role on the app, see below) |
| `User.EnableDisableAccount.All` | Block or allow sign-in |
| `User.RevokeSessions.All` | Sign a user out everywhere |
| `GroupMember.ReadWrite.All` | Add and remove group members |
| `RoleManagement.ReadWrite.Directory` | Assign or remove allow-listed directory roles, scoped to the administrative unit |
| `LicenseAssignment.ReadWrite.All` | Assign and remove licences |
| `UserAuthenticationMethod.Read.All` | Show registered sign-in methods (MFA, passkey, phone), masked |
| `AuditLog.Read.All` | Last sign-in and directory audit events (needs Entra ID P1) |
| `Mail.Send` | Email the temporary password to the manager. **Lock it to one mailbox** with Exchange Online RBAC for Applications or an application access policy. |
| `Sites.Selected` | Save Excel reports. **Grant write on the reports site only.** |

After adding them, click **Grant admin consent**.

## Not granted, on purpose

- `Directory.ReadWrite.All`: would work for almost everything, and is far wider than needed.
- `AdministrativeUnit.ReadWrite.All`: with it the app could add anyone to the administrative unit and walk past its own fence. New users are created with `POST /users`; adding them to the unit is reported as a step for an administrator to finish (or assign an AU-scoped role, which needs Entra ID P1).

## The directory role

Graph permissions alone don't let an app reset passwords. The app's service principal also needs the **User Administrator** role, assigned **scoped to the administrative unit**, not tenant-wide. That scope is what keeps the app away from admins and anyone outside the unit.

## Re-check before you build

Microsoft adds narrower permissions over time. Check the permissions table on each Graph API page on Microsoft Learn before you grant anything.
