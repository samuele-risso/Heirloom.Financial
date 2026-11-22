using System.Globalization;

namespace Heirloom.Financial.Tests.Risk;

/// <summary>
/// Legge i file di riferimento di rischio (TestData/Reference): una riga per valore,
/// con il nome del caso e il risultato atteso ripetuto su ogni riga.
/// </summary>
internal static class ReferenceSeries
{
    public static Dictionary<string, (List<decimal> Values, double Expected)> Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Reference", fileName);
        var cases = new Dictionary<string, (List<decimal> Values, double Expected)>();

        foreach (var line in File.ReadLines(path).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(',');
            var value = decimal.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            var expected = double.Parse(parts[2], CultureInfo.InvariantCulture);

            if (!cases.TryGetValue(parts[0], out var entry))
            {
                entry = (new List<decimal>(), expected);
                cases[parts[0]] = entry;
            }

            entry.Values.Add(value);
        }

        return cases;
    }

    public static TheoryData<string> Names(string fileName)
    {
        var data = new TheoryData<string>();
        foreach (var name in Load(fileName).Keys)
        {
            data.Add(name);
        }

        return data;
    }
}