// textGenerator.cs - 
// Andrew S. Gordon
// August 2022

// A template-based text generation utility for EtcAbduction.

using System;
using System.Collections.Generic;
using System.Linq;

namespace EtcAbduction
{
    using StringList = List<string>; // helpful

    public class TextGenerator 
    {
        public Knowledgebase Kb {get; set;}
        public Knowledgebase Tkb {get; set;}
        public Dictionary<String,List<String>> ProperNouns {get; set;}
        public Dictionary<String,List<String>> CommonNouns {get; set;}
        public Dictionary<String,String> Pronouns {get; set;}

        // Ablation studies
        static bool AblatePronounIntroduction = false;
        static bool AblateDeterminerSelection = false;

        static List<String> SkolemConstantCommonNouns = new List<String>() {"unknown entity"};
        
        // Pronoun directives appear before variables in text literals
        static HashSet<string> PronounDirectives = new HashSet<string>{
            "Subject", // HE went
            "Object", // to HER
            "DependentPossessive", // at THEIR house  
            "IndependentPossessive", // which was really HERS
            "Reflexive" // that she owned by HERSELF.
            };
        
        // Pronoun classes define which pronouns an entity should use
        // static HashSet<string> PronounClasses = new HashSet<string>{
        //     "Masculine", // he, him, his, his, himself
        //     "Feminine", // she, her, her, hers, herself
        //     "Neuter", // it, it, its, its, itself
        //     "Plural" // they, them, their, theirs, themselves
        //     }; 
        
        // Text predicates are used in Text Knowledge Bases to direct text generation
        // static HashSet<string> TextPredicates = new HashSet<string>{
        //     "proper_noun", // e.g. (proper_noun PERSON1 "Samantha")
        //     "pronouns", // e.g. (pronouns PERSON1 Feminine)
        //     "common_noun", // e.g. (common_noun PERSON1 "accountant")
        //     "text", // e.g. (text Subject ?x "paid the fare to" Object ?y)
        //     "texta", // e.g. (texta Subject ?x "asked" Object ?y "to pay the fare")
        //     "textc", // e.g. (textc Subject ?x "paid the fare")
        // };

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
                }
                if (tob.Predicate == "pronouns")
                {
                    String key = tob.Terms[0].Text;
                    String value = tob.Terms[1].Text;
                    this.Pronouns.Add(key, value);
                }
                if (tob.Predicate == "common_noun")
                {
                    String key = tob.Terms[0].Text;
                    List<String> value = tob.Terms.Skip(1).Select(t => t.Text).ToList(); // terms, not strings.
                    this.CommonNouns.Add(key, value);
                }
            }
        } 

        // Get the structure of the given solution
        List<Entailment> Entailments(List<Literal> solution)
        {
            return Forward.Entailments(Kb, solution);
        }

        // Select which literals in the graph to use for text generation

        // Method "Shallow Causes", selecting the immediate justification of each observation
        List<Literal> ShallowCauses(List<Entailment> entailments, List<Literal> solution, List<Literal> observations)
        {
            var result = new List<Literal>();
            foreach (Literal ob in observations)
            {
                if (ob.IsEtceteraLiteral) // useful.
                {
                    result.Add(ob);
                }
                else
                {
                    result.Add(EntailmentWithConsequent(entailments, ob).Triggers.First<EtcAbduction.Literal>(lit => lit.IsEtceteraLiteral)); 
                }
            }
            return result;
        }

        // Method "Latest News", selecting the bottom-up justification for the most recent observation
        List<Literal> LatestNews(List<Entailment> entailments, List<Literal> solution, List<Literal> observations)
        {
            var latest = new List<Literal>();
            latest.Add(observations.Last());  // only the last observable to be considered.
            return (UpwardStripes(entailments, solution, latest));
        }

        // Method "Upward Stripes", selecting the bottom-up justification for each observation in order
        List<Literal> UpwardStripes(List<Entailment> entailments, List<Literal> solution, List<Literal> observations)
        {
            var result = new List<Literal>();
            var consequents = new List<Literal>();
            foreach (Literal obs in observations)
            {
                consequents.Add(obs);
                while (consequents.Count > 0)
                {
                    var element = consequents[0];
                    consequents.RemoveAt(0); // pop

                    if (element.IsEtceteraLiteral) // got one
                    {
                        if (!Contains(result, element))
                        {
                            result.Add(element);
                        }
                    }
                    else
                    {
                        var entailment = EntailmentWithConsequent(entailments, element);
                        foreach (Literal trigger in entailment.Triggers)
                        {
                            consequents.Add(trigger);
                        }
                    }
                }
            }
            return result;
        }

        Entailment EntailmentWithConsequent(List<Entailment> entailments, Literal consequent)
        {
            foreach (Entailment entailment in entailments)
            {
                if (entailment.Entailed.Equals(consequent)) // Repr() == consequent.Repr())
                {
                    return entailment;
                }
            }
            return null;
        }

        bool Contains(List<Literal> literals, Literal literal)
        {
            foreach (Literal item in literals)
            {
                if (item.Equals(literal))
                {
                    return true;
                }
            }
            return false;
        }

        // Apply the text knowlegebase to the selected content, using only the first matching rule
        List<Literal> TextLiterals(List<Literal> content)
        {
            var result = new List<Literal>();
            foreach (Literal contentLiteral in content)
            {
                var textEntailments = Forward.Entailments(Tkb, new List<Literal>() {contentLiteral});
                foreach (Entailment textEntailment in textEntailments)
                {
                    if (textEntailment.Entailed.Predicate == "text") // what about textc?
                    {
                        result.Add(textEntailment.Entailed);
                        break; // found one, so break out of the inner foreach loop
                    }
                }
            }
            return result;
        }

        // Convert each text literal into a realization, i.e. a list of strings
        List<StringList> Realizations(List<Literal> textLiterals)
        {
            var result = new List<StringList>();
            foreach (Literal textLiteral in textLiterals)
            {
                result.Add(textLiteral.Terms.Select(x => x.Repr()).ToList());
            }
            return result;
        }

        // Rewrite realizations to introduce pronouns, proper nouns, and common nouns
        List<StringList> Rewrite(List<StringList> realizations)
        {
            var withPronouns = SwapPronouns(realizations);
            var withProperNouns = new List<StringList>();
            foreach(StringList realization in withPronouns)
            {
                withProperNouns.Add(SwapProperNouns(realization));
            }
            var result = SwapCommonNouns(withProperNouns);
            return result;
        }

        // Swap pronouns based on reader knowledge of pronoun class
        List<StringList> SwapPronouns(List<StringList> realizations)
        {
            var known = new HashSet<string>(); // reader knows pronoun classes of these entities
            HashSet<string> previous; // who participated in the previous sentence
            var current = new HashSet<string>(); // who participated in the current sentence thus far
            var result = new List<StringList>();

            for (int r = 0; r < realizations.Count; r++) 
            {
                previous = current; 
                current = new HashSet<string>(); 
                StringList realization = realizations[r];
                for (int i = 0; i < realization.Count; i++) 
                {
                    if (PronounDirectives.Contains(realization[i]))
                    {
                        String directive = realization[i];
                        realization[i] = ""; // hide directive
                        i++; // increment i, will skip over the next argument in the for loop
                        string entity = realization[i];
                        string pronounClass = PronounClass(entity); 
                        if (CanSwapPronoun(entity, directive, previous, current, known))
                        {
                            known.Add(entity); 
                            realization[i] = Pronoun(directive, pronounClass); // swap
                        }
                        current.Add(entity); 
                    }
                }
                result.Add(realization);
            }
            return result;
        }

        // Get the pronoun class of an entity, if it has ben provided
        string PronounClass(string entity)
        {
            if (this.Pronouns.ContainsKey(entity)) return this.Pronouns[entity];
            return "Neuter"; // default
        }

        // Determine if a pronoun can be used given the current context
        bool CanSwapPronoun(string entity, string directive, HashSet<string> previous, HashSet<string> current, HashSet<string> known)
        {
            if (AblatePronounIntroduction) return false; 
            // No, if entity not already seen in current or previous sentence
            HashSet<string> union = new HashSet<string>();
            union.UnionWith(previous);
            union.UnionWith(current);
            if ((directive != "Subject") && (directive != "Object")) return true; // why not
            if (!union.Contains(entity)) return false; // not recently seen
            // No, if entity shares the same pronoun class as a recently seen entity
            foreach (string member in union)
            {
                if ((member != entity) && (PronounClass(member) == PronounClass(entity))) return false; // would be ambiguous
            }
            // Yes, if entity's class is known already, assuming all plurals and neuters are known. 
            foreach (string member in union)
            {
                if ((PronounClass(entity) == "Plural") || (PronounClass(entity) == "Neuter")) known.Add(entity);
            }
            if (known.Contains(entity)) return true; // unambiguous
            // No, if there is more than 1 entity with an unknown class
            HashSet<string> unknowns = new HashSet<string>();
            unknowns.UnionWith(union); // start with the union
            unknowns.ExceptWith(known); // remove all the known
            if (unknowns.Count > 1) return false; // Cannot introduce because more than 1 unknown 
            // Yes, all good!
            return true; // All good!
        }

        // Identify the pronoun to use for a given directive and pronoun class
        string Pronoun(string directive, string pronounClass)
        {
            if (directive == "Subject")
            {
                switch(pronounClass)
                {
                    case "Masculine": return "he";
                    case "Feminine": return "she"; 
                    case "Neuter": return "it";
                    case "Plural": return "they";
                }
            }
            if (directive == "Object")
            {
                switch(pronounClass)
                {
                    case "Masculine": return "him";
                    case "Feminine": return "her";
                    case "Neuter": return "it";
                    case "Plural": return "them";
                }
            }
            if (directive == "DependentPossessive")
            {
                switch(pronounClass)
                {
                    case "Masculine": return "his";
                    case "Feminine": return "her"; 
                    case "Neuter": return "its";
                    case "Plural": return "their";
                }
            }
            if (directive == "IndependentPossessive")
            {
                switch(pronounClass)
                {
                    case "Masculine": return "his"; 
                    case "Feminine": return "hers";
                    case "Neuter": return "its"; // wrong?
                    case "Plural": return "theirs";
                }
            }
            if (directive == "Reflexive")
            {
                switch(pronounClass)
                {
                    case "Masculine": return "himself"; 
                    case "Feminine": return "herself"; 
                    case "Neuter": return "itself";
                    case "Plural": return "themselves"; // or themself
                }
            }
            return ("???");
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

        // Simple version for swapping common nouns and adding a determiner. A better version would add "another" when introducing a new entity that shares a previously used common noun.
        List<StringList> SwapCommonNouns(List<StringList> realizations)
        {
            HashSet<string> introduced = new HashSet<string>(); // Who has been introduced  
            HashSet<string> introducedNouns = new HashSet<string>(); // what are their nouns
            List<StringList> result = new List<StringList>();
            foreach (StringList realization in realizations)
            {
                for (int i = 0; i < realization.Count; i++)
                {
                    string entity = realization[i];
                    // Handle Skolem constants, e.g. $3:2 or $4
                    if (IsSkolemConstant(entity))
                    {
                        CommonNouns[entity] = SkolemConstantCommonNouns;
                    }
                    // 
                    if (this.CommonNouns.ContainsKey(entity))
                    {
                        if (introduced.Contains(entity))
                        {
                            realization[i] = AddDefiniteArticle(this.CommonNouns[entity][0]);
                        }
                        else 
                        {
                            if (introducedNouns.Contains(this.CommonNouns[entity][0]))
                            {
                                realization[i] = AddAdditiveDeterminer(this.CommonNouns[entity][0]);
                            }
                            else
                            {
                                realization[i] = AddIndefiniteArticle(this.CommonNouns[entity][0]);
                            }
                            introduced.Add(entity);
                            introducedNouns.Add(this.CommonNouns[entity][0]);
                        }
                    }
                    
                }
                result.Add(realization);
            }
            return result;
        }

        bool IsSkolemConstant(string entity)
        {
            return (entity.Length != 0) && entity[0] == '$';
        }

        // Definite articles, e.g., the triangle, the arrow, the American soldier
        String AddDefiniteArticle(string commonNoun)
        {
            if (AblateDeterminerSelection) return AddIndefiniteArticle(commonNoun);
            return "the " + commonNoun;
        }

        // Indefinite articles, e.g., a triangle, an arrow, an American soldier
        String AddIndefiniteArticle(string commonNoun)
        {
            List<char> vowelish = new List<char>() {'a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U'};
            if (vowelish.Contains(commonNoun[0]))
            {
                return "an " + commonNoun; 
            }
            else
            {
                return "a " + commonNoun;
            }
        }

        // Additive determiner (another)
        String AddAdditiveDeterminer(string commonNoun)
        {
            if (AblateDeterminerSelection) return AddIndefiniteArticle(commonNoun);
            return "another " + commonNoun;
        }


        // Convert (rewritten) realizations into strings
        List<string> Sentences(List<StringList> rewrites)
        {
            var result = new List<string>();
            foreach (StringList parts in rewrites)
            {
                result.Add(Cleanup(String.Join(" ", parts)));
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
            while (messy.Contains(" 's "))
            {
                messy = messy.Replace(" 's ", "'s ");
            }
            return messy.First().ToString().ToUpper() + messy.Substring(1) + ". "; // final space
        }

        // Compose a list of sentences into a paragraph of text
        string Compose(List<string> sentences)
        {
            string result = "";
            foreach (string sentence in sentences)
            {
                result += sentence;
            }
            return result;
        }

        // Version 3 of the text generation algorithm (2022), accepts ordered content as input
        string GenerateV3(List<Literal> content)
        {
            List<Literal> textLiterals = TextLiterals(content);
            List<StringList> realizations = Realizations(textLiterals);
            List<StringList> rewrites = Rewrite(realizations);
            List<string> sentences = Sentences(rewrites);
            return Compose(sentences);
        }

        // Public function for converting content into text
        public string ConvertToText(List<Literal> content)
        {
            return GenerateV3(content);
        }

        // Public function for generating text
        public string Generate(List<Literal> solution, List<Literal> observations, string selector = "shallow_causes", string ranker = "longest")
        {
            List<Literal> content = null;
            if (selector == "latest_news")
            {
                content = LatestNews(Entailments(solution), solution, observations);
            }
            else if (selector == "upward_stripes")
            {
                content = UpwardStripes(Entailments(solution), solution, observations);
            }
            else // "shallow_causes"
            {
                content = ShallowCauses(Entailments(solution), solution, observations);
            }
            return ConvertToText(content);
        }   
    }
}

// Future versions: determiners, clause chains
// Correctly handle 's (singular and plural possessive nouns)
// Correctly recognize when entity is in previous or current sentence when not using pronouns.
// Repeated events
// Variations on names