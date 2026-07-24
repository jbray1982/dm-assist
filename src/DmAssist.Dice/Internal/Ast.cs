namespace DmAssist.Dice.Internal;

/// <summary>
/// Which keep/drop selector a dice term carries. Count of dice affected defaults to 1 when the
/// source expression omits it (<c>kh</c> == <c>kh1</c>) — the parser resolves that default, so
/// nothing downstream ever sees an "omitted" selector count.
/// </summary>
internal enum SelectorKind
{
    KeepHighest,
    KeepLowest,
    DropHighest,
    DropLowest
}

internal enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide
}

/// <summary>
/// Base of the parse-tree node hierarchy. This is the grammar's extension point — a future
/// exploding-dice or reroll feature is a new node kind here, not a new branch bolted onto an
/// existing one. <see cref="Position"/> locates the node in the original expression text for
/// error reporting.
/// </summary>
internal abstract record AstNode(int Position);

/// <summary>A literal integer, e.g. the <c>3</c> in <c>1d6+3</c>.</summary>
internal sealed record NumberNode(int Value, int Position) : AstNode(Position);

/// <summary>
/// A dice term, e.g. <c>4d6kh3</c>. <see cref="Count"/> and <see cref="SelectorCount"/> are
/// already defaulted by the parser (omitted count == 1); nothing downstream re-derives a default.
/// </summary>
internal sealed record DiceNode(
    int Count,
    int Sides,
    SelectorKind? Selector,
    int? SelectorCount,
    int Position) : AstNode(Position);

/// <summary>A binary arithmetic operation, e.g. <c>+</c> in <c>1d6+3</c>.</summary>
internal sealed record BinaryNode(BinaryOperator Operator, AstNode Left, AstNode Right, int Position) : AstNode(Position);

/// <summary>Unary negation, e.g. the leading <c>-</c> in <c>-1d6</c>.</summary>
internal sealed record NegateNode(AstNode Operand, int Position) : AstNode(Position);
