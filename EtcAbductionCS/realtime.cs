// realtime.cs
// Andrew S. Gordon
// September 2019

using System;
using System.Collections;
using System.Collections.Generic;
using static EtcAbduction.Knowledgebase;
using static EtcAbduction.Unify;
using static EtcAbduction.Forward;
using static EtcAbduction.Etcetera;
using static EtcAbduction.Incremental;

namespace EtcAbduction 
{
    using Solution = List<Literal>;

    public class Realtime {

        public Knowledgebase knowledgebase {get; set;}
        public List<Literal> observables {get; set;}
        public List<Solution> contexts {get; set;}
        public int cached {get; set;}
        public int window {get; set;}
        public int beam {get; set;}
        public int depth {get; set;}
        public int current {get; set;} // index of next unprocessed observable
        public List<Solution> solutions {get; set;}

        public Realtime(Knowledgebase kb, int depth, int window, int beam)
        {
            this.knowledgebase = kb;
            this.observables = new List<Literal>();
            this.contexts = new List<Solution>();
            this.current = 0;
            this.cached = 0;
            this.solutions = new List<Solution>();
            this.depth = depth;
            this.window = window;
            this.beam = beam;
        }

        public void Observe(Literal ob)
        {
            this.observables.Add(ob);
        }

        public void AdvanceOneObservable()
        {
            if (this.current < this.observables.Count) // more to do
            {
                this.current += 1;
                if (this.current <= this.window)
                {
                    var res = Etcetera.NBest(this.observables.GetRange(0,this.current), this.knowledgebase, this.depth, this.beam, false);
                    var pre = "$" + this.current.ToString() + ":";
                    this.solutions = new List<Solution>(); // blow away previous
                    foreach (Solution s in res)
                    {
                        this.solutions.Add(skolemize_with_prefix(s, pre));
                    }
                }
                else 
                {
                    if (this.current > this.cached + this.window) // treat old solutions as contexts
                    {
                        this.contexts = this.solutions;
                        this.cached += this.window;
                    }
                    var current_window = this.observables.GetRange(this.cached, this.current - this.cached);
                    var solution_list = Incremental.ContextualEtcAbduction(this.observables, current_window, knowledgebase, contexts, depth, beam, current);
                    if (solution_list.Count > this.beam)
                    {
                        solution_list.RemoveRange(this.beam, solution_list.Count - this.beam);
                    }
                    this.solutions = solution_list;
                }
            }
        }

        public List<Solution> FinalSolutions()
        {
            while (this.current < this.observables.Count)
            {
                AdvanceOneObservable();
            }
            return this.solutions;
        }
    }

    public static class Realtime_Sidecar
    {
        public static List<Solution> incremental_alternative(List<Literal> obs, Knowledgebase kb, int maxdepth, int n, int w, int b, bool sk)
        {
            var rt = new Realtime(kb, maxdepth, w, b);
            foreach (Literal ob in obs)
            {
                rt.Observe(ob);
            }
            return rt.FinalSolutions();
        }

    }
}
