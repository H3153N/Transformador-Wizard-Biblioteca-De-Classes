using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
            string caminho = System.IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), PastaMestre);
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
                File.AppendAllText(caminho, item.Frequencia.ToString(CultureInfo.InvariantCulture) + '\t' + item.Admitancia.ToString() + '\t' + item.Fase.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
            }
        }

        public static Tuple<string,string> LerVisaString()
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

        public static void SalvarVistaString(string osciloscopio, string gerador)
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
