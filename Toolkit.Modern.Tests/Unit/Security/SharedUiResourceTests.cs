using ByteForge.Toolkit.Security.Rbac;
using ByteForge.Toolkit.Utilities;

namespace ByteForge.Toolkit.Modern.Tests.Unit.Security;

[TestClass]
[TestCategory("Unit")]
public class SharedUiResourceTests
{
    [TestMethod]
    public void AccountAndInvitationAssetsResolveFromModernAssembly()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(TelcoRbacUiAssets.ReadStyleSheet()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TelcoRbacUiAssets.ReadScript()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TelcoRbacUiAssets.ReadAccountScript()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TelcoRbacUiAssets.ReadAccountStyleSheet()));
        var invitation = TelcoRbacUiAssets.ReadInvitationPage("/api/invitation", "/login");
        Assert.IsFalse(invitation.Contains("{{API_URL}}"));
        Assert.IsFalse(invitation.Contains("{{SCRIPT}}"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(TelcoPortalHandoffPage.Render("<input name='csrf'>")));
    }

    [TestMethod]
    public void HtmlLineBreakConversionIsOptIn()
    {
        var processor = new TemplateProcessor();
        Assert.AreEqual("one\ntwo", processor.Process("one\ntwo"));
        processor.ReplaceNewLines = true;
        Assert.AreEqual("one<br/>two", processor.Process("one\r\ntwo"));
    }
}
