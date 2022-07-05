// convert cli.cs 
// Andrew S. Gordon
// June 2022

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EtcAbduction
{

    public class ConvertCli
    {

        static string HELP_MESSAGE = @"Usage: mono convert.exe [options]

    Options:
    -i, --in NAME             set input file name
    -t, --templates NAME      set the templates file name
    -h, --help                print this help menu";


        public string Program {get; set;}
        public string Input_path {get; set;}
        public string Templates_path {get; set;}
        public string Output_path {get; set;}
        public bool Help_flag {get; set;}

        public ConvertCli(String[] args) 
        {

            this.Help_flag = false;

            for (int i = 0; i < args.Length; i++) 
            {
                if (args[i] == "-i" || args[i] == "--in")
                {
                    i += 1; // advance one
                    this.Input_path = args[i];
                }
                else if (args[i] == "-t" || args[i] == "--templates")
                {
                    i += 1;
                    this.Templates_path = args[i];
                }
                else if (args[i] == "-o" || args[i] == "--out")
                {
                    i += 1;
                    this.Output_path = args[i];
                }
                else if (args[i] == "-h" || args[i] == "--help")
                {
                    this.Help_flag = true;
                }
            }
        }

        public static void Main(string[] cli_args)
        {

            var args = new ConvertCli(cli_args);

            if (args.Help_flag) 
            {
                    Console.WriteLine(HELP_MESSAGE);
                    System.Environment.Exit(1);
            }

            // read buffers
            var buffer_i = "";
            var buffer_t = "";

            if (args.Input_path != null) 
            {
                buffer_i = File.ReadAllText(args.Input_path);
            }
            else 
            {
                if (Console.IsInputRedirected)
                {
                    using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                    {
                        buffer_i = reader.ReadToEnd();
                    }
                }
            }

            // create knowledgebase (unused)
            var kb = new Knowledgebase();

            // create templates knowledgebase
            buffer_t = File.ReadAllText(args.Templates_path);
            var tkb = new Knowledgebase();
            var tobs = tkb.Add(buffer_t);  
            TextGenerator tg = new TextGenerator(kb, tkb, tobs);    // refactor later to remove useless kb

            // read the content
            var ckb = new Knowledgebase();
            var content = ckb.Add(buffer_i); // content literals only

            var result = tg.ConvertToText(content);

            // output
            if (args.Output_path != null)
            {   
                using (StreamWriter file = new StreamWriter(args.Output_path))
                {
                    file.WriteLine(result);
                }
            }
            else 
            {
                Console.WriteLine(result);
            }
        }
    }
}