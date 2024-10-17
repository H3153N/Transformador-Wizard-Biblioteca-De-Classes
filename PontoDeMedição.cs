using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class PontoDeMedição(double admitancia, double fase, double frequencia, int houveErro, double escalaTensao, double escalaCorrente)
    {
        public double Admitancia { get; set; } = admitancia;
        public double Fase { get;  set; } = fase;
        public double Frequencia { get; private set; } = frequencia;
        public int houveErro { get; private set; } = houveErro;

        public double EscalaVerticalTensao { get; private set; } = escalaTensao;
        public double EscalaVerticalCorrente { get; private set; } = escalaCorrente;
        public DetalhesMedição DetalhesMedição { get; set; }
    }
}
