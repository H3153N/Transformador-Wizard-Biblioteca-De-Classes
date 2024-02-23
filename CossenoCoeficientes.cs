using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class CossenoCoeficientes
    {
        public double Offset { get; private set; }
        public double Amplitude { get; private set; }
        public double Fase { get; private set; }

        public CossenoCoeficientes(double offset, double amplitude, double fase)
        {
            Offset = offset;
            Amplitude = amplitude;
            Fase = fase;
        }
    }
}
