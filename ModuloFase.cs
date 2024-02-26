using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class ModuloFase
    {
        public List<DataPoint> Modulo{ get; set; } = new List<DataPoint>();
        public List<DataPoint> Fase { get; set; } = new List<DataPoint>();
        public int NumeroDePontos { get; set; } = -1;

        public ModuloFase( List<DataPoint> modulo, List<DataPoint> fase)
        {
            Modulo = modulo;
            Fase = fase;        
            NumeroDePontos = modulo.Count;
        }

        public ModuloFase() { }
    }
}
