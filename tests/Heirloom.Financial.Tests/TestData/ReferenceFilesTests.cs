namespace Heirloom.Financial.Tests.TestData;

public class ReferenceFilesTests
{
    [Fact]
    public void I_csv_di_riferimento_vengono_copiati_accanto_ai_test()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Reference", "esempio.csv");

        Assert.True(File.Exists(path), $"File non trovato: {path}");
    }
}