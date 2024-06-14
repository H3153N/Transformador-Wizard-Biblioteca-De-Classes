using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public static class ProcessamentoDeDados
    {
        public static CossenoCoeficientes RegressãoLinearCosseno(FormaDeOnda formaDeOnda)
        {
            try
            {
                double omega = 2 * Math.PI * formaDeOnda.Frequencia;
                double[] parametros = Fit.LinearCombination(formaDeOnda.Dados.Item2, formaDeOnda.Dados.Item1,
                    d => 1.0, d => Math.Sin(d * omega), d => Math.Cos(d * omega));

                double termoIndependente = parametros[0];
                double Vmax = Math.Sqrt(Math.Pow(parametros[1], 2) + Math.Pow(parametros[2], 2));

                double fase = Math.Atan2(-parametros[1], parametros[2]) * 180 / Math.PI;

                Debug.WriteLine($"Regressao linear: {Vmax}V,  {fase} graus");
                return new CossenoCoeficientes(termoIndependente, Vmax, fase);
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine(ae.Message);
                throw;
            }
            
        }
    }
}
