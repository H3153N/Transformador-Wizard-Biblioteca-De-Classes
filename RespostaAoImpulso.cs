using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class RespostaAoImpulso
    {
        ///CRIAR CLASSE PARENTE "TESTE" e herdar propriedades basicas
        public string Path  = "";
        public DateTime Criação { get; set; }
        public string Comentário { get; set; }
        public string Random { get; private set; }
        public string NomeArquivo { get; set; } = string.Empty;
        private static Random random = new();
        

        public List<Canal> Canais{ get; set; }
        public ParametrosTesteImpulsivo ParametrosDoImpulso { get; set; }

        public RespostaAoImpulso() 
        { 
        
        }

        public RespostaAoImpulso(ParametrosTesteImpulsivo parametros, string path)
        {
            ParametrosDoImpulso = parametros;
            this.Canais = parametros.CanaisUsados;
            this.Comentário = parametros.Comentário;          
            this.Path = path;

            NomeArquivo = Comentário + " "+ DateTime.Now.Ticks + " " + RandomString(5) + ".json";
        }

        public RespostaAoImpulso(ParametrosTesteImpulsivo parametros)
        {
            ParametrosDoImpulso = parametros;
            this.Canais = parametros.CanaisUsados;
            this.Comentário = parametros.Comentário;
            NomeArquivo = Comentário + " " + DateTime.Now + " " + RandomString(5) + ".json";
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
    }
}
