using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace com.sgcombo.RpnLib
{

    class RPNExec
    {
        public Func<double, double, double> f;
        private List<RPNToken> Tokens = new List<RPNToken>();
        private Stack<object> al;
        private Dictionary<string, RPNArguments> vars = new Dictionary<string, RPNArguments>();
        static private RPNEnvironment Environment = new RPNEnvironment();
        public RPNExec(List<RPNToken> _Tokens)
        {
            Tokens = _Tokens;
           
        }

        static RPNExec()
        {
            RPNFunctions.RegisterFunctions(Environment);
        }

        

        public Func<double, double, double> Exec()
        {
            List<Func<double, double, double>> funcs = new List<Func<double, double, double>>();
            List<double> variables = new List<double>();
            double tempDouble;
            object ret = "";
            int i = 0;
            double a = 0;
            double b = 0;
            bool a1 = true;
            bool b1 = true;
            string tok = string.Empty;
            //Go though the tokens
            al = new Stack<object>();
            int x = 0;
            while (i < Tokens.Count)
            {
                tok = Tokens[i].sToken;

                switch (Tokens[i].sType)
                {
                    case RPNTokenType.NONE:
                        break;
                    case RPNTokenType.BOOL:
                        if (tok.ToLower().Equals("true"))
                        {
                            al.Push(true);
                            break;
                        }
                        else
                        {
                            al.Push(false);
                            break;
                        }
                        break;
                    case RPNTokenType.ALPHA:
                        if (vars.TryGetValue(tok, out RPNArguments arg))
                        {
                            a = arg.value;
                            variables.Add(a);
                            al.Push(a);
                        }
                        else
                        {
                            throw new Exception($"Value [{tok}] not exists");
                        }
                        break;
                    case RPNTokenType.NUMBER:
                        tempDouble = 0;
                        RPNUtils.TryToDouble(tok, out tempDouble);
                        al.Push(tempDouble);
                        break;
                    case RPNTokenType.STRING:
                        al.Push(tok.Substring(1, tok.Length - 2));
                        break;
                    case RPNTokenType.FUNCTION:

                        if (Environment != null)
                        {
                            String funcName = Tokens[i].sToken;
                            funcName = funcName.Substring(0, funcName.Length - 1);


                            var func = Environment.FindFunction(funcName);
                            if (func != null)
                            {
                                FunctionAttribute funcAttrib = (FunctionAttribute)Attribute.GetCustomAttribute(func.GetType(), typeof(FunctionAttribute));
                                List<object> arguments = new List<object>();



                                if (funcAttrib.ParamTypes != null && funcAttrib.ParamTypes.Length > 0)
                                {
                                    foreach (var paramType in funcAttrib.ParamTypes)
                                    {
                                        object obj = al.Pop();
                                        func.Params.Add(obj);
                                    }

                                }

                                object retObject = func.Calc();

                                al.Push(retObject);
                            }

                        }
                        break;
                    case RPNTokenType.OPERAND:
                        object r = 0;

                        switch (Tokens[i].Operation)
                        {
                            case RPNOperandType.JUSTPLUS:
                                a = Convert.ToDouble(al.Pop());
                                r = +a;
                                break;
                            case RPNOperandType.PLUS:
                                a = Convert.ToDouble(al.Pop());
                                var b11 = al.Pop();
                                if (b11 is DateTime)
                                {
                                    DateTime dDateTime = (DateTime)b11;
                                    r = dDateTime.AddHours(a);
                                }
                                else
                                {
                                    b = Convert.ToDouble(b11);
                                    funcs.Add((m, n) => n + m);
                                    r = a + b;
                                }
                                break;
                            case RPNOperandType.JUSTMINUS:
                                a = Convert.ToDouble(al.Pop());
                                r = -a;
                                break;
                            case RPNOperandType.MINUS:
                                var t = al.Pop();
                                a = Convert.ToDouble(t);
                                var b12 = al.Pop();
                                if ( b12 is DateTime)
                                {
                                    DateTime dDateTime = (DateTime)b12;
                                    r = dDateTime.AddHours(-a);
                                }
                                else
                                {
                                    b = Convert.ToDouble(b12);
                                    funcs.Add((m, n) => n - m);
                                    r =  b - a;
                                }
                                break;
                            case RPNOperandType.MULITIPLY:
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                funcs.Add((m, n) => n * m);
                                r = a * b;
                                break;
                            case RPNOperandType.DIVIDE:
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                funcs.Add((m, n) => n / m);
                                r = (b / a);
                                break;
                            case RPNOperandType.EXPONENTIATION:
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                funcs.Add((m, n) => Math.Pow(n, m));
                                r = Math.Pow(b, a);
                                break;

                            case RPNOperandType.DIV:  // "/="
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                funcs.Add((m, n) => n / m);
                                r = (b / a);
                                break;
                            case RPNOperandType.MOD:  //"%=",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                funcs.Add((m, n) => n %= m);
                                r = (b %= a);
                                break;
                            case RPNOperandType.LESS:  //"<",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b < a);
                                break;
                            case RPNOperandType.GREATER:  //">",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b > a);
                                break;
                            case RPNOperandType.LESSOREQUAL:  //"<=",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b <= a);
                                break;
                            case RPNOperandType.GREATEOREQUAL:  //">=",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b >= a);
                                break;

                            case RPNOperandType.NOTEQUAL:  //"!=",
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b != a);
                                break;
                            case RPNOperandType.EQUAL:  //"=="
                                a = Convert.ToDouble(al.Pop());
                                b = Convert.ToDouble(al.Pop());
                                r = (b == a);
                                break;

                            case RPNOperandType.OR:  //"||",
                                a1 = Convert.ToBoolean(al.Pop());
                                b1 = Convert.ToBoolean(al.Pop());
                                r = (b1 || a1);
                                break;


                            case RPNOperandType.AND:  //"&&",

                                a1 = Convert.ToBoolean(al.Pop());
                                b1 = Convert.ToBoolean(al.Pop());
                                r = (b1 && a1);
                                break;

                            case RPNOperandType.NOT:  //"!",
                                a1 = Convert.ToBoolean(al.Pop());
                                r = (!a1);
                                break;

                        }
                        al.Push(r);
                        break;
                }
                i++;
            }
            f = (m, n) =>
            {
                double res = funcs[0](m, n);
                for (int k = 1; k < funcs.Count; k++)
                {
                    res = funcs[k](res, variables[k+1]);   
                }
                
                return res;
            };
            while (x < al.Count) { ret = al.Pop(); }

            return f;
        }

        public void AddVar(string key, double num)
        {
            vars[key] = (new RPNArguments(key, num));
        }

        internal void AddVar(List<RPNArguments> argument)
        {
            foreach (var item in argument)
            {
                vars[item.name] = item;
            }
        }

        public object Result
        {
            get
            {
                return Exec();
            }
        }


    }
}
