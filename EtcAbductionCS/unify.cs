// unify.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.Collections.Generic;
using static EtcAbduction.Knowledgebase;

namespace EtcAbduction 
{
    public static class Unify
    {
        public static int VARCOUNTER = 0;
        
        public static Dictionary<Term,Term> unify(Literal x, Literal y) 
        {
            return unify_helper(x,y, new Dictionary<Term,Term>());
        }

        public static Dictionary<Term,Term> unify_helper(Literal x, Literal y, Dictionary<Term,Term> theta)
        {
            if (x.Terms.Count != y.Terms.Count)
            {
                return null;
            }
            if (x.Predicate != y.Predicate)
            {
                return null;
            }
            for (int i = 0; i < x.Terms.Count; i++) 
            {
                var s = x.Terms[i];
                var t = y.Terms[i];
                while (theta.ContainsKey(s))
                {
                    s = theta[s];
                }
                while (theta.ContainsKey(t))
                {
                    t = theta[t];
                }
                if (s != t) // should work for Terms! (overloaded)
                {
                    if (s.Type == Term.TermType.VARIABLE) // s is a variable
                    {
                        if (t.Type == Term.TermType.VARIABLE && s < t) // t is a variable and s < t // ** need to implement < for Term
                        {
                            theta[t] = s;
                        }
                        else
                        {
                            theta[s] = t;
                        }
                    }
                    else
                    {
                        if (t.Type == Term.TermType.VARIABLE) // t is a variable
                        {
                            theta[t] = s;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            return theta;
        }

        public static string new_variable()
        {
            VARCOUNTER += 1;
            return String.Format("?#{0}", VARCOUNTER);
        }

        public static bool foreign_var(Term t) // doesn't start with ?#
        {
            if (t.Type != Term.TermType.VARIABLE || t.Text.Length < 3 || t.Text.Substring(0,2) != "?#")
            {
                return true; // looks like a foreign variable
            }
            return false;
        }

        public static List<Literal> standardize(List<Literal> inlist)
        {
            var subs = new Dictionary<Term,Term>();
            foreach (Literal literal in inlist)
            {
                foreach (Term term in literal.Terms) 
                {
                    //Console.WriteLine(term.Text + " " + foreign_var(term));
                    if (term.Type == Term.TermType.VARIABLE && foreign_var(term) && !subs.ContainsKey(term))
                    {
                        subs[term] = new Term(new_variable());
                    }

                }
            }
            var res = new List<Literal>();
            foreach (Literal c in inlist)
            {
                res.Add(c.Subst(subs));
            }
            return res;
        }

        public static List<Literal> skolemize(List<Literal> inlist)
        {
            return skolemize_with_prefix(inlist, "$");
        }

        public static List<Literal> skolemize_with_prefix(List<Literal> inlist, String prefix)
        {
            var counter = 0;
            var subs = new Dictionary<Term,Term>();
            foreach (Literal literal in inlist)
            {
                foreach (Term term in literal.Terms)
                {
                    if (term.Type == Term.TermType.VARIABLE && !subs.ContainsKey(term)) 
                    {
                        counter += 1;
                        var new_constant = new Term();
                        new_constant.Type = Term.TermType.CONSTANT;
                        new_constant.Text = prefix + counter.ToString();
                        subs[term] = new_constant;
                    }
                }
            }
            var res = new List<Literal>();
            foreach (Literal c in inlist) 
            {
                res.Add(c.Subst(subs));
            }
            return res;
        }


        public static void Test() // change to Test() later
        {

            Console.WriteLine("\nSexp.cs tests:");
            
            Console.WriteLine("New variable 1: " + new_variable());
            Console.WriteLine("New variable 2: " + new_variable());
            Console.WriteLine("New variable 3: " + new_variable());

            String[] teststrs = {
                "(one two three)",
                "(two three four)",
                "(one two)",
                "(one ?a ?b)",
                "(one ?c ?d)",
                "(one ?e three)",
                "(etc0_yup 0.99 e x y z)",
                "(etc0_yup 0.99 EVENTUALITY1 A B C)"
            };

            for (int i = 0; i < teststrs.Length; i++)
            {
                for (int j = 0; j < teststrs.Length; j++)
                {
                    var res = unify(new Literal(teststrs[i]), new Literal(teststrs[j]));
                    if (res != null) {
                        Console.WriteLine(teststrs[i] + " unifies with " + teststrs[j]);
                    }
                    else
                    {
                        Console.WriteLine(teststrs[i] + " does not unify with " + teststrs[j]);
                    }
                }
            }
        }
    }
}