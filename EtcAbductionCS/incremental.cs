// incremental.cs : incremental etcetera abduction in c#
// Andrew S. Gordon
// September 2019

using System;
using System.Collections;
using System.Collections.Generic;
using static EtcAbduction.Knowledgebase;
using static EtcAbduction.Unify;
using static EtcAbduction.Forward;
using static EtcAbduction.Etcetera;

namespace EtcAbduction 
{
    using Solution = List<Literal>;

    public class Incremental {

        public static List<Solution> DoIncremental (List<Literal> obs, Knowledgebase kb, int maxdepth, int n, int w, int b, bool sk)
        {
            var iteration = 1; // count for skolem constants
            var window_start = 0;
            var window_end = Math.Min(w * iteration, obs.Count);
            var previous = new List<Solution>();

            // first, interpret the first window as normal
            var res = Etcetera.NBest(obs.GetRange(window_start, window_end - window_start), kb, maxdepth, b, false);
            var pre = "$" + iteration.ToString() + ":";
            foreach (Solution s in res)
            {
                previous.Add(skolemize_with_prefix(s, pre));
            }

            // then iterate through remaining windows
            while (window_end < obs.Count) // some remain
            {
                iteration += 1; //advance
                window_start = window_end;
                window_end = Math.Min(w * iteration, obs.Count);
                res = ContextualEtcAbduction(obs.GetRange(window_start, window_end - window_start), kb, previous, maxdepth, b, iteration);
                previous = res;
            }
            previous.RemoveRange(n, previous.Count - n); // truncate excess
            return previous;

        }

        public static List<Literal> GetContext(Solution solution, List<Literal> obs, Knowledgebase kb)
        {
            var res = new List<Literal>();
            foreach (Entailment entailment in Entailments(kb, solution))
            {
                if (!obs.Contains(entailment.Entailed) && !res.Contains(entailment.Entailed))
                {
                    res.Add(entailment.Entailed);
                }
            }
            return res;
        }

        private static List<List<Literal>> ContextualAndOrLeaflists(List<Literal> remaining, Knowledgebase kb, int depth, List<Literal> context, List<Literal> antecedents, List<Literal> assumptions)
        {
            if (depth == 0 && antecedents.Count == 0) // fail with empty list
            {
                return new List<Solution>(); 
            }
            else if (remaining.Count == 0) // done with this level
            {  
                if (antecedents.Count == 0) // found one
                {
                    
                    return new List<Solution>() { assumptions }; // list of lists
                }
                else 
                {
                    return ContextualAndOrLeaflists(antecedents, kb, depth - 1, context, new List<Literal>(), assumptions);
                }
            }

            else // more to go on this level
            { 
                var literal = remaining[0]; // pop pt 1
                remaining.RemoveAt(0); // pop pt 2
                var predicate = literal.Predicate;
                if (!kb.IndexByConsequent.ContainsKey(predicate))
                {
                    assumptions.Add(literal); // shift literal to assumptions
                    return ContextualAndOrLeaflists(remaining, kb, depth, context, antecedents, assumptions);
                }

                else
                {
                    var res = new List<Solution>();
                    var rules = kb.IndexByConsequent[predicate];
                    foreach (DefiniteClause rule in rules)
                    {
                        var consequent = rule.Consequent;
                        var theta = unify(literal, consequent);
                        if (theta != null) // unifies
                        {
                            if (depth == 0) // no depth for revision
                            {
                                return new List<Solution>(); // (empty)
                            }
                            else 
                            {
                                var new_remaining = new List<Literal>();
                                foreach (Literal c in remaining)
                                {
                                    new_remaining.Add(c.Subst(theta));
                                }

                                var new_antecedents = new List<Literal>();
                                foreach (Literal c in rule.Antecedents)
                                {
                                    new_antecedents.Add(c.Subst(theta));
                                }
                                new_antecedents = standardize(new_antecedents);
                                foreach (Literal c in antecedents)
                                {
                                    new_antecedents.Add(c.Subst(theta));
                                }

                                var new_assumptions = new List<Literal>();
                                foreach (Literal c in assumptions)
                                {
                                    new_assumptions.Add(c.Subst(theta)); // !!!! BUG
                                }

                                res.AddRange(ContextualAndOrLeaflists(new_remaining, kb, depth, context, new_antecedents, new_assumptions));
                            }
                        }
                    } 
                    foreach (Literal context_literal in context)
                    {
                        var theta = unify(literal, context_literal);
                        if (theta != null) // unifies
                        {
                            var new_remaining = new List<Literal>();
                            foreach (Literal c in remaining)
                            {
                                new_remaining.Add(c.Subst(theta));
                            }

                            var new_antecedents = new List<Literal>();
                            foreach (Literal c in antecedents)
                            {
                                new_antecedents.Add(c.Subst(theta));
                            }

                            var new_assumptions = new List<Literal>();
                            foreach (Literal c in assumptions)
                            {
                                new_assumptions.Add(c.Subst(theta));
                            }

                            res.AddRange(ContextualAndOrLeaflists(new_remaining, kb, depth, context, new_antecedents, new_assumptions));
                        }
                    }
                    return res;
                }
                
            }
        }

        public static List<Solution> ContextualEtcAbduction(List<Literal> window, Knowledgebase kb, List<Solution> previous, int maxdepth, int beam, int iteration)
        {
            var ln_pr_to_beat = double.NegativeInfinity;
            var n_best = new List<Solution>();
            var n_best_ln_pr = new List<Double>();

            foreach (Solution previous_solution in previous)
            {
                var previous_solution_jlpr = JointLnProbability(previous_solution);
                var context = GetContext(previous_solution, window, kb);
                var list_of_lists = new List<List<Solution>>();
                foreach (Literal c in window)
                {
                    var remaining = new List<Literal>() { c };
                    list_of_lists.Add(ContextualAndOrLeaflists(remaining, kb, maxdepth, context, new List<Literal>(), new List<Literal>()));
                }
                var combiner = new Combiner(list_of_lists);
                foreach (Solution s in combiner)
                {
                    if (BestCaseLnProbability(s) > ln_pr_to_beat) // maybe!
                    {
                        foreach (Solution solution in Abduction.Crunch(s))
                        {
                            var jlpr = JointLnProbability(solution) + previous_solution_jlpr; //add ln
                            if (jlpr > ln_pr_to_beat) 
                            {
                                solution.AddRange(previous_solution); // important!
                                var insert_at = n_best_ln_pr.Count;
                                for (int i = 0; i < n_best_ln_pr.Count; i++)
                                {
                                    if (n_best_ln_pr[i] > jlpr)
                                    {
                                        insert_at = i;
                                        break;
                                    }
                                }
                                n_best.Insert(insert_at, solution);
                                n_best_ln_pr.Insert(insert_at, jlpr);
                                if (n_best.Count > beam)
                                {
                                    n_best.RemoveAt(0);
                                    n_best_ln_pr.RemoveAt(0);
                                    ln_pr_to_beat = n_best_ln_pr[0]; // second worst [0] is now lowest
                                }
                            }
                        }
                    }
                }
            }

            n_best.Reverse(); // 0 is now highest;
            var pre = "$" + iteration.ToString() + ":"; 
            var res = new List<Solution>();
            foreach (Solution c in n_best)
            {
                 res.Add(skolemize_with_prefix(c, pre));
            }
            return res;
        }

    }
}