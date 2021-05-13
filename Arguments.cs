using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlDeploymentExecutor
{
    public class Arguments
    {
        [Option('c', "cs", Required = true, HelpText = "The connection string.")]
        public string cs { get; set; }

        [Option('d', "directory", Required = true, HelpText = "The directory of sql files to run, assuming no subdirectories and files are ordered by name.")]
        public string Path { get; set; }

        [Option('h', "header", Required = false, HelpText = "Ignore running files that contain the following text in their first line.", Default = "EXECUTED")]
        public string ExecutedHeader { get; set; }

        [Option('e', "ext", HelpText = "Only run these exts.", Default =".sql")]
        public string Ext { get; set; }

        [Option('t', "timeout", HelpText = "Sql timeout for each file.", Default = 300)]
        public int Timeout { get; set; }

        [Option('u', "user", HelpText = "Explicitly state the person executing. Otherwise uses the machine name.")]
        public string User { get; set; }

    }
}
