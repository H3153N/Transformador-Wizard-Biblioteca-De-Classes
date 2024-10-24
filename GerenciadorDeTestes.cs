using Ivi.Visa;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
        /// <param name="canalTensão"></param>
        /// <param name="canalCorrente"></param>
        /// <returns></returns>
        public static PontoDeMedição RealizarMediçãoFrequencia(MediçãoTipo tipo, CanalFonte canalTensão, CanalFonte canalCorrente, bool invertido1, bool invertido2, bool testeCauteloso, bool usaShunt, double Rshunt, CanalFonte gatilho, bool ajusteFino)
        {
            tempo = DateTime.Now;
            double frequencia = Comunicação.GetFrequenciaNoGerador();
            Debug.WriteIf(debug, "inicio frequencia " + frequencia.ToString() + "Hz" + "\n");
            Debug.WriteIf(debug, "Run" + "\n");

            Comunicação.RunStop(true);
            Thread.Sleep(20);

            if (testeCauteloso)
            {
                Comunicação.AutoSet();
                Debug.WriteIf(debug, "AutoSet" + "\n");
            }

            

            DateTime tempoInicial = DateTime.Now;
            TimeSpan tempoAjusteCanalA = new TimeSpan(0);
            TimeSpan tempoAjusteCanalB = new TimeSpan(0);
            Comunicação.SetEscalaDeTempo();
            if (ajusteFino)
            {
                List<CanalFonte> canals = new List<CanalFonte>() { canalTensão, canalCorrente };
                Comunicação.AjustarEscalaVerticalRígido(canals);
                Comunicação.EscolherGatilho(canalTensão, canalCorrente, gatilho, frequencia);
            }
            else
            {
                //Comunicação.SetFonteTrigger(gatilho);
                Comunicação.EscolherGatilho(canalTensão, canalCorrente, gatilho, frequencia);

                gatilho = CanalFonte.CH1;  // FORÇA O GATILHO PARA O CANAL 1                              <<<<<<TIRAR DEPOIS
                Debug.WriteIf(debug, "AutoSet fim \n Inicio SetEscalaDeTempo");
                
                Debug.WriteIf(debug, "SetEscalaDeTempo Fim" + "\n");

                Debug.WriteIf(debug, "Offset" + "\n");

                Comunicação.SetOffsetVertical(canalTensão, 0);
                Comunicação.SetOffsetVertical(canalCorrente, 0);

                Thread.Sleep(250);

                #region Escala vertical

                tempoInicial = DateTime.Now;
                Debug.WriteIf(debug, "SetEscalaDeVertical 1" + "\n");
                int delay = 25; if (testeCauteloso) delay = 50;

                Comunicação.AjustarEscalaVertical(canalTensão, 2, delay, true);
                tempoAjusteCanalA = DateTime.Now - tempoInicial;

                tempoInicial = DateTime.Now;
                Debug.WriteIf(debug, "SetEscalaDeVertical 2 " + "\n");
                Comunicação.AjustarEscalaVertical(canalCorrente, 2, delay, true);
                tempoAjusteCanalB = DateTime.Now - tempoInicial;
                #endregion
            }

            Thread.Sleep(50);
            
           
            int delayMillis = 5 + (int)((1 / frequencia) * 1000);

            Comunicação.InverterCanal(canalTensão, invertido1);
            Comunicação.InverterCanal(canalCorrente, invertido2);


            Debug.WriteIf(debug, "STOP");
            Comunicação.RunStop(false);
            Debug.WriteIf(debug, "RUN SINGLE");

            Comunicação.RunSingle();
            Thread.Sleep(10);

            Debug.Write("Inicio médias" + "\n");
            DateTime inicioMédias = DateTime.Now;
            bool runSingleRodando = true;
            while (runSingleRodando)
            {

                string resposta = Comunicação.InquerirOsciloscópio("ACQuire:AVERage:COMPlete?", true);
                if (resposta == "0\n")
                {
                    Thread.Sleep(delayMillis);
                    Debug.WriteIf(debug, "resposta: " + resposta + " delay: " + delayMillis.ToString() + "\n");
                }
                else if (resposta == "1\n") 
                {
                    runSingleRodando = false;
                }
            }
            TimeSpan tempoMédias = DateTime.Now - inicioMédias;
            Debug.Write($"Fim médias: {tempoMédias.TotalMilliseconds} ms" + "\n");

            Thread.Sleep(150);
            
            if (tipo == MediçãoTipo.Admitancia)
            {
                Debug.WriteIf(debug, "forma de onda 1" + "\n");

                CossenoCoeficientes formaDeOnda1 = new CossenoCoeficientes();
                CossenoCoeficientes formaDeOnda2 = new CossenoCoeficientes();

                int tentativaA = 0;
                int tentativaB = 0;
                bool sucessoA = false;
                bool sucessoB = false;

                TimeSpan tempoDownloadCanalA = new TimeSpan();
                TimeSpan tempoDownloadCanalB = new TimeSpan();
                try
                {
                    //formaDeOnda1 = ProcessamentoDeDados.RegressãoLinearCosseno(Comunicação.GetFormaDeOnda(canalTensão, frequencia));

                    DateTime inicioDownloadCanalA = DateTime.Now;
                    while (tentativaA < 3 && !sucessoA)
                    {
                        try
                        {
                                            

                            List<FormaDeOnda> ondas = Comunicação.GetFormasDeOnda(new List<CanalFonte> { CanalFonte.CH1, CanalFonte.CH2 }, frequencia, out TimeSpan[] t, false);
                            tempoDownloadCanalA = t[0];
                            tempoDownloadCanalB = t[1];

                            formaDeOnda1 = ProcessamentoDeDados.RegressãoLinearCosseno(ondas[0]);
                            formaDeOnda2 = ProcessamentoDeDados.RegressãoLinearCosseno(ondas[1]);
                            sucessoA = true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            tentativaA++;
                            Thread.Sleep(100);
                        }
                    }
                     

                    Debug.WriteIf(debug, "forma de onda 2\n\n");

                    if (!usaShunt)
                    {
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

                        PontoDeMedição medição = new(admitancia, fase, frequencia, 0,
                            Comunicação.GetTensãoDePicoMedida(canalTensão),
                            Comunicação.GetTensãoDePicoMedida(canalCorrente));

                        Debug.WriteLine((new DateTime(DateTime.Now.Ticks - tempo.Ticks)).Second + " segundos\n");
                        DetalhesMedição detalhesMedição = new DetalhesMedição(tempoDownloadCanalA, tempoDownloadCanalB, tempoMédias, new TimeSpan(2000), tempoAjusteCanalA, tempoAjusteCanalB, DateTime.Now - tempo, tentativaA, tentativaB, sucessoA, sucessoB);
                        medição.DetalhesMedição = detalhesMedição;
                        return medição;
                    }                    
                    else 
                    {
                        double real1 = formaDeOnda1.Amplitude * Math.Cos(formaDeOnda1.Fase * Math.PI / 180D);
                        double imaginario1 = formaDeOnda1.Amplitude * Math.Sin(formaDeOnda1.Fase * Math.PI / 180D);

                        Complex tensãoNaSaidaDaFonte = new Complex(real1, imaginario1);

                        double real2 = formaDeOnda2.Amplitude * Math.Cos(formaDeOnda2.Fase * Math.PI / 180D);
                        double imaginario2 = formaDeOnda2.Amplitude * Math.Sin(formaDeOnda2.Fase * Math.PI / 180D);

                        Complex tensãoShunt = new Complex(real2, imaginario2);

                        Complex tensãoTransformador = tensãoNaSaidaDaFonte - tensãoShunt;

                        Complex correnteShunt = tensãoShunt / Rshunt;

                        Complex admitanciaTransformador = correnteShunt / tensãoTransformador; 

                        PontoDeMedição medição = new(admitanciaTransformador.Magnitude, admitanciaTransformador.Phase, frequencia, 0,
                            Comunicação.GetTensãoDePicoMedida(canalTensão),
                            Comunicação.GetTensãoDePicoMedida(canalCorrente));

                        Debug.WriteLine((new DateTime(DateTime.Now.Ticks - tempo.Ticks)).Second + " segundos\n");

                        DetalhesMedição detalhesMedição = new DetalhesMedição(tempoDownloadCanalA, tempoDownloadCanalB, tempoMédias, new TimeSpan(2000), tempoAjusteCanalA, tempoAjusteCanalB, DateTime.Now - tempo, tentativaA, tentativaB, sucessoA, sucessoB);

                        medição.DetalhesMedição = detalhesMedição;
                        return medição;
                    }
                }
                catch (Exception e)
                {
                    if (e is ErroDeTransferência)
                    {
                        PontoDeMedição medição = new PontoDeMedição(-1, 0, frequencia, 1, 0, 0);
                        medição.DetalhesMedição = new DetalhesMedição(tempoDownloadCanalA, tempoDownloadCanalB, tempoMédias, new TimeSpan(2000), tempoAjusteCanalA, tempoAjusteCanalB, DateTime.Now - tempo, tentativaA, tentativaB, sucessoA, sucessoB);

                        return medição;
                    }
                    else if (e is ArgumentException)
                    {
                        Debug.WriteLine(e.ToString());
                        
                        PontoDeMedição medição = new PontoDeMedição(-1, 0, frequencia, 1, -2, -2);
                        medição.DetalhesMedição = new DetalhesMedição(tempoDownloadCanalA, tempoDownloadCanalB, tempoMédias, new TimeSpan(2000), tempoAjusteCanalA, tempoAjusteCanalB, DateTime.Now - tempo, tentativaA, tentativaB, sucessoA, sucessoB);
                        return medição;
                    }
                }
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
        public static bool VarreduraDeFrequencia(CanalFonte canalTensao, CanalFonte canalCorrente,bool invertido1, bool invertido2, double tensaoDePico, int offset, List<double> frequencias, bool testeCauteloso, bool shunt, double resistencia, int numMédias, CanalFonte gatilho)
        {
            pontosDeMedição.Clear();

            Comunicação.AlterarSinalDoGerador("SIN", frequencias[0], tensaoDePico, Tensão.Vpp, offset, true);
            //Comunicação.AutoSet();
            //Thread.Sleep(2000);
            int j = 0;
            foreach (double i in frequencias)
            {

                bool ajusteFino = false;
                if (j == 0)
                {
                    ajusteFino = true;
                }
                else
                {
                    ajusteFino = Comunicação.MudouDeDecada(frequencias[j - 1], i);
                }
                
                try
                {
                    // COLOCAR TODOS ESSES PARAMETROS COMO CONFIGURACOES PREVIAS
                    Comunicação.ConfigurarAquisiçãoOsciloscópio(10, 10000, numMédias, AquisiçãoModo.Médias);
                    Comunicação.AlterarSinalDoGerador("SIN", i, tensaoDePico, Tensão.Vpp , offset, true);
                    pontosDeMedição.Add(RealizarMediçãoFrequencia(MediçãoTipo.Admitancia, canalTensao, canalCorrente, invertido1, invertido2, testeCauteloso, shunt, resistencia, gatilho, ajusteFino));
                }
                catch(IOTimeoutException timeout)
                {
                    Debug.WriteIf(debug, $"Varredura de frequencia: Timeout: {timeout.Message}" + "\n");
                }
                j++;
            }
            Comunicação.AlterarSinalDoGerador("SIN", 10, tensaoDePico, Tensão.Vpp, offset, false);      
            
            for(int i = 0; i < 5; i++) 
            {
                Comunicação.AlterarSinalDoGerador("SIN", 10, 0, Tensão.Vpp, offset, false);
                Thread.Sleep(100);
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
            if (pontosDeMedição != null)
            {
                foreach (var ponto in pontosDeMedição)
                {
                    bool erroDeComunicação = ponto.houveErro == 1;
                    bool erroDeMedição = !double.IsFinite(ponto.EscalaVerticalTensao);
                    bool admitanciaNegativa = ponto.Admitancia < 0;

                    if (erroDeComunicação || erroDeMedição || admitanciaNegativa)
                    {
                        frequencias.Add(ponto.Frequencia);
                    }
                }
                return frequencias;
            }
            else
            {
                return new List<double>();
            }
        }
        public static void PrepararOscilosCópioParaImpulso(double janela, double offset,int medias, List<Canal> canaisAtivos)
        {
            Comunicação.RunStop(true);            
            Comunicação.ConfigurarAquisiçãoOsciloscópio(1, 10000, medias, AquisiçãoModo.Médias);
            Comunicação.AutoSet();
            //Thread.Sleep(1200);

            Comunicação.SetEscalaDeTempo(janela, offset);

            foreach (var canal in canaisAtivos)
            {
                Comunicação.AjustarEscalaVertical(canal.Fonte, 3, 25, true);
            }
        }

        public static RespostaAoImpulso MedirRespostaImpulsiva(ParametrosTesteImpulsivo parametros, bool TelaAjustada)
        {
            //ativar sinal
            Comunicação.AlterarSinalDoGerador(TiposDeOnda.TipoParaString(parametros.FunçãoTipo), 
                                              parametros.Frequencia,
                                              parametros.Amplitude, 
                                              parametros.TensãoTipo,
                                              parametros.Offset,
                                              true);
            Comunicação.SetEscalaDeTempo(parametros.JanelaDeMedição, 1);
            Comunicação.ConfigurarAquisiçãoOsciloscópio(1, 10000, parametros.NumeroDeMédias, AquisiçãoModo.Médias);

            Comunicação.RunSingle();
            Thread.Sleep(10);

            int delayMillis = 5 + (int)((1 / parametros.Frequencia) * 1000);

            bool runSingleRodando = true;
            while (runSingleRodando)
            {
                string resposta = Comunicação.InquerirOsciloscópio("ACQuire:AVERage:COMPlete?", true);
                if (resposta == "0\n")
                {
                    Thread.Sleep(delayMillis);
                    Debug.WriteIf(debug, "resposta: " + resposta + " delay: " + delayMillis.ToString() + "\n");
                }
                else if (resposta == "1\n")
                {
                    runSingleRodando = false;
                }
            }

            

            foreach (Canal canal in parametros.CanaisUsados)
            {
                canal.FormaDeOnda = Comunicação.GetFormaDeOnda(canal.Fonte, parametros.Frequencia);
            }

            RespostaAoImpulso respostaAoImpulso = new RespostaAoImpulso(parametros);

            return respostaAoImpulso;
        }
        /// <summary>
        /// SUBSTITUIR PARAMETROS DO GERADOR POR VALORES MALEAVEIS
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
                    Comunicação.AlterarSinalDoGerador("SIN", ponto, 10, Tensão.Vpp, 0, true);
                    Comunicação.ConfigurarAquisiçãoOsciloscópio(10, 10000, 16, AquisiçãoModo.Médias);
                    pontosRefeitos.Add(RealizarMediçãoFrequencia(parametros.MediçãoTipo, parametros.CanalFonte1, parametros.CanalFonte2, parametros.Canal1Invertido, parametros.Canal2Invertido, true, parametros.UsaShunt, parametros.ResistenciaShunt, parametros.Gatilho, false));
                }
                catch (IOTimeoutException timeout)
                {
                    Debug.WriteIf(debug, $"Varredura de frequencia: Timeout: {timeout.Message}");
                }                
            }
            return pontosRefeitos;
        }


        /// <summary>
        /// essa função é burrice, dá pra criar um construtor para parametros de medição que receba RespostaEmFrequencia como argumento
        /// </summary>
        /// <param name="teste"></param>
        /// <returns></returns>
        public static ParametrosDaMedição GetParametrosDeMedição(RespostaEmFrequência teste)
        {
            return new ParametrosDaMedição(teste.CanalFonte1, teste.CanalFonte2,teste.Canal1Invertido ,teste.Canal2Invertido ,teste.AtenuaçãoCanalFonte1, teste.AtenuaçãoCanalFonte2,teste.TensãoGerador, 0, "SIN", 10, 10000, teste.NúmeroDeMédias, teste.PontosPorDecada, MediçãoTipo.Admitancia, teste.TesteShunt, teste.ResistenciaShunt, teste.FonteGatilho);
        }
    }
}
