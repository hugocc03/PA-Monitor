using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PAMonitor.XrmToolBox
{
    public sealed class FlowApiSettingsForm : Form
    {
        private readonly TextBox _txtClientId;
        private readonly TextBox _txtRedirectUri;

        public string ClientId => (_txtClientId.Text ?? string.Empty).Trim();
        public string RedirectUri => (_txtRedirectUri.Text ?? string.Empty).Trim();

        public FlowApiSettingsForm(Settings settings)
        {
            Text = "Flow API settings";
            Width = 760;
            Height = 700;
            MinimumSize = new Size(720, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;

            var info = new RichTextBox
            {
                Left = 12,
                Top = 12,
                Width = 720,
                Height = 430,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                DetectUrls = true,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Text =
                    "WHY THIS IS NEEDED\r\n" +
                    "------------------\r\n" +
                    "Action-level errors (the real InvalidTemplate / divide-by-zero message) come from the Power Automate\r\n" +
                    "Flow API, not from Dataverse. Microsoft blocks the Dynamics/XrmToolBox sample app from that API\r\n" +
                    "(error AADSTS65002). You need an Entra ID Application (client) ID from your tenant.\r\n" +
                    "If you cannot open Azure/Entra, ask a tenant admin to do these steps and send you the Client ID.\r\n\r\n" +
                    "STEP-BY-STEP (admin or someone with Entra access)\r\n" +
                    "-----------------------------------------------\r\n" +
                    "1) Open App registrations:\r\n" +
                    "   https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade\r\n" +
                    "   (or Azure Portal: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade )\r\n\r\n" +
                    "2) Click + New registration\r\n" +
                    "   - Name: PA Run Monitor (or any name)\r\n" +
                    "   - Supported account types: Accounts in this organizational directory only (Single tenant)\r\n" +
                    "   - Redirect URI: leave empty for now → Register\r\n\r\n" +
                    "3) On the Overview page, copy Application (client) ID\r\n" +
                    "   It looks like: 00000000-0000-0000-0000-000000000000\r\n" +
                    "   Paste it in the field below in this dialog.\r\n\r\n" +
                    "4) Authentication (left menu)\r\n" +
                    "   - Add a platform → Mobile and desktop applications\r\n" +
                    "   - Enable / enter Redirect URI: http://localhost\r\n" +
                    "   - At the bottom: Allow public client flows = Yes → Save\r\n\r\n" +
                    "5) API permissions (left menu) → + Add a permission\r\n" +
                    "   - APIs my organization uses\r\n" +
                    "   - Search: Microsoft Flow Service   (sometimes shown as Power Automate)\r\n" +
                    "   - Delegated permissions → check Flows.Read.All → Add permissions\r\n" +
                    "   - Click Grant admin consent for <your tenant> → Yes\r\n\r\n" +
                    "6) Back in this dialog: paste Client ID, keep Redirect URI = http://localhost → Save\r\n" +
                    "7) In PA Run Monitor, select a failed run again. Sign in when MSAL asks (once).\r\n\r\n" +
                    "DOCS\r\n" +
                    "----\r\n" +
                    "Register an app:\r\n" +
                    "https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app\r\n" +
                    "Public client / desktop redirect URIs:\r\n" +
                    "https://learn.microsoft.com/en-us/entra/identity-platform/reply-url#native-applications\r\n"
            };
            info.LinkClicked += (_, e) => OpenUrl(e.LinkText);

            var linksPanel = new FlowLayoutPanel
            {
                Left = 12,
                Top = 450,
                Width = 720,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            linksPanel.Controls.Add(CreateLink("Open Entra app registrations",
                "https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade"));
            linksPanel.Controls.Add(new System.Windows.Forms.Label { Text = "  |  ", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
            linksPanel.Controls.Add(CreateLink("Open Azure app registrations",
                "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade"));
            linksPanel.Controls.Add(new System.Windows.Forms.Label { Text = "  |  ", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
            linksPanel.Controls.Add(CreateLink("Microsoft Learn: register app",
                "https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app"));

            var lblClient = new System.Windows.Forms.Label
            {
                Text = "Application (client) ID  — paste the GUID from the app Overview page",
                Left = 12,
                Top = 488,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _txtClientId = new TextBox
            {
                Left = 12,
                Top = 510,
                Width = 720,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = settings?.FlowAppClientId ?? string.Empty
            };

            var lblRedirect = new System.Windows.Forms.Label
            {
                Text = "Redirect URI  — must match Authentication in the app (default http://localhost)",
                Left = 12,
                Top = 544,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _txtRedirectUri = new TextBox
            {
                Left = 12,
                Top = 566,
                Width = 720,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = string.IsNullOrWhiteSpace(settings?.FlowRedirectUri)
                    ? "http://localhost"
                    : settings.FlowRedirectUri
            };

            var btnOk = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Width = 90,
                Height = 28,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 90,
                Height = 28,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            void PlaceButtons()
            {
                btnCancel.Left = ClientSize.Width - btnCancel.Width - 12;
                btnCancel.Top = ClientSize.Height - btnCancel.Height - 12;
                btnOk.Left = btnCancel.Left - btnOk.Width - 8;
                btnOk.Top = btnCancel.Top;
            }

            PlaceButtons();
            Resize += (_, __) => PlaceButtons();

            Controls.AddRange(new Control[]
            {
                info, linksPanel, lblClient, _txtClientId, lblRedirect, _txtRedirectUri, btnOk, btnCancel
            });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private static LinkLabel CreateLink(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            link.Links.Add(0, text.Length, url);
            link.LinkClicked += (_, e) => OpenUrl(e.Link.LinkData as string ?? url);
            return link;
        }

        private static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open browser:\r\n" + ex.Message + "\r\n\r\nURL:\r\n" + url,
                    "Flow API settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtClientId.Focus();
        }
    }
}
