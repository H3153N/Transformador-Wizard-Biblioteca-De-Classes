using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class Ensaio
    {
        //TITULO;tipo(0,1,2);canalTensao(1,2,3,4);canalCorrente(1,2,3,4);AtenuacaoTensao;AtenuacaoCorrente
        //                                                                X1, X10, X100, X1000

        //TITULO;1;1;1;10X;100X;1-2-3-4-5-6


        //TITULO;TIPO;DATE-TIME
        public string Path { get; private set; } = string.Empty;
        public ObservableCollection<Teste> Testes { get; private set; } = [];
        public string Titulo { get; private set; } = string.Empty;
        public DateTime Criação { get; private set; } = DateTime.MinValue;
        public string NomePasta { get; private set; }

        public EnsaioTipo Tipo { get; set; }


        /// <summary>
        /// cria um Ensaio a partir de um arquivo
        /// </summary>
        /// <param name="path">caminho absoluto</param>
        public Ensaio(string path)
        {
            Path = path;
            path = path.Split('\\').Last();
            NomePasta = path;
            var dados = path.Split(';');

            Titulo = dados[0];
            Tipo = (EnsaioTipo)Enum.Parse(typeof(EnsaioTipo), dados[1]);
            Criação = new DateTime(long.Parse(dados[2]));

            Testes = GerenciadorDeArquivos.GetTestes(Path);
        }  
        public Ensaio(string titulo, EnsaioTipo tipo, DateTime dateTime)
        {
            Titulo = titulo;
            Tipo = tipo;
            Criação = dateTime;

            NomePasta = titulo + ';' + ((int)tipo).ToString() + ';' + dateTime.Ticks.ToString();
        }

        /// <summary>
        /// retorna uma string com o nome padrão para salvar a pasta
        /// </summary>
        /// <returns></returns>
        public void AtualizarTestes()
        {            
            Testes.Clear();           
            Testes = GerenciadorDeArquivos.GetTestes(Path);
        }
    }
}
