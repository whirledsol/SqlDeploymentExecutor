using CommandLine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlDeploymentExecutor
{
    class Program
    {
        /// <summary>
        /// START HERE!
        /// </summary>
        /// <param name="unparsedArgs"></param>
        static void Main(string[] unparsedArgs)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                Parser.Default.ParseArguments<Arguments>(unparsedArgs)
                  .WithParsed<Arguments>(args=>MainParsed(args))
                  .WithNotParsed((err)=>throw new ArgumentException(err.FirstOrDefault()?.ToString()));
            }
            catch(Exception ex)
            {
                Console.WriteLine($"\t\t{new String('#', 80)}");
                Console.WriteLine("\t\tEXCEPTION OCCURRED");
                Console.WriteLine($"\t\t{ex.Message}");
                Console.WriteLine($"\t\t{ex.StackTrace}");
                Console.WriteLine($"\t\t{new String('#', 80)}");
            }

        }

        /// <summary>
        /// Driver
        /// </summary>
        /// <param name="args"></param>
        static void MainParsed(Arguments args)
        {
            args.User = args.User ?? Environment.MachineName;
            var files = GetSqlFiles(args.Path,args.Ext);
            files = FindUnexecutedFiles(files, args.ExecutedHeader);
            ExecuteFiles(files, args);
            
            Console.WriteLine(new String('#', 80));
            Console.WriteLine($"SDP Completed on {DateTime.Now} after executing {files.Count} scripts");
            Console.WriteLine(new String('#', 80));
            Console.WriteLine("Press Any Key to Exit...");
            _ = Console.ReadKey();
        }


        /// <summary>
        /// Gets all files with the ext and their contents (trimmed of whitespace)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        static Dictionary<string, string> GetSqlFiles(string path, string ext)
        {
            var files = Directory.GetFiles(path).ToList();

            files = files.Where(f => f.EndsWith(ext)).ToList();

            return files.ToDictionary(f => f, f => File.ReadAllText(f).Trim().Trim('\n', '\r'));
        }


        /// <summary>
        /// Go through all files and exclude files that have the executed header text in their first line
        /// </summary>
        /// <param name="files"></param>
        /// <param name="executedHeader"></param>
        /// <returns></returns>
        static Dictionary<string, string> FindUnexecutedFiles(Dictionary<string,string> files, string executedHeader)
        {
            foreach(var file in files.Keys)
            {
                var contents = files[file];
                if (String.IsNullOrWhiteSpace(contents)) {
                    files.Remove(file);
                    ConsoleWriteStatus(false, file, $"unexecuted due to no content");
                }
                string firstline = contents.Contains("\n") ? contents.Split("\n")[0] : contents;
                if (firstline.ToUpper().Contains(executedHeader.ToUpper()))
                {
                    files.Remove(file);
                    ConsoleWriteStatus(false, file, $"unexecuted due to header \"{firstline.Trim()}\"");
                }
                if (ContainsExecutableGO(contents))
                {
                    files.Remove(file);
                    ConsoleWriteStatus(false, file, $"unexecuted due to GO keyword");
                }
            }
            return files;
        }


        /// <summary>
        /// See if the sql contians an executable GO statement which will error out. Inform the user.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        static bool ContainsExecutableGO(string content)
        {
            var linebreak = @"\r|\n";
            var blockComments = @"\/\*(.*)?\*\/";
            var lineComments = @"--(.*?)\r?\n";
            var strings = @"'.*[^']'";
            var go = @"(\s|;|$)GO(\s|;|$)";

            // remove single line comments first since it requires end of line in the regex
            content = Regex.Replace(content, lineComments, " ");
            // remove linebreaks for other statements to work
            content = Regex.Replace(content, linebreak, " ");
            // remove multiline comments
            content = Regex.Replace(content, blockComments, " ");
            // remove strings (could be multiline)
            content = Regex.Replace(content, strings, " ");

            // now see if the remaining text (all executable) contains the GO regex
            return Regex.IsMatch(content, go);
        }


        /// <summary>
        /// Execute the files in filename order
        /// </summary>
        /// <param name="files"></param>
        /// <param name="args"></param>
        static void ExecuteFiles(Dictionary<string, string> files, Arguments args)
        {
            if (files.Count > 0)
            {
                using var con = new SqlConnection(args.cs);
                con.Open();
                //enforce ordering by name of file but this probably isn't needed
                foreach (var file in files.Keys.OrderBy(f => f))
                {
                    var content = files[file];
                    try
                    {
                        using var cmd = new SqlCommand(content, con);
                        cmd.CommandTimeout = args.Timeout;
                        cmd.ExecuteNonQuery();
                        ConsoleWriteStatus(true, file, $"executed on server");
                        UpdateFileWithHeader(file, content, args);
                    }
                    catch (SqlException ex)
                    {
                        ConsoleWriteStatus(false, file, $"unexecuted due to sql exception: {ex.Message}");
                        //throw;
                    }
                }
            }
        }


        /// <summary>
        /// Overwrite the file with header, marking it as executed
        /// </summary>
        /// <param name="file"></param>
        /// <param name="content"></param>
        /// <param name="args"></param>
        static void UpdateFileWithHeader(string file, string content, Arguments args)
        {
            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(args.cs);
            string database = connectionStringBuilder.InitialCatalog;
            var header = $"-- {args.ExecutedHeader} USING {database ?? "____"} ON {DateTime.Now} BY {args.User}\r\n";
            File.WriteAllText(file, header + content);
        }


        /// <summary>
        /// Shorthand for showing the status of the file
        /// </summary>
        /// <param name="success"></param>
        /// <param name="filepath"></param>
        /// <param name="message"></param>
        static void ConsoleWriteStatus(bool success, string filepath, string message)
        {
            //string symbol = success ? "\u2713" : "\u274C";
            string symbol = success ? "*" : "x";
            Console.WriteLine("{0} {1,-50}{2}", symbol, Path.GetFileName(filepath), message);
        }
    }
}
