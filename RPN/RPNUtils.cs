using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace com.sgcombo.RpnLib
{
    internal class RPNUtils
    {


        public static bool IsWhiteSpace(char chr)
        {
            return char.IsWhiteSpace(chr) || chr == '\r' || chr == '\n' || chr == '\t';
        }

        public static bool isOperation(char c)
        {
            for (int i = 0; i < OperationConvertor.operators.Length; i++)
                if (c == OperationConvertor.operators[i])
                    return true;

            return false;
        }

        public static bool isDoubleOperation(string c)
        {
            for (int i = 0; i < OperationConvertor.doubleOperators.Length; i++)
                if (c == OperationConvertor.doubleOperators[i])
                    return true;

            return false;
        }

        public static bool TryToDouble(string input, out double d)
        {
            bool isSpace = false;
            bool isComma = false;
            int countPoint = 0;
            int l = input.Length;
            char ch;
            for (int j = 0; j < l; j++)
            {
                ch = input[j];
                if (ch == ' ') isSpace = true;
                else
                if (ch == ',')
                {
                    isComma = true;
                    countPoint++;
                }
                else
                if (ch == '.')
                {
                    countPoint++;
                }
            }

            // Unify string (no spaces, only .)
            string output = input;
            if (isComma)
            {
                output = output.Replace(',', '.');
            }

            if (isSpace)
            {
                output = output.Replace(" ", String.Empty);
            }


            if (countPoint > 1)
            {
                // Split it on points
                string[] split = output.Split('.');

                // Take all parts except last
                output = string.Join("", split.Take(split.Length - 1).ToArray());

                // Combine token parts with last part
                output = string.Format("{0}.{1}", output, split.Last());
            }

            // Parse double invariant
            bool ret = double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
            return ret;
        }

        public static bool GetTokens(string expr, List<RPNToken> Tokens)
        {


            int i = 0;
            string tok = string.Empty;
            RPNToken token;

            bool IsGood = true;
            Tokens.Clear();

            while (i <= expr.Length)
            {
                tok = string.Empty;
                token = new RPNToken();

                if (i > expr.Length - 1) { break; }

                if (RPNUtils.IsWhiteSpace(expr[i]))
                {
                    while (RPNUtils.IsWhiteSpace(expr[i]))
                    {
                        i++;
                        if (i > expr.Length - 1) { break; }
                    }
                }

                if (i > expr.Length - 1) { break; }

                if (expr[i] == '\'')
                {
                    do
                    {
                        tok += expr[i];
                        i++;
                        if (i > expr.Length - 1)
                        {
                            throw new Exception($"Invalid string [{tok}] , Expression [{expr}]");
                        }
                    } while (!(expr[i] == '\''));

                    if (i <= expr.Length - 1)
                    {
                        tok += expr[i];
                        i++;
                    }
                    else
                    {
                        throw new Exception($"Invalid string [{tok}] , Expression [{expr}]");
                    }

                    token.sType = RPNTokenType.STRING;
                    token.sToken = tok;
                    Tokens.Add(token);
                }

                if (i > expr.Length - 1) { break; }

                if (expr[i] == '[')
                {
                    while (!(expr[i] == ']'))
                    {
                        tok += expr[i];
                        i++;
                        if (i > expr.Length - 1) { break; }
                    }
                    if (i <= expr.Length - 1)
                    {
                        {
                            tok += expr[i];
                            i++;
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid string [{tok}] , Expression [{expr}]");
                    }
                    token.sType = RPNTokenType.ALPHA;
                    token.sToken = tok;
                    Tokens.Add(token);
                }

                if (i > expr.Length - 1) { break; }

                if (expr[i] == '"')
                {
                    while (!(expr[i] == '"'))
                    {
                        tok += expr[i];
                        i++;
                        if (i > expr.Length - 1)
                        {
                            throw new Exception($"Invalid string [{tok}] , Expression [{expr}]");
                        }
                    }
                    if (i <= expr.Length - 1)
                    {
                        {
                            tok += expr[i];
                            i++;
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid string [{tok}] , Expression [{expr}]");
                    }
                    token.sType = RPNTokenType.STRING;
                    token.sToken = tok;
                    Tokens.Add(token);
                }

                if (i > expr.Length - 1) { break; }

                if (char.IsLetter(expr[i]) || (expr[i] == ':'))
                {

                    do
                    {
                        tok += expr[i];
                        i++;

                        if (i > expr.Length - 1) { break; }

                    } while (char.IsLetter(expr[i]) || (char.IsDigit(expr[i]) || (expr[i] == '_')));
                    token.sType = RPNTokenType.ALPHA;
                    var t = tok.ToLower();
                    if (t.Equals("true") || t.Equals("false"))
                    {
                        token.sType = RPNTokenType.BOOL;
                        tok = t;
                    }
                    else
                    {
                        if (i < expr.Length)
                        {
                            if (expr[i] == '(')
                            {
                                tok += "$";

                                token.sType = RPNTokenType.FUNCTION;
                            }
                            if (expr[i] == '$')
                            {
                                tok += "$";
                                i++;

                                token.sType = RPNTokenType.FUNCTION;
                            }
                        }
                    }
                    token.sToken = tok;
                    Tokens.Add(token);
                }
                else if (char.IsDigit(expr[i]))
                {
                    while (char.IsDigit(expr[i]) || (expr[i] == '.'))
                    {
                        tok += expr[i];
                        i++;
                        if (i > expr.Length - 1) { break; }
                    }
                    token.sType = RPNTokenType.NUMBER;
                    token.sToken = tok;
                    Tokens.Add(token);
                }
                else
                if ((i + 1 < expr.Length) && ((isDoubleOperation(expr[i].ToString() + expr[i + 1]))))
                {
                    tok = expr[i].ToString();
                    tok = tok + expr[i + 1];
                    i++;
                    token.sType = RPNTokenType.OPERAND;
                    token.sToken = tok;
                    token.OperandType = RPNOperandType.ARIFMETICAL;
                    OperationConvertor.GetOperation.TryGetValue(tok, out token.Operation);
                    Tokens.Add(token);
                    i++;

                    if (i > expr.Length - 1) { break; }

                }
                else
                if (isOperation(expr[i]))
                {
                    tok = expr[i].ToString();
                    token.sType = RPNTokenType.OPERAND;
                    token.sToken = tok;

                    token.OperandType = RPNOperandType.ARIFMETICAL;
                    OperationConvertor.GetOperation.TryGetValue(tok, out token.Operation);

                    if (token.Operation == RPNOperandType.MINUS)
                    {
                        if (Tokens.Count == 0)
                        {
                            token.Operation = RPNOperandType.JUSTMINUS;
                            token.sToken = tok = "~";
                        }
                        else
                        if (Tokens.Count > 0 && ((Tokens[Tokens.Count - 1].sType == RPNTokenType.OPERAND)))
                        {
                            if (Tokens[Tokens.Count - 1].Operation != RPNOperandType.END_PARENTHESES)
                            {
                                token.Operation = RPNOperandType.JUSTMINUS;
                                token.sToken = tok = "~";
                            }
                        }
                    }
                    if (token.Operation == RPNOperandType.PLUS)
                    {
                        if (Tokens.Count > 0 && Tokens[Tokens.Count - 1].sType == RPNTokenType.OPERAND)
                        {
                            if (Tokens[Tokens.Count - 1].Operation != RPNOperandType.END_PARENTHESES)
                            {
                                token.Operation = RPNOperandType.JUSTPLUS;
                            }
                        }
                    }
                    Tokens.Add(token);
                    i++;
                    if (i > expr.Length - 1) { break; }
                }
                else
                {
                    IsGood = false;
                    Tokens.Clear();
                    if (i < expr.Length)
                    {
                        throw new Exception($"Invalid token type [{expr[i]}] Expression [{expr}]");
                    }
                    else
                    {
                        throw new Exception($"Invalid Expression [{expr}]");
                    }
                }

            }
            return IsGood;
        }


    }
}
