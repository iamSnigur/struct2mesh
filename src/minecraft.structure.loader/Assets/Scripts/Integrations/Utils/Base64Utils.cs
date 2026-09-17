using System;
using System.Text;

namespace Integrations.Utils
{
    public static class Base64Utils
    {
        public static string Decode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            string base64 = input
                .Trim()
                .Replace('-', '+')
                .Replace('_', '/');
         
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}