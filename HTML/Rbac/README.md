# Shared TELCO invitations

## Shared user editor

Praxis, Walgreens, Stern Law, and Global RBAC use `ByteForge-rbac.js`. Authorized project editors can update an existing user's username, display name, and email; those profile changes affect the shared TELCO identity. Client Admin profile restrictions remain in place, and a Super Admin profile requires a Super Admin. Project editors do not change global active status or provide an existing-user password field.

Managed profile inputs receive distinct opaque names for every editor instance, while API payload keys remain `username`, `displayName`, `email`, and `password`. Password-manager ignore hints apply to forms and inputs. Synthetic input changes are restored; a silent value change is restored and blocks the first save for review. This is protection against accidental autofill, not a security boundary against browser extensions.

Client Admin is an ordinary assignable project role that is created if missing during project initialization. Its active role membership enables local user/role management. Effective permissions are the union of all active roles; Admin and Super Admin bypass project permission checks. Delegated saves validate both existing targets and proposed grants, reject Admin assignment, protect self/full-admin role edits, and cannot modify another global identity profile or password. Retired per-user Client Admin tables are ignored and preserved for audit; their grants are not merged into role permissions. Deploy Toolkit and all four hosts together. No existing production grant is rewritten by this change.

Run `node PraxisAPI.Tests/rbac-profile-editor.test.cjs` from MainProjects with Playwright available to verify project/global edits, JSON/form serialization, new field names, and simulated extension writes in light and dark themes. Live LastPass behavior and deployed persistence require browser/application verification after building.

## My profile in the page header

Praxis reporting/script pages, Walgreens authenticated layouts, Stern Law, and Global RBAC pages host the shared `ByteForge-account` control at the top right. Toolkit serves its CSS and native-dialog modal through the applications' asset controllers. Any signed-in project member can use it; RBAC administration access is not required.

Self-service allows display-name and password updates only. Username/email are displayed as text and remain admin-only. The write model has no identity ID, username, email, status, or role properties; controllers derive the ID from the authenticated session or bearer identity. MVC hosts validate anti-forgery tokens; the Global RBAC cookie endpoint requires its same-origin custom header and validates any Origin header; Praxis requires an interactive portal bearer token.

Name-only saves update the header and keep the session. Password changes require the current password and matching new passwords, invalidate outstanding reset/invitation tokens, and sign out the current application session. These are shared TELCO credentials; AMCH remains separate. Existing sessions in other browsers/applications retain their existing session expiration behavior.

The shared store saves the display name/password atomically and never writes admin-only fields. `PraxisAPI.Tests/self-profile-ui.test.cjs` exercises all four host transport configurations in light/dark themes. `PraxisAPI.Tests/self-profile-source-check.ps1` compiles the actual shared source against an isolated database double to verify its input contract, account boundary, password checks, SQL intent, and rollback control flow. Actual database integration and deployed cookie/iframe flows still require local solution builds and HTTPS verification.

## Invitations

`TelcoInvitationService` owns invitation email delivery, token hashing/validation, and password setup. Applications supply a project-scoped `TelcoRbacStore` and configured `TelcoInvitationSettings`.

For blank-password creation, validate mail/settings first, call the explicit `CreateOrGrantUser(..., allowInvitation: true)` overload, save authorized project roles, and call `Send`. Existing identities retain their password; only active project members without a prior password or login receive setup tokens. The original overload continues to require a password for new identities.

Host `TelcoRbacUiAssets.ReadInvitationPage(apiPath, loginPath)` at the application's local setup route. Provide anonymous POST routes at `apiPath + "/validate"` and `apiPath + "/accept"` that call `IsValid(store, token)` and `Accept(store, token, password)`. Return `{ isValid: true/false }` for validation, a successful JSON response for acceptance, and a non-success response with `message` for failures. These routes authenticate through the bearer invitation, not a user session.

The setup page uses `#token=...`, removes the fragment from browser history, and submits tokens/passwords only in POST bodies. Do not log those bodies. Serve the page and API responses with `Cache-Control: no-store`; retain the hosting application's configured iframe policy. The shared page supports light/dark system themes.

Embed `HTML/Rbac/invitation.html` and `HTML/JavaScript/ByteForge-invitation.js` under the logical resource names used by `TelcoRbacUiAssets`. Praxis and Stern Law host integrations at `/invite` and `/api/invitation`. Walgreens and Global RBAC already resolve their existing local setup routes from the application host. AMCH remains exempt from the common database workflow.

Resolve runtime settings with `configured.ForApplication(requestUrl, localSetupPath)` before creating `TelcoInvitationService`. Email links use the host serving the application/login page; the route remains local and includes the virtual-directory prefix. No separate `sSetupUrl` configuration is required. The helper requires HTTPS and copies lifetime settings without mutating cached configuration. IIS/proxy bindings must restrict accepted hostnames and preserve the public HTTPS scheme. `iTokenLifetimeDays` defaults to seven days. Delivery uses Toolkit's existing `[Mail Server]` configuration. Setup uses the application's existing TELCO database; no Global RBAC application URL or separate deployment is required.

## Password recovery

Render the same page with `requestReset: true` for the forgot-password form. Host an anonymous POST at `apiPath + "/request"` that accepts `{ identifier }`, invokes `RequestPasswordReset`, and always returns `PasswordResetRequestedMessage` regardless of account existence, activity, throttling, or email delivery. Log server errors without logging the request body. The same validation/acceptance routes handle both invitation and reset tokens. Reset tokens default to 60 minutes; the account-wide email cooldown defaults to 60 seconds (`iResetTokenLifetimeMinutes` and `iResetRequestCooldownSeconds`). Changing the password consumes all outstanding tokens; requesting a reset does not change the password.

Praxis exposes the request page at `/forgot-password`, linked from both login screens. Walgreens retains its existing `Account/ForgotPassword` flow.

## Optional email customization

Both applications read `[TELCO Account Emails]` through `TelcoAccountEmailTemplates`. Omit the section or individual keys to use the shared defaults; no customization is required. Set `sInvitationSubject`, `sInvitationBodyHtml`, `sResetSubject`, and/or `sResetBodyHtml` to override messages independently. HTML values belong on one INI line.

Subjects support `{{ProjectName}}`. Bodies additionally support `{{DisplayName}}`, `{{Username}}`, `{{SetupUrl}}`, and `{{ExpiresAtUtc}}`. A body must include `{{SetupUrl}}`. Dynamic HTML values are encoded; unknown placeholders, missing links, and subject line breaks fail clearly. Template HTML is trusted deployment configuration. Examples are included as comments in each application's Debug/Release INI files.

Project user lists include the signed-in account and protected users. The server returns `canManage` separately from visibility; the shared editor disables edit/invite/remove actions for protected rows and points the current user to My profile. Delegated mutation checks still use `GetDelegableUsers`, never the visible list.

## User passwords and membership removal

Admins and Client Admins can leave a new user's password blank to send a welcome/setup invitation, or enter an initial password directly. Editing a managed user accepts an optional new password; blank keeps the current password. Password changes apply to the shared identity and invalidate outstanding setup/reset links. Existing identity matching never replaces its password during project linking.

Revoked project memberships remain stored but disappear from that project's user list. Super administrators retain their global access. Global RBAC exposes Delete only for identities with no active membership in any registered project and no Super Admin access. The server rechecks every membership during deletion, protects the actor's own identity, and rolls back if remaining references prevent deletion.
