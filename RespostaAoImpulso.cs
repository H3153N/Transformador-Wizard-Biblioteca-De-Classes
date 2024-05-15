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
        ParametrosTesteImpulsivo ParametrosDoImpulso { get; set; }

        public RespostaAoImpulso() 
        { 
        
        }

        public RespostaAoImpulso(string comentario, ParametrosTesteImpulsivo parametros)
        {
            this.Canais = parametros.CanaisUsados;
            this.Comentário = comentario;
            this.ParametrosDoImpulso = parametros;

            NomeArquivo = comentario + " "+ DateTime.Now + " " + RandomString(5) + ".json";
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
