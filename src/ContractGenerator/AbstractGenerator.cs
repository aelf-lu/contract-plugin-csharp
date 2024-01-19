namespace ContractGenerator;

public abstract class AbstractGenerator : IndentPrinter
{
    protected readonly GeneratorOptions Options;

    protected AbstractGenerator(GeneratorOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// Enclose the content written in <paramref name="action"/> with a <c>#region</c> <c>#endregion</c> block.
    /// </summary>
    /// <param name="action">The action to be executed within the block. Use it to code the logic for content to be
    /// written within the block.</param>
    protected void InRegion(string name, Action action)
    {
        _($"#region {name}");
        action();
        _($"#endregion {name}");
    }

    /// <summary>
    /// Enclose the content written in <paramref name="action"/> with a pair of braces. The content written in
    /// <paramref name="action"/> will be indented.
    /// </summary>
    /// <param name="action">The action to be executed within the block. Use it to code the logic for content to be
    /// written within the block.</param>
    protected void InBlock(Action action)
    {
        _("{");
        Indent();
        action();
        Outdent();
        _("}");
    }

    /// <summary>
    /// Enclose the content written in <paramref name="action"/> with a pair of braces. The content written in
    /// <paramref name="action"/> will be indented. This method is similar to <see cref="InBlock"/> except that an
    /// additional <c>;</c> is written after the closing brace.
    /// </summary>
    /// <param name="action">The action to be executed within the block. Use it to code the logic for content to be
    /// written within the block.</param>
    protected void InBlockWithSemicolon(Action action)
    {
        _("{");
        Indent();
        action();
        Outdent();
        _("};");
    }

    /// <summary>
    /// Enclose the content written in <paramref name="action"/> with a pair of braces. The content written in
    /// <paramref name="action"/> will be indented. This method is similar to <see cref="InBlock"/> except that an
    /// additional <c>,</c> is written after the closing brace.
    /// </summary>
    /// <param name="action">The action to be executed within the block. Use it to code the logic for content to be
    /// written within the block.</param>
    protected void InBlockWithComma(Action action)
    {
        _("{");
        Indent();
        action();
        Outdent();
        _("},");
    }

    protected void Indented(Action a)
    {
        Indent();
        a();
        Outdent();
    }

    protected void DoubleIndented(Action a)
    {
        Indent();
        Indent();
        a();
        Outdent();
        Outdent();
    }
    public abstract string? Generate();
}
