using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public static class GerenciadorDeTestes
    {
        public static ObservableCollection<PontoDeMedição> pontosDeMedição = [];
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pontosPorDecada"></param>
        /// <returns></returns>
        public static List<double> GetFrequencias(List<int>pontosPorDecada)
        {
            int index = 1;

            List<double> frequencias = [];

            foreach (int numPontos in pontosPorDecada)
            {
                int decadaAtual         = (int)Math.Pow(10, index);
                int proximaDecada       = decadaAtual * 10;

                double potenciaDoPasso  = 1d / numPontos;
                double tamanhoDoPasso   = Math.Pow(10, potenciaDoPasso); //https://en.wikipedia.org/wiki/Decade_(log_scale)#Calculations

                double frequenciaAtual = decadaAtual;

                while (frequenciaAtual <= proximaDecada) 
                {
                    frequencias.Add(frequenciaAtual);
                    frequenciaAtual = frequenciaAtual *= tamanhoDoPasso;
                }
                index++;
            }
            return frequencias;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tipo"></param>
        /// <param name="canalFonte1"></param>
        /// <param name="canalfonte2"></param>
        /// <returns></returns>
        public static PontoDeMedição RealizarMedição(MediçãoTipo tipo, Canal canalFonte1, Canal canalfonte2)
        {            
            Comunicação.RunStop(true);        
            //Thread.Sleep(50);
            Comunicação.AutoSet();
            Thread.Sleep(2000);

            Comunicação.SetEscalaDeTempo();
            Comunicação.SetOffset(canalFonte1, 0);
            Comunicação.SetOffset(canalfonte2, 0);

            Thread.Sleep(50);

            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalFonte1), canalFonte1, 2);
            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalfonte2), canalfonte2, 2);

            Thread.Sleep(50);
            
            double frequencia = Comunicação.GetFrequenciaNoGerador();

            
            Thread.Sleep(50);
            Comunicação.RunStop(false);
            Thread.Sleep(1000);
            
            if(tipo == MediçãoTipo.Admitancia)
            {
                CossenoCoeficientes formaDeOnda1 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalFonte1, frequencia));
                CossenoCoeficientes formaDeOnda2 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalfonte2, frequencia));

                //                    corrente                  tensao
                double admitancia = formaDeOnda2.Amplitude / formaDeOnda1.Amplitude;
                double fase = formaDeOnda2.Fase - formaDeOnda1.Fase;

                PontoDeMedição medição = new(admitancia, fase, frequencia);
                return medição;
            }
            return new PontoDeMedição(0, 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canalTensao"></param>
        /// <param name="canalCorrente"></param>
        /// <param name="tensaoDePico"></param>
        /// <param name="offset"></param>
        /// <param name="frequencias"></param>
        /// <returns></returns>
        public static bool VarreduraDeFrequencia(Canal canalTensao, Canal canalCorrente, int tensaoDePico, int offset, List<double> frequencias)
        {
            

            foreach (int i in frequencias)
            {
                Comunicação.ConfigurarAquisiçãoOscilosóopio(10, 10000, 16);
                Comunicação.AlterarSinalDoGerador("SIN", i, tensaoDePico, offset, true);
                pontosDeMedição.Add(RealizarMedição(MediçãoTipo.Admitancia, canalTensao, canalCorrente));
            }
            return true;
        }
    }
}
