using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class MediçãoEventArgs : EventArgs
    {
        public PontoDeMedição Medição { get; set; }

        public MediçãoEventArgs(PontoDeMedição medição)
        {
            Medição = medição;
        }
    }
}
