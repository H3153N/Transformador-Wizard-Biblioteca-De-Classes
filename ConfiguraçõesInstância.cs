using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class ConfiguraçõesInstância
    {
        public readonly string ArquivoConfiguraçãoNome = "Configurações.json";
        public string EndereçoOsciloscópio { get; set; } = string.Empty;
        public string EndereçoGerador { get; set; } = string.Empty;
        public int PontosPorDecadaPadrão { get; set; } = 10;
    }
}
