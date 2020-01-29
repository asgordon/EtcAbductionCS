// knowledgebase.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.Collections.Generic;
using System.Linq;
using static EtcAbduction.Sexp;

namespace EtcAbduction 
{
    public class Knowledgebase 
    {
        public List<DefiniteClause> Axioms {get; set;}
        public Dictionary<String,List<DefiniteClause>> IndexByConsequent {get; set;}

        public Knowledgebase() {
            this.Axioms = new List<DefiniteClause>();
        }
        
        public List<Literal> Add(string src)
        {
            var sexps = Sexp.FromSrc("("+src+")").Children; // wrapped
            var observations = new List<Literal>();
            foreach (Sexp s in sexps)
            {
                if (this.IsDefiniteClause(s)) 
                {
                    this.Axioms.Add(new DefiniteClause(s));
                }
                else if (this.IsConjunction(s))
                {
                    for (int i = 1; i < s.Children.Count; i++)
                    {
                        observations.Add(new Literal(s.Children[i]));
                    }
                }
                else if (this.IsLiteral(s))
                {
                    observations.Add(new Literal(s));
                }
                else 
                {
                    throw new Exception($"Unrecognizable logic expression: {s.Repr()}");
                }
            }
            this.ComputeIndexByConsequent();
            return observations;
        }

        private bool IsDefiniteClause(Sexp s) 
        {
            return(s.Type == Sexp.SexpType.LIST &&
                s.Children.Count == 3 &&
                s.Children[0].Repr() == "if");
        }

        private bool IsLiteral(Sexp s)
        {
            return (s.Type == Sexp.SexpType.LIST &&
                s.Children.Count > 0 &&
                s.Children[0].Type == Sexp.SexpType.SYMBOL);
        }

        private bool IsConjunction(Sexp s)
        {
            return (s.Type == Sexp.SexpType.LIST &&
                s.Children.Count > 2 &&
                s.Children[0].Repr() == "and");
        }

        private void ComputeIndexByConsequent()
        {
            var res = new Dictionary<String,List<DefiniteClause>>();

            foreach (DefiniteClause dc in this.Axioms) 
            {
                var predicate = dc.Consequent.Predicate;
                if (res.ContainsKey(predicate))
                {
                    res[predicate].Add(dc);
                }
                else
                {
                    res[predicate] = new List<DefiniteClause>();
                    res[predicate].Add(dc);
                }
            }
            this.IndexByConsequent = res;
        }

    }

    public class DefiniteClause
    {
        public List<Literal> Antecedents {get; set;}
        public Literal Consequent {get; set;}

        public DefiniteClause(Sexp s)
        {
            var ants = new List<Literal>();
            if (IsConjunction(s.Children[1]))
            {
                for (int i = 1; i < s.Children[1].Children.Count; i++)
                {
                    ants.Add(new Literal(s.Children[1].Children[i]));
                }
            }
            else
            {
                ants.Add(new Literal(s.Children[1]));
            }
            this.Antecedents = ants;
            this.Consequent = new Literal(s.Children[2]);
        }

        public bool IsConjunction(Sexp s)
        {
            return (s.Type == Sexp.SexpType.LIST &&
                s.Children.Count > 2 &&
                s.Children[0].Repr() == "and");
        }

        public string Repr() 
        {
            if (this.Antecedents.Count == 1) {
                return "(if " + this.Antecedents[0].Repr() + " " + this.Consequent.Repr() + ")";
            }
            else 
            {
                return $"(if (and {String.Join(" ",this.Antecedents.Select(a => a.Repr()))}) {this.Consequent.Repr()})";
            }
        }
    }

    public class Literal : IEquatable<Literal>, IComparer<Literal>, IComparable<Literal>
    {
        public string Predicate {get; set;}
        public List<Term> Terms  {get; set;}
        public bool IsEtceteraLiteral {get; set;}
        public double LnProbability {get; set;}

        public Literal() {}

        public Literal(Sexp s) 
        {
            if (s.Children.Count > 1 &&
                s.Children[1].Type == SexpType.NUMBER)
            {
                this.IsEtceteraLiteral = true;
                this.LnProbability = Math.Log(s.Children[1].Number);
            }   
            else 
            {
                this.IsEtceteraLiteral = false;
            }
            this.Predicate = s.Children[0].Text;
            var res = new List<Term>();
            for (int i = 1; i < s.Children.Count; i++) 
            {
                res.Add(new Term(s.Children[i]));
            }
            this.Terms = res;
        }

        public Literal(string src)
        {
            var temp = new Literal(Sexp.FromSrc(src));
            this.Predicate = temp.Predicate;
            this.Terms = temp.Terms;
            this.IsEtceteraLiteral = temp.IsEtceteraLiteral;
            this.LnProbability = temp.LnProbability;
        }

        public Literal Subst(Dictionary<Term,Term> theta)
        {
            var newterms = new List<Term>();
            foreach (Term t in this.Terms)
            {
                var newt = t;
                while (theta.ContainsKey(newt))
                {
                    newt = theta[newt];
                }
                newterms.Add(newt);
            }
            var res = new Literal();
            res.Predicate = this.Predicate;
            res.Terms = newterms;
            res.IsEtceteraLiteral = this.IsEtceteraLiteral;
            res.LnProbability = this.LnProbability;
            return res;
        }

        public string Repr()
        {
            var reprs = new List<String>() {this.Predicate};
            reprs.InsertRange(1, this.Terms.Select(t => t.Repr()));
            return $"({String.Join(" ", reprs)})";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Literal objAsLiteral = obj as Literal;
            if (objAsLiteral == null) return false;
            else return Equals(objAsLiteral);
        }

        public override int GetHashCode()
        {
            return this.Repr().GetHashCode(); // Yuk!
        }

        public bool Equals(Literal other)
        {
            if (other == null) return false;
            return this.GetHashCode() == other.GetHashCode();
        }

        public int Compare(Literal a, Literal b)
        {           
            if (a.Predicate != b.Predicate)
                return String.Compare(a.Predicate, b.Predicate);
            if (a.Terms.Count != b.Terms.Count)
                return a.Terms.Count.CompareTo(b.Terms.Count);
            for (int i = 0; i < a.Terms.Count; i++)
            {
                if (a.Terms[i] != b.Terms[i])
                    return String.Compare(a.Terms[i].Text, b.Terms[i].Text);
            }
            return 0;
        }

        public int CompareTo(Literal that)
        {
            return Compare(this, that);
        }


        // public int Compare(Literal a, Literal b)
        // {
        //     if (a.Predicate == b.Predicate)
        //     {
        //         return a.Terms.CompareTo(b.Terms);
        //     }
        //     else 
        //     {
        //         return a.Predicate.CompareTo(that.Predicate);
        //     }
        // }
    }

    public class Term : IEquatable<Term>//, IComparer<Term>
    {
        public enum TermType {VARIABLE, CONSTANT};

        public TermType Type {get; set;}
        public string Text {get; set;}

        public Term() {}

        public Term(Sexp s)
        {
            if (s.Type == Sexp.SexpType.SYMBOL )
            {
                this.Text = s.Text;
                if (this.IsVariable(s.Text))
                {
                    this.Type = TermType.VARIABLE;
                }
                else 
                {
                    this.Type = TermType.CONSTANT;
                }
            }
            else if (s.Type == Sexp.SexpType.STRING)
            {    
                this.Text = s.Text;
                this.Type = TermType.CONSTANT;
            }
            else if (s.Type == Sexp.SexpType.NUMBER)
            {
                this.Text = s.Number.ToString();
                this.Type = TermType.CONSTANT;
            }
            else {
                throw new Exception($"Unrecognizable term type: {s.Repr()}");
            }
       }

       public Term(String src)
       {
           var temp = new Term(Sexp.FromSrc(src));
           this.Type = temp.Type;
           this.Text = temp.Text;
       }

       private bool IsVariable(string s) 
       {
           var c = s[0];
           return (Char.IsLower(c) || c == '?');
       }

       public string Repr() 
       {
           return this.Text;
       }

       public override bool Equals(object other)
       {
           return Equals(other as Term);
       }

       public bool Equals(Term other)
       {
           if ((object) other == null)
           {
               return false;
           }
           else 
           {
               return Type == other.Type && Text == other.Text;
           }
       }

       public override int GetHashCode()
       {
           var res = "";
           if (Type == TermType.VARIABLE) 
           {
               res = "VARIABLE_" + Text;
           }
           else
           {
               res = "CONSTANT_" + Text;
           }
           return res.GetHashCode();
       }

       public static bool operator ==(Term left, Term right)
       {
           return object.Equals(left, right);
       }

       public static bool operator !=(Term left, Term right)
       {
           return !(left == right);
       }

       public static bool operator <(Term left, Term right)
       {
           return left.Text.CompareTo(right.Text) < 0;
       }

       public static bool operator >(Term left, Term right)
       {
           return left.Text.CompareTo(right.Text) > 0;
       }

    //    public int CompareTo(Term that)
    //    {
    //        return this.Text.CompareTo(that.Text);
    //    }

    }


}
