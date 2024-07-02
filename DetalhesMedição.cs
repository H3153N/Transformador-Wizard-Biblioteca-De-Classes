using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public record DetalhesMedição(TimeSpan TempoDownloadCanalA, TimeSpan TempoDownloadCanalB, TimeSpan TempoMédias, TimeSpan TempoAutoSet, TimeSpan TempoAjusteCanalA, TimeSpan TempoAjusteCanalB, TimeSpan tempoTotal, int TentativasCanalA, int TentativasCanalB, bool falhouA, bool falhouB)
    {
        public TimeSpan TempoDownloadCanalA { get; private set; } = TempoDownloadCanalA;
        public TimeSpan TempoDownloadCanalB { get; private set; } = TempoDownloadCanalB;
        public TimeSpan TempoMédias { get; private set; } = TempoMédias;
        public TimeSpan TempoAutoSet { get; private set; } = TempoAutoSet;
        public TimeSpan TempoAjusteCanalA { get; private set; } = TempoAjusteCanalA;
        public TimeSpan TempoAjusteCanalB { get; private set; } = TempoAjusteCanalB;
        public TimeSpan TempoTotal { get; private set; } = tempoTotal;
        public int TentativasCanalA { get; private set; } = TentativasCanalA;
        public int TentativasCanalB { get; private set; } = TentativasCanalB;
        public bool FalhouA { get; private set; } = falhouA;
        public bool FalhouB { get; private set; } = falhouB;
        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
