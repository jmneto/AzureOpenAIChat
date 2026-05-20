// Azure Open AI Chat Client (Using Semantic Kernel)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace AzureOpenAIChat
{
    // helper to access the Windows Registry
    internal static class RegistryHelper
    {
        // Registry Key
        private const string AppKey = "SOFTWARE\\AzureOpenAIChat";

        // Keys that contain secrets and must be encrypted at rest
        private static readonly HashSet<string> ProtectedKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "CLIENTSECRET"
        };

        // Write/Read Registry
        public static void WriteAppInfo(string key, string value)
        {
            using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(AppKey))
            {
                if (ProtectedKeys.Contains(key))
                {
                    byte[] encrypted = ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(value),
                        null,
                        DataProtectionScope.CurrentUser);
                    registryKey.SetValue(key, Convert.ToBase64String(encrypted));
                }
                else
                {
                    registryKey.SetValue(key, value);
                }
            }
        }

        public static string? ReadAppInfo(string key)
        {
            using (RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(AppKey))
            {
                if (registryKey == null)
                    return null;

                var raw = (string?)registryKey.GetValue(key);
                if (raw == null)
                    return null;

                if (ProtectedKeys.Contains(key))
                {
                    try
                    {
                        byte[] encrypted = Convert.FromBase64String(raw);
                        byte[] decrypted = ProtectedData.Unprotect(
                            encrypted,
                            null,
                            DataProtectionScope.CurrentUser);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                    catch (FormatException)
                    {
                        // Legacy unencrypted value – return as-is (will be encrypted on next save)
                        return raw;
                    }
                    catch (CryptographicException)
                    {
                        return raw;
                    }
                }

                return raw;
            }
        }
    }
}
