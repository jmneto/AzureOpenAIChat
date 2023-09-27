// jmneto Azure Open AI Chat Client (Using Semantic Kernel)
// Sept 2024 - Version 1.0

using Microsoft.Win32;

namespace AzureOpenAIChat
{
    // helper to access the Windows Registry
    internal static class RegistryHelper
    {
        // Registry Key
        private const string AppKey = "SOFTWARE\\AzureOpenAIChat";

        // Write/Read Registry
        public static void WriteAppInfo(string key, string value)
        {
            using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(AppKey))
            {
                registryKey.SetValue(key, value);
            }
        }

        public static string? ReadAppInfo(string key)
        {
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(AppKey))
            {
                if (registryKey == null) return null;
                return (string)registryKey.GetValue(key);
            }
        }
    }
}
