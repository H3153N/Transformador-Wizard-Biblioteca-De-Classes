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
        static bool debug = false;
        public static event EventHandler<MediçãoEventArgs> MediçãoCompleta;
        static DateTime tempo;

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
            tempo = DateTime.Now;
            double frequencia = Comunicação.GetFrequenciaNoGerador();
            Debug.WriteIf(debug,"inicio frequencia " + frequencia.ToString() + "Hz");
            Debug.WriteIf(debug, "Run");
            
            Comunicação.RunStop(true);
            Thread.Sleep(50);
            Comunicação.AutoSet();
            Debug.WriteIf(debug, "AutoSet");
            Thread.Sleep(1900);
            Debug.WriteIf(debug, "AutoSet fim\nInicio SetEscalaDeTempo");
            Comunicação.SetEscalaDeTempo();
            Debug.WriteIf(debug, "SetEscalaDeTempo Fim");

            Debug.WriteIf(debug, "Offset");

            Comunicação.SetOffset(canalFonte1, 0);            
            Comunicação.SetOffset(canalfonte2, 0);

            Thread.Sleep(50);

            Debug.WriteIf(debug, "SetEscalaDeVertical 1 Start");
            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalFonte1), canalFonte1, 2);
            Debug.WriteIf(debug, "SetEscalaDeVertical 2 Start");
            Comunicação.AjustarEscalaVerticalTensão(Comunicação.GetTensãoDePicoMedida(canalfonte2), canalfonte2, 2);

            Thread.Sleep(50);
            
           
            int delayMillis = 5 + (int)((1 / frequencia) * 1000);


            Debug.WriteIf(debug, "STOP");
            Comunicação.RunStop(false);
            Debug.WriteIf(debug, "RUN SINGLE");

            Comunicação.RunSingle();
            Thread.Sleep(10);

            bool runSingleRodando = true;
            while (runSingleRodando)
            {
                string resposta = Comunicação.InquerirOsciloscópio("ACQuire:AVERage:COMPlete?", true);
                if (resposta == "0\n")
                {
                    Thread.Sleep(delayMillis);
                    Debug.WriteIf(debug, "resposta: " + resposta + " delay: " + delayMillis.ToString());
                }
                else if (resposta == "1\n") 
                {
                    runSingleRodando = false;
                }
            }

            
            
            Thread.Sleep(900);
            if (tipo == MediçãoTipo.Admitancia)
            {
                Debug.WriteIf(debug, "forma de onda 1" );

                CossenoCoeficientes formaDeOnda1;
                CossenoCoeficientes formaDeOnda2;
                try
                {
                    formaDeOnda1 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalFonte1, frequencia));
                    Thread.Sleep(5);
                    formaDeOnda2 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalfonte2, frequencia));
                }
                catch (ErroDeTransferência e)
                {

                    return new PontoDeMedição(-1, 0, e.FrequenciaErro, 1, -1, -1);
                }
                
                Debug.WriteIf(debug, "forma de onda 2\n\n" );
                

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

                PontoDeMedição medição = new(admitancia, fase, frequencia,0, 
                    Comunicação.GetTensãoDePicoMedida(canalFonte1), 
                    Comunicação.GetTensãoDePicoMedida(canalfonte2)); 

                Debug.WriteLine((new DateTime(DateTime.Now.Ticks - tempo.Ticks)).Second + " segundos\n");
                return medição;
            }
            
            return new PontoDeMedição(0, 0, frequencia, 0, 0, 0);
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
                    // COLOCAR TODOS ESSES PARAMETROS COMO CONFICURACOES PREVIAS
                    Comunicação.ConfigurarAquisiçãoOscilosóopio(10, 10000, 16, AquisiçãoModo.Médias);
                    Comunicação.AlterarSinalDoGerador("SIN", i, tensaoDePico, offset, true);
                    pontosDeMedição.Add(RealizarMedição(MediçãoTipo.Admitancia, canalTensao, canalCorrente));
                }
                catch(IOTimeoutException timeout)
                {
                    Debug.WriteIf(debug, $"Varredura de frequencia: Timeout: {timeout.Message}");
                }
            }
            return true;
        }


        /// <summary>                                                    \\\\\\\\\\\\\\\\\/////////////////
        /// 
        ///                                                              CRIAR OVERLOAD PARA ABRIR ARQUIVO ?
        /// </summary>
        /// <param name="pontosDeMedição"></param>                       /////////////////\\\\\\\\\\\\\\\\\
        /// <returns></returns>
        public static List<double> GetFrequênciasComErro(List<PontoDeMedição> pontosDeMedição)
        {
            List<double> frequencias = new List<double>();

            foreach (var ponto in pontosDeMedição)
            {
                bool erroDeComunicação = ponto.houveErro == 1;
                bool erroDeMedição = !double.IsFinite(ponto.EscalaVerticalTensao);

                if (erroDeComunicação || erroDeMedição)
                {
                    frequencias.Add(ponto.Frequencia);
                }
            }

            return frequencias;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequenciasComErro"></param>
        /// <param name="parametros"></param>
        /// <returns></returns>
        public static List<PontoDeMedição> RefazerMedições(List<double>frequenciasComErro, ParametrosDaMedição parametros)
        {
            List<PontoDeMedição> pontosRefeitos = new List<PontoDeMedição>();

            foreach (var ponto in frequenciasComErro) 
            {
                try
                {
                    Comunicação.ConfigurarAquisiçãoOscilosóopio(10, 10000, 16, AquisiçãoModo.Médias);
                    pontosRefeitos.Add(RealizarMedição(parametros.MediçãoTipo, parametros.CanalFonte1, parametros.CanalFonte2));
                }
                catch (IOTimeoutException timeout)
                {
                    Debug.WriteIf(debug, $"Varredura de frequencia: Timeout: {timeout.Message}");
                }                
            }

            return pontosRefeitos;
        }


#region funções async
        public static async Task GetRespostaEmFrequenciaAsync(ParametrosDaMedição parametros)
        {

            double[] ppds = GetFrequencias(parametros.PontosPorDecada).ToArray();
            foreach (double frequencia in ppds)
            {
                
                Comunicação.ConfigurarAquisiçãoOscilosóopio(parametros.SingleCount, parametros.PontosDeAquisição, parametros.NumeroDeMédias, AquisiçãoModo.Médias);                                
                Comunicação.AlterarSinalDoGerador(parametros.FormaDeOnda, frequencia, parametros.TensãoNoGerador, parametros.OffsetNoGerador, true);
                
                PontoDeMedição medição = await RealizarMediçãoAsync(parametros.MediçãoTipo, parametros.CanalFonte1, parametros.CanalFonte2);
                OnMediçãoCompleta(new MediçãoEventArgs(medição));
            }
        }
        private static void OnMediçãoCompleta(MediçãoEventArgs mediçãoEventArgs)
        {
            Debug.WriteIf(debug, $"OnMediçãoCompleta-> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            MediçãoCompleta?.Invoke(null, mediçãoEventArgs);
        }
        public static async Task<PontoDeMedição> RealizarMediçãoAsync(MediçãoTipo mediçãoTipo, CanalFonte canalFonte1, CanalFonte canalFonte2 )
        {
            Debug.WriteIf(debug, "RealizarMediçãoAsync-> Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
            return await Task.FromResult(RealizarMedição(mediçãoTipo, canalFonte1, canalFonte2));
        }
#endregion
    }
}
