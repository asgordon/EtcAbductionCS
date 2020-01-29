// forward.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.Collections.Generic;
using System.Linq;
using static EtcAbduction.Knowledgebase;
using static EtcAbduction.Unify;

namespace EtcAbduction 
{

    public class Entailment 
    {
        public Literal Entailed {get; set;}
        public List<Literal> Triggers {get; set;}

        public Entailment() {}

        public string Repr()
        {
            return($"{this.Entailed.Repr()} <- {String.Join(", ", this.Triggers.Select(t => t.Repr()))}");
        }
    }

    public class Production
    {
        public List<Literal> Antecedents {get; set;}
        public Literal Consequent {get; set;}
        public List<Literal> Triggers {get; set;}

        public Production() {}

        public static List<Production> FromKb(Knowledgebase kb)
        {
            var res = new List<Production>();
            foreach (DefiniteClause c in kb.Axioms)
            {
                var one = new Production();
                one.Antecedents = c.Antecedents;
                one.Consequent = c.Consequent;
                one.Triggers = new List<Literal>();
                res.Add(one);
            }
            return res;
        }
    }


    public static class Forward {

        public static List<Entailment> Entailments(Knowledgebase kb, List<Literal> assumptions)
        {
            var stack = new Stack<Literal>(assumptions);
            var productions = Production.FromKb(kb);
            var entailed = new List<Entailment>();

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var new_productions = new List<Production>();
                foreach (Production prod in productions)                
                {                    
                    for (int i = 0; i < prod.Antecedents.Count; i++)
                    {
                        var ant = prod.Antecedents[i]; 
                        var theta = Unify.unify(current, ant);
                        if (theta != null)
                        {
                            var new_consequent = prod.Consequent.Subst(theta); 
                            var new_triggers = new List<Literal>(prod.Triggers); // deep!
                            new_triggers.Add(current);
                            if (prod.Antecedents.Count == 1) // last one
                            {
                                var new_entailment = new Entailment();
                                new_entailment.Entailed = new_consequent;
                                new_entailment.Triggers = new_triggers;
                                entailed.Add(new_entailment);
                                stack.Push(new_consequent);
                            }
                            else
                            {
                                var new_antecedents = new List<Literal>();
                                for (int j = 0; j < prod.Antecedents.Count; j++)
                                {
                                    if (i != j) // skip matching antecedent.
                                    {
                                        new_antecedents.Add(prod.Antecedents[j].Subst(theta));
                                    }                                 
                                }
                                var new_production = new Production();
                                new_production.Antecedents = new_antecedents;
                                new_production.Consequent = new_consequent;
                                new_production.Triggers = new_triggers;
                                new_productions.Add(new_production);
                            }
                        }
                    }
                }
                foreach (Production p in new_productions)
                {
                    productions.Add(p);
                }
            }
            return entailed;
        }

        public static String Graph(List<Literal> assumptions, List<Entailment> entailments, List<Literal> targets)
        {
            var res = "digraph proof {\n graph [rankdir=\"TB\"]\n";
            var samestr = "";
            // nodes
            var nodes = new List<Literal>();
            foreach (Literal a in assumptions)
            {
                nodes.Add(a);
            } 
            foreach (Entailment e in entailments)
            {
                nodes.Add(e.Entailed);
            }

            for (int num = 0; num < nodes.Count; num++)
            {
                var node = nodes[num];
                if (assumptions.Contains(node))
                {
                    res += " n" + num.ToString() + " [label=\"" + node.Repr() +"\"];\n";
                }
                else if (targets.Contains(node))
                {
                    res += " n" + num.ToString() + " [shape=box peripheries=2 label=\"" + node.Repr() +"\"];\n";
                    samestr += " n" + num.ToString();
                }
                else 
                {
                    res += " n" + num.ToString() + " [shape=box label=\"" + node.Repr() +"\"];\n";
                }
            }
            // arcs
            foreach (Entailment e in entailments)
            {
                foreach (Literal a in e.Triggers)
                {
                    // doesn't work. nodes.IndexOf(a) returns -1
                    res += " n" + nodes.IndexOf(a) + " -> n" + nodes.IndexOf(e.Entailed) + "\n";
                }
            }
            //rank = same
            if (samestr != "")
            {
                res += " {rank=same" + samestr + "}\n";
            }
            //close
            res += "}\n";

            return res;
        }

    }

}

// TODO
// . Use "nodeLabel function instead of Repr() for text on graph nodes.
