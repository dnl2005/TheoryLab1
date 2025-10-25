
namespace Domain;

// Базовый класс для всех операторов
public abstract class Statement
{
    public abstract Expression CalculateWP(Expression postcondition);
}

// Присваивание
public class Assignment : Statement
{
    public string Variable { get; set; }
    public Expression Value { get; set; }

    public override Expression CalculateWP(Expression postcondition)
    {
        // Заменяем переменную на выражение в постусловии
        return postcondition.Substitute(Variable, Value);
    }
}

// Условный оператор
public class IfStatement : Statement
{
    public Expression Condition { get; set; }
    public Statement ThenBranch { get; set; }
    public Statement ElseBranch { get; set; }

    public override Expression CalculateWP(Expression postcondition)
    {
        var wpThen = ThenBranch.CalculateWP(postcondition);
        var wpElse = ElseBranch.CalculateWP(postcondition);

        // (Condition ∧ wpThen) ∨ (¬Condition ∧ wpElse)
        return new OrExpression(
            new AndExpression(Condition.Clone(), wpThen),
            new AndExpression(new NotExpression(Condition.Clone()), wpElse)
        );
    }
}

// Последовательность операторов
public class Sequence : Statement
{
    public List<Statement> Statements { get; set; } = new();

    public override Expression CalculateWP(Expression postcondition)
    {
        // Идём с конца к началу
        Expression current = postcondition;
        for (int i = Statements.Count - 1; i >= 0; i--)
        {
            current = Statements[i].CalculateWP(current);
        }
        return current;
    }
}