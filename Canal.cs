using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class Canal
    {
        public Canal()
        {
        }

        public Canal(string nome, Gatilho gatilho, CanalFonte fonte, Atenuação atenuação, bool fonteTrigger, bool shunt, double shuntValor, FormaDeOnda formaDeOnda)
        {
            Nome = nome;
            Gatilho = gatilho;
            Fonte = fonte;
            Atenuação = atenuação;
            FonteTrigger = fonteTrigger;
            Shunt = shunt;
            ShuntValor = shuntValor;
            FormaDeOnda = formaDeOnda;
        }

        public Canal(string nome, CanalFonte fonte, Atenuação atenuação, bool fonteTrigger)
        {
            Nome = nome;            
            Fonte = fonte;
            Atenuação = atenuação;
            FonteTrigger = fonteTrigger;                 
        }

        public FormaDeOnda FormaDeOnda  { get; set; }
        public string Nome              { get; set; } = string.Empty;
        public Gatilho Gatilho          { get; set; }
        public CanalFonte Fonte         { get; set; }
        public Atenuação Atenuação      { get; set; }
        public bool FonteTrigger        { get; set; }
        public bool Shunt               { get; set; }
        public double ShuntValor        { get; set; }
        
    }
}
