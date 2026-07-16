using System;

namespace PAMonitor.XrmToolBox
{
    /// <summary>
    /// Persistent tool settings (XrmToolBox SettingsManager).
    /// </summary>
    public class Settings
    {
        public int DefaultTop { get; set; } = 100;
        public int AutoRefreshSeconds { get; set; } = 0;
        public DateTime? LastFromLocal { get; set; }
        public DateTime? LastToLocal { get; set; }

        /// <summary>
        /// Entra ID app registration (public client) with Flows.Read.All.
        /// Required because first-party Dynamics clients cannot request Flow tokens (AADSTS65002).
        /// </summary>
        public string FlowAppClientId { get; set; }

        /// <summary>Redirect URI registered on the app. Default: http://localhost</summary>
        public string FlowRedirectUri { get; set; } = "http://localhost";

        /// <summary>
        /// True after the user was asked once to configure Flow API settings when selecting a run.
        /// </summary>
        public bool FlowApiSettingsPromptShown { get; set; }
    }
}
