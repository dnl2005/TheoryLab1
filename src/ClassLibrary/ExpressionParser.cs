using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public class ExpressionParser
    {
        private int _position;
        private string _input;

        private static readonly Dictionary<string, int> OperatorPrecedence = new()
        {
            { "or", 1 },
            { "and", 2 },
            { "==", 3 }, { "!=", 3 },
            { ">", 4 }, { "<", 4 }, { ">=", 4 }, { "<=", 4 },
            { "+", 5 }, { "-", 5 },
            { "*", 6 }, { "/", 6 }
        };

        private static readonly HashSet<string> BinaryOperators = new()
        {
            "+", "-", "*", "/", ">", "<", ">=", "<=", "==", "!=", "and", "or"
        };

        public Expression Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be empty");

            _input = input.Trim();
            _position = 0;

            return ParseExpression();
        }

        private Expression ParseExpression(int precedence = 0)
        {
            Expression left = ParsePrimary();

            while (_position < _input.Length)
            {
                SkipWhitespace();
                if (_position >= _input.Length) break;

                string op = ReadOperator();
                if (string.IsNullOrEmpty(op) || !BinaryOperators.Contains(op) || OperatorPrecedence[op] <= precedence)
                    break;

                _position += op.Length;
                Expression right = ParseExpression(OperatorPrecedence[op]);
                left = CreateBinaryExpression(left, op, right);
            }

            return left;
        }

        private Expression ParsePrimary()
        {
            SkipWhitespace();
            if (_position >= _input.Length)
                throw new Exception("Unexpected end of expression");

            char current = _input[_position];

            if (current == '(')
            {
                _position++;
                Expression expr = ParseExpression();
                SkipWhitespace();
                if (_position >= _input.Length || _input[_position] != ')')
                    throw new Exception("Expected ')'");
                _position++;
                return expr;
            }

            if (current == 'n' && PeekWord() == "not")
            {
                _position += 3;
                SkipWhitespace();
                return new NotExpression(ParsePrimary());
            }

            if (char.IsLetter(current))
            {
                string word = ReadWord();
                if (word == "abs")
                {
                    SkipWhitespace();
                    if (_position >= _input.Length || _input[_position] != '(')
                        throw new Exception("Expected '(' after 'abs'");
                    _position++;
                    Expression absExpr = ParseExpression();
                    SkipWhitespace();
                    if (_position >= _input.Length || _input[_position] != ')')
                        throw new Exception("Expected ')' after abs expression");
                    _position++;
                    return absExpr;
                }
                else
                {
                    return new VariableExpression(word);
                }
            }

            if (char.IsDigit(current) || current == '-')
            {
                string number = ReadNumber();
                if (int.TryParse(number, out int value))
                    return new ConstantExpression(value);
                else
                    throw new Exception($"Invalid number: {number}");
            }

            throw new Exception($"Unexpected character: {current}");
        }

        private string ReadOperator()
        {
            if (_position >= _input.Length) return "";

            // Двухсимвольные операторы
            if (_position + 1 < _input.Length)
            {
                string twoChar = _input.Substring(_position, 2);
                if (twoChar is ">=" or "<=" or "==" or "!=")
                    return twoChar;
            }

            // Односимвольные операторы
            char current = _input[_position];
            if (current is '>' or '<' or '+' or '-' or '*' or '/')
                return current.ToString();

            // Логические операторы
            if (current == 'a' && PeekWord() == "and")
                return "and";
            if (current == 'o' && PeekWord() == "or")
                return "or";

            return "";
        }

        private string ReadWord()
        {
            int start = _position;
            while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
            {
                _position++;
            }
            return _input.Substring(start, _position - start);
        }

        private string PeekWord()
        {
            int savedPos = _position;
            string word = ReadWord();
            _position = savedPos;
            return word;
        }

        private string ReadNumber()
        {
            int start = _position;

            // Обрабатываем знак минус
            if (_input[_position] == '-')
                _position++;

            while (_position < _input.Length && char.IsDigit(_input[_position]))
            {
                _position++;
            }

            return _input.Substring(start, _position - start);
        }

        private void SkipWhitespace()
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }
        }

        private Expression CreateBinaryExpression(Expression left, string op, Expression right)
        {
            return op.ToLower() switch
            {
                "and" => new AndExpression(left, right),
                "or" => new OrExpression(left, right),
                _ => new BinaryExpression(left, op, right)
            };
        }
    }
}
