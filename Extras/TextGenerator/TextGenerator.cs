// textGenerator.cs - 
// Andrew S. Gordon
// September 2019

// Core Functionality:
// - Pronouns, Common nouns, Proper Nouns

// Currently realizers:
// ShallowCauses: Just realize the immediate factors that explain each observable.

// Todo:
// 1. Proper and common name variations after first mention
// 2. Correct determiner for common nouns
// 3. Roles in place of common nouns


using System;
using System.Collections.Generic;
using System.Linq;

namespace EtcAbduction
{
    public class TextGenerator 
    {

        public Knowledgebase Kb {get; set;}
        public Knowledgebase Tkb {get; set;}
        public Dictionary<String,List<String>> ProperNouns {get; set;}
        public Dictionary<String,int> ProperNameMentions {get; set;}
        public Dictionary<String,List<String>> CommonNouns {get; set;}
        public Dictionary<String,int> CommonNameMentions {get; set;}
        public Dictionary<String,String> Pronouns {get; set;}

        public TextGenerator(Knowledgebase kb, Knowledgebase tkb, List<Literal> tobs) {
            this.Kb = kb;
            this.Tkb = tkb;
            this.ProperNouns = new Dictionary<String,List<String>>();
            this.Pronouns = new Dictionary<String,String>();
            this.CommonNouns = new Dictionary<String,List<String>>();

            foreach (Literal tob in tobs)
            {
                if (tob.Predicate == "proper_noun")
                {
                    String key = tob.Terms[0].Text;
                    List<String> value = tob.Terms.Skip(1).Select(t => t.Text).ToList(); // terms, not strings.
                    this.ProperNouns.Add(key, value);
                    //Console.WriteLine($"{key} {value.Count}");
                }
                if (tob.Predicate == "pronouns")
                {
                    String key = tob.Terms[0].Text;
                    String value = tob.Terms[1].Text;
                    this.Pronouns.Add(key, value);
                    Console.WriteLine($"{key} {value}");
                }
                if (tob.Predicate == "common_noun")
                {
                    String key = tob.Terms[0].Text;
                    List<String> value = tob.Terms.Skip(1).Select(t => t.Text).ToList(); // terms, not strings.
                    this.CommonNouns.Add(key, value);
                    Console.WriteLine($"{key} {value}");
                }
            }

        }

        public string Shortest(List<Literal> solution, List<Literal> obs)
        {
            List<String> allTexts = AllTexts(solution, obs);
            allTexts.Sort((a, b) => a.Length.CompareTo(b.Length));
            return allTexts[0];
        }

        public string Longest(List<Literal> solution, List<Literal> obs)
        {
            List<String> allTexts = AllTexts(solution, obs);
            allTexts.Sort((a, b) => b.Length.CompareTo(a.Length)); // reversed
            return allTexts[0];
        }

        public List<String> AllTexts(List<Literal> solution, List<Literal> obs)
        {
            var entailments = Forward.Entailments(Kb, solution);

            List<String> res = new List<String>();

            res.AddRange(ShallowCauses(entailments, solution, obs));

            return res;
        }

        public List<String> ShallowCauses(List<Entailment> entailments, List<Literal> solution, List<Literal> obs)
        {
            List<String> res = new List<String>(){""}; // start with one empty string.

            // 1 iterate through each observation, in order
            foreach (Literal ob in obs)
            {
            // 2 locate the etc literal that is part of its direct antecedent
                var entailment = entailments.First<EtcAbduction.Entailment>(ent => ob.Equals(ent.Entailed));
                var etc = entailment.Triggers.First<EtcAbduction.Literal>(lit => solution.Contains(lit));

            // 3 forward chain on only this one etc literal to find possible realizations
                var textEntailments = Forward.Entailments(Tkb, new List<Literal>() {etc});
                //List<Literal> textLiterals = new List<Literal>();
                List<List<String>> realizations = new List<List<String>>();
                foreach (Entailment textEntailment in textEntailments)
                {
                    Literal textLiteral = textEntailment.Entailed;
                    if (textLiteral.Predicate == "text") // only use full causal realizations (not textc)
                    {
                        realizations.Add(textLiteral.Terms.Select(x => x.Repr()).ToList());
                    }                   
                }
            // 4 handle any special directives in the realizations, e.g. pronouns, names
                for (int i = 0; i < realizations.Count; i++)
                {
                    // first do pronouns
                    realizations[i] = SwapPronouns(realizations[i]);
                    // then proper nouns
                    realizations[i] = SwapProperNouns(realizations[i]); 
                    // then common nouns
                    realizations[i] = SwapCommonNouns(realizations[i]);
                }

            // 5 replace the res list with expanded variations, for each possible realization.
                List<String> newresult = new List<String>();
                foreach (List<String> item in realizations)
                {
                    String realization = Cleanup(String.Join(" ",item));
                    foreach (String previous in res)
                    {
                        newresult.Add(previous + realization);
                    }
                }
                if (realizations.Count > 0) 
                {
                    res = newresult;
                }
            }
            

            return res;
        }

        public List<String> SwapPronouns(List<String> realization)
        {
            for (int i = 0; i < realization.Count; i++)
            {
                if (realization[i] == "SubjectPronoun" ||
                    realization[i] == "ObjectPronoun" ||
                    realization[i] == "DependentPossessivePronoun" ||
                    realization[i] == "IndependentPossessivePronoun" ||
                    realization[i] == "ReflexivePronoun")                    
                {
                    String directive = realization[i];
                    realization[i] = ""; // hide
                    i++; // increment i, will skip the next argument
                    String pronounClass = "Neuter"; // default
                    if (this.Pronouns.ContainsKey(realization[i]))
                    {
                        pronounClass = this.Pronouns[realization[i]];
                    }
                    if (directive == "SubjectPronoun")
                    {
                        switch(pronounClass)
                        {
                            case "Masculine": realization[i] = "he"; break;
                            case "Feminine": realization[i] = "she"; break;
                            case "Neuter": realization[i] = "it"; break;
                            case "Plural": realization[i] = "they"; break;
                        }
                    }
                    if (directive == "ObjectPronoun")
                    {
                        switch(pronounClass)
                        {
                            case "Masculine": realization[i] = "him"; break;
                            case "Feminine": realization[i] = "her"; break;
                            case "Neuter": realization[i] = "it"; break;
                            case "Plural": realization[i] = "them"; break;
                        }
                    }
                    if (directive == "DependentPossessivePronoun")
                    {
                        switch(pronounClass)
                        {
                            case "Masculine": realization[i] = "his"; break;
                            case "Feminine": realization[i] = "her"; break;
                            case "Neuter": realization[i] = "its"; break;
                            case "Plural": realization[i] = "their"; break;
                        }
                    }
                    if (directive == "IndependentPossessivePronoun")
                    {
                        switch(pronounClass)
                        {
                            case "Masculine": realization[i] = "his"; break;
                            case "Feminine": realization[i] = "hers"; break;
                            case "Neuter": realization[i] = "its"; break; // wrong
                            case "Plural": realization[i] = "theirs"; break;
                        }
                    }
                    if (directive == "ReflexivePronoun")
                    {
                        switch(pronounClass)
                        {
                            case "Masculine": realization[i] = "himself"; break;
                            case "Feminine": realization[i] = "herself"; break;
                            case "Neuter": realization[i] = "itself"; break; 
                            case "Plural": realization[i] = "themselves"; break; // or themself
                        }
                    }
                }
            }
            return realization;

        }

        public List<String> SwapProperNouns(List<String> realization)
        {
            for (int i = 0; i < realization.Count; i++)
            {
                if (this.ProperNouns.ContainsKey(realization[i]))
                {
                    realization[i] = this.ProperNouns[realization[i]][0];
                }
            }
            
            return realization;
        }

        public List<String> SwapCommonNouns(List<String> realization)
        {
            String determiner = "a";
            for (int i = 0; i < realization.Count; i++)
            {
                if (this.CommonNouns.ContainsKey(realization[i]))
                {
                    realization[i] = determiner + " " + this.CommonNouns[realization[i]][0];
                }
            }
            
            return realization;
        }

        public String Cleanup(String messy)
        {
            messy = messy.Trim();
            while (messy.Contains("  "))
            {
                messy = messy.Replace("  ", " ");
            }
            while (messy.Contains(" ,"))
            {
                messy = messy.Replace(" ,", ",");
            }
            return messy.First().ToString().ToUpper() + messy.Substring(1) + ". "; // final space
        }
    }
}
