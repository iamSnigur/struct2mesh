using System;

namespace Editor.Utils
{
    public static class EnvironmentUtils
    {
        public static bool TryGetVariable(string variableName, out string ev)
        {
            ev = Environment.GetEnvironmentVariable(variableName);
            return !string.IsNullOrEmpty(ev);
        }
    }
}