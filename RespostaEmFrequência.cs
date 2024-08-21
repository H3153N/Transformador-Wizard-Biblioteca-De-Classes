using Newtonsoft.Json;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Biblioteca
{
    public class RespostaEmFrequência
    {
        

        public CanalFonte FonteGatilho { get; set; }
        public double TensãoGerador { get; set; }
        public string Path { get;  set; } = "";
        public ModuloFase ModuloFase { get;  set; } = new ModuloFase();
        public List<PontoDeMedição> medições { get; set; }
        public int NúmeroDeMédias { get; set; }
        public DateTime Criação { get;  set; }      
        public string Comentário { get; set; }
        public string Random { get;  set; }
        public string NomeArquivo { get; set; }

        private static Random random = new();

        public double ResistenciaShunt { get;  set; }
        public bool TesteShunt { get;  set; }

        #region canais


        //TROCAR PARA DOIS CAMPOS DE TIPO CANAL
        public CanalFonte CanalFonte1 { get;  set; }
        public CanalFonte CanalFonte2 { get;  set; }

        public Atenuação AtenuaçãoCanalFonte1 { get;  set; }
        public Atenuação AtenuaçãoCanalFonte2 { get;  set; }
        #endregion
        public List<int> PontosPorDecada { get;  set; }

        public RespostaEmFrequência()
        {

        }

        public RespostaEmFrequência(string comentario, CanalFonte f1, CanalFonte f2, Atenuação a1, Atenuação a2, int[] ppd, bool usaShunt, double shuntValor)
        {
            Comentário = comentario;
            CanalFonte1 = f1;
            CanalFonte2 = f2;
            AtenuaçãoCanalFonte1 = a1;
            AtenuaçãoCanalFonte2 = a2;
            PontosPorDecada = ppd.ToList();
            Criação = DateTime.Now;

            TesteShunt = usaShunt;
            ResistenciaShunt = shuntValor;

            string boolstring = usaShunt ? "1" : "0";

            Random = RandomString(5);

            NomeArquivo = boolstring+ "-" + Comentário + "-" + Random + ".json";
        }        
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            string codigo = "";

            for (int i = 0; i < 8; i++)
            {
                codigo += Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()[0].ToString();
            }
            return codigo;
        }


        public void CarregarModuloFase()
        {
            if (medições != null)
            {
                List<DataPoint> modulo = new List<DataPoint>();
                List<DataPoint> fase = new List<DataPoint>();

                foreach (var item in medições)
                {
                    modulo.Add(new(item.Frequencia, item.Admitancia));
                    fase.Add(new(item.Frequencia, item.Fase));
                }

                this.ModuloFase = new ModuloFase(modulo, fase);
            }
        }

        public void AnularModuloFase()
        {
            ModuloFase.Modulo = null;
            ModuloFase.Fase = null;
            ModuloFase.NumeroDePontos = -1;
        }

        public static RespostaEmFrequência GetDados(string path)
        {
            string dados= File.ReadAllText(path);
            return JsonConvert.DeserializeObject<RespostaEmFrequência>(dados);
        }

        public string GetPPDString()
        {
            string pontosString = ";";
            for (int i = 0; i < PontosPorDecada.Count; i++)
            {
                pontosString += PontosPorDecada[i] + "-";
            }
            return pontosString.Trim('-');
        }
    }
}
