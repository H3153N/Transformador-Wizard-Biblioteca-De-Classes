using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class ParametrosTesteImpulsivo
    {
        public string Comentário { get; set; }
        public Função FunçãoTipo {  get; set;} 
        public double Frequencia { get; set; }        
        public double Amplitude  { get; set; }
        public Tensão TensãoTipo { get; set; }
        public double Offset     { get; set; }

        public int NumeroDeMédias           { get; set; }//para o osciloscopio
        public double JanelaDeMedição       { get; set; }
        public double Gatilho { get; set; }
        public int Amostras { get; set; }
        public List<Canal> CanaisUsados     { get; set; } = new List<Canal>();

        public int numeroDePulsos { get; set; }//para o gerador

        public double CicloDeTrabalho { get; set; } // 0 a 1, para pulso e onda quadrada
        public double Simetria { get; set; } // 0 a 1, para onda triangular
        


        ///PARA PULSO
        public double LarguraDoPulso    { get; set; }
        public double TempoDeQuina      { get; set; }        
        public double Periodo           { get; set; }
        public double TempoEntrePulsos  { get; set; }
    }
}
