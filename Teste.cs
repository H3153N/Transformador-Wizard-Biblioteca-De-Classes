using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Biblioteca
{
    public class Teste
    {
        

        //comentario;canalTensao;canalCorrente;AtenuacaoTensao;AtenuacaoCorrente;PPD;datetime;random
        //                                                                X1, X10, X100, X1000

        //TITULO;1;1;1;10X;100X;1-2-3-4-5-6
        public string Path { get; private set; } = "";
        public ModuloFase ModuloFase { get;  set; } = new ModuloFase();
        public DateTime Criação { get; private set; } = DateTime.MinValue;      
        public string Comentário { get; set; }
        public string Random { get; private set; }
        public string NomeArquivo { get; set; } = string.Empty;

        private static Random random = new();

        #region canais

        public CanalFonte CanalFonte1 { get; private set; }
        public CanalFonte CanalFonte2 { get; private set; }

        public Atenuação AtenuaçãoCanalFonte1 { get; private set; }
        public Atenuação AtenuaçãoCanalFonte2 { get; private set; }
        #endregion
        public List<int> PontosPorDecada { get; private set; } = [0, 0, 0, 0, 0, 0];


        public Teste(string path)
        {
            //
            this.Path = path;
            path = path.Split('\\').Last();
            NomeArquivo = path;
            var dados = path.Split(';');

            Comentário = dados[0];
            
            CanalFonte1 = (CanalFonte)Enum.Parse(typeof(CanalFonte), dados[1]);
            CanalFonte2 = (CanalFonte)Enum.Parse(typeof(CanalFonte), dados[2]);

            AtenuaçãoCanalFonte1 = (Atenuação)Enum.Parse(typeof(Atenuação), dados[3]);
            AtenuaçãoCanalFonte2 = (Atenuação)Enum.Parse(typeof(Atenuação), dados[4]);


            var numeros = dados[5].Split('-');
            PontosPorDecada = new List<int>();
            for (int i = 0; i < numeros.Length; i++)
            {
                PontosPorDecada.Add(Convert.ToInt32(numeros[i]));
            }

            Criação = new DateTime(long.Parse(dados[6]));
            Random = dados[7];
            PopularModuloFase();
        }

        

        public Teste(string comentario, DateTime dataDeCriação, CanalFonte f1, CanalFonte f2, Atenuação a1, Atenuação a2, int[] ppd)
        {
            Comentário = comentario;
            CanalFonte1 = f1;
            CanalFonte2 = f2;
            AtenuaçãoCanalFonte1 = a1;
            AtenuaçãoCanalFonte2 = a2;
            PontosPorDecada = ppd.ToList();

            NomeArquivo = comentario + ';' + ((int)f1).ToString() + ';' + ((int)f2).ToString() +';' + a1.ToString() + ';' + a2.ToString() + GetPPDString() + ';'+ dataDeCriação.Ticks.ToString() + ';' + RandomString(5);

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

        public void PopularModuloFase()
        {
            ModuloFase = GerenciadorDeArquivos.GetPontos(Path).Result;
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
