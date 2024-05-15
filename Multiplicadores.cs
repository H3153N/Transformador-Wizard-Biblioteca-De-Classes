using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public static class Multiplicadores
    {
        public static double GetMultiplicador(MultiplicadorPositivo multiplicadorPositivo)
        {
            return Math.Pow(10, 3 * ((int)multiplicadorPositivo));
        }

        public static double GetMultiplicador(MultiplicadorNegativo multiplicadorPositivo)
        {
            return Math.Pow(10, -3 * ((int)multiplicadorPositivo));
        }

        public static double RMS_Vpp_DB(Tensão tensão)
        {
            if (tensão == Tensão.Vpp)
            {
                return 1;
            }
            if (tensão == Tensão.Vrms)
            {
                return 1 * Math.Sqrt(2);
            }
            else
            {
                return 1;
            }
        }
    }
}
