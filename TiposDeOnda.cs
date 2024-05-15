using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public static class TiposDeOnda
    {
        public static readonly string Senoidal = "SINusoid";
        public static readonly string Quadrada = "SQUare";
        public static readonly string Triangular = "RAMP";
        public static readonly string Pulso = "PULSe";
        public static readonly string Ruido = "NOISe";
        public static readonly string DC = "DC";
        public static readonly string Arbitraria = "USER";
        public static string TipoParaString(Função waveform)
        {
            switch (waveform)
            {
                case Função.Senoidal:
                    return "SINusoid";
                case Função.Quadrada:
                    return "SQUare";
                case Função.Triangular:
                    return "RAMP";
                case Função.Pulso:
                    return "PULSe";
                case Função.Ruído:
                    return "NOISe";
                case Função.DC:
                    return "DC";
                case Função.Arbitrária:
                    return "USER";
                default:
                    throw new ArgumentOutOfRangeException(nameof(waveform), waveform, null);
            }
        }
    }
}
