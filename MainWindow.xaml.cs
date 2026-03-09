// Azure Open AI Chat Client (Using Semantic Kernel)

using System;
using System.Windows;
using System.Globalization;

namespace AzureOpenAIChat
{
    public partial class MainWindow : Window
    {
        // Azure OpenAI Chat Client (Using Semantic Kernel)
        SKHelper? sk;

        public MainWindow()
        {
            InitializeComponent();

            // Init/Load From Registry
            if (string.IsNullOrEmpty(txtAPIEndPoint.Text = RegistryHelper.ReadAppInfo("APIENDPOINT")))
                txtAPIEndPoint.Text = "Your Azure OpenAI Endpoint";

            if (string.IsNullOrEmpty(txtTenantId.Text = RegistryHelper.ReadAppInfo("TENANTID")))
                txtTenantId.Text = "Your Entra Tenant ID";

            if (string.IsNullOrEmpty(txtDeployment.Text = RegistryHelper.ReadAppInfo("DEPLOYMENT")))
                txtDeployment.Text = "Your Azure OpenAI Model Deployment Name";

            if (string.IsNullOrEmpty(txtClientId.Text = RegistryHelper.ReadAppInfo("CLIENTID")))
                txtClientId.Text = "Your App Registration Client ID";

            if (string.IsNullOrEmpty(txtClientSecret.Text = RegistryHelper.ReadAppInfo("CLIENTSECRET")))
                txtClientSecret.Text = "Your App Registration Client Secret";

            if (string.IsNullOrEmpty(txtTemperature.Text = RegistryHelper.ReadAppInfo("TEMPERATURE")))
                txtTemperature.Text = "0.5";

            if (string.IsNullOrEmpty(txtMaxTokens.Text = RegistryHelper.ReadAppInfo("MAXTOKENS")))
                txtMaxTokens.Text = "2048";

            var left = RegistryHelper.ReadAppInfo("WINDOWLEFT");
            var top = RegistryHelper.ReadAppInfo("WINDOWTOP");
            if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(top))
            {
                this.Left = double.Parse(left);
                this.Top = double.Parse(top);
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
            // Save to Registry
            RegistryHelper.WriteAppInfo("APIENDPOINT", txtAPIEndPoint.Text);
            RegistryHelper.WriteAppInfo("TENANTID", txtTenantId.Text);
            RegistryHelper.WriteAppInfo("DEPLOYMENT", txtDeployment.Text);
            RegistryHelper.WriteAppInfo("CLIENTID", txtClientId.Text);
            RegistryHelper.WriteAppInfo("CLIENTSECRET", txtClientSecret.Text);
            RegistryHelper.WriteAppInfo("TEMPERATURE", txtTemperature.Text);
            RegistryHelper.WriteAppInfo("MAXTOKENS", txtMaxTokens.Text);
            RegistryHelper.WriteAppInfo("WINDOWLEFT", this.Left.ToString());
            RegistryHelper.WriteAppInfo("WINDOWTOP", this.Top.ToString());
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
                    string.IsNullOrWhiteSpace(clientSecret))
                {
                    MessageBox.Show("Please fill in all required settings.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(txtTemperature.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double temperature))
                {
                    MessageBox.Show("Temperature must be a valid number (example: 0.5).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtMaxTokens.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxtokens))
                {
                    MessageBox.Show("Max Tokens must be a valid integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    sk = new SKHelper(deployment, apiEndpoint, tenantId, clientId, clientSecret, maxtokens, temperature);

                    // make Setup readonly
                    txtAPIEndPoint.IsReadOnly = true;
                    txtTenantId.IsReadOnly = true;
                    txtDeployment.IsReadOnly = true;
                    txtClientId.IsReadOnly = true;
                    txtClientSecret.IsReadOnly = true;
                    txtTemperature.IsReadOnly = true;
                    txtMaxTokens.IsReadOnly = true;

                    // Programmatically hide the top area
                    topRow.Height = new GridLength(0);
                }
                catch (Exception ex)
                {
                    lblCompletion.Content = "Authentication/Initialization failed. Exception below";
                    txtCompletion.Text = GetFullExceptionMessage(ex);
                    MarkdownViewer.Markdown = string.Empty;
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
                MarkdownViewer.Markdown = string.Empty;
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
            MarkdownViewer.Markdown = string.Empty;
            btnSend.IsEnabled = false;

            try
            {
                var completionText = await sk.Chat(myprompt);
                txtCompletion.Text = completionText;
                MarkdownViewer.Markdown = completionText;
                btnClearCtx.IsEnabled = true;
                btnCopyCtx.IsEnabled = true;
                lblCompletion.Content = "Completion";
            }
            catch (Exception ex)
            {
                lblCompletion.Content = "The request was unsuccessful. Exception below";
                txtCompletion.Text = GetFullExceptionMessage(ex);
                MarkdownViewer.Markdown = string.Empty;
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
        private static string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            if (ex.InnerException != null)
            {
                message += "\n\nInner Exception: " + GetFullExceptionMessage(ex.InnerException);
            }
            return message;
        }
    }
}
