// sexp.cs 
// Andrew S. Gordon
// September 2019

using System;
using System.Collections.Generic;
using System.Linq;

namespace EtcAbduction 
{

    public class Sexp
    {
        public enum SexpType { LIST, SYMBOL, NUMBER, STRING };

        public SexpType Type { get; set; }
        public string Text { get; set; }
        public double Number { get; set; }
        public List<Sexp> Children { get; set; }

        public Sexp() {}

        public string Repr() // textual representation
        {
            if (this.Type == SexpType.SYMBOL)
            {
                return this.Text;
            }
            else if (this.Type == SexpType.NUMBER)
            {
                return this.Number.ToString();
            }
            else if (this.Type == SexpType.STRING)
            {
                return $"\"{this.Text}\"";
            }
            else // LIST 
            {                
                return $"({String.Join(" ", this.Children.Select(c => c.Repr()))})";           
            }
        }

        public static Sexp FromSrc(string src) 
        {
            Parser p = new Parser(src);
            return p.ParseFirst();
        }

    }

    public class Parser
    {
        string Input { get; set; }
        int Pos { get; set; }
        int Depth { get; set; }

        public Parser(string src) {
            this.Pos = 0;
            this.Depth = 0;
            this.Input = src + " "; // helpful in case of atomic s-expressions
        }

        public Sexp ParseFirst() 
        {
            this.ConsumeWhitespaceAndComments();
            if (this.EOF())
            {
                throw new Exception("Unexpected End-Of-File. No s-expression found.");
            }
            return this.ParseSexp();
        }

        private List<Sexp> ParseSexps()
        {
            List<Sexp> sexps = new List<Sexp>();
            while (true)
            {
                this.ConsumeWhitespaceAndComments();
                if (this.StartsWith(")")) 
                {
                    return sexps;
                }
                sexps.Add(this.ParseSexp());
            }
        }

        private void ConsumeWhitespaceAndComments() 
        {
            this.ConsumeWhitespace();
            if (this.StartsWith(";"))
            {
                this.ConsumeComment();
                this.ConsumeWhitespaceAndComments();
            }
        }

        private Sexp ParseSexp()
        {   
            var next_char = this.NextChar();
            if ("0123456789".Contains(next_char)) // number
            {
                return(this.ParseNumber());
            }
            else if (next_char == '\"') // string
            {
                return(this.ParseString());
            }
            else if (next_char == '(') // list
            {
                return(this.ParseList());
            }
            else // symbol
            {
                return(this.ParseSymbol());
            }
        }

        private Sexp ParseSymbol() 
        {
            var res = "";
            var cur_char = this.NextChar();
            while (" ;)\n\t".Contains(cur_char) == false)
            {
                res += this.ConsumeChar();
                cur_char = this.NextChar();
            }
            var res_sexp = new Sexp();
            res_sexp.Type = Sexp.SexpType.SYMBOL;
            res_sexp.Text = res;

            return res_sexp;
        }

        private Sexp ParseNumber() // funny! Can't handle negative numbers
        {
            var res = "";
            var cur_char = this.NextChar();
            while (" ;)\n\t".Contains(cur_char) == false)
            {
                res += this.ConsumeChar();
                cur_char = this.NextChar();
            }
            var res_number = Double.Parse(res);
            var res_sexp = new Sexp();
            res_sexp.Type = Sexp.SexpType.NUMBER;
            res_sexp.Number = res_number;
            return res_sexp;
        }

        private Sexp ParseString() 
        {
            var res = "";
            this.ConsumeChar(); // eat opening "
            var cur_char = this.NextChar();
            while (cur_char != '\"')
            {
                res += this.ConsumeChar();
                cur_char = this.NextChar();
            }
            this.ConsumeChar(); // eat ending "
            var res_sexp = new Sexp();
            res_sexp.Type = Sexp.SexpType.STRING;
            res_sexp.Text = res;
            return res_sexp;
        }

        
        private Sexp ParseList()
        {
            this.ConsumeChar(); // eat opening paren
            this.Depth += 1;
            var children = this.ParseSexps();
            this.ConsumeChar();; // eat closing paren
            this.Depth -= 1;
            var res_sexp = new Sexp();
            res_sexp.Type = Sexp.SexpType.LIST;
            res_sexp.Children = children;
            return res_sexp;
            
        }
               
        private void ConsumeComment() 
        {
            while (this.NextChar() != '\n') {
                ConsumeChar();
            }
        }

        private void ConsumeWhitespace()
        {
            while (System.Char.IsWhiteSpace(this.NextChar()))
            {
                ConsumeChar();
            }
        }

        private char ConsumeChar()
        {
            if (this.EOF())
            {
                throw new Exception("Unexpected End-Of-File.");
            }
            var cur_char = this.Input[this.Pos];
            this.Pos += 1;
            return cur_char;
        }

        private char NextChar()
        {            
            if (this.EOF())
            {
                throw new Exception("Unexpected End-Of-File.");
            }
            return this.Input[this.Pos];
        }

        private bool StartsWith(string s) 
        {
            return s.Equals(this.Input.Substring(this.Pos, s.Length));
        }

        private bool EOF()
        {
            return (this.Pos >= this.Input.Length);
        }

    }

}