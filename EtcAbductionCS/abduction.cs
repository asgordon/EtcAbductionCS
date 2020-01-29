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

    public static class Abduction 
    {

        public static List<Solution> DoAbduction(List<Literal> obs, Knowledgebase kb, int maxdepth, bool sk)
        {
            var list_of_lists = new List<List<Solution>>();
            foreach (Literal c in obs)
            {
                var remaining = new List<Literal>() { c };
                list_of_lists.Add(AndOrLeaflists(remaining, kb, maxdepth, new List<Literal>(), new List<Literal>()));
            }
            var combiner = new Combiner(list_of_lists);
            var res = new List<Solution>();
            foreach (Solution s in combiner)
            {
                foreach (Solution c in Crunch(s))
                {
                    if (sk) 
                    {
                        res.Add(skolemize(c));
                    }
                    else 
                    {
                        res.Add(c);
                    }
                }
            }
            return res.Distinct().ToList();
        }

        public static List<Solution> AndOrLeaflists(List<Literal> remaining, Knowledgebase kb, int depth, List<Literal> antecedents, List<Literal> assumptions)
        {
            if (depth == 0 && antecedents.Count > 0) // fail with empty list
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
                    return AndOrLeaflists(antecedents, kb, depth - 1, new List<Literal>(), assumptions);
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
                    return AndOrLeaflists(remaining, kb, depth, antecedents, assumptions);
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
                                    new_assumptions.Add(c.Subst(theta));
                                }

                                res.AddRange(AndOrLeaflists(new_remaining, kb, depth, new_antecedents, new_assumptions));
                            }
                        }
                    } 
                    return res;
                    
                }
                
            }
        }


        public static List<Solution> Crunch(Solution conjunction)
        {
            conjunction = DistinctLiterals(conjunction);
            if (conjunction.Count < 2) 
            {
                return new List<Solution>() { conjunction };
            }
            else 
            {
                return DistinctSolutions(Cruncher(conjunction, 0));        
            }
        }

        private static List<Solution> Cruncher(Solution conjunction, int idx)
        {
            if (idx == conjunction.Count - 1) // last one
            {
                return new List<Solution> { DistinctLiterals(conjunction) };
            }
            else 
            {
                var res = new List<Solution>();
                for (int i = idx + 1; i < conjunction.Count; i++ ) // iterate from idx+1 onward
                {
                    var theta = unify(conjunction[idx], conjunction[i]);
                    if (theta != null) // match!
                    {
                        var new_solution = new Solution();
                        for (int j = 0; j < conjunction.Count; j++)
                        {
                            if (j != idx) // not the one that unifies
                            {
                                new_solution.Add(conjunction[j].Subst(theta));
                            }
                        }
                        res.AddRange(Cruncher(new_solution, 0)); 
                        // terribly inefficient!?
                    }
                }
                res.AddRange(Cruncher(conjunction, idx + 1));
                return res;
            }
        }

        private static Solution DistinctLiterals(Solution conjunction)
        {        
            conjunction.Sort();   
            return conjunction.Distinct().ToList();
        }

        private static List<Solution> DistinctSolutions(List<Solution> all_solutions)
        {    
            return all_solutions.Distinct(new SolutionEqCompare()).ToList();
        }

    }

    class SolutionEqCompare : IEqualityComparer<Solution>
    {
        public bool Equals(Solution x, Solution y)
        {
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (!x[i].Equals(y[i]))
                    return false;
            }
            return true;
        }

    public int GetHashCode(Solution obj)
    {
            int hash = 0;
            foreach (Literal lit in obj)
                hash = hash ^ lit.GetHashCode();

            return hash;
        }
    }

    public class Combiner : IEnumerable<Solution>
    {
        public List<List<Solution>> Parts {get; set;}
        public List<int> Lengths {get; set;}
        public List<int> Indicies {get; set;}

        public Combiner(List<List<Solution>> parts)
        {

            this.Parts = new List<List<Solution>>();
            this.Lengths = new List<int>();
            this.Indicies = new List<int>();
            foreach (List<Solution> part in parts)
            {
                this.Lengths.Add(part.Count);
                this.Indicies.Add(0);
            }
            if (this.Lengths.Contains(0))
            {
                this.Parts = new List<List<Solution>>();
                this.Lengths = new List<int>();
                this.Indicies = new List<int>();
            }
            else{
                this.Parts = parts;
            }
        }

        private IEnumerable<Solution> Solutions() 
        {
            var vlen = this.Indicies.Count;
            while ((vlen > 0) && (this.Indicies[vlen - 1] != this.Lengths[vlen - 1])) // not done
            {
                var res = new Solution();
                for (int i = 0; i < vlen; i++) {
                    var clone = this.Parts[i][this.Indicies[i]];
                    res.AddRange(clone);
                }
                // increment the counter
                this.Indicies[0] += 1;
                // tumble except the last one
                for (int i = 0; i < vlen - 1; i++) {
                    if (this.Indicies[i] == this.Lengths[i]) {
                        this.Indicies[i] = 0;
                        this.Indicies[i + 1] += 1;
                    }
                }
                yield return res;
            }

        }

        public IEnumerator<Solution> GetEnumerator()
        {
            return Solutions().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


}