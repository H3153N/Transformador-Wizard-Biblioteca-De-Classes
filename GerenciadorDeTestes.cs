using Ivi.Visa;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public static class GerenciadorDeTestes
    {

        public static event EventHandler<MediçãoEventArgs> MediçãoCompleta;

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
        public static PontoDeMedição RealizarMedição(MediçãoTipo tipo, CanalFonte canalFonte1, CanalFonte canalfonte2)
        {            
            Comunicação.RunStop(true);
            Thread.Sleep(50);
            Comunicação.AutoSet();
            Debug.WriteLine($"AutoSet-> - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            Thread.Sleep(2000);

            Comunicação.SetEscalaDeTempo();
            Debug.WriteLine($"SetEscalaDeTempo-> - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            Comunicação.SetOffset(canalFonte1, 0);
            Comunicação.SetOffset(canalfonte2, 0);

            Thread.Sleep(50);

            Debug.WriteLine($"Escala canal 1-> - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalFonte1), canalFonte1, 2);
            Debug.WriteLine($"Escala canal 2-> - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalfonte2), canalfonte2, 2);

            Thread.Sleep(50);
            Debug.WriteLine($"GetFrequenciaNoGerador -> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            double frequencia = Comunicação.GetFrequenciaNoGerador();
            Debug.WriteLine($"GetFrequenciaNoGerador -> {frequencia}Hz Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());


            Thread.Sleep(50);
            Comunicação.RunStop(false);
            Debug.WriteLine($"RunStop false -> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            Thread.Sleep(1000);
            
            if(tipo == MediçãoTipo.Admitancia)
            {
                Debug.WriteLine($"GetFormaDeOnda1 -> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
                CossenoCoeficientes formaDeOnda1 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalFonte1, frequencia));
                Debug.WriteLine($"GetFormaDeOnda2 -> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
                CossenoCoeficientes formaDeOnda2 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalfonte2, frequencia));

                //                    corrente                  tensao
                double admitancia = formaDeOnda2.Amplitude / formaDeOnda1.Amplitude;
                double fase = formaDeOnda2.Fase - formaDeOnda1.Fase;

                if (fase > 180)
                {
                    fase -= 360;
                }
                if (fase < -180)
                {
                    fase += 360;
                }

                PontoDeMedição medição = new(admitancia, fase, frequencia);
                Debug.WriteLine($"medição frequencia:{frequencia} -> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
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
        public static bool VarreduraDeFrequencia(CanalFonte canalTensao, CanalFonte canalCorrente, int tensaoDePico, int offset, List<double> frequencias)
        {
            pontosDeMedição.Clear();

            foreach (int i in frequencias)
            {
                try
                {
                    Comunicação.ConfigurarAquisiçãoOscilosóopio(10, 10000, 16);
                    Comunicação.AlterarSinalDoGerador("SIN", i, tensaoDePico, offset, true);
                    pontosDeMedição.Add(RealizarMedição(MediçãoTipo.Admitancia, canalTensao, canalCorrente));
                }
                catch(IOTimeoutException timeout)
                {
                    Debug.WriteLine($"Timeout: {timeout.Message}");
                }
            }
            return true;
        }

        public static async Task GetRespostaEmFrequenciaAsync(ParametrosDaMedição parametros)
        {

            Debug.WriteLine("GetRespostaEmFrequencia-> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            double[] ppds = GetFrequencias(parametros.PontosPorDecada).ToArray();

            foreach (double frequencia in ppds)
            {
                Debug.WriteLine($"ConfigurarAquisiçãoOscilosóopio->frequencia {frequencia}Hz - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
                Comunicação.ConfigurarAquisiçãoOscilosóopio(parametros.SingleCount,
                                                        parametros.PontosDeAquisição,
                                                        parametros.NumeroDeMédias);

                Debug.WriteLine($"AlterarSinalDoGerador->frequencia {frequencia}Hz - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
                Comunicação.AlterarSinalDoGerador(parametros.FormaDeOnda, 
                                                  frequencia,
                                                  parametros.TensãoNoGerador,
                                                  parametros.OffsetNoGerador, 
                                                  true);

                Debug.WriteLine($"RealizarMediçãoAsync->frequencia {frequencia}Hz - Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
                PontoDeMedição medição = await RealizarMediçãoAsync(parametros.MediçãoTipo, parametros.CanalFonte1, parametros.CanalFonte2);
                OnMediçãoCompleta(new MediçãoEventArgs(medição));
            }
        }

        private static void OnMediçãoCompleta(MediçãoEventArgs mediçãoEventArgs)
        {
            Debug.WriteLine($"OnMediçãoCompleta-> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            MediçãoCompleta?.Invoke(null, mediçãoEventArgs);
        }

        public static async Task<PontoDeMedição> RealizarMediçãoAsync(MediçãoTipo mediçãoTipo, CanalFonte canalFonte1, CanalFonte canalFonte2 )
        {
            Debug.WriteLine("RealizarMediçãoAsync-> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            return await Task.FromResult(RealizarMedição(mediçãoTipo, canalFonte1, canalFonte2));
        }
    }
}
