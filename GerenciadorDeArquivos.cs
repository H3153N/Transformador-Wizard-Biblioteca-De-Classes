using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OxyPlot;

namespace Biblioteca
{

    public static class GerenciadorDeArquivos
    {
        //public static string Caminho = "C:\\Users\\Pedro Miranda\\Desktop\\pasta_teste";
        public static readonly string PastaMestre = "Transformador-Wizard";
        public static readonly string PastaDados = PastaMestre + "\\Dados";
        public static readonly string PastaDispositivos = PastaDados + "\\Dispositivos";
        public static readonly string PastaConfigurações = PastaMestre + "\\Config";

        //TEMPORARIO -> REMOVER ANTES DE RL 2.0
        public static readonly string ArquivoConexões = PastaConfigurações + "\\conexões.txt";
        public static ObservableCollection<Dispositivo> GetDispositivos(string pastaMestre)
        {
            ObservableCollection<Dispositivo> dispositivos = [];
            string[] dispStrings = Directory.GetDirectories(pastaMestre);

            foreach (string dispString in dispStrings)
            {
                dispositivos.Add(new Dispositivo(dispString));
            }
            return dispositivos;
        }
        public static ObservableCollection<Ensaio> GetEnsaios(string pastaEnsaios)
        {
            ObservableCollection<Ensaio> ensaios = [];
            string[] ensaioStrings =Directory.GetDirectories(pastaEnsaios);

            foreach (string ensaioString in ensaioStrings )
            {
                ensaios.Add(new Ensaio(ensaioString));
            }
            return ensaios;
        }
        public static List<T> GetTeste<T>(string pastaTestes)
        {
            List<T> testes = new List<T>();
            string[] arquivos = Directory.GetFiles(pastaTestes);

            foreach (var arquivo in arquivos)
            {
                string dados = File.ReadAllText(arquivo);
                T teste = JsonConvert.DeserializeObject<T>(dados);
                testes.Add(teste);
            }
            return testes;
        }


        //será inutilizado
        
        public static List<PontoDeMedição> MergirPontosCorrigidosNaListaOriginal(List<PontoDeMedição> pontosAlterados, List<PontoDeMedição> pontosOriginais)
        {
            List<int> indices = new List<int>();

            if (pontosOriginais != null && pontosAlterados != null)
            {
                for (int i = 0; i < pontosOriginais.Count; i++)
                {
                    for (int j = 0; j < pontosAlterados.Count; j++)
                    {
                        if (pontosOriginais[i].Frequencia == pontosAlterados[j].Frequencia)
                        {
                            indices.Add(i);
                        }
                    }
                }

                for (int i = 0; i < pontosAlterados.Count; i++)
                {
                    pontosOriginais[indices[i]] = pontosAlterados[i];
                }

                return pontosOriginais;
            }
            else
            {
                return new List<PontoDeMedição> { };
            }
            
        }
        public static void SalvarDispositivo(Dispositivo dispositivo)
        {
            string caminho = System.IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), PastaDispositivos);
            if (Path.Exists(caminho)!)
            {
                Directory.CreateDirectory(caminho);
            }
            Directory.CreateDirectory(Path.Combine(caminho, dispositivo.ArquivoNome));
            
        }
        public static void SalvarEnsaio(Ensaio ensaio, string local)
        {
            string caminho = Path.Combine(local, ensaio.NomePasta);
            Directory.CreateDirectory(caminho);
        }

        //MUDAR PARA "CRIAR TESTE"?
        public static void CriarTeste(RespostaEmFrequência teste, string localEnsaio)
        {
            string caminho = Path.Combine(localEnsaio, teste.NomeArquivo);
            File.Create(caminho).Close();            
        }

        


        /// <summary>
        /// NAO ERA PRA PRECISAR DE UMA STRING COM O LOCAL DO ENSAIO, ESSA INFORMACAO JA TA NO TESTE
        /// </summary>
        /// <param name="teste"></param>
        /// <param name="localEnsaio"></param>
        public static void SalvarDados(RespostaEmFrequência teste,string localEnsaio)
        {
            string caminho = Path.Combine(localEnsaio, teste.NomeArquivo);
            teste.Path = caminho;
            string dados = JsonConvert.SerializeObject(teste);
            Salvar(dados, caminho);
        }

        public static void SalvarDados(RespostaEmFrequência teste)
        {
            string dados = JsonConvert.SerializeObject(teste);
            Salvar(dados, teste.Path);
        }

        public static void SalvarDados(RespostaAoImpulso teste)
        {
            string dados = JsonConvert.SerializeObject(teste);
            Salvar(dados, Path.Combine(teste.Path, teste.NomeArquivo));
        }

        static void Salvar(string dados, string caminho)
        {
            if (!File.Exists(caminho))
            {
                File.Create(caminho).Close();
            }
            File.WriteAllText(caminho, dados);
        }

        public static Tuple<string, string> LerVisaString()
        {
            string caminho = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ArquivoConexões);
            if (!File.Exists(caminho))
            {
                string pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), PastaConfigurações);
                Directory.CreateDirectory(pasta);
                File.Create(caminho).Close();
            }
            try
            {
                string dados = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ArquivoConexões));
                Tuple<string, string> tuple = Tuple.Create(dados.Split('\n')[0].TrimEnd('\r'), dados.Split('\n')[1].TrimEnd('\r'));
                return tuple;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return (Tuple.Create("TCPIP::192.168.0.0::INSTR", "TCPIP::192.168.0.1::INSTR"));
            }
        }
       
        public static void SalvarVisaString(string osciloscopio, string gerador)
        {
            string caminho = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ArquivoConexões);
            if (!File.Exists(caminho))
            {
                File.Create(caminho).Close();
            }
            string texto = osciloscopio + Environment.NewLine + gerador;
            File.WriteAllText(caminho, texto);
        }

        public static List<double> GetPontosFormaDeOndaArbitrária(string path)
        {
            List<double> numeros = new List<double>();
            string dados = File.ReadAllText(path).Replace(",",".");
            string[] valores = dados.Split("\r\n");
            foreach (var item in valores)
            {
                if (double.TryParse(item, CultureInfo.InvariantCulture, out double numero))
                {
                    numeros.Add(numero);
                }
            }
            return numeros;
        }

        public static string GetStringFormaDeOndaArbitrária(string path)
        {
            try
            {
                string dados = File.ReadAllText(path).Replace(",", ".").Replace("\r\n", ",");
                // string[] valores = dados.Split("\r\n");

                return dados.TrimEnd(',');
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
