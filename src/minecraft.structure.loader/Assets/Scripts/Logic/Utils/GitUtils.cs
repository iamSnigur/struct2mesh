using System.Diagnostics;

namespace Logic.Utils
{
    public static class GitUtils
    {
        public static string RunCommand(string args)
        {
            Process process = new();

            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Trim();
        }
    }
}