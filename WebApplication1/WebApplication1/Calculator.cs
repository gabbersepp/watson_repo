using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WebApplication1
{
    public class Calculator
    {
        public static bool IsValid(string input)
        {
            var items = Extract(input);
            if (items.Item3 == 0 && items.Item2 == "/")
            {
                return false;
            }

            return true;
        }

        public static double? Calc(double z1, string input, string @operator)
        {
            var regex = new Regex(@"([0-9\.])");
            return Operator.TryParse(z1, Double.Parse(regex.Match(input).Groups[1].Value), @operator);
        }

        public static double? Calc(string input)
        {
            var items = Extract(input);
            return Operator.TryParse(items.Item1, items.Item3, items.Item2);
        }


        public static Tuple<double, string, double> Extract(string input)
        {
            var regex = new Regex(@"([^0-9]*)([0-9\.]+)([^0-9]*)([0-9\.]+)[^0-9]*");
            var match = regex.Match(input);
            var possiblePreText = match.Groups[1].Value;
            var possibleOp1 = match.Groups[2].Value;
            var possibleOperator = match.Groups[3].Value;
            var possibleOp2 = match.Groups[4].Value;

            var opRegex = new Regex(@".*([+-/\*]).*");
            var opMatches = opRegex.Match(possibleOperator);
            string @operator;

            if (opMatches.Success)
            {
                @operator = opMatches.Groups[1].Value;
            }
            else
            {
                @operator = Operator.Map(possiblePreText);
            }

            double z1;
            double z2;

            if (!Double.TryParse(possibleOp1, NumberStyles.Any, CultureInfo.InvariantCulture, out z1))
            {
                z1 = int.MinValue;
            }

            if (!Double.TryParse(possibleOp2, NumberStyles.Any, CultureInfo.InvariantCulture, out z2))
            {
                z2 = int.MinValue;
            }

            if (@operator == "-" && possibleOperator.Contains("von"))
            {
                var t = z1;
                z1 = z2;
                z2 = t;
            }

            return new Tuple<double, string, double>(z1, @operator, z2);
        }

        public class Operator
        {
            private static Dictionary<string, Func<double, double, double>> dict = new Dictionary<string, Func<double, double, double>>
            {
                {"+", (a, b) => a + b},
                {"-", (a, b) => a - b},
                {"/", (a, b) => a / b},
                {"*", (a, b) => a * b}
            };

            public static double? TryParse(double z1, double z2, string op)
            {
                op = Map(op.Trim());
                if (!dict.ContainsKey(op))
                {
                    return null;
                }

                return dict[op](z1, z2);
            }

            public static string Map(string word)
            {
                word = word.ToLower();
                if (word.Contains("summ"))
                {
                    return "+";
                }

                if (word.Contains("multipl") || word.Contains("produkt"))
                {
                    return "*";
                }

                if (word.Contains("divi") || word.Contains("quotient"))
                {
                    return "/";
                }

                if (word.Contains("subtra"))
                {
                    return "-";
                }

                return word;
            }
        }
    }
}