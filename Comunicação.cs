using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ivi.Visa;

namespace Biblioteca
{
    public class Comunicação
    {
        public static string EndereçoGeradorLAN { get; private set; } = "TCPIP::192.168.0.100::INSTR";
        public static string EndereçoOsciloscopioLAN { get; private set; } = "TCPIP::192.168.0.101::INSTR";

        public static IMessageBasedSession? ConexãoGeradorFunções { get; private set; }
        public static IMessageBasedSession? ConexãoOsciloscópio { get; private set; }
        public static IVisaSession? sessãoVisaGerador { get; private set; }
        public static IVisaSession? sessãoVisaOsciloscópio { get; private set; }
        public static Version? visaNetSharedComponentsVersão { get; private set; } = typeof(GlobalResourceManager).Assembly.GetName().Version;
        public static int IniciarConexãoTimeout { get; private set; } = 2000;

        public static bool debug = true;


        public static void ConfigurarConexões(string osciloscopioString, string geradorString, int timeout)
        {
            EndereçoGeradorLAN = geradorString;
            EndereçoOsciloscopioLAN = osciloscopioString;
            IniciarConexãoTimeout = timeout;
        }
        public static double FrequenciaAplicada
        {
            get { return GetFrequenciaNoGerador(); }
            set { AlterarFrequenciaDoGerador(value); }
        }
        public static double EscalaDeTempo
        {
            get { return GetEscalaDeTempo(); }
        }
        public static double JanelaDeTempo
        {
            get { return 12 * GetEscalaDeTempo(); }
        }

        public static int Amostras
        {
            get { return int.Parse(InquerirOsciloscópio("ACQuire:POINts:VALue?", true)); }
        }

        public static int NúmeroDeMédias
        {
            get { return int.Parse(InquerirOsciloscópio("ACQuire:AVERage:COUNt?", true)); }
        }
        public static bool ConectarOsciloscópio()
        {

            //REMOVER, USAR CATCH PARA PEGAR TIMEOUT E RESOURCE LOCKED

            try
            {
                sessãoVisaOsciloscópio = GlobalResourceManager.Open(EndereçoOsciloscopioLAN, AccessModes.ExclusiveLock, IniciarConexãoTimeout);
                if (sessãoVisaOsciloscópio is IMessageBasedSession connOsciloscópio)
                {
                    ConexãoOsciloscópio = connOsciloscópio;
                    ConexãoOsciloscópio.TerminationCharacterEnabled = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (VisaException e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        public static bool ConectarGerador()
        {
            if (ConexãoGeradorFunções != null && (ConexãoGeradorFunções.ResourceLockState == ResourceLockState.ExclusiveLock))
            {
                return true;
            }
            try
            {
                sessãoVisaGerador = GlobalResourceManager.Open(EndereçoGeradorLAN, AccessModes.None, IniciarConexãoTimeout);
                if (sessãoVisaGerador is IMessageBasedSession connGerador)
                {
                    ConexãoGeradorFunções = connGerador;
                    ConexãoGeradorFunções.TerminationCharacterEnabled = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (VisaException)
            {
                return false;
            }
        }
        public static async Task<bool> ConectarGeradorAsync()
        {
            return await Task.Run(() => ConectarGerador());
        }
        public static async Task<bool> ConectarOsciloscópioAsync()
        {
            return await Task.Run(() => ConectarOsciloscópio());
        }




        public static string IniciarConexões()
        {
#if NET5_0_OR_GREATER
            // Preloading installed VISA implementation assemblies for NET 5+
            GacLoader.LoadInstalledVisaAssemblies();
#endif
            try
            {
                sessãoVisaGerador = GlobalResourceManager.Open(EndereçoGeradorLAN, AccessModes.ExclusiveLock, 10000);
                if (sessãoVisaGerador is IMessageBasedSession connGerador)
                {
                    ConexãoGeradorFunções = connGerador;
                    ConexãoGeradorFunções.TerminationCharacterEnabled = true;
                }
                else
                {
                    return "Gerador: Not a message based session";
                }
                sessãoVisaOsciloscópio = GlobalResourceManager.Open(EndereçoOsciloscopioLAN, AccessModes.ExclusiveLock, 10000);

                if (sessãoVisaOsciloscópio is IMessageBasedSession connOsciloscópio)
                {
                    ConexãoOsciloscópio = connOsciloscópio;
                    ConexãoOsciloscópio.TerminationCharacterEnabled = true;
                }
                else
                {
                    return "Osciloscópio: Not a message based session";
                }
                return "0";
            }
            catch (Exception exception)
            {
                if (exception is TypeInitializationException && exception.InnerException is DllNotFoundException)
                {
                    // VISA Shared Components is not installed.
                    return $"VISA implementation compatible with VISA.NET Shared Components {visaNetSharedComponentsVersão} not found. Please install corresponding vendor-specific VISA implementation first.";
                }
                else if (exception is VisaException && exception.Message == "No vendor-specific VISA .NET implementation is installed.")
                {
                    // Vendor-specific VISA.NET implementation is not available.
                    return $"VISA implementation compatible with VISA.NET Shared Components {visaNetSharedComponentsVersão} not found. Please install corresponding vendor-specific VISA implementation first.";
                }
                else if (exception is EntryPointNotFoundException)
                {
                    // Installed VISA Shared Components are not compatible with VISA.NET Shared Components.
                    return $"Installed VISA Shared Components version {visaNetSharedComponentsVersão} does not support VISA.NET. Please upgrade VISA implementation.";
                }
                else
                {
                    // Handle remaining errors.
                    return $"Exception: {exception.Message}";
                }
            }
        }

        /// <summary>
        /// Ajusta a escala vertical do canal de Tensão (não funciona para o canal da corrente)
        /// </summary>
        /// <param name="valorDePicoMedido">tensão de referencia para inicio dos ajustes. a tensão sendo aplicada pelo gerador de tensão</param>
        /// <param name="canal">canal do osciloscópio</param>
        /// <param name="numeroIterações">numero de ajustes feitos, mais ajustes levam a medições mais precisas porém aumentam o tempo das medições de maneira significativa, recomendado de 3 a 5</param>
        /// <exception cref="Exception">especificar</exception>
        public static void AjustarEscalaVertical(CanalFonte canal, int numeroIterações, int delayEntreAjustes, bool ajustarOffset)
        {
            DateTime dateTime = DateTime.Now;
            double numeroDeQuadradosParaAjustar = 3.5;
            int numTentativasReiterativas = 20;

            Thread.Sleep(1 * delayEntreAjustes);

            //delay = 25ms
            if (ajustarOffset)
            {
                SetOffsetVertical(canal, 0);
            }

            double tensão = GetTensãoDePicoMedida(canal);
            double escala = GetEscalaVertical(canal);
            double quadrados = tensão / escala;

            bool ajusteAceitável = quadrados >= 2 && quadrados <= 4.5;



            for (int i = 0; i < numeroIterações; i++)
            {
                if (ajusteAceitável)
                {
                    break;
                }

                bool fora = OndaForaDaTela(canal, out double real_valor);

                tensão = real_valor;
                escala = GetEscalaVertical(canal);
                if (fora)
                {
                    escala = 3 * GetEscalaVertical(canal);
                    SetEscalaVertical(canal, escala);
                }

                quadrados = tensão / escala;
                ajusteAceitável = quadrados >= 2 && quadrados <= 4.5;


                if (ajusteAceitável)
                {
                    break;
                }

                if (quadrados < 1)
                {
                    int count = 0;
                    while (quadrados < 1 && count < numTentativasReiterativas)
                    {
                        double multiplicador = (1 / 3f);
                        if (count > 0)
                        {
                            multiplicador = (2 / 3f);
                        }
                        escala = multiplicador * GetEscalaVertical(canal);
                        SetEscalaVertical(canal, escala);
                        tensão = GetTensãoDePicoMedida(canal);

                        quadrados = tensão / escala;
                        ajusteAceitável = quadrados >= 2 && quadrados <= 4.5;
                        count++;
                    }

                    if (ajusteAceitável)
                    {
                        break;
                    }
                }

                //SetEscalaVertical(canal, tensão, numeroDeQuadradosParaAjustar);
                break;
            }

            Debug.WriteLine($"Ajuste canal {canal}, {(DateTime.Now - dateTime).TotalMilliseconds}ms");
        }
        public static void AjustarEscalaVerticalRígido(List<CanalFonte> canaisAtivos)
        {
            AutoSet();
            Thread.Sleep(1000);
            foreach (var canal in canaisAtivos)
            {                
                SetOffsetVertical(canal, 0);
                Thread.Sleep(150);
            }
            
            foreach (var canal in canaisAtivos)
            {
                Thread.Sleep(150);
                double tensãoDePico = GetTensãoDePicoMedida(canal);
                SetEscalaVertical(canal, tensãoDePico / 4);
            }
        }
        public static bool MudouDeDecada(double frequênciaAnterior, double proximaFrequência)
        {
            int ordem1 = (int)Math.Log(frequênciaAnterior);
            int ordem2 = (int)Math.Log(proximaFrequência);

            return ordem1 != ordem2;
        }
        public static void AjustarEscalaParaImpulso(List<Canal> canaisAtivos)
        {
            //autoset
            //offset vertical zero todos os canais
            //ajuste normal de escala todos os canais
            //offset de -Vp no canal de tensão
            //dobrar escala vertical no canal de tensão
            //escala de tempo
            //offset de tempo
            Comunicação.AutoSet();
            //Thread.Sleep(2000);

            foreach (var canal in canaisAtivos)
            {
                Comunicação.AjustarEscalaVertical(canal.Fonte, 2, 20, true);
                if (canal.FonteTrigger)
                {
                    double Vpp = Comunicação.GetTensãoDePicoMedida(canal.Fonte);
                    Comunicação.AjustarEscalaVertical(canal.Fonte, 2, 20, true);
                    Comunicação.SetOffsetVertical(canal.Fonte, -Vpp);
                    Comunicação.AjustarEscalaVertical(canal.Fonte, 2, 20, false);
                }
            }

            Thread.Sleep(500);
            Comunicação.SetEscalaDeTempo(400E-6, 0.2);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleCount"></param>
        /// <param name="pontosAdquiridos"></param>
        /// <param name="averageCount"></param>
        /// <exception cref="Exception"></exception>
        public static void ConfigurarAquisiçãoOsciloscópio(int singleCount, int pontosAdquiridos, int averageCount, AquisiçãoModo modo)
        {
            if (ConexãoOsciloscópio != null)
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine("FORMat:DATA ASCii, 0");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:POINts:VALue {pontosAdquiridos}");

                if (modo == AquisiçãoModo.AltaResolução)
                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:HRESolution AUTO");
                }
                else if (modo == AquisiçãoModo.Médias)

                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:HRESolution OFF");
                    ConexãoOsciloscópio.FormattedIO.WriteLine("ACQuire:TYPE AVERage");
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:NSINgle:COUNt {singleCount}");
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:AVERage:COUNt {averageCount}");
                }
            }
        }
        
        public static void SetEscalaVertical(CanalFonte canal, double escala)
        {
            escala = Math.Clamp(escala, 0, 5);
            if (ConexãoOsciloscópio != null)
            {
                Debug.WriteLine($"Set Escala : {Math.Round(escala, 3)} V/V");
                string EscalaString = (escala).ToString("G").Replace(',', '.');
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe {EscalaString}");
            }
        }
        public static double GetEscalaVertical(CanalFonte canal)
        {
            if (ConexãoOsciloscópio != null)
            {
                DateTime dateTime = DateTime.Now;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe?");
                double escala = double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
                //Debug.WriteLine($"Get escala vertical: {(DateTime.Now - dateTime).TotalMilliseconds}ms");
                return escala;
            }
            else
            {
                return double.NaN;
            }
        }
        public static bool OndaForaDaTela(CanalFonte canal, out double valorDePico)
        {
            if (ConexãoOsciloscópio != null)
            {
                valorDePico = GetTensãoDePicoMedida(canal);
                bool a = valorDePico > 9E+10;
                return a;
            }
            else
            {
                throw new Exception();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequencia"></param>
        /// <exception cref="Exception"></exception>
        public static void AlterarFrequenciaDoGerador(double frequencia)
        {
            ConexãoGeradorFunções?.FormattedIO.WriteLine($"FREQuency {frequencia.ToString("E1")}");
        }
        public static double GetFrequenciaNoGerador()
        {
            if (ConexãoGeradorFunções != null)
            {
                ConexãoGeradorFunções.FormattedIO.WriteLine("FREQuency?");
                return double.Parse(ConexãoGeradorFunções.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
            }
            else
            {
                return double.NaN;
            }
        }
        public static bool SaídaGeradorAtiva()
        {
            if (ConexãoGeradorFunções != null)
            {
                ConexãoGeradorFunções.FormattedIO.WriteLine("OUTPut?");
                string resposta = ConexãoGeradorFunções.FormattedIO.ReadLine();
                if (resposta == "0\n")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        public static void AlterarSinalDoGerador(string formaDeOnda, double frequencia, double amplitude, Tensão tensão, double offset, bool ativarSaida)
        {

            if (ConexãoGeradorFunções != null)
            {
                string query = frequencia.ToString("G").Replace(',', '.') + "," + amplitude.ToString("G").Replace(',', '.') + " " + tensão.ToString().ToUpper() + "," + offset.ToString("G").Replace(',', '.');
                ConexãoGeradorFunções.FormattedIO.WriteLine($"APPLy:{formaDeOnda} {query}");
                if (ativarSaida)
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }
                else
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine("OUTPut:STATe 0");
                }
            }
        }

        public static void AlterarSinalDoGerador(Função função, ParametrosTesteImpulsivo parametros)
        {
            string onda = TiposDeOnda.TipoParaString(função);
            if (ConexãoGeradorFunções != null)
            {
                string query = parametros.Frequencia.ToString("G").Replace(',', '.') + "," + parametros.Amplitude.ToString("G").Replace(',', '.') + " " + parametros.TensãoTipo.ToString().ToUpper() + "," + parametros.Offset.ToString("G").Replace(',', '.');
                if (função == Função.Senoidal)
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
                }

                if (função == Função.Triangular)
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"FUNCtion:RAMP:SYMMetry {parametros.Simetria.ToString("G").Replace(',', '.')}");
                }
                if (função == Função.Pulso)
                {
                    query = parametros.Frequencia.ToString("G").Replace(',', '.') +
                        "," + parametros.Amplitude.ToString("G").Replace(',', '.') + " "
                        + parametros.TensãoTipo.ToString().ToUpper() + "," + parametros.Offset.ToString("G").Replace(',', '.');


                    ConexãoGeradorFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"PULSe:PERiod {parametros.Periodo.ToString("G").Replace(',', '.')}");
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"FUNCtion:PULSe:DCYCle {parametros.CicloDeTrabalho.ToString("G").Replace(',', '.')}");
                    ConexãoGeradorFunções.FormattedIO.WriteLine($"FUNCtion:PULSe:TRANsition {parametros.TempoDeQuina.ToString("G").Replace(',', '.')}");
                }
            }
        }
        public static void SetEscalaDeTempo()
        {
            double escalaDeTempo = 4 * (1 / FrequenciaAplicada) / 12;
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',', '.')}");
        }
        public static void SetEscalaDeTempo(double janelaDeMedição, double QuadradosOffset)
        {
            if (ConexãoOsciloscópio != null)
            {
                double escalaDeTempo = janelaDeMedição / 12;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',', '.')}");

                double offset = janelaDeMedição / 2 - escalaDeTempo * QuadradosOffset;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:POSition {offset.ToString("G").Replace(',', '.')}");
            }
        }
        static double GetEscalaDeTempo()
        {
            if (ConexãoOsciloscópio != null)
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine("TIMebase:SCALe?");
                return double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
            }
            else
            {
                return double.NaN;
            }
        }
        public static double GetTensãoDePicoMedida(CanalFonte fonteDoSinal)
        {
            if (ConexãoOsciloscópio != null)
            {
                DateTime dateTime = DateTime.Now;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"MEASurement{(int)fonteDoSinal}:SOURce {fonteDoSinal.ToString()}");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"MEASurement{(int)fonteDoSinal}:MAIN UPE");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"MEASurement{(int)fonteDoSinal}:RESult:ACTual? UPE");

                Debug.WriteLine($"Get tensao de pico: {(DateTime.Now - dateTime).TotalMilliseconds}ms");

                return double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
            }
            else
            {
                return double.NaN;
            }
        }

        public static void SetOffsetVertical(CanalFonte canal, double offset)
        {
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:OFFSet {offset.ToString("G").Replace(',', '.')}");
        }

        public static void SetFonteTrigger(CanalFonte canalFonte)
        {
            if (ConexãoOsciloscópio != null)
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine($"TRIGger:A:SOURce {canalFonte.ToString()}");
                ConexãoOsciloscópio.FormattedIO.WriteLine("TRIGger:A:SOURce?");
                Debug.WriteLine($"Canal gatilho: {ConexãoOsciloscópio.FormattedIO.ReadLine()}");
            }
        }

        public static void EscolherGatilho(CanalFonte canal1, CanalFonte canal2, CanalFonte canalPadrão, double frequenciaAtual)
        {
            double amplitude1 = GetTensãoDePicoMedida(canal1);
            double amplitude2 = GetTensãoDePicoMedida(canal2);

            if (frequenciaAtual < 1000)
            {
                SetFonteTrigger(canalPadrão);
            }
            else 
            {
                if (amplitude1 > 1.05*amplitude2)
                    SetFonteTrigger(canal1);
                if (amplitude2 > 1.05*amplitude1)                
                    SetFonteTrigger(canal2);                
            }            
        }
        public static void SetNivelDeTrigger(CanalFonte canalDeTrigger, double nivel)
        {
            int n = (int)canalDeTrigger;
            string nivelS = nivel.ToString("G").Replace(',', '.');
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"TRIGger:A:LEVel{n}:VALue {nivelS}");
        }
        /// <summary>
        /// altera o numero de medições que o osciloscópio salva (record lenght)
        /// </summary>
        /// <param name="amostras">aceita 10k, 20k, 50k, 100k, 200k, 500k, 1M, 2M, 5M, 10M</param>
        public static void SetNumeroDeAmostras(int amostras)
        {
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"ACQuire:POINts:VALue {amostras}");
        }
        public static void SetNumeroDeMedias(int medias)
        {
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"ACQuire:AVERage:COUNt {medias}");
        }
        public static FormaDeOnda GetFormaDeOnda(CanalFonte canal, double frequencia)
        {
            if (ConexãoOsciloscópio != null)
            {
                DateTime dateTime = DateTime.Now;
                Debug.WriteIf(debug, $"Download onda canal {(int)canal}");

                string header;
                string dados;
                //Debug.WriteLine("GET FORMA DE ONDA");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA:HEADer?");
                header = ConexãoOsciloscópio.FormattedIO.ReadLine();
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA:POIN DEF");
                // Debug.WriteLine("DATA?");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA?");
                dados = ConexãoOsciloscópio.FormattedIO.ReadLine();

                if (debug)
                {
                    DateTime dateTime1 = DateTime.Now;
                    TimeSpan dateTime2 = dateTime1 - dateTime;
                    Debug.WriteLine($" - {dateTime2.TotalMilliseconds} ms\n");
                }

                return new FormaDeOnda(header, dados, frequencia);
            }
            else
            {
                throw new ErroDeTransferência();
            }
        }

        public static List<FormaDeOnda> GetFormasDeOnda(List<CanalFonte> canais, double frequencia, out TimeSpan[] tempos, bool maisTempo)
        {
            try
            {
                List<FormaDeOnda> formasDeOnda = new List<FormaDeOnda>();
                tempos = new TimeSpan[canais.Count];

                int i = 0;
                foreach (var canal in canais)
                {
                    DateTime dateTime = DateTime.Now;
                    string header = InquerirOsciloscópio($"CHAN{(int)canal}:DATA:HEAD?", true);
                    string dados = InquerirOsciloscópio($"CHAN{(int)canal}:DATA?", true);
                    tempos[i] = (DateTime.Now - dateTime);
                    formasDeOnda.Add(new FormaDeOnda(header, dados, frequencia));

                    if (maisTempo)
                    {
                        Thread.Sleep(400);
                    }
                    Thread.Sleep(100);
                }

                return formasDeOnda;
            }
            catch (Ivi.Visa.IOTimeoutException ea)
            {
                tempos = new TimeSpan[1];
                Debug.WriteLine("timeout download onda");
                return new List<FormaDeOnda>();
            }
        }

        public static bool TryGetFormasDeOndaLento(List<CanalFonte> canais, out List<FormaDeOnda> formasDeOnda)
        {
            try
            {
                Thread.Sleep(300);
                formasDeOnda = new List<FormaDeOnda>();

                foreach (var canal in canais)
                {
                    Debug.WriteLine($"CH{(int)canal}");


                    Debug.WriteLine("header");
                    string header = InquerirOsciloscópio($"CHAN{(int)canal}:DATA:HEAD?", true);
                    Debug.WriteLine("dados");
                    string dados = InquerirOsciloscópio($"CHAN{(int)canal}:DATA?", true);

                    bool deuTimeout = dados.Split(" ").First().Trim(' ') == "Timeout";

                    if (!deuTimeout)
                    {
                        formasDeOnda.Add((new FormaDeOnda(header, dados)));
                    }
                    else
                    {
                        Debug.WriteLine($"timeout CH{(int)canal}");
                    }
                    Debug.WriteLine("sleep 200");
                    Thread.Sleep(200);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                formasDeOnda = new List<FormaDeOnda>();
                return false;
            }
        }



        public static FormaDeOnda GetFormaDeOnda(CanalFonte canal)
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                Debug.WriteIf(debug, $"Download onda canal {(int)canal}");

                if (ConexãoOsciloscópio != null)
                {
                    string header;
                    string dados;
                    //Debug.WriteLine("GET FORMA DE ONDA");
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA:HEADer?");
                    header = ConexãoOsciloscópio.FormattedIO.ReadLine();
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA:POIN DEF");
                    // Debug.WriteLine("DATA?");
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:DATA?");
                    dados = ConexãoOsciloscópio.FormattedIO.ReadLine();

                    if (debug)
                    {
                        DateTime dateTime1 = DateTime.Now;
                        TimeSpan dateTime2 = dateTime1 - dateTime;
                        Debug.WriteLine($"Download canal {(int)canal}: {dateTime2.TotalMilliseconds} ms");
                    }

                    return new FormaDeOnda(header, dados);


                }
                else
                {
                    throw new ErroDeTransferência();
                }
            }
            catch (Ivi.Visa.IOTimeoutException ea)
            {

                Debug.WriteLine("timeout download onda");
                return new FormaDeOnda();
            }
        }

        public static string InquerirOsciloscópio(string pergunta, bool EsperarResposta)
        {
            try
            {
                if (ConexãoOsciloscópio != null)
                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine(pergunta);
                    if (EsperarResposta)
                    {
                        return ConexãoOsciloscópio.FormattedIO.ReadLine();
                    }
                    return "-1";
                }
                else
                {
                    return "-2";
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static void RunSingle()
        {
            ConexãoOsciloscópio?.FormattedIO.WriteLine($"RUNSingle");
        }
        public static void RunStop(bool run)
        {
            if (run) { ConexãoOsciloscópio?.FormattedIO.WriteLine("RUN"); }
            else { ConexãoOsciloscópio?.FormattedIO.WriteLine("STOP"); }
        }
        public static void AutoSet()
        {
            // Initial polling loop to demonstrate status before Autoset


            Debug.WriteIf(debug, "Autoset\n");
            DateTime t1 = DateTime.Now;
            ConexãoOsciloscópio?.FormattedIO.WriteLine("*CLS");
            ConexãoOsciloscópio?.FormattedIO.WriteLine("AUToscale");
            ConexãoOsciloscópio?.FormattedIO.WriteLine("*OPC");

            Thread.Sleep(800);

            try
            {
                string registroDeEstado = GetESR();
                int esrValor = Convert.ToInt32(registroDeEstado, 2);

                if ((esrValor & 1) == 1)
                {
                    Debug.WriteLine($"AutoSet Completo: {(DateTime.Now - t1).TotalMilliseconds}ms");
                }

            }
            catch (Exception ex)
            {

            }
        }

        public static List<CanalFonte> ToCanalFonte(List<Canal> canais)
        {
            List<CanalFonte> fontes = new List<CanalFonte>();
            foreach (var canal in canais)
            {
                fontes.Add(canal.Fonte);
            }
            return fontes;
        }

        public static void SetAtenuação(CanalFonte canal, Atenuação atenuação)
        {
            string mensagem = $"PROBe{(int)canal}:SETup:ATTenuation:MANual {(int)atenuação}";
            ConexãoOsciloscópio?.FormattedIO.WriteLine(mensagem);
        }
        public static void AlternarCanal(CanalFonte canal, bool ligadoDesligado)
        {
            if (ConexãoOsciloscópio != null)
            {
                string ligado = ligadoDesligado ? "ON" : "OFF";
                ConexãoOsciloscópio?.FormattedIO.WriteLine($"CHANnel{(int)canal}:STATe {ligado}");
            }
        }

        public static void AlternarSaídaGerador(bool ligadoDesligado)
        {
            if (ConexãoGeradorFunções != null)
            {
                if (ligadoDesligado)
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }
                if (!ligadoDesligado)
                {
                    ConexãoGeradorFunções.FormattedIO.WriteLine("OUTPut:STATe 0");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetESR()
        {
            if (ConexãoOsciloscópio != null)
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine("*ESR?");
                string statusByte = ConexãoOsciloscópio.FormattedIO.ReadLine();
                int numero = int.Parse(statusByte);
                statusByte = Convert.ToString(numero, 2).PadLeft(8, '0');

                return statusByte;
            }

            return "-1";
        }

        public static void SetFormaDeOndaArbitrária(string pontosFormatados)
        {
            try
            {
                if (ConexãoGeradorFunções != null)
                {
                    string dados = "DATA VOLATILE";
                    dados += "," + pontosFormatados;
                    ConexãoGeradorFunções.FormattedIO.WriteLine(dados);
                }
            }
            catch (Ivi.Visa.NativeVisaException e )
            {
                throw;
            }
        }

        public static void InverterCanal(CanalFonte canal, bool invertido)
        {
            if (ConexãoOsciloscópio != null)
            {
                if (invertido)
                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{(int)canal}:POLarity INVerted");
                }
                else
                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{(int)canal}:POLarity NORMal");
                }                
            }
        }

        public static void SalvarFormaDeOndaNoGerador()
        {
            if (ConexãoGeradorFunções != null)
            {
                ConexãoGeradorFunções.FormattedIO.WriteLine("DATA:COPY PROGRAMA, VOLATILE");                
                ConexãoGeradorFunções.FormattedIO.WriteLine("FUNC:USER PROGRAMA");
                ConexãoGeradorFunções.FormattedIO.WriteLine("APPLy:USER");
            }
        }
    }
}
