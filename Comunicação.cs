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
                sessãoVisaOsciloscópio = GlobalResourceManager.Open(EndereçoOsciloscopioLAN, AccessModes.None, IniciarConexãoTimeout);
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
        public static void AjustarEscalaVertical(CanalFonte canal, int numeroIterações, int delayEntreAjustes, bool ajustarOffset)
        {
            int numeroDeQuadrados = 4;
            int numTentativasReiterativas = 5;

            //delay = 25ms
            if (ajustarOffset)
            {
                SetOffsetVertical(canal, 0);
            }
            
            Debug.WriteLine("Set offset");

            Thread.Sleep(delayEntreAjustes);

            for (int i = 0; i < numeroIterações; i++)
            {
                Debug.WriteLine($"ITERACAO {i}," + canal.ToString());
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
            Thread.Sleep(2000);

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

        public static void SetEscalaVertical(CanalFonte canal, double tensãoDePico, int numeroDeQuadrados)
        {
            if (ConexãoOsciloscópio != null)
            {
                string EscalaString = (tensãoDePico / numeroDeQuadrados).ToString("G").Replace(',', '.');
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe {EscalaString}");
            }
        }
        public static void SetEscalaVertical(CanalFonte canal, double escala)
        {
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
                ConexãoOsciloscópio.FormattedIO.WriteLine($"CHANnel{((int)canal).ToString()}:SCALe?");
                double escala = double.Parse(ConexãoOsciloscópio.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
                return escala;
            }
            else
            {
                return double.NaN;
            }
        }
        public static double GetRazãoPicoEscala(CanalFonte canal)
        {
            if (ConexãoOsciloscópio != null)
            {
                return GetTensãoDePicoMedida(canal) / GetEscalaVertical(canal);
            }
            else { return double.NaN; }
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
                ConexãoGeradoFunções?.FormattedIO.WriteLine($"FREQuency {frequencia.ToString("E1")}");
        }
        public static double GetFrequenciaNoGerador()
        {
            if (ConexãoGeradoFunções != null)
            {
                ConexãoGeradoFunções.FormattedIO.WriteLine("FREQuency?");
                return double.Parse(ConexãoGeradoFunções.FormattedIO.ReadLine(), CultureInfo.InvariantCulture);
            }
            else
            {
                return double.NaN;
            }
        }
        public static void AlterarSinalDoGerador(string formaDeOnda, double frequencia, double amplitude, Tensão tensão, double offset, bool ativarSaida)
        {

            if (ConexãoGeradoFunções != null)
            {
                string query = frequencia.ToString("G").Replace(',', '.') + "," + amplitude.ToString("G").Replace(',', '.') + " " + tensão.ToString().ToUpper() + "," + offset.ToString("G").Replace(',', '.');
                ConexãoGeradoFunções.FormattedIO.WriteLine($"APPLy:{formaDeOnda} {query}");
                if (ativarSaida)
                {
                    ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }
            }    
        }

        public static void AlterarSinalDoGerador(Função função, ParametrosTesteImpulsivo  parametros)
        {
            string onda = TiposDeOnda.TipoParaString(função);
            if (ConexãoGeradoFunções != null)
            {
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

                
            }
        }
        public static void SetEscalaDeTempo()
        {
                double escalaDeTempo = 4 * (1 / FrequenciaAplicada) / 12;
                ConexãoOsciloscópio?.FormattedIO.WriteLine($"TIMebase:SCALe {escalaDeTempo.ToString("G").Replace(',','.')}");                
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

        double GetEscalaDeTempo()
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
                ConexãoOsciloscópio.FormattedIO.WriteLine($"MEASurement2:SOURce {fonteDoSinal.ToString()}");
                ConexãoOsciloscópio.FormattedIO.WriteLine("MEASurement2:MAIN UPE");
                ConexãoOsciloscópio.FormattedIO.WriteLine("MEASurement2:RESult:ACTual? UPE");

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
            else
            {
                throw new ErroDeTransferência();
            }
        }

        public static FormaDeOnda GetFormaDeOnda(CanalFonte canal)
        {
            try
            {
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

                    return new FormaDeOnda(header, dados);
                }
                else
                {
                    throw new ErroDeTransferência();
                }
            }
            catch (Ivi.Visa.IOTimeoutException)
            {

                throw;
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
            catch (Exception e )
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
            if (run) { ConexãoOsciloscópio?.FormattedIO.WriteLine("RUN");}
            else { ConexãoOsciloscópio?.FormattedIO.WriteLine("STOP"); }
        }
        public static void AutoSet() 
        {
            ConexãoOsciloscópio?.FormattedIO.WriteLine("AUToscale");
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
            if (ConexãoGeradoFunções != null)
            {
                if (ligadoDesligado)
                {
                    ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 1");
                }
                if (!ligadoDesligado)
                {
                    ConexãoGeradoFunções.FormattedIO.WriteLine("OUTPut:STATe 0");
                }
            }
        }



        /// <summary>
        /// Ativa os canais listados, desativa o restante
        /// </summary>
        /// <param name="canaisParaAtivar"></param>
        public static void AlterarCanal(List<CanalFonte> canaisParaAtivar)
        {
            List<CanalFonte> todosOsCanais = new List<CanalFonte>() { CanalFonte.CH1, CanalFonte.CH2, CanalFonte.CH3, CanalFonte.CH4 };

            foreach (var canal in todosOsCanais)
            {
                if (canaisParaAtivar.Contains(canal))
                {
                    AlternarCanal(canal, true);
                }
                else
                {
                    AlternarCanal(canal, false);
                }
            }
        }
        /// <summary>
        /// Envia forma de onda para memória volátil
        /// </summary>
        /// <param name="pontos"></param>
        public static void SetFormaDeOndaArbitrária(double[] pontos)
        {
            if (ConexãoGeradoFunções != null)
            {
                string dados = "DATA VOLATILE";
                foreach (var ponto in pontos)
                {
                    dados += ","+ponto.ToString().Replace(",", ".");
                }
                ConexãoGeradoFunções.FormattedIO.WriteLine(dados);
            }
        }

        public static void SetFormaDeOndaArbitrária(string pontosFormatados)
        {
            if (ConexãoGeradoFunções != null)
            {
                string dados = "DATA VOLATILE";
                dados += "," + pontosFormatados;
                ConexãoGeradoFunções.FormattedIO.WriteLine(dados);
            }

        }
    }
}
