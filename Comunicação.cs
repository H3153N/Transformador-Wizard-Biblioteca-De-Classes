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
                sessãoVisaGerador = GlobalResourceManager.Open(EndereçoGeradorLAN, AccessModes.None, IniciarConexãoTimeout);
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
        /// <param name="valorDePicoMedido">tensão de referencia para inicio dos ajustes. a tensão sendo aplicada pelo gerador de tensão</param>
        /// <param name="canal">canal do osciloscópio</param>
        /// <param name="numeroIterações">numero de ajustes feitos, mais ajustes levam a medições mais precisas porém aumentam o tempo das medições de maneira significativa, recomendado de 3 a 5</param>
        /// <exception cref="Exception">especificar</exception>
        public static void AjustarEscalaVertical(CanalFonte canal, int numeroIterações, int delayEntreAjustes)
        {
            int numeroDeQuadrados = 4;
            int numTentativasReiterativas = 5;
            double escalaMaxima = 6; // V/V
            //delay = 25ms
            SetOffset(canal, 0);
            Debug.WriteLine("Set offset");

            Thread.Sleep(delayEntreAjustes);

            for (int i = 0; i < numeroIterações; i++)
            {
                Debug.WriteLine("ITERACAO 1," + canal.ToString());
                #region TENSAO OK
                double escalaVertical = GetEscalaVertical(canal);
                double tensaoDePico = GetTensãoDePicoMedida(canal);
                double numQuadrados = tensaoDePico / escalaVertical;

                bool valorOK = numQuadrados < 4.5 && numQuadrados > 1;
                bool aprovado = numQuadrados < 4.5 && numQuadrados > 2;

                if (valorOK)
                {
                    Debug.WriteLine("VALOR OK");
                    SetEscalaVertical(canal, GetTensãoDePicoMedida(canal), numeroDeQuadrados);
                }
                if (aprovado)
                {
                    Debug.WriteLine("APROVADO");
                    break;
                }
                #endregion

                #region SE TENSAO MUITO GRANDE
                if (!valorOK && OndaForaDaTela(canal, out _))
                {
                    Debug.WriteLine("Onda Fora da Tela");

                    int numTentativas = 0;

                    if (escalaVertical < 0.05)
                    {
                        SetEscalaVertical(canal, 2);
                    }
                    //diminui a escala até que o sinal se encontre dentro da tela novamente
                    while (OndaForaDaTela(canal, out double pico) && numTentativas < numTentativasReiterativas)
                    {
                        Debug.WriteLine($"tentativa: {numTentativas}, valor de pico: {pico} V/V");
                        SetEscalaVertical(canal, (4f/3f)*GetEscalaVertical(canal));                        
                        Thread.Sleep(delayEntreAjustes);
                        numTentativas++;
                    }

                    /*
                    if(!OndaForaDaTela(canal,out double valorDePicoAtual))
                    {
                        Debug.WriteLine($"tentativas terminadas");
                        // depois que a onda já está na tela, ajusta para metade da escala desejada (assim para evitar erros de imprecisão), 
                        // para que depois possa ser reajustado para o valor certo
                        SetEscalaVertical(canal, 2 * valorDePicoAtual, numeroDeQuadrados);
                        Debug.WriteLine($"valor de pico: {valorDePicoAtual} V/V");
                    }
                    */
                }
                #endregion

                #region SE TENSAO MUITO PEQUENA
                if (!valorOK && GetTensãoDePicoMedida(canal) <= 1.5 || escalaVertical > 25)
                {
                    Debug.WriteLine($"Tensao muito pequena");
                    int numTentativas = 0;
                    //diminui a escala até que o sinal ocupe 2 quadrados
                    if (escalaVertical > 40)
                    {
                        SetEscalaVertical(canal, 4);
                    }
                    while (GetRazãoPicoEscala(canal)<1 && numTentativas < numTentativasReiterativas)
                    {
                        Debug.WriteLine($"tentativa: {numTentativas}");
                        SetEscalaVertical(canal, (1f/2f)* GetEscalaVertical(canal));
                        Thread.Sleep(delayEntreAjustes);
                    }
                }
                #endregion
            }
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
            ConexãoOsciloscópio.FormattedIO.WriteLine("FORMat:DATA ASCii, 0");
            ConexãoOsciloscópio.FormattedIO.WriteLine($"ACQuire:POINts:VALue {pontosAdquiridos}");

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

        public static void SetEscalaVertical(CanalFonte canal, double tensãoDePico, int numeroDeQuadrados)
        {
            string EscalaString = (tensãoDePico / numeroDeQuadrados).ToString("G").Replace(',', '.');
            ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe {EscalaString}");
        }
        public static void SetEscalaVertical(CanalFonte canal, double escala)
        {
            Debug.WriteLine($"Set Escala : {Math.Round(escala, 3)} V/V");
            string EscalaString = (escala).ToString("G").Replace(',', '.');
            ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe {EscalaString}");
        }
        public static double GetEscalaVertical(CanalFonte canal)
        {
            ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe?");
            double escala = double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
            return escala;
        }
        public static double GetRazãoPicoEscala(CanalFonte canal)
        {
            return GetTensãoDePicoMedida(canal) / GetEscalaVertical(canal);
        }
        public static bool OndaForaDaTela(CanalFonte canal, out double valorDePico)
        {
            valorDePico = GetTensãoDePicoMedida(canal);
            bool a = valorDePico > 9E+10;
            return a;
        }
        public static bool OndaForaDaTela(double valorDePico)
        {
            return !double.IsFinite(valorDePico);
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
        public static void AlterarSinalDoGerador(string formaDeOnda, double frequencia, double amplitude, Tensão tensão, double offset, bool ativarSaida)
        {
            
                string query = frequencia.ToString("G").Replace(',', '.') + "," + amplitude.ToString("G").Replace(',', '.') +" " + tensão.ToString().ToUpper() + "," + offset.ToString("G").Replace(',', '.');
                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{formaDeOnda} {query}");
                if (ativarSaida)
                {
                    ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }                

        }

        public static void AlterarSinalDoGerador(Função função, ParametrosTesteImpulsivo  parametros)
        {
            string onda = TiposDeOnda.TipoParaString(função);

            string query = parametros.Frequencia.ToString("G").Replace(',', '.') + "," + parametros.Amplitude.ToString("G").Replace(',', '.') + " " + parametros.TensãoTipo.ToString().ToUpper() + "," + parametros.Offset.ToString("G").Replace(',', '.');
            if (função == Função.Senoidal)
            {
                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
            }
            
            if (função == Função.Triangular)
            {
                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
                ConexãoGeradoFunções.FormattedIO.WriteLine($"FUNCtion:RAMP:SYMMetry {parametros.Simetria.ToString("G").Replace(',', '.')}");                
            }
            if (função == Função.Pulso)
            {
                query = parametros.Frequencia.ToString("G").Replace(',', '.') + 
                    "," + parametros.Amplitude.ToString("G").Replace(',', '.') + " " 
                    + parametros.TensãoTipo.ToString().ToUpper() + "," + parametros.Offset.ToString("G").Replace(',', '.');


                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{onda} {query}");
                ConexãoGeradoFunções.FormattedIO.WriteLine($"PULSe:PERiod {parametros.Periodo.ToString("G").Replace(',', '.')}");
                ConexãoGeradoFunções.FormattedIO.WriteLine($"FUNCtion:PULSe:DCYCle {parametros.CicloDeTrabalho.ToString("G").Replace(',', '.')}");
                ConexãoGeradoFunções.FormattedIO.WriteLine($"FUNCtion:PULSe:TRANsition {parametros.TempoDeQuina.ToString("G").Replace(',', '.')}");
            }

            ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
        }
        public static void SetEscalaDeTempo()
        {
                double escalaDeTempo = 4 * (1 / FrequenciaAplicada) / 12;
                ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',','.')}");                
        }

        public static void SetEscalaDeTempo(double janelaDeMedição, double QuadradosOffset)
        {
            double escalaDeTempo = janelaDeMedição / 12;
            ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',','.')}");

            double offset = janelaDeMedição / 2 - escalaDeTempo * QuadradosOffset;
            ConexãoOsciloscópio.FormattedIO.WriteLine($"TIMebase:POSition {offset.ToString("G").Replace(',', '.')}");
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

        public static FormaDeOnda GetFormaDeOnda(CanalFonte canal)
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

            return new FormaDeOnda(header, dados);
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
