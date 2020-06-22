// cli.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static EtcAbduction.Sexp;
using static EtcAbduction.Knowledgebase;
using static EtcAbduction.Forward;
using static EtcAbduction.Etcetera;

namespace EtcAbduction
{

    public class Cli
    {

        static string HELP_MESSAGE = @"Usage: mono cli.exe [options]

    Options:
    -i, --in NAME             set input file name
    -k, --knowledgebase NAME  set knowledgebase file name
    -o, --out NAME            set output file name
    -p, --parse               only parse the input, no reasoning
    -h, --help                print this help menu
    -f, --forward             forward-chain on observations
    -g, --graph               generate graph in dot format
    -d, --depth NUMBER        backchaining depth (default = 5)
    -s, --solution NUMBER     rank of solution to graph (default = 1)
    -a, --all                 generate all solutions
    -n, --nbest NUMBER        generate only n-best solutions (default = 10)
    -c, --incremental         use incremental abduction
    -w, --window NUMBER       incremental abduction window size (default = 4)
    -b, --beam NUMBER         incremental abduction beam size (default = 10)
    -e, --entailments         show entailed literals instead of assumptions";

        public string Program {get; set;}
        public string Input_path {get; set;}
        public string Knowledge_path {get; set;}
        public string Output_path {get; set;}
        public bool Parse_flag {get; set;}
        public bool Help_flag {get; set;}
        public bool Forward_flag {get; set;}
        public bool Graph_flag {get; set;}
        public int Depth {get; set;}
        public bool All_flag {get; set;}
        public int Solution {get; set;}
        public int Nbest {get; set;}
        public bool Incremental_flag {get; set;}
        public int Window {get; set;}
        public int Beam {get; set;}
        public bool EntailmentsFlag {get; set;}

        public Cli(String[] args) 
        {
            this.Parse_flag = false;
            this.Help_flag = false;
            this.Forward_flag = false;
            this.Graph_flag = false;
            this.All_flag = false;
            this.Depth = 5;
            this.Solution = 1;
            this.Nbest = 10;
            this.Incremental_flag = false;
            this.Window = 4;
            this.Beam = 10;
            this.EntailmentsFlag = false;

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
                else if (args[i] == "-o" || args[i] == "--out")
                {
                    i += 1;
                    this.Output_path = args[i];
                }
                else if (args[i] == "-h" || args[i] == "--help")
                {
                    this.Help_flag = true;
                }
                else if (args[i] == "-p" || args[i] == "--parse")
                {
                    this.Parse_flag = true;
                }
                else if (args[i] == "-f" || args[i] == "--forward")
                {
                    this.Forward_flag = true;
                }
                else if (args[i] == "-g" || args[i] == "--graph")
                {
                    this.Graph_flag = true;
                }
                else if (args[i] == "-a" || args[i] == "--all")
                {
                    this.All_flag = true;
                }
                else if (args[i] == "-d" || args[i] == "--depth")
                {
                    i += 1;
                    this.Depth = Int32.Parse(args[i]);
                }
                else if (args[i] == "-s" || args[i] == "--solution")
                {
                    i += 1;
                    this.Solution = Int32.Parse(args[i]);
                }
                else if (args[i] == "-n" || args[i] == "--nbest")
                {
                    i += 1;
                    this.Nbest = Int32.Parse(args[i]);
                }
                else if (args[i] == "-c" || args[i] == "--incremental")
                {
                    this.Incremental_flag = true;
                }
                else if (args[i] == "-w" || args[i] == "--window")
                {
                    i += 1;
                    this.Window = Int32.Parse(args[i]);
                }
                else if (args[i] == "-b" || args[i] == "--beam")
                {
                    i += 1;
                    this.Beam = Int32.Parse(args[i]);
                }
                else if (args[i] == "-e" || args[i] == "--entailments")
                {
                    this.EntailmentsFlag = true;
                }
                
            }
        }

        public static void Main(string[] cli_args)
        {

            var args = new Cli(cli_args);

            if (args.Help_flag) 
            {
                    Console.WriteLine(HELP_MESSAGE);
                    System.Environment.Exit(1);
            }


            // read buffers
            var buffer_i = "";
            var buffer_k = "";

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

            // compose result
            var result = "";

            if (args.Parse_flag == true) // parse only
            {
                result = String.Join("\n", kb.Axioms.Select(dc => dc.Repr()));
            }

            else if (args.Forward_flag == true) // Forward chain
            {
                var entailments = Forward.Entailments(kb, obs);
                if (args.Graph_flag == true) // dot format graph
                {
                    result = Forward.Graph(obs, entailments, new List<Literal>());
                }
                else 
                {
                    result = String.Join("\n", entailments.Select(e => e.Repr()));
                }               
            }

            else // Abduction 
            {  
                var all_solutions = new List<List<Literal>>(); // void

                if (args.All_flag) // All flag
                {
                    all_solutions = Etcetera.DoEtcAbduction(obs, kb, args.Depth,true); 
                }

                // else if incremental here

                else if (args.Incremental_flag)
                {
                    all_solutions = Incremental.DoIncremental(obs, kb, args.Depth, args.Nbest, args.Window, args.Beam, true);
                    //all_solutions = Realtime_Sidecar.incremental_alternative(obs, kb, args.Depth, args.Nbest, args.Window, args.Beam, true);
                }

                // else n-best
                else 
                {
                    all_solutions = Etcetera.NBest(obs, kb, args.Depth, args.Nbest, true); 
                }

                // Then decide what to output
                if (args.Graph_flag)
                {
                    var entailments = Forward.Entailments(kb, all_solutions[args.Solution - 1]);
                    result = Forward.Graph(all_solutions[args.Solution - 1], entailments, obs);
                }
                else if (all_solutions.Count == 0) 
                {
                        result = "0 solutions.";
                }                
                else {
                    var reslist = new List<String>();
                    if (args.EntailmentsFlag)
                    {
                        foreach (List<Literal> solution in all_solutions)
                        {
                            var entailed = Incremental.GetContext(solution, obs, kb);
                            if (entailed.Count == 0)
                            {
                                reslist.Add("none.");
                            }
                            else
                            {
                                reslist.Add($"({String.Join(" ",entailed.Select(e => e.Repr()))})");   
                            }                           
                        }
                    }
                    else 
                    {
                        foreach (List<Literal> solution in all_solutions)
                        {
                            var reprs = new List<String>();
                            foreach (Literal l in solution) 
                            {
                                reprs.Add(l.Repr());
                            }
                            var reprstr = $"({String.Join(" ", reprs)})";
                            reslist.Add(reprstr);
                        }
                    }
                    result = $"{String.Join("\n",reslist)}\n{reslist.Count} solutions.";
                }
            }
        
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