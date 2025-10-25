using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain;

public class WPCalculator
{
    public List<string> CalculationSteps { get; private set; } = new();

    public Expression CalculateWP(Statement program, Expression postcondition)
    {
        CalculationSteps.Clear();
        CalculationSteps.Add($"Начальное постусловие: {postcondition.ToHumanReadable()}");

        var result = CalculateWithTrace(program, postcondition, "Программа");

        CalculationSteps.Add($"Финальное предусловие: {result.ToHumanReadable()}");
        return result;
    }

    private Expression CalculateWithTrace(Statement stmt, Expression post, string context)
    {
        var result = stmt.CalculateWP(post);
        CalculationSteps.Add($"{context}: {result.ToHumanReadable()}");
        return result;
    }

    public string GetHoareTriad(Expression precondition, Statement program, Expression postcondition)
    {
        return $"{{ {precondition.ToHumanReadable()} }} программа {{ {postcondition.ToHumanReadable()} }}";
    }
}
