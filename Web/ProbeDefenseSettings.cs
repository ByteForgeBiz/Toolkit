#if NET48
using ByteForge.Toolkit.Configuration;
using System;
using System.ComponentModel;

namespace ByteForge.Toolkit.Web
{
    /// <summary>
    /// Represents configuration settings for blocking and throttling obvious probe requests.
    /// </summary>
    [Description("Rules for blocking and throttling obvious web probe requests.")]
    public class ProbeDefenseSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether probe blocking is enabled.
        /// </summary>
        [ConfigName("bEnabled")]
        [DefaultValue(true)]
        [Description("Enables early blocking for configured probe paths.")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the denied request paths that should be short-circuited early.
        /// </summary>
        [ConfigName("DeniedPaths")]
        [Array("ProbeDefense#DeniedPaths")]
        [Description("Exact request paths that should be blocked before MVC routing.")]
        public string[] DeniedPaths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the denied path fragments that should be blocked wherever they appear in the request path.
        /// </summary>
        [ConfigName("DeniedFragments")]
        [Array("ProbeDefense#DeniedFragments")]
        [Description("Path fragments that should be blocked wherever they appear in the request path.")]
        public string[] DeniedFragments { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether repeated denied probes should be throttled.
        /// </summary>
        [ConfigName("bThrottlingEnabled")]
        [DefaultValue(true)]
        [Description("Enables throttling after repeated denied probe requests.")]
        public bool ThrottlingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the probe throttling window length in seconds.
        /// </summary>
        [ConfigName("iThrottleWindowSeconds")]
        [DefaultValue(60)]
        [Description("Length of the probe throttling window in seconds.")]
        public int ThrottleWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum denied probe requests allowed per window before throttling engages.
        /// </summary>
        [ConfigName("iThrottleMaxRequestsPerWindow")]
        [DefaultValue(12)]
        [Description("Maximum denied probe requests allowed per throttling window.")]
        public int ThrottleMaxRequestsPerWindow { get; set; } = 12;
    }
}
#endif
