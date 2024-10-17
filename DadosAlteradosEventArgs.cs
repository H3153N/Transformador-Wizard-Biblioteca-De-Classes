using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class DadosAlteradosEventArgs : EventArgs
    {
        public int Index { get; private set; }

        public DadosAlteradosEventArgs(int index)
        {
            Index = index;
        }
    }
}
