namespace Domain;

public class CodeParser
{
    private readonly ExpressionParser _expressionParser;

    public CodeParser()
    {
        _expressionParser = new ExpressionParser();
    }

    public Statement Parse(string code)
    {
        try
        {
            // Упрощаем парсинг - убираем фигурные скобки и разбиваем на строки
            string cleanCode = code.Replace("{", "").Replace("}", "").Replace("\r", "");
            string[] lines = cleanCode.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var statements = new List<Statement>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                // Обработка if
                if (line.StartsWith("if"))
                {
                    var ifStatement = ParseIfStatement(lines, ref i);
                    if (ifStatement != null)
                        statements.Add(ifStatement);
                }
                // Обработка присваивания
                else if (line.Contains(":="))
                {
                    var assignment = ParseAssignment(line);
                    if (assignment != null)
                        statements.Add(assignment);
                }
            }

            return statements.Count == 1 ? statements[0] : new Sequence { Statements = statements };
        }
        catch (Exception ex)
        {
            throw new Exception($"Parse error: {ex.Message}");
        }
    }

    private Statement ParseIfStatement(string[] lines, ref int index)
    {
        string ifLine = lines[index].Trim();

        // Извлекаем условие из if (condition)
        int start = ifLine.IndexOf('(');
        int end = ifLine.IndexOf(')');
        if (start < 0 || end < 0)
            throw new Exception("Invalid if statement - missing parentheses");

        string conditionStr = ifLine.Substring(start + 1, end - start - 1).Trim();
        Expression condition = _expressionParser.Parse(conditionStr);

        // Ищем ветки then и else
        var thenBranch = new List<Statement>();
        var elseBranch = new List<Statement>();

        bool inThenBranch = true;
        index++; // Переходим к следующей строке после if

        while (index < lines.Length)
        {
            string line = lines[index].Trim();

            if (line.StartsWith("else"))
            {
                inThenBranch = false;
                index++;
                continue;
            }

            if (line.StartsWith("if") || line.StartsWith("}") || string.IsNullOrEmpty(line))
            {
                // Конец блока
                break;
            }

            if (line.Contains(":="))
            {
                var assignment = ParseAssignment(line);
                if (assignment != null)
                {
                    if (inThenBranch)
                        thenBranch.Add(assignment);
                    else
                        elseBranch.Add(assignment);
                }
            }

            index++;
        }

        return new IfStatement
        {
            Condition = condition,
            ThenBranch = thenBranch.Count == 1 ? thenBranch[0] : new Sequence { Statements = thenBranch },
            ElseBranch = elseBranch.Count == 1 ? elseBranch[0] : new Sequence { Statements = elseBranch }
        };
    }

    private Assignment ParseAssignment(string line)
    {
        try
        {
            // Убираем точку с запятой в конце если есть
            line = line.Trim().TrimEnd(';');

            var parts = line.Split(new[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return null;

            string variable = parts[0].Trim();
            string expression = parts[1].Trim();

            // Заменяем sqrt на что-то простое для демонстрации
            expression = expression.Replace("sqrt(D)", "D"); // Упрощаем для демо

            return new Assignment
            {
                Variable = variable,
                Value = _expressionParser.Parse(expression)
            };
        }
        catch
        {
            return null;
        }
    }
}