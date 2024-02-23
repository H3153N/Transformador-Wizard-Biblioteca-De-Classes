using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class PontoDeMedição(double admitancia, double fase, double frequencia)
    {
        public double Admitancia { get; private set; } = admitancia;
        public double Fase { get; private set; } = fase;
        public double Frequencia { get; private set; } = frequencia;
    }
}
