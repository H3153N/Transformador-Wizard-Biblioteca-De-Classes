using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class CorreçãoCompletaEventArgs : EventArgs
    {
        public int Index { get; private set; }

        public CorreçãoCompletaEventArgs(int index)
        {
            Index = index;
        }
    }
}
