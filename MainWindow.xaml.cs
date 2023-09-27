// jmneto Azure Open AI Chat Client (Using Semantic Kernel)
// Sept 2024 - Version 1.0

using System;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using System.Windows.Media.Animation;

namespace AzureOpenAIChat
{
    public partial class MainWindow : Window
    {

        // Azure OpenAI Chat Client (Using Semantic Kernel)
        SKHelper sk;

        public MainWindow()
        {
            InitializeComponent();

            // Center form on Screen
            Window currentWindow = Application.Current.MainWindow;
            Size windowSize = new Size(currentWindow.Width, currentWindow.Height);
            Rect screenSize = SystemParameters.WorkArea;
            currentWindow.Left = (screenSize.Width / 2) - (windowSize.Width / 2);
            currentWindow.Top = (screenSize.Height / 2) - (windowSize.Height / 2);

            // Set focus to promt field
            txtPrompt.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Init/Load From Registry
            txtAPIEndPoint.Text = RegistryHelper.ReadAppInfo("APIENDPOINT");
            if (txtAPIEndPoint.Text == string.Empty)
                txtAPIEndPoint.Text = "Your Azure OpenAI Endpoint";

            txtAPIKey.Text = RegistryHelper.ReadAppInfo("APIKEY");
            if (txtAPIKey.Text == string.Empty)
                txtAPIKey.Text = "Your Azure OpenAI API Service Key";

            txtDeployment.Text = RegistryHelper.ReadAppInfo("DEPLOYMENT");
            if (txtDeployment.Text == string.Empty)
                txtDeployment.Text = "Your Azure OpenAI Model Deployment Name";

            txtTemperature.Text = RegistryHelper.ReadAppInfo("TEMPERATURE");
            if (txtTemperature.Text == string.Empty)
                txtTemperature.Text = "0.5";

            txtMaxTokens.Text = RegistryHelper.ReadAppInfo("MAXTOKENS");
            if (txtMaxTokens.Text == string.Empty)
                txtMaxTokens.Text = "2048";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save to Registry
            RegistryHelper.WriteAppInfo("APIENDPOINT", txtAPIEndPoint.Text);
            RegistryHelper.WriteAppInfo("APIKEY", txtAPIKey.Text);
            RegistryHelper.WriteAppInfo("DEPLOYMENT", txtDeployment.Text);
            RegistryHelper.WriteAppInfo("TEMPERATURE", txtTemperature.Text);
            RegistryHelper.WriteAppInfo("MAXTOKENS", txtMaxTokens.Text);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

            if(sk == null)
            {
                string apiEndpoint = txtAPIEndPoint.Text;
                string apikey = txtAPIKey.Text;
                string deployment = txtDeployment.Text;
                double temperature = double.Parse(txtTemperature.Text);
                int maxtokens = int.Parse(txtMaxTokens.Text);

                sk = new SKHelper(deployment, apiEndpoint, apikey, maxtokens, temperature);

                // make TxtDeployment readonly
                txtAPIEndPoint.IsReadOnly = true;
                txtAPIKey.IsReadOnly = true;
                txtDeployment.IsReadOnly = true;
                txtTemperature.IsReadOnly = true;
                txtMaxTokens.IsReadOnly = true;

                groupedElements.Visibility = Visibility.Hidden;
                gridcompletion.Margin = new Thickness(10, 10, 10, 220);
            }

            // Trim Prompt
            string myprompt = txtPrompt.Text;

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

        private void btnClearCtx_Click(object sender, RoutedEventArgs e)
        {
            btnClearCtx.IsEnabled = false;
            sk.InitContext();
            lblCompletion.Content = "Completion context cleared";
        }
    }
}
