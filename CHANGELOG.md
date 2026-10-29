# Changelog

All notable changes to this repo. Dates are in the format YYYY-MM-DD.

## [1.0.0] - 2026-10-29

First public release, alongside Architect's Build Lab, Episode 1.

### Added

- C# Dataverse plugin behind the `ab_ExecuteAdminAction` Custom API, with 33 operations for user onboarding, user management, group management, Entra ID administration and reports.
- "Run admin action" agent flow definition with an approval branch for `ResetPassword`, `AssignRole`, `RemoveRole`, `RevokeSessions`, `DisableUser` and `OffboardUser`.
- Copilot Studio agent instructions, tool description and test prompts for "IT Admin Assistant".
- Setup guide for a demo tenant, Graph permission list, operations reference, architecture deep dive and security notes.
- XML documentation on every public member; the build now produces the documentation file.

### Security

- 15 least-privilege Graph application permissions; `Directory.ReadWrite.All` and `AdministrativeUnit.ReadWrite.All` are not used.
- Fixed before release: hidden Unicode characters in `Json.cs` that broke the build, and a quiet permission problem that would have let the safety fence approve itself.
