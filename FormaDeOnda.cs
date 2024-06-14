using OxyPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Biblioteca
{
    public class FormaDeOnda
    {
        public string? HeaderString { get; set; }

        /// <summary>
        /// Dupla contendo duas listas, a primeira contém a forma de onda e a segunda contém os pontos de tempo
        /// </summary>
        public Tuple<double[], double[]> Dados { get; set; } 
        public List<DataPoint> Traço { get; set; }

        public double XStart { get; set; }
        public double XStop { get; set; }
        public int NumeroDePontos { get; set; }
        public int? NumValoresPorAmostra { get; set; }
        public double Frequencia { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="dados"></param>
        public FormaDeOnda(string header, string dados, double frequencia)
        {
            Frequencia = frequencia;

            HeaderString = header;

            var headerDados = header.Split(',');
            XStart = double.Parse(headerDados[0], CultureInfo.InvariantCulture);
            XStop = double.Parse(headerDados[1], CultureInfo.InvariantCulture);
            NumeroDePontos = int.Parse(headerDados[2]);
            NumValoresPorAmostra = int.Parse(headerDados[3]);

            var numerosStrings = dados.Split(',');
            List<double> doubles = new List<double>();


            foreach (var numerosString in numerosStrings)
            {

                try
                {
                    doubles.Add(double.Parse(numerosString, CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw new ErroDeTransferência { FrequenciaErro = frequencia };
                }
            }
            

            List<double> pontosDeTempo = new List<double>();

            double deltaT = (XStop - XStart) / (NumeroDePontos - 1);

            for (int i = 0; i < NumeroDePontos; i++)
            {
                pontosDeTempo.Add(deltaT * i + XStart);
            }

            Dados = Tuple.Create(doubles.ToArray(), pontosDeTempo.ToArray());
        }
        public FormaDeOnda(string header, string dados)
        {
            HeaderString = header;

            var headerDados = header.Split(',');
            XStart = double.Parse(headerDados[0], CultureInfo.InvariantCulture);
            XStop = double.Parse(headerDados[1], CultureInfo.InvariantCulture);
            NumeroDePontos = int.Parse(headerDados[2]);
            NumValoresPorAmostra = int.Parse(headerDados[3]);

            var numerosStrings = dados.Split(',');
            List<double> doubles = new List<double>();

            foreach (var numerosString in numerosStrings)
            {

                try
                {
                    doubles.Add(double.Parse(numerosString, CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw new ErroDeTransferência {  };
                }
            }


            List<double> pontosDeTempo = new List<double>();

            double deltaT = (XStop - XStart) / (NumeroDePontos - 1);

            for (int i = 0; i < NumeroDePontos; i++)
            {
                pontosDeTempo.Add(deltaT * i + XStart);
            }

            Dados = Tuple.Create(doubles.ToArray(), pontosDeTempo.ToArray());
        }
        public FormaDeOnda()
        {

        }

        public void CriarPontos()
        {
            if (Traço == null)
            {
                Traço = new List<DataPoint>();
            }
            else
            {
                Traço.Clear();
            }

            int tamanho = Dados.Item1.Length;

            for (int i = 0; i < tamanho; i++)
            {
                Traço.Add(new DataPoint(Dados.Item2[i], Dados.Item1[i]));
            }
        }
    }
}
