using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ivi.Visa;

namespace Biblioteca
{
    public class Comunicação
    {
        public static string EndereçoGeradorLAN { get; private set; } = "TCPIP::192.168.0.109::INSTR";
        public static string EndereçoOsciloscopioLAN { get; private set; } = "TCPIP::192.168.0.118::INSTR";

        public static IMessageBasedSession? ConexãoGeradoFunções { get; private set; }
        public static IMessageBasedSession? ConexãoOsciloscópio { get; private set; }
        public static IVisaSession? sessãoVisaGerador { get; private set; }
        public static IVisaSession? sessãoVisaOsciloscópio { get; private set; }
        public static Version? visaNetSharedComponentsVersão { get; private set; } = typeof(GlobalResourceManager).Assembly.GetName().Version;
        public static int IniciarConexãoTimeout { get; private set; } = 2000;


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
        public double EscalaDeTempo
        {
            get { return GetEscalaDeTempo(); }
        }
        public static bool ConectarOsciloscópio()
        {

            //REMOVER, USAR CATCH PARA PEGAR TIMEOUT E RESOURCE LOCKED
            if(ConexãoOsciloscópio != null && (ConexãoOsciloscópio.ResourceLockState == ResourceLockState.ExclusiveLock))
            {
                return true;
            }
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
            catch(VisaException e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        public static bool ConectarGerador()
        {
            if (ConexãoGeradoFunções != null && (ConexãoGeradoFunções.ResourceLockState == ResourceLockState.ExclusiveLock))
            {
                return true;
            }
            try
            {
                sessãoVisaGerador = GlobalResourceManager.Open(EndereçoGeradorLAN, AccessModes.ExclusiveLock, IniciarConexãoTimeout);
                if (sessãoVisaGerador is IMessageBasedSession connGerador)
                {
                    ConexãoGeradoFunções = connGerador;
                    ConexãoGeradoFunções.TerminationCharacterEnabled = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (VisaException e)
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
                    ConexãoGeradoFunções = connGerador;
                    ConexãoGeradoFunções.TerminationCharacterEnabled = true;
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
        /// <param name="tensãoDePicoMedida">tensão de referencia para inicio dos ajustes. a tensão sendo aplicada pelo gerador de tensão</param>
        /// <param name="canalDaTensão">canal do osciloscópio</param>
        /// <param name="numeroIterações">numero de ajustes feitos, mais ajustes levam a medições mais precisas porém aumentam o tempo das medições de maneira significativa, recomendado de 3 a 5</param>
        /// <exception cref="Exception">especificar</exception>
        public static void AjustarEscalaVerticalTensão(double tensãoDePicoMedida, CanalFonte canalDaTensão, int numeroIterações)
        {
            SetOffset(canalDaTensão, 0);
            // envia comando ajustando a escala preliminar com base no valor aplicado pelo gerador de funções
                
            List<string> escalasAnteriores = new List<string>();
            escalasAnteriores.Add(GetEscalaVerticalString(tensãoDePicoMedida));

            ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canalDaTensão).ToString()}:SCALe {escalasAnteriores.First()}");
            
            Thread.Sleep(100);
            for (int i = 0; i < numeroIterações; i++)
            {
                Thread.Sleep(75);
                // reajusta a escala várias vezes com base em novas medições da tensão de pico
                    string escalaAtual = GetEscalaVerticalString(GetTensãoDePicoMedida(canalDaTensão));
                float escala = float.Parse(escalaAtual);

                bool táNaMargem = float.Parse(escalasAnteriores.Last()) < escala*1.1f && float.Parse(escalasAnteriores.Last()) > escala * 0.9f;
                if (táNaMargem)
                {
                    break;
                }
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canalDaTensão).ToString()}:SCALe {escalaAtual}");

                var vppMedido = GetTensãoDePicoMedida(canalDaTensão);
                
                if (vppMedido < 20)
                {
                    escalasAnteriores.Add(escalaAtual);
                }
                else
                {
                    ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canalDaTensão).ToString()}:SCALe {escalasAnteriores.Last()}");
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleCount"></param>
        /// <param name="pointsValue"></param>
        /// <param name="averageCount"></param>
        /// <exception cref="Exception"></exception>
        public static void ConfigurarAquisiçãoOscilosóopio(int singleCount, int pointsValue, int averageCount, AquisiçãoModo modo)
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine("FORMat:DATA ASCii, 0");
            ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:POINts:VALue {pointsValue}");

            if (modo == AquisiçãoModo.AltaResolução)
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:HRESolution AUTO");
            }
            else if(modo == AquisiçãoModo.Médias)

            {
                ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:HRESolution OFF");
                ConexãoOsciloscópio.FormattedIO.WriteLine("ACQuire:TYPE AVERage");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:NSINgle:COUNt {singleCount}");
                ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:AVERage:COUNt {averageCount}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequencia"></param>
        /// <exception cref="Exception"></exception>
        public static void AlterarFrequenciaDoGerador(double frequencia)
        {
                ConexãoGeradoFunções.FormattedIO.WriteLine($"FREQuency {frequencia.ToString("E1")}");
        }
        public static double GetFrequenciaNoGerador()
        {
            ConexãoGeradoFunções.FormattedIO.WriteLine("FREQuency?");
            return double.Parse(ConexãoGeradoFunções.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
        }
        public static string AlterarSinalDoGerador(string formaDeOnda, double frequencia, double amplitude, double offset, bool ativarSaida)
        {
            
                string query = frequencia.ToString("G").Replace(',', '.') + "," + amplitude.ToString("G").Replace(',', '.')+ "," + offset.ToString("G").Replace(',', '.');
                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{formaDeOnda} {query}");
                if (ativarSaida)
                {
                    ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }
                return "0";
        }
        public static string SetEscalaDeTempo()
        {
                double escalaDeTempo = 4 * (1 / FrequenciaAplicada) / 12;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',','.')}");
                return "0";
        }
        double GetEscalaDeTempo()
        {
                ConexãoOsciloscópio.FormattedIO.WriteLine("TIMebase:SCALe?");
                return double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
        }
        public static double GetTensãoDePicoMedida(CanalFonte fonteDoSinal)
        { 
                ConexãoOsciloscópio.FormattedIO.WriteLine($"MEASurement2:SOURce {fonteDoSinal.ToString()}");
                ConexãoOsciloscópio.FormattedIO.WriteLine("MEASurement2:MAIN UPE");
                ConexãoOsciloscópio.FormattedIO.WriteLine("MEASurement2:RESult:ACTual? UPE");

                return double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// retorna a escala em volts por divisão presumindo que o pico do sinal deve estar no topo da quarta divisão da tela do osciloscópio
        /// </summary>
        /// <param name="tensaoDePico">tensão de pico Vp+ do sinal, RMS ou média resultarão em erros</param>
        /// <returns>string no formato 1.2e+3 </returns>
        static string GetEscalaVerticalString(double tensaoDePico)
        {
            return (tensaoDePico * 1 / 4).ToString("G").Replace(',', '.');
        }
        public static void SetOffset(CanalFonte canal, double offset)
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:OFFSet {offset.ToString("G").Replace(',', '.')}");
        }
        public static void SetNivelDeTrigger(CanalFonte canalDeTrigger, double nivel)
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine($"TRIGger:A:LEVel{((int)canalDeTrigger).ToString()}:VALue{nivel.ToString("G").Replace(',', '.')}");
        }
        public static FormaDeOnda GetFormaDeOnda(CanalFonte canal, double frequencia)
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

            return new FormaDeOnda(header, dados, frequencia);
        }

        public static string InquerirOsciloscópio(string pergunta, bool EsperarResposta)
        {
            try
            {
                ConexãoOsciloscópio.FormattedIO.WriteLine(pergunta);
                if (EsperarResposta)
                {
                    return ConexãoOsciloscópio.FormattedIO.ReadLine();
                }
                return "-1";
            }
            catch (Exception e )
            {
                return e.Message;   
            }
        }
        public static void RunSingle()
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine($"RUNSingle");
        }
        public static void RunStop(bool run)
        {
            if (run) { ConexãoOsciloscópio.FormattedIO.WriteLine("RUN");}
            else { ConexãoOsciloscópio.FormattedIO.WriteLine("STOP"); }
        }
        public static void AutoSet() 
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine("AUToscale");
        }
        public static void SetAtenuação(CanalFonte canal, Atenuação atenuação)
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine($"PROBe<{(int)canal}>:SETup:ATTenuation:MANual {(int)atenuação}");
        }
    }
}
