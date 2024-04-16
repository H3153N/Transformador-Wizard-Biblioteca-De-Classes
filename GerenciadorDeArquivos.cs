using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        public static ObservableCollection<Teste> GetTestes(string pastaTestes)
        {
            
            ObservableCollection<Teste> testes = [];
            string[] testesStrings = Directory.GetFiles(pastaTestes);
            foreach (string testesString in testesStrings)
            {
                //comentario;datetime;random

                testes.Add(new Teste(testesString));
            }
            return testes;
        }

        public static void AddPontoEmTeste(PontoDeMedição ponto, string pathTeste)
        {
            File.AppendAllText(pathTeste, ponto.Frequencia.ToString() + ';' + ponto.Admitancia.ToString() + ';' + ponto.Fase.ToString() + Environment.NewLine);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">caminho absoluto do arquivo contendo os dados do teste</param>
        /// <returns></returns>
        public static List<PontoDeMedição> GetPontosSalvos(string path)
        {
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
                    Debug.WriteLine("GET PONTOS SALVOS ERRO: " + e.Message);
                }                
            }

            return todosOsPontos;
        }
        
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
        public static void SalvarTeste(Teste teste, string localEnsaio)
        {
            string caminho = Path.Combine(localEnsaio, teste.NomeArquivo);
            File.Create(caminho).Close();
            
        }

        public static void SalvarDados(Teste teste,string localEnsaio)
        {
            string caminho = Path.Combine(localEnsaio, teste.NomeArquivo);
            foreach (var item in GerenciadorDeTestes.pontosDeMedição)
            {
                string  linha  = item.Frequencia.ToString(CultureInfo.InvariantCulture)             + '\t';
                        linha += item.Admitancia.ToString(CultureInfo.InvariantCulture)             + '\t';
                        linha += item.Fase.ToString(CultureInfo.InvariantCulture)                   + '\t';
                        linha += item.houveErro.ToString(CultureInfo.InvariantCulture)              + '\t';
                        linha += item.EscalaVerticalTensao.ToString(CultureInfo.InvariantCulture)   + '\t';
                        linha += item.EscalaVerticalCorrente.ToString(CultureInfo.InvariantCulture) + Environment.NewLine;


                File.AppendAllText(caminho, linha);
            }
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
