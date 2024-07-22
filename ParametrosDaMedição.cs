
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class ParametrosDaMedição(CanalFonte canalFonte1, CanalFonte canalFonte2, Atenuação atenuaçãoCanalFonte1, Atenuação atenuaçãoCanalFonte2, double tensãoNoGerador, double offsetNoGerador, string formaDeOnda, int singleCount, int pontosDeAquisição, int numeroDeMédias, List<int> pontosPorDecada, MediçãoTipo tipo, bool usaShunt, double Rshunt, CanalFonte gatilho)
    {
        public MediçãoTipo MediçãoTipo { get; private set; } = tipo;
        public CanalFonte CanalFonte1 { get; private set; } = canalFonte1;
        public CanalFonte CanalFonte2 { get; private set; } = canalFonte2;
        public CanalFonte Gatilho { get; private set; } = gatilho;

        public Atenuação AtenuaçãoCanalFonte1 { get; private set; } = atenuaçãoCanalFonte1;
        public Atenuação AtenuaçãoCanalFonte2 { get; private set; } = atenuaçãoCanalFonte2;

        public double TensãoNoGerador { get; private set; } = tensãoNoGerador;
        public double OffsetNoGerador { get; private set; } = offsetNoGerador;
        public string FormaDeOnda { get; private set; } = formaDeOnda;

        public int SingleCount { get; private set; } = singleCount;
        public int PontosDeAquisição { get; private set; } = pontosDeAquisição;
        public int NumeroDeMédias { get; private set; } = numeroDeMédias;

        public bool UsaShunt { get; set; } = usaShunt;
        public double ResistenciaShunt { get; set; } = Rshunt;

        public List<int> PontosPorDecada { get; private set; } = pontosPorDecada;
    }
}
