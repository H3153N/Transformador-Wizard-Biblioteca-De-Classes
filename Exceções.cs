using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class ErroDeTransferência : Exception
    {
        public ErroDeTransferência() : base() { }
        public ErroDeTransferência(string message) : base(message) { }
        public ErroDeTransferência(string message, Exception innerException) : base(message, innerException) { }

        public double FrequenciaErro {  get; set; }

    }
}
