using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Data de query string no formato estrito <c>yyyy-MM-dd</c>. O <see cref="DateOnly"/> puro aceitaria
/// outros formatos (como <c>01/03/2026</c>, lido como mês/dia); aqui qualquer outro formato vira 400.
/// </summary>
public readonly record struct DataIso(DateOnly Valor) : IComparable<DataIso>
{
    public const string Formato = "yyyy-MM-dd";

    public static bool TryParse([NotNullWhen(true)] string? texto, IFormatProvider? provider, out DataIso data)
    {
        var valida = DateOnly.TryParseExact(texto, Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var valor);
        data = new DataIso(valor);
        return valida;
    }

    public int CompareTo(DataIso other) => Valor.CompareTo(other.Valor);

    public static bool operator <(DataIso esquerda, DataIso direita) => esquerda.CompareTo(direita) < 0;

    public static bool operator >(DataIso esquerda, DataIso direita) => esquerda.CompareTo(direita) > 0;

    public static bool operator <=(DataIso esquerda, DataIso direita) => esquerda.CompareTo(direita) <= 0;

    public static bool operator >=(DataIso esquerda, DataIso direita) => esquerda.CompareTo(direita) >= 0;

    public override string ToString() => Valor.ToString(Formato, CultureInfo.InvariantCulture);
}
