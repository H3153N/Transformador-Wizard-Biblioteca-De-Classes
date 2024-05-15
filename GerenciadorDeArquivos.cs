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
        public static ObservableCollection<RespostaEmFrequência> GetRespostaEmFrequência(string pastaTestes)
        {
            ObservableCollection<RespostaEmFrequência> testes = [];
            string[] testesStrings = Directory.GetFiles(pastaTestes);
            foreach (string testesString in testesStrings)
            {
                //comentario;datetime;random
                try
                {
                    string dados = File.ReadAllText(testesString);
                    testes.Add(JsonConvert.DeserializeObject<RespostaEmFrequência>(dados));
                }
                catch (Exception e )
                {
                    Debug.WriteLine(e);
                    throw;
                }
                
            }
            return testes;



            /*
            ObservableCollection<RespostaEmFrequência> testes = [];
            string[] testesStrings = Directory.GetFiles(pastaTestes);
            foreach (string testesString in testesStrings)
            {
                //comentario;datetime;random

                testes.Add(new RespostaEmFrequência(testesString));
            }
            return testes;
            */
        }


        //será inutilizado
        public static List<PontoDeMedição> GetPontosSalvos(string path)
        {

            return null;

            /*
            List<string> linhas = File.ReadAllLines(path).ToList();
            List<PontoDeMedição> todosOsPontos = new List<PontoDeMedição>();

            foreach (var linhaDeDados in linhas)
            {
                try
                {
                    string[] argumentos = linhaDeDados.Split('\t');

                    double admitancia = double.Parse(argumentos[1], CultureInfo.InvariantCulture);
                    double fase =       double.Parse(argumentos[2], CultureInfo.InvariantCulture);
                    double frequencia = double.Parse(argumentos[0], CultureInfo.InvariantCulture);

                    int houveErro =     int.Parse(argumentos[3], CultureInfo.InvariantCulture);

                    double escalaTensao =   double.Parse(argumentos[4], CultureInfo.InvariantCulture);
                    double escalaCorrente = double.Parse(argumentos[5], CultureInfo.InvariantCulture);

                    todosOsPontos.Add(new PontoDeMedição(admitancia, fase, frequencia, houveErro, escalaTensao, escalaCorrente));
                }
                catch (Exception e)
                {
                    throw;
                    Debug.WriteLine("GET PONTOS SALVOS ERRO: " + e.Message);
                }                
            }

            return todosOsPontos;

            */
        }
        public static List<PontoDeMedição> MergirPontosCorrigidosNaListaOriginal(List<PontoDeMedição> pontosAlterados, List<PontoDeMedição> pontosOriginais)
        {
            List<int> indices = new List<int>();

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
        //será inutilizado
        public static void SalvarPontosCorrigidos(List<PontoDeMedição> pontosAlterados, List<PontoDeMedição>pontosOriginais, RespostaEmFrequência teste)
        {
            List<int> indices = new List<int>();

            for (int i = 0;  i < pontosOriginais.Count; i++)
            {
                for(int j = 0; j < pontosAlterados.Count; j++)
                {
                    if (pontosOriginais[i].Frequencia == pontosAlterados[j].Frequencia)
                    {
                        indices.Add(i);
                    }
                }
            }

            for (int i = 0;i < pontosAlterados.Count ;i++)
            {
                pontosOriginais[indices[i]] = pontosAlterados[i];
            }

            GerenciadorDeTestes.pontosDeMedição.Clear();


            foreach (var item in pontosOriginais)
            {
                GerenciadorDeTestes.pontosDeMedição.Add(item);
            }

            //TA MUITO BAGUNÇADO, REMOVER VARIAVEL ESTATICA NA CLASSE GERENCIADOR DE TESTE, PADRONIZAR PARAMETROS DAS FUNCOES
            //SALVAR OS DADOS NAO DEVE DEPENDER DA CLASSE DE TESTES

            List<string> path = teste.Path.Split('\\').ToList();
            path.RemoveAt(path.Count - 1);
            string localEnsaio = String.Join("\\", path);

            SalvarDados(teste, localEnsaio);
        }
        //será inutilizado
        public static Task<ModuloFase> GetPontos(string path)
        {            
            List<DataPoint> modulo = new List<DataPoint>();
            List<DataPoint> fase = new List<DataPoint>();
            
               
            using (var sr = new StreamReader(path))
            {
                String line;
                while((line = sr.ReadLine()) != null)
                {
                    string[] strings = line.Replace(',','.').Split("\t");
                    modulo.Add(new DataPoint(double.Parse(strings[0], CultureInfo.InvariantCulture), double.Parse(strings[1], CultureInfo.InvariantCulture)));
                    fase.Add(new DataPoint(double.Parse(strings[0], CultureInfo.InvariantCulture), double.Parse(strings[2], CultureInfo.InvariantCulture)));
                }
            }    
            ModuloFase moduloFase = new ModuloFase(modulo, fase);
            
            return Task.FromResult(moduloFase);
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
            Salvar(dados, teste.Path);
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

        
    }
}
