// Azure Open AI Chat Client (Using Semantic Kernel)

using System;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Printing;

namespace AzureOpenAIChat
{
    public partial class MainWindow : Window
    {
        // Azure OpenAI Chat Client (Using Semantic Kernel)
        SKHelper sk;

        public MainWindow()
        {
            InitializeComponent();

            // Init/Load From Registry
            if (string.IsNullOrEmpty(txtAPIEndPoint.Text = RegistryHelper.ReadAppInfo("APIENDPOINT")))
                txtAPIEndPoint.Text = "Your Azure OpenAI Endpoint";

            if (string.IsNullOrEmpty(txtAPIKey.Text = RegistryHelper.ReadAppInfo("APIKEY")))
                txtAPIKey.Text = "Your Azure OpenAI API Service Key";

            if (string.IsNullOrEmpty(txtDeployment.Text = RegistryHelper.ReadAppInfo("DEPLOYMENT")))
                txtDeployment.Text = "Your Azure OpenAI Model Deployment Name";

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
            RegistryHelper.WriteAppInfo("APIKEY", txtAPIKey.Text);
            RegistryHelper.WriteAppInfo("DEPLOYMENT", txtDeployment.Text);
            RegistryHelper.WriteAppInfo("TEMPERATURE", txtTemperature.Text);
            RegistryHelper.WriteAppInfo("MAXTOKENS", txtMaxTokens.Text);
            RegistryHelper.WriteAppInfo("WINDOWLEFT", this.Left.ToString());
            RegistryHelper.WriteAppInfo("WINDOWTOP", this.Top.ToString());
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (sk == null)
            {
                // Get the parameters from the screen
                string apiEndpoint = txtAPIEndPoint.Text;
                string apikey = txtAPIKey.Text;
                string deployment = txtDeployment.Text;
                double temperature = double.Parse(txtTemperature.Text);
                int maxtokens = int.Parse(txtMaxTokens.Text);

                sk = new SKHelper(deployment, apiEndpoint, apikey, maxtokens, temperature);

                // make Setup readonly
                txtAPIEndPoint.IsReadOnly = true;
                txtAPIKey.IsReadOnly = true;
                txtDeployment.IsReadOnly = true;
                txtTemperature.IsReadOnly = true;
                txtMaxTokens.IsReadOnly = true;

                // Programmatically hide the top area
                topRow.Height = new GridLength(0);
            }

            // Trim Prompt
            string myprompt = txtPrompt.Text.Trim();

            // If prompt is empty reset and return
            if (myprompt == string.Empty)
            {
                // Reset Completion
                txtCompletion.Text = "";
                lblCompletion.Content = "Completion context cleared";
                btnClearCtx.IsEnabled = false;
                sk.InitContext();
                return;
            }

            // Wait message 
            lblCompletion.Content = "Request is processing...";
            txtCompletion.Text = "";

            // Call the REST API on a separate thread from UI
            Task.Run(() =>
            {
                try
                {
                    // Call the REST API
                    var completionText = sk.Chat(myprompt).Result;

                    // If not empty update the screen
                    if (completionText != string.Empty)
                        Dispatcher.Invoke(() =>
                        {
                            txtCompletion.Text = completionText;
                            btnClearCtx.IsEnabled = true;
                            lblCompletion.Content = "Completion";
                        });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lblCompletion.Content = "The request was unsuccessful. Exception below";
                        txtCompletion.Text = ex.Message;
                    });
                }
            });
        }

        // Clear Context
        private void btnClearCtx_Click(object sender, RoutedEventArgs e)
        {
            btnClearCtx.IsEnabled = false;
            sk.InitContext();
            lblCompletion.Content = "Completion context cleared";
        }
    }
}
