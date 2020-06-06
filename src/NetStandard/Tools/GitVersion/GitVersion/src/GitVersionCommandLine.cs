﻿using System.Threading.Tasks;
using static SimpleExec.Command;

namespace Teronis.Tools.GitVersion
{
    public class GitVersionCommandLine
    {
        private const string GitVersionExecutableName = "GitVersion.exe";

        public static string ExecuteGitVersion(string args) =>
            Read(GitVersionExecutableName, args);

        public static Task<string> ExecuteGitVersionAsync(string args) =>
            ReadAsync(GitVersionExecutableName, args);
    }
}