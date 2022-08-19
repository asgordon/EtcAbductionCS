// text generator cli.cs 
// Andrew S. Gordon
// June 2020
// November 2020

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EtcAbduction
{

    public class TextGeneratorCli
    {

        static string HELP_MESSAGE = @"Usage: mono tgcli.exe [options]

    Options:
    -i, --in NAME             set input file name
    -k, --knowledgebase NAME  set knowledgebase file name
    -r, --ranker OPTION       set the ranker to shortest, longest, or all
    -t, --templates NAME      set the templates file name
    -s, --solution NAME       set the solution file name
    -o, --out NAME            set output file name
    -h, --help                print this help menu";


        public string Program {get; set;}
        public string Input_path {get; set;}
        public string Knowledge_path {get; set;}

        public string Templates_path {get; set;}
        public string Solution_path {get; set;}
        public string Output_path {get; set;}

        public string Ranker {get; set;}
        public string Method { get; set; }

        public bool Help_flag {get; set;}

        public TextGeneratorCli(String[] args) 
        {

            this.Help_flag = false;
            this.Ranker = "default";
            this.Method = "shallow_causes";

            for (int i = 0; i < args.Length; i++) 
            {
                if (args[i] == "-i" || args[i] == "--in")
                {
                    i += 1; // advance one
                    this.Input_path = args[i];
                }
                else if (args[i] == "-k" || args[i] == "--knowledgebase")
                {
                    i += 1;
                    this.Knowledge_path = args[i];
                }
                else if (args[i] == "-t" || args[i] == "--templates")
                {
                    i += 1;
                    this.Templates_path = args[i];
                }
                else if (args[i] == "-r" || args[i] == "--ranker")
                {
                    i += 1;
                    this.Ranker = args[i];
                }
                else if (args[i] == "-m" || args[i] == "--method")
                {
                    i += 1;
                    this.Method = args[i];
                }
                else if (args[i] == "-s" || args[i] == "--solution")
                {
                    i += 1;
                    this.Solution_path = args[i];
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

            var args = new TextGeneratorCli(cli_args);

            if (args.Help_flag) 
            {
                    Console.WriteLine(HELP_MESSAGE);
                    System.Environment.Exit(1);
            }


            // read buffers
            var buffer_i = "";
            var buffer_k = "";
            var buffer_t = "";
            var buffer_s = "";

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

            if (args.Knowledge_path != null)
            {
                buffer_k = File.ReadAllText(args.Knowledge_path);
            }

            // create knowledgebase
            var kb = new Knowledgebase();
            var obs = kb.Add(buffer_i);
            obs.AddRange(kb.Add(buffer_k));

            // create templates knowledgebase
            buffer_t = File.ReadAllText(args.Templates_path);
            var tkb = new Knowledgebase();
            var tobs = tkb.Add(buffer_t);  
            TextGenerator tg = new TextGenerator(kb, tkb, tobs);    

            // read the solution 
            buffer_s = File.ReadAllText(args.Solution_path);
            var skb = new Knowledgebase();
            var solution = skb.Add(buffer_s); // solution literals only


            var result = "";    
            
            result = $"{String.Join(" || ",tg.Generate(solution, obs, selector: args.Method, ranker: args.Ranker ))}";                         
        
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