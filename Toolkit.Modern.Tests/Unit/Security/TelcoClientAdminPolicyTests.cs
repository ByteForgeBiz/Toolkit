using ByteForge.Toolkit.Security.Rbac;

namespace ByteForge.Toolkit.Tests.Unit.Security;

[TestClass]
[TestCategory("Unit")]
public class TelcoClientAdminPolicyTests
{
    [TestMethod]
    public void ApprovedGrantsAndLandingPageAreCaseInsensitive()
    {
        Assert.IsTrue(TelcoClientAdminPolicy.Allows(new[] { "Reports", "Export" }, new[] { "reports", "EXPORT" }, "REPORTS"));
    }

    [TestMethod]
    public void ForgedPermissionAndLandingPageAreRejected()
    {
        Assert.IsFalse(TelcoClientAdminPolicy.Allows(new[] { "Reports" }, new[] { "Reports", "Finance" }, "Reports"));
        Assert.IsFalse(TelcoClientAdminPolicy.Allows(new[] { "Reports" }, new[] { "Reports" }, "Finance"));
        Assert.IsFalse(TelcoClientAdminPolicy.Allows(Array.Empty<string>(), new[] { "Reports" }, null));
    }

    [TestMethod]
    public void FullAdminCannotBeDelegatedButCommonRolesCan()
    {
        var ceiling = new[] { "Reports" };
        Assert.IsFalse(TelcoClientAdminPolicy.AllowsRole(ceiling, new TelcoRbacRole { Name = "aDmIn" }));
        Assert.IsFalse(TelcoClientAdminPolicy.AllowsRole(ceiling, new TelcoRbacRole { Name = " Admin " }));
        Assert.IsTrue(TelcoClientAdminPolicy.AllowsRole(ceiling, new TelcoRbacRole { Name = " ClientAdmin " }));
        Assert.IsTrue(TelcoClientAdminPolicy.AllowsRole(ceiling, new TelcoRbacRole { Name = "Reader", IsSystem = true }));
        Assert.IsTrue(TelcoClientAdminPolicy.AllowsRole(ceiling, new TelcoRbacRole
        { Name = "Reader", Privileges = new() { "Reports" }, LandingController = "Reports" }));
    }

    [TestMethod]
    public void NewlyPublishedPermissionIsNotAutomaticallyGranted()
    {
        Assert.IsFalse(TelcoClientAdminPolicy.Allows(new[] { "Reports" }, new[] { "NewPermission" }, null));
    }
}
