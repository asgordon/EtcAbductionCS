// textGenerator.cs - 
// Andrew S. Gordon
// September 2019
// November 2020

// A template-based text generation utility for EtcAbduction.

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
        public Dictionary<String,List<String>> CommonNouns {get; set;}
        public Dictionary<String,String> Pronouns {get; set;}

        // Base class constructor is tied to specific knowledge base and temlate axioms
    
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
        
        // Get the structure of the given solution
        List<Entailment> GetEntailments(List<Literal> solution)
        {
            return Forward.Entailments(Kb, solution);
        }

        // Select which literals in the graph to use for text generation
        List<Literal> GetShallowCauses(List<Entailment> entailments, List<Literal> solution, List<Literal> observations)
        {
            var result = new List<Literal>();
            foreach (Literal ob in observations)
            {
                var entailment = entailments.First<EtcAbduction.Entailment>(ent => ob.Equals(ent.Entailed));
                var etc = entailment.Triggers.First<EtcAbduction.Literal>(lit => solution.Contains(lit));
                result.Add(etc);
            }
            return result;
        }

        // Apply the text knowledgebase to the selected literals, with the resulting list containing lists of variants for each input literal
        List<List<Literal>> GetTextLiterals(List<Literal> content)
        {
            var result = new List<List<Literal>>(); // A list of variations for each content literal
            foreach (Literal contentLiteral in content)
            {
                var variations = new List<Literal>();
                var textEntailments = Forward.Entailments(Tkb, new List<Literal>() {contentLiteral});
                foreach (Entailment textEntailment in textEntailments)
                {
                    Literal textLiteral = textEntailment.Entailed;
                    if (textLiteral.Predicate == "text") // what about textc?
                    {
                        variations.Add(textLiteral);
                    }
                }
                result.Add(variations);
            }
            return result;
        }
        
        // Convert each text literal to a list of strings (a "realization")
        List<List<List<string>>> ConvertToRealizations(List<List<Literal>> textLiterals)
        {
            var result = new List<List<List<string>>>();
            foreach (List<Literal> variations in textLiterals)
            {
                var options = new List<List<string>>();
                foreach (Literal variation in variations)
                {
                    options.Add(variation.Terms.Select(x => x.Repr()).ToList());
                }
                result.Add(options);
            }
            return result;
        }

        // Rewrite each list of strings (realization) by replacing pronouns, common nouns, and proper nouns
        List<List<List<string>>> RewriteRealizations(List<List<List<string>>> listOfOptionsOfRealizations)
        {
            var result = new List<List<List<string>>>();
            foreach (List<List<string>> options in listOfOptionsOfRealizations)
            {
                 var newOptions = new List<List<string>>();
                 foreach (List<string> realization in options)
                 {
                     var newRealization = SwapPronouns(realization);
                     newRealization = SwapProperNouns(newRealization);
                     newRealization = SwapCommonNouns(newRealization);
                     newOptions.Add(newRealization);
                 }
                 result.Add(newOptions);
            }
            return result;
        }

        // Swap prounouns base don the prunoun class
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

        // Swap constants for proper nouns based on any names provided
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

        // Swap constants for common nouns based on any names provided
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


        // Convert each realization to a single string / sentence.
        List<List<string>> RewriteAsSentences(List<List<List<string>>> listOfOptionsOfRealizations)
        {
            var result = new List<List<string>>();
            foreach (List<List<string>> options in listOfOptionsOfRealizations)
            {
                var newOptions = new List<string>();
                foreach (List<string> parts in options)
                {
                    newOptions.Add(Cleanup(String.Join(" ",parts)));
                }
                result.Add(newOptions);
            }
            return result;
        }

        // Cleanup function applied to each string to ensure it looks like a pleasant sentence.
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

        // Compose sentences into a documents, with a different variant for every available option
        List<string> ComposeVariations(List<List<string>> listOfOptionsOfSentences)
        {
            var variations = new List<string>(){ "" }; // one empty realization to start
            foreach (List<string> options in listOfOptionsOfSentences)
            {
                foreach (string option in options)
                {
                    var newVariations = new List<string>();
                    foreach (string previous in variations)
                    {
                        newVariations.Add(previous + option);
                    }
                    variations = newVariations;
                }
            }
            return variations;
        }

        // Bundle the core generation functions into a single call
        List<string> GenerateVariations(List<Literal> content)
        {
            var textLiterals = GetTextLiterals(content); // requires textkb
            var realizations = ConvertToRealizations(textLiterals);
            var rewrites = RewriteRealizations(realizations); // requires nouns
            var sentences = RewriteAsSentences(rewrites);
            var variations = ComposeVariations(sentences);
            return variations;
        }

        // Provide a convienient generate function that provides a couple of options.
        public string Generate(List<Literal> solution, List<Literal> observations, string selector = "shallow_causes", string ranker = "longest")
        {
            // Select content
            List<Literal> content;
            switch (selector)
            {
                case "shallow_causes":
                default:
                    content = GetShallowCauses(GetEntailments(solution), solution, observations);
                    break;
            }

            // Generate variations
            List<String> variations = GenerateVariations(content);

            // Rank variations
            switch (ranker)
            {
                case "all":
                    variations = new List<String> {  String.Join("\n",variations) };
                    break;
                case "shortest":
                    variations.Sort((a, b) => a.Length.CompareTo(b.Length));
                    break;
                case "longest":
                default:
                    variations.Sort((a, b) => b.Length.CompareTo(a.Length)); // reversed
                    break;
            }

            // Return highest-ranked variation
            return variations[0];
        }

    }
}
