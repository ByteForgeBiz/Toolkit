using ByteForge.Toolkit.Security.Rbac;
using ByteForge.Toolkit.Tests.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace ByteForge.Toolkit.Tests.Unit.Security;

[TestClass]
[TestCategory("SQLServer")]
[DoNotParallelize]
public class TelcoSecurityIntegrationTests
{
    private TelcoSecurityFixture fixture = null!;
    private TelcoRbacStore store => fixture.One;

    [TestInitialize]
    public void Initialize() => fixture = new TelcoSecurityFixture();

    [TestCleanup]
    public void Cleanup() => fixture?.Dispose();

    [TestMethod]
    public void PermissionsAreUnionedWithinActiveProjectAndRoleBoundaries()
    {
        Assert.IsTrue(store.HasPrivilege(2, "Reports"));
        Assert.IsTrue(store.HasPrivilege(2, "Rbac.Delegate"));
        Assert.IsFalse(store.HasPrivilege(2, "Finance"));
        Assert.IsFalse(store.HasPrivilege(3, "Finance"), "A role in another project must not leak permissions.");
        Assert.IsTrue(fixture.Two.HasPrivilege(3, "Finance"));
        Assert.IsTrue(store.HasPrivilege(5, "Finance"), "Project Admin bypasses role privilege lists.");
        Assert.IsTrue(fixture.Two.HasPrivilege(1, "Finance"), "Super Admin has global access without membership.");
        fixture.Execute("UPDATE tbl_TELCO_Roles SET IsActive=0 WHERE Id=2");
        Assert.IsFalse(store.HasPrivilege(2, "Reports"));
        Assert.IsTrue(store.HasPrivilege(2, "Rbac.Delegate"));
        fixture.Execute("UPDATE tbl_TELCO_Privileges SET IsActive=0 WHERE Id=3");
        Assert.IsFalse(store.IsClientAdministrator(2));
        fixture.Execute("UPDATE tbl_TELCO_UserProjects SET IsActive=0 WHERE UserId=3 AND ProjectId=2");
        Assert.IsFalse(fixture.Two.HasPrivilege(3, "Finance"));
    }

    [TestMethod]
    public void DelegationComesFromPrivilegeAndProtectsExistingAndProposedGrants()
    {
        Assert.IsTrue(store.IsClientAdministrator(2), "An ordinary role with Rbac.Delegate enables delegation.");
        Assert.IsFalse(store.IsClientAdministrator(6), "The ClientAdmin name alone grants no authority.");
        Assert.ThrowsExactly<UnauthorizedAccessException>(() => fixture.Two.RequireClientAdministration(2));
        var visible = store.GetVisibleUsers(2);
        Assert.IsTrue(visible.Single(user => user.Id == 3).CanManage);
        Assert.IsFalse(visible.Single(user => user.Id == 2).CanManage);
        Assert.IsFalse(visible.Single(user => user.Id == 5).CanManage);
        CollectionAssert.AreEquivalent(new[] { 2, 4, 6 }, store.GetDelegableRoles(2).Select(role => role.Id).ToArray());
        foreach (var protectedUser in new[] { 1, 2, 4, 5 })
            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                store.SaveDelegatedUser(2, protectedUser, "", "", "", null, false, null));
        foreach (var forbiddenRole in new[] { 1, 3, 5 })
            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                store.SaveDelegatedUser(2, 3, "", "", "", null, true, new[] { forbiddenRole }));
        Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
            store.SaveDelegatedRole(2, 4, "Operators", "Changed", null, "Reports", 100, true, new[] { "Reports" }));
        Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
            store.SaveDelegatedRole(2, 2, "Reader", "Changed", null, "Finance", 100, true, new[] { "Finance" }));
        Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
            store.SaveDelegatedRole(2, 0, "Forged", "Forged", null, "Finance", 100, true, new[] { "Finance" }));
        Assert.IsTrue(store.HasPrivilege(3, "Reports"), "Rejected grants preserve existing membership and roles.");
        Assert.IsFalse(store.HasPrivilege(3, "Finance"));
    }

    [TestMethod]
    public void DelegatedSavePreservesGlobalIdentityAndOtherProjectMembership()
    {
        store.SaveDelegatedUser(2, 3, "Forged", "Forged", "forged@example.invalid", null, true, new[] { 2 });
        var profile = store.GetSelfProfile(3);
        Assert.AreEqual("Member", profile.Username);
        Assert.AreEqual("Member", profile.DisplayName);
        Assert.AreEqual("Member@example.invalid", profile.Email);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            store.SaveDelegatedUser(2, 0, "Foreign", "Forged", "Foreign@example.invalid", "Replacement-123!", true, new[] { 2 }));
        store.SaveDelegatedUser(2, 3, "", "", "", null, false, null);
        Assert.IsNull(store.GetAccess(3));
        Assert.IsNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsNotNull(fixture.Identity("Two").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsTrue(fixture.Two.HasPrivilege(3, "Finance"));
    }

    [TestMethod]
    public void FailedRoleAssignmentRollsBackEarlierDeletion()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => store.ReplaceUserRoles(3, new[] { 3, 5 }));
        CollectionAssert.AreEquivalent(new[] { 2 }, store.GetUsers().Single(user => user.Id == 3).RoleIds.ToArray());
        Assert.IsTrue(store.HasPrivilege(3, "Reports"));
        Assert.IsFalse(store.HasPrivilege(3, "Finance"));
        Assert.IsTrue(fixture.Two.HasPrivilege(3, "Finance"));
    }

    [TestMethod]
    public void RevokedOrInactiveIdentityCannotAuthenticateOrReadSelfProfile()
    {
        Assert.IsNull(fixture.Identity("One").Authenticate("Foreign", TelcoSecurityFixture.Password));
        Assert.ThrowsExactly<UnauthorizedAccessException>(() => store.GetSelfProfile(4));
        fixture.Execute("UPDATE tbl_TELCO_Users SET IsActive=0 WHERE Id=3");
        Assert.IsNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.ThrowsExactly<UnauthorizedAccessException>(() => store.SaveSelfProfile(3, new TelcoSelfProfileUpdate { DisplayName = "Forged" }));
    }

    [TestMethod]
    public void SelfProfileRequiresCurrentPasswordAndPreservesExactNewPassword()
    {
        Assert.ThrowsExactly<ArgumentException>(() => store.SaveSelfProfile(3, new TelcoSelfProfileUpdate
        { DisplayName = "Forged", CurrentPassword = "Wrong-password", NewPassword = "Replacement-123!" }));
        Assert.AreEqual("Member", store.GetSelfProfile(3).DisplayName);
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        var token = IssueReset("Member");
        store.SaveSelfProfile(3, new TelcoSelfProfileUpdate
        { DisplayName = "Updated member", CurrentPassword = TelcoSecurityFixture.Password, NewPassword = "  Replacement-123!  " });
        Assert.IsNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsNull(fixture.Identity("Two").Authenticate("Member", "Replacement-123!"));
        Assert.IsNotNull(fixture.Identity("Two").Authenticate("Member", "  Replacement-123!  "));
        Assert.IsNull(store.GetProjectSetupToken(Hash(token)));
        var profile = store.GetSelfProfile(3);
        Assert.AreEqual("Updated member", profile.DisplayName);
        Assert.AreEqual("Member", profile.Username);
        Assert.AreEqual("Member@example.invalid", profile.Email);
        Assert.IsTrue(fixture.Two.HasPrivilege(3, "Finance"));
    }

    [TestMethod]
    public void NameOnlySelfUpdateLeavesPasswordAndResetLinkIntact()
    {
        var token = IssueReset("Member");
        store.SaveSelfProfile(3, new TelcoSelfProfileUpdate { DisplayName = "Updated member" });
        Assert.AreEqual("Updated member", store.GetSelfProfile(3).DisplayName);
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsNotNull(store.GetProjectSetupToken(Hash(token)));
    }

    [TestMethod]
    public void FailedTokenInvalidationRollsBackBothProfileAndPassword()
    {
        var token = IssueReset("Member");
        fixture.ExecuteDdl(@"CREATE TRIGGER RejectTokenUpdate ON tbl_TELCO_UserPasswordResetTokens AFTER UPDATE AS
BEGIN RAISERROR('Injected token failure',16,1); ROLLBACK TRANSACTION; END");
        Assert.ThrowsExactly<InvalidOperationException>(() => store.SaveSelfProfile(3, new TelcoSelfProfileUpdate
        { DisplayName = "Must roll back", CurrentPassword = TelcoSecurityFixture.Password, NewPassword = "Replacement-123!" }));
        Assert.AreEqual("Member", store.GetSelfProfile(3).DisplayName);
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsNull(fixture.Identity("One").Authenticate("Member", "Replacement-123!"));
        Assert.IsNotNull(store.GetProjectSetupToken(Hash(token)));
    }

    [TestMethod]
    public void InvitationCannotCrossMembershipBoundaryAndCanBeAcceptedOnlyOnce()
    {
        var pending = store.CreateOrGrantUser("Pending", "Pending", "pending@example.invalid", null, true, allowInvitation: true);
        var raw = Guid.NewGuid().ToString("N");
        Assert.IsTrue(store.CreateProjectInvitationToken(pending, Hash(raw), DateTime.UtcNow.AddMinutes(10), null));
        var invitations = InvitationService();
        Assert.IsTrue(invitations.IsValid(store, raw));
        Assert.IsFalse(invitations.IsValid(fixture.Two, raw));
        Assert.ThrowsExactly<InvalidOperationException>(() => invitations.Accept(fixture.Two, raw, "Replacement-123!"));
        Assert.IsNull(fixture.Identity("One").Authenticate("Pending", "Replacement-123!"));
        invitations.Accept(store, raw, "Replacement-123!");
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Pending", "Replacement-123!"));
        Assert.IsFalse(invitations.IsValid(store, raw));
        Assert.ThrowsExactly<InvalidOperationException>(() => invitations.Accept(store, raw, "Another-password-456!"));
        Assert.IsFalse(store.CreateProjectInvitationToken(3, Hash(Guid.NewGuid().ToString("N")), DateTime.UtcNow.AddMinutes(10), null),
            "Established accounts cannot receive new-account invitations.");
    }

    [TestMethod]
    public void ExpiredAndReplacedInvitationsCannotChangePassword()
    {
        var pending = store.CreateOrGrantUser("Pending", "Pending", "pending@example.invalid", null, true, true);
        var old = Guid.NewGuid().ToString("N");
        Assert.IsTrue(store.CreateProjectInvitationToken(pending, Hash(old), DateTime.UtcNow.AddMinutes(-5), null));
        Assert.IsFalse(InvitationService().IsValid(store, old));
        Assert.ThrowsExactly<InvalidOperationException>(() => InvitationService().Accept(store, old, "Replacement-123!"));
        var fresh = Guid.NewGuid().ToString("N");
        Assert.IsTrue(store.CreateProjectInvitationToken(pending, Hash(fresh), DateTime.UtcNow.AddMinutes(10), null));
        Assert.IsNull(store.GetProjectSetupToken(Hash(old)));
        Assert.IsTrue(InvitationService().IsValid(store, fresh));
        store.SetProjectAccess(pending, false);
        Assert.IsFalse(InvitationService().IsValid(store, fresh));
        Assert.ThrowsExactly<InvalidOperationException>(() => InvitationService().Accept(store, fresh, "Replacement-123!"));
    }

    [TestMethod]
    public void ResetCooldownAndEligibilityDoNotReplaceAValidToken()
    {
        var token = IssueReset("Member");
        Assert.IsNull(store.CreateProjectPasswordResetToken("Member", Hash("throttled"), DateTime.UtcNow.AddMinutes(10), null, 60));
        Assert.IsNotNull(store.GetProjectSetupToken(Hash(token)));
        foreach (var ineligible in new[] { "Unknown", "Foreign" })
            Assert.IsNull(store.CreateProjectPasswordResetToken(ineligible, Hash(ineligible), DateTime.UtcNow.AddMinutes(10), null, 60));
        fixture.Execute("UPDATE tbl_TELCO_Users SET IsActive=0 WHERE Id=4");
        Assert.IsNull(fixture.Two.CreateProjectPasswordResetToken("Foreign", Hash("inactive"), DateTime.UtcNow.AddMinutes(10), null, 60));
    }

    [TestMethod]
    public void ResetAcceptanceIsSingleUseAndInvalidatesAllOutstandingLinks()
    {
        var token = IssueReset("Member");
        var sibling = Guid.NewGuid().ToString("N");
        fixture.Execute("INSERT INTO tbl_TELCO_UserPasswordResetTokens(UserId,TokenHash,ExpiresAtUtc,IsInvitation) VALUES(3,@Hash,@Expiry,0)",
            Hash(sibling), DateTime.UtcNow.AddMinutes(10));
        InvitationService().Accept(store, token, "Replacement-123!");
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", "Replacement-123!"));
        Assert.IsNull(store.GetProjectSetupToken(Hash(sibling)));
        Assert.ThrowsExactly<InvalidOperationException>(() => InvitationService().Accept(store, token, "Another-password-456!"));
    }

    [TestMethod]
    public void ConcurrentResetAcceptanceHasExactlyOneWinner()
    {
        var token = IssueReset("Member");
        using var start = new System.Threading.ManualResetEventSlim();
        var attempts = new[] { "First-password-123!", "Second-password-456!" }.Select(password => Task.Run(() =>
        {
            var isolatedStore = fixture.Store("One");
            start.Wait();
            try { InvitationService().Accept(isolatedStore, token, password); return password; }
            catch (InvalidOperationException error)
            {
                Assert.IsNull(error.InnerException, "The losing request must fail token validation, not database execution.");
                return null;
            }
        })).ToArray();
        start.Set();
        var results = Task.WhenAll(attempts).GetAwaiter().GetResult();
        Assert.AreEqual(1, results.Count(password => password != null));
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", results.Single(password => password != null)!));
        Assert.IsFalse(InvitationService().IsValid(store, token));
    }

    [TestMethod]
    public void ExpiredResetDoesNotChangePasswordOrConsumeAnUnrelatedValidLink()
    {
        var token = IssueReset("Member");
        fixture.Execute("INSERT INTO tbl_TELCO_UserPasswordResetTokens(UserId,TokenHash,ExpiresAtUtc,IsInvitation) VALUES(3,@Hash,@Expiry,0)",
            Hash("expired-reset"), DateTime.UtcNow.AddMinutes(-5));
        Assert.IsFalse(InvitationService().IsValid(store, "expired-reset"));
        Assert.ThrowsExactly<InvalidOperationException>(() => InvitationService().Accept(store, "expired-reset", "Replacement-123!"));
        Assert.IsNotNull(fixture.Identity("One").Authenticate("Member", TelcoSecurityFixture.Password));
        Assert.IsTrue(InvitationService().IsValid(store, token));
    }

    private string IssueReset(string username)
    {
        var raw = Guid.NewGuid().ToString("N");
        Assert.IsNotNull(store.CreateProjectPasswordResetToken(username, Hash(raw), DateTime.UtcNow.AddMinutes(10), null, 60));
        return raw;
    }

    private static TelcoInvitationService InvitationService() => new(new TelcoInvitationSettings(), new TelcoAccountEmailTemplates());

    private static string Hash(string raw)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-", "").ToLowerInvariant();
    }
}
