public abstract class Expression
{
    public abstract Expression Substitute(string variable, Expression value);
    public abstract Expression Clone();
    public abstract string ToHumanReadable();
}

public class VariableExpression : Expression
{
    public string Name { get; set; }

    public VariableExpression() { }

    public VariableExpression(string name)
    {
        Name = name;
    }

    public override Expression Substitute(string variable, Expression value)
    {
        return variable == Name ? value.Clone() : Clone();
    }

    public override Expression Clone() => new VariableExpression(Name);
    public override string ToHumanReadable() => Name;
}

public class ConstantExpression : Expression
{
    public int Value { get; set; }

    public ConstantExpression() { }

    public ConstantExpression(int value)
    {
        Value = value;
    }

    public override Expression Substitute(string variable, Expression value) => Clone();
    public override Expression Clone() => new ConstantExpression(Value);
    public override string ToHumanReadable() => Value.ToString();
}

public class BinaryExpression : Expression
{
    public Expression Left { get; set; }
    public Expression Right { get; set; }
    public string Operator { get; set; }

    public BinaryExpression() { }

    public BinaryExpression(Expression left, string op, Expression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override Expression Substitute(string variable, Expression value)
    {
        return new BinaryExpression(
            Left.Substitute(variable, value),
            Operator,
            Right.Substitute(variable, value)
        );
    }

    public override Expression Clone() => new BinaryExpression(
        Left.Clone(), Operator, Right.Clone()
    );

    public override string ToHumanReadable() => $"({Left.ToHumanReadable()} {Operator} {Right.ToHumanReadable()})";
}

public class AndExpression : Expression
{
    public Expression Left { get; set; }
    public Expression Right { get; set; }

    public AndExpression() { }

    public AndExpression(Expression left, Expression right)
    {
        Left = left;
        Right = right;
    }

    public override Expression Substitute(string variable, Expression value)
    {
        return new AndExpression(
            Left.Substitute(variable, value),
            Right.Substitute(variable, value)
        );
    }

    public override Expression Clone() => new AndExpression(
        Left.Clone(), Right.Clone()
    );

    public override string ToHumanReadable() => $"{Left.ToHumanReadable()} и {Right.ToHumanReadable()}";
}

public class OrExpression : Expression
{
    public Expression Left { get; set; }
    public Expression Right { get; set; }

    public OrExpression() { }

    public OrExpression(Expression left, Expression right)
    {
        Left = left;
        Right = right;
    }

    public override Expression Substitute(string variable, Expression value)
    {
        return new OrExpression(
            Left.Substitute(variable, value),
            Right.Substitute(variable, value)
        );
    }

    public override Expression Clone() => new OrExpression(
        Left.Clone(), Right.Clone()
    );

    public override string ToHumanReadable() => $"{Left.ToHumanReadable()} или {Right.ToHumanReadable()}";
}

public class NotExpression : Expression
{
    public Expression Expression { get; set; }

    public NotExpression() { }

    public NotExpression(Expression expression)
    {
        Expression = expression;
    }

    public override Expression Substitute(string variable, Expression value)
    {
        return new NotExpression(Expression.Substitute(variable, value));
    }

    public override Expression Clone() => new NotExpression(Expression.Clone());

    public override string ToHumanReadable() => $"не({Expression.ToHumanReadable()})";
}