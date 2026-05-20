// Azure Open AI Chat Client (Using Semantic Kernel)

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using Markdig;

namespace AzureOpenAIChat
{
    public partial class MainWindow : Window
    {
        // Azure OpenAI Chat Client (Using Semantic Kernel)
        SKHelper? sk;

        /// <summary>Markdig pipeline for converting Markdown to HTML.</summary>
        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        /// <summary>Regex to strip dangerous HTML elements for the IE-based WebBrowser control.</summary>
        private static readonly Regex HtmlSanitizeRegex = new(
            @"<script[^>]*>.*?</script>|<script[^>]*/?>|<iframe[^>]*>.*?</iframe>|<iframe[^>]*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex HtmlEventAttrRegex = new(
            @"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*'|\S+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MainWindow()
        {
            InitializeComponent();

            // Get version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = $"Azure OpenAI Chat v{version?.Major}.{version?.Minor}";

            // Init/Load From Registry
            txtAPIEndPoint.Text = NullIfEmpty(RegistryHelper.ReadAppInfo("APIENDPOINT")) ?? "Your Azure OpenAI Endpoint";
            txtTenantId.Text = NullIfEmpty(RegistryHelper.ReadAppInfo("TENANTID")) ?? "Your Entra Tenant ID";
            txtDeployment.Text = NullIfEmpty(RegistryHelper.ReadAppInfo("DEPLOYMENT")) ?? "Your Azure OpenAI Model Deployment Name";
            txtClientId.Text = NullIfEmpty(RegistryHelper.ReadAppInfo("CLIENTID")) ?? "Your App Registration Client ID";
            txtClientSecret.Text = NullIfEmpty(RegistryHelper.ReadAppInfo("CLIENTSECRET")) ?? "Your App Registration Client Secret";

            var left = RegistryHelper.ReadAppInfo("WINDOWLEFT");
            var top = RegistryHelper.ReadAppInfo("WINDOWTOP");
            if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(top)
                && double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLeft)
                && double.TryParse(top, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedTop))
            {
                this.Left = parsedLeft;
                this.Top = parsedTop;
            }
            else
            {
                // Center Window on Screen
                Size windowSize = new Size(this.Width, this.Height);
                Rect screenSize = SystemParameters.WorkArea;
                this.Left = (screenSize.Width / 2) - (windowSize.Width / 2);
                this.Top = (screenSize.Height / 2) - (windowSize.Height / 2);
            }

            // Set focus to promt field
            txtPrompt.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save to Registry (skip placeholder values)
            if (!txtAPIEndPoint.Text.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                RegistryHelper.WriteAppInfo("APIENDPOINT", txtAPIEndPoint.Text);
            if (!txtTenantId.Text.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                RegistryHelper.WriteAppInfo("TENANTID", txtTenantId.Text);
            if (!txtDeployment.Text.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                RegistryHelper.WriteAppInfo("DEPLOYMENT", txtDeployment.Text);
            if (!txtClientId.Text.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                RegistryHelper.WriteAppInfo("CLIENTID", txtClientId.Text);
            if (!txtClientSecret.Text.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                RegistryHelper.WriteAppInfo("CLIENTSECRET", txtClientSecret.Text);
            RegistryHelper.WriteAppInfo("WINDOWLEFT", this.Left.ToString(CultureInfo.InvariantCulture));
            RegistryHelper.WriteAppInfo("WINDOWTOP", this.Top.ToString(CultureInfo.InvariantCulture));
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (sk == null)
            {
                // Get the parameters from the screen
                string apiEndpoint = txtAPIEndPoint.Text;
                string tenantId = txtTenantId.Text;
                string deployment = txtDeployment.Text;
                string clientId = txtClientId.Text;
                string clientSecret = txtClientSecret.Text;

                if (string.IsNullOrWhiteSpace(apiEndpoint) ||
                    string.IsNullOrWhiteSpace(tenantId) ||
                    string.IsNullOrWhiteSpace(deployment) ||
                    string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(clientSecret) ||
                    apiEndpoint.StartsWith("Your ", StringComparison.OrdinalIgnoreCase) ||
                    tenantId.StartsWith("Your ", StringComparison.OrdinalIgnoreCase) ||
                    deployment.StartsWith("Your ", StringComparison.OrdinalIgnoreCase) ||
                    clientId.StartsWith("Your ", StringComparison.OrdinalIgnoreCase) ||
                    clientSecret.StartsWith("Your ", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Please fill in all required settings.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    sk = new SKHelper(deployment, apiEndpoint, tenantId, clientId, clientSecret);

                    // make Setup readonly
                    txtAPIEndPoint.IsReadOnly = true;
                    txtTenantId.IsReadOnly = true;
                    txtDeployment.IsReadOnly = true;
                    txtClientId.IsReadOnly = true;
                    txtClientSecret.IsReadOnly = true;

                    // Programmatically hide the top area
                    topRow.Height = new GridLength(0);
                }
                catch (Exception ex)
                {
                    lblCompletion.Content = "Authentication/Initialization failed. Exception below";
                    txtCompletion.Text = GetFullExceptionMessage(ex);
                    return;
                }
            }

            // Trim Prompt
            string myprompt = txtPrompt.Text.Trim();

            // If prompt is empty reset and return
            if (myprompt == string.Empty)
            {
                // Reset Completion
                txtCompletion.Text = string.Empty;
                lblCompletion.Content = "Completion context cleared";
                btnClearCtx.IsEnabled = false;
                btnCopyCtx.IsEnabled = false;
                if (sk != null)
                    sk.InitContext();
                return;
            }

            // Wait message 
            lblCompletion.Content = "Request is processing...";
            txtCompletion.Text = string.Empty;
            btnSend.IsEnabled = false;

            try
            {
                var completionText = await sk.Chat(myprompt);
                txtCompletion.Text = completionText;
                btnClearCtx.IsEnabled = true;
                btnCopyCtx.IsEnabled = true;
                lblCompletion.Content = "Completion";
            }
            catch (Exception ex)
            {
                lblCompletion.Content = "The request was unsuccessful. Exception below";
                txtCompletion.Text = GetFullExceptionMessage(ex);
            }
            finally
            {
                btnSend.IsEnabled = true;
            }
        }

        // Clear Context
        private void btnClearCtx_Click(object sender, RoutedEventArgs e)
        {
            btnClearCtx.IsEnabled = false;
            btnCopyCtx.IsEnabled = false;
            if (sk != null)
                sk.InitContext();
            lblCompletion.Content = "Completion context cleared";
        }

        // Tab selection changed - render preview when Preview tab is selected
        private void tabCompletionControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabCompletionControl) return;
            if (tabCompletionControl.SelectedIndex == 1 && wbPreview != null)
            {
                RenderMarkdownPreview();
            }
        }

        /// <summary>
        /// Renders the current result text as HTML in the preview browser.
        /// </summary>
        private void RenderMarkdownPreview()
        {
            var markdown = txtCompletion.Text ?? string.Empty;
            var html = Markdig.Markdown.ToHtml(markdown, MarkdownPipeline);
            // Strip dangerous HTML elements and event handler attributes
            html = HtmlSanitizeRegex.Replace(html, string.Empty);
            html = HtmlEventAttrRegex.Replace(html, string.Empty);
            var fullHtml = $"<html><head><meta charset=\"utf-8\"><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><style>body{{font-family:'Segoe UI',sans-serif;font-size:14px;padding:10px;}}code{{background:#f0f0f0;padding:2px 4px;border-radius:3px;}}pre{{background:#f0f0f0;padding:10px;border-radius:5px;overflow-x:auto;}}table{{border-collapse:collapse;}}th,td{{border:1px solid #ccc;padding:6px 10px;}}</style></head><body>{html}</body></html>";
            wbPreview.NavigateToString(fullHtml);
        }

        // Copy Context
        private void btnCopyCtx_Click(object sender, RoutedEventArgs e)
        {
            string? context = sk?.GetFullContext();
            if (string.IsNullOrWhiteSpace(context))
            {
                lblCompletion.Content = "No context to copy";
                return;
            }

            Clipboard.SetText(context);
            lblCompletion.Content = "Completion context copied to clipboard";
        }

        // Helper method to get full exception details including inner exceptions
        private static string GetFullExceptionMessage(Exception ex, int maxDepth = 5)
        {
            var message = ex.Message;
            if (ex.InnerException != null && maxDepth > 0)
            {
                message += "\n\nInner Exception: " + GetFullExceptionMessage(ex.InnerException, maxDepth - 1);
            }
            return message;
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrEmpty(value) ? null : value;
    }
}
