using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Biblioteca
{
    public class Dispositivo//  \\NOME;DIMENSAO
    {
        public int NumTestes { get; private set; }
        public string Titulo { get; private set; }
        public string ArquivoNome { get; private set; }  
        public string Path{ get; private set; } = string.Empty;
        public int Dimensao { get; private set; }

        public List<Ensaio> Ensaios { get; private set; } = [];

        public Dispositivo(string path)
        {
            ArquivoNome = path.Split("\\").ToList().Last();
            Titulo = ArquivoNome.Split(";")[0];
            Dimensao = int.Parse(ArquivoNome.Split(";")[1]);    
            Path = path;
            
            Ensaios = GerenciadorDeArquivos.GetEnsaios(path);
            NumTestes = Ensaios.Count;
            ArquivoNome = Titulo + ";" + Dimensao;
        }        

        public Dispositivo(string titulo, int dimensao) 
        {
            Titulo = titulo;
            Dimensao = dimensao;
            ArquivoNome = Titulo + ";" + Dimensao;
        }

        public void AtualizarEnsaios()
        {
            Ensaios.Clear();
            Ensaios = GerenciadorDeArquivos.GetEnsaios(Path);
        }
        public Tuple<int, int> GetEnsaiosETestes()
        {
            int numTotalTestes = 0;

            foreach (Ensaio item in Ensaios)
            {
                numTotalTestes += item.Testes.Count;
            }
            return Tuple.Create(Ensaios.Count, numTotalTestes);
        }
    }
}
