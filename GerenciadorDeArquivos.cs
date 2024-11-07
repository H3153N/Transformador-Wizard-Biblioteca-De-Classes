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
        public static async Task<List<Dispositivo>> GetDispositivos(string[] dispStrings)
        {
            List<Task<Dispositivo>> dispositivosTask = [];

            foreach (var caminho in dispStrings)
            {
                dispositivosTask.Add(Task.Run(()=> new Dispositivo(caminho)));
            }

            List<Dispositivo>  dispositivos = new((await Task.WhenAll(dispositivosTask)));

            return dispositivos;
        }
        public static List<Ensaio> GetEnsaios(string pastaEnsaios)
        {
            List<Ensaio> ensaios = [];
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

        public static string GetStringFormaDeOndaArbitrária(string path)
        {
            try
            {
                string dados = File.ReadAllText(path).Replace(",", ".").Replace("\r\n", ",");
                return dados.TrimEnd(',');
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void ExcluirArquivo(string path)
        {
            if (File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            
        }
    }
}
