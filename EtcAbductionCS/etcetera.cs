// abduction.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static EtcAbduction.Knowledgebase;
using static EtcAbduction.Unify;

namespace EtcAbduction 
{
    using Solution = List<Literal>;

    public static class Etcetera {

        public static List<Solution> DoEtcAbduction(List<Literal> obs, Knowledgebase kb, int maxdepth, bool sk)
        {
            var solutions = Abduction.DoAbduction(obs, kb, maxdepth, sk);
            solutions.Sort((x,y) => JointLnProbability(y).CompareTo(JointLnProbability(x))); // reverse
            return solutions;
        }

        public static double JointLnProbability(Solution s)
        {
            var res = 0.0;
            foreach (Literal l in s)
            {
                res += l.LnProbability;
            }
            return res;
        }

        public static double BestCaseLnProbability(Solution s)
        {
            var predicates = new HashSet<String>();
            var res = 0.0;
            foreach (Literal l in s)
            {
                if (!predicates.Contains(l.Predicate))
                {
                    res += l.LnProbability;
                    predicates.Add(l.Predicate);
                }
            }
            return res;
        }

        public static List<Solution> NBest(List<Literal> obs, Knowledgebase kb, int maxdepth, int n, bool sk)
        {
            var ln_pr_to_beat = double.NegativeInfinity;
            var n_best = new List<Solution>();
            var n_best_ln_pr = new List<Double>();
            var list_of_lists = new List<List<Solution>>();
            foreach (Literal c in obs)
            {
                var remaining = new List<Literal>() { c };
                list_of_lists.Add(Abduction.AndOrLeaflists(remaining, kb, maxdepth, new List<Literal>(), new List<Literal>()));
            }
            var combiner = new Combiner(list_of_lists);
            foreach (Solution s in combiner)
            {
                if (BestCaseLnProbability(s) > ln_pr_to_beat) 
                {
                    foreach (Solution solution in Abduction.Crunch(s))
                    {
                        var jpr = JointLnProbability(solution);
                        if (jpr > ln_pr_to_beat) 
                        {
                            var insert_at = n_best_ln_pr.Count;
                            for (int i = 0; i < n_best_ln_pr.Count; i++)
                            {
                                if (n_best_ln_pr[i] > jpr)
                                {
                                    insert_at = i;
                                    break;
                                }
                            }
                            n_best.Insert(insert_at, solution);
                            n_best_ln_pr.Insert(insert_at, jpr);
                            if (n_best.Count > n)
                            {
                                n_best.RemoveAt(0);
                                n_best_ln_pr.RemoveAt(0);
                                //ln_pr_to_beat = jpr; // shouldn't this be the last item instead?
                                ln_pr_to_beat = n_best_ln_pr[0]; // second worst [0] is now lowest
                            }
                        }
                    }
                }
            }
            n_best.Reverse(); // 0 is now highest;
            if (sk)  // skolemize here
            {
                var res = new List<Solution>();
                foreach (Solution c in n_best)
                {
                    res.Add(skolemize(c));
                }
                return res;
            }
            else 
            {
                return n_best;
            }

        }
    }
}
