# Toolkit RBAC

Toolkit's shared identity implementation lives in
`Toolkit.Modern/Security/Rbac`. These classes use the SQL `tbl_TELCO_*` schema;
they are shared services that applications host, not an automatic authorization
middleware or a database migration system.

## Identity, project, role, privilege

- A user is a global identity: username, email, password hash, active status, and
  optional Super Admin status are shared across applications using this database.
- A project has a stable key supplied when constructing `TelcoRbacStore` or
  `TelcoIdentityStore`. Active project membership grants access to that application.
- Roles belong to a project, and a user can hold several roles. A role's privileges
  describe the actions/pages it permits. The effective permission set is the union
  of the user's active roles and active privileges within that project.
- Super Admin bypasses project membership and permission checks. An active project
  member with the active `Admin` role has full access within that project.

For example, an Orders Reader role might grant `Orders.View` and an Exporter role
might grant `Orders.Export`. A member holding both can view and export orders.
Neither role grants permissions in another project, even if that identity belongs
to both applications. Revoked memberships and inactive identities/roles/privileges
stop granting access.

`TelcoIdentityStore.Authenticate` verifies a username or email and a PBKDF2-SHA256
password hash, checking active account/project access. The host application creates
and validates its own session/cookie/token after authentication. Portal routing has
a separate authentication method; selecting a destination still requires that
destination's access checks.

## Delegated administration

The `Rbac.Delegate` privilege enables project-scoped delegation. The ordinary
`ClientAdmin` role name alone does not enable it. A custom role can carry the same
privilege, and the delegation authority is limited by the actor's effective
permissions.

Use the guarded `SaveDelegatedUser`, `SaveDelegatedRole`, and
`UpdateManagedPassword` operations for a delegated administrator. They check both
the existing target and proposed grants, reject assignment of full Admin authority,
protect the actor's own roles and full administrators, and preserve other projects'
memberships. Visibility is separate from mutation rights: `GetVisibleUsers` can
include protected rows with `CanManage=false`.

Some methods, such as `SaveRole`, `UpdateUser`, and `ReplaceUserRoles`, are lower
level administrative operations without an actor argument. The host must authorize
their callers. Possessing a `TelcoRbacStore` object does not itself enforce the
current HTTP user's authority.

## Host responsibilities

Construct the store with the application's configured database and project key.
Derive the actor ID from the authenticated server context, never from request
fields. Call `HasPrivilege(actorId, "Orders.View")` or the appropriate administration
guard before the endpoint's protected action; hiding a button is insufficient.
Keep CSRF protection, session expiry/revocation, and error responses in the host.
Do not expose password material, token hashes, or request bodies in logs.

The host also owns installing/upgrading the identity schema and registering its
project, roles, and privileges. `SynchronizePrivileges` and page catalog helpers
support registration; the library does not automatically grant newly registered
privileges to ordinary users.

## Account setup and self-service

Invitation and recovery tokens are stored as SHA-256 hashes, have expiration and
consumption checks, and respect active project/account eligibility. Accepting a
valid setup/reset token atomically changes the shared password and consumes all
outstanding setup links for that identity. Reset requests use an account-wide
cooldown; hosts return the generic public result to avoid revealing account
existence.

`SaveSelfProfile` accepts only display name, current password, and optional new
password. Its identity ID comes from the host's authenticated context. Password
changes require the current password and invalidate outstanding links; display
name and password changes roll back together if a database operation fails.
Username, email, membership, and role changes belong to administrative flows.
Existing sessions in other applications retain their host-defined expiry behavior.

## Regression coverage

`TelcoSecurityIntegrationTests` uses a minimal shared-identity schema inside a
uniquely named disposable SQL database for each test. It covers project boundaries,
permission unions, inactive/revoked access, delegated grants, protected identities,
transaction rollback, self-profile password validation, invitation/reset expiry,
replacement, cooldown, and concurrent single-use reset acceptance. It does not send
mail or use production accounts. These are store-level contract tests; host HTTP
authorization, production migrations, and email delivery require separate tests.
