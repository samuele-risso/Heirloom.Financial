namespace Heirloom.Financial.Positions;

/// <summary>
/// Come si decide quale costo "esce" dal portafoglio quando si vende una parte di una posizione.
/// </summary>
/// <remarks>
/// La scelta cambia il guadagno realizzato di ogni vendita (non il totale finale, quando si è venduto tutto),
/// quindi va fatta una volta e usata ovunque.
/// </remarks>
public enum CostBasisMethod
{
    /// <summary>
    /// Media ponderata: ogni pezzo costa il prezzo medio di tutti i pezzi posseduti.
    /// Una vendita non cambia il prezzo medio dei pezzi che restano.
    /// </summary>
    WeightedAverage = 1,

    /// <summary>
    /// FIFO (first in, first out): escono per primi i pezzi comprati per primi.
    /// </summary>
    Fifo = 2,
}