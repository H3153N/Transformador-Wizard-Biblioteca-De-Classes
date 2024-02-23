using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Biblioteca
{
    public class FormaDeOnda
    {
        public string? HeaderString { get; private set; }

        /// <summary>
        /// Dupla contendo duas listas, a primeira contém a forma de onda e a segunda contém os pontos de tempo
        /// </summary>
        public Tuple<double[], double[]> Dados { get; private set; } 

        public double XStart { get; private set; }
        public double XStop { get; private set; }
        public int NumeroDePontos { get; private set; }
        public int? NumValoresPorAmostra { get; private set; }
        public double Frequencia { get; private set; }

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

                    var asda = "";
                    asda = numerosString.Replace("\u0001", "1");
                    asda = numerosString.Replace("\u0002", "2");
                    asda = numerosString.Replace("\u0003", "3");
                    asda = numerosString.Replace("\u0004", "4");
                    asda = numerosString.Replace("\u0005", "5");
                    asda = numerosString.Replace("\u0006", "6");
                    asda = numerosString.Replace("\u0007", "7");
                    asda = numerosString.Replace("\u0008", "8");
                    asda = numerosString.Replace("\u0009", "9");

                    asda = asda.Replace("\t", string.Empty);


                    

                    doubles.Add(double.Parse(asda, CultureInfo.InvariantCulture));
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


        public string GetPontosComoString()
        {
            string stringCompleta = "";


            stringCompleta += Dados.Item1[0].ToString("G").Replace(',', '.') +','+ Dados.Item2[0].ToString("G").Replace(',', '.');
            
            
            for (int i = 1; i < Dados.Item1.Length; i++)
            {
                stringCompleta += Dados.Item1[i].ToString("G").Replace(',', '.') + ',' + Dados.Item2[i].ToString("G").Replace(',', '.') + Environment.NewLine ;
            }
            return stringCompleta;
        }
    }
}
