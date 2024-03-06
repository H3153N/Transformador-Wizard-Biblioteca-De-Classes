using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public class Configurações
    {
        public readonly string ArquivoConfiguraçãoNome = "Configurações.json";
        public string EndereçoOsciloscópio   { get; set; } = string.Empty;
        public string EndereçoGerador        { get; set; } = string.Empty;
        public int PontosPorDecadaPadrão     { get; set; } = 10;

        public bool LerConfigurações()
        {
            string caminhoArquivo = Path.Combine(GerenciadorDeArquivos.PastaConfigurações, ArquivoConfiguraçãoNome);
            if (File.Exists(caminhoArquivo)!)
            {
                File.Create(caminhoArquivo).Close();
                return false;
            }
            else
            {
                string stringDados = File.ReadAllText(caminhoArquivo);
                ConfiguraçõesInstância objetoDesSerializado = JsonConvert.DeserializeObject<ConfiguraçõesInstância>(stringDados);

                Type tipoClasseEstática = typeof(Configurações);
                Type tipoClasseNãoEstática = typeof(ConfiguraçõesInstância);

                foreach (PropertyInfo propriedadeNaClasseInstanciada in tipoClasseNãoEstática.GetProperties())
                {
                    PropertyInfo propriedadeEquivalenteNaClasseEstática = tipoClasseEstática.GetProperty(propriedadeNaClasseInstanciada.Name);

                    if (propriedadeEquivalenteNaClasseEstática != null && propriedadeEquivalenteNaClasseEstática.PropertyType == propriedadeNaClasseInstanciada.PropertyType)
                    {
                        object value = propriedadeNaClasseInstanciada.GetValue(objetoDesSerializado);
                        propriedadeEquivalenteNaClasseEstática.SetValue(null, value);
                    }
                }
                return true;
            }
        }
        public void SalvarConfigurações()
        {
            ConfiguraçõesInstância instância = new ConfiguraçõesInstância();
            PropertyInfo[] propriedadesDaClasseInstanciada = typeof(ConfiguraçõesInstância).GetProperties();

            foreach(PropertyInfo propriedadeDaInstância  in propriedadesDaClasseInstanciada)
            {
                PropertyInfo propriedadeDaClasseEstática = typeof(Configurações).GetProperty(propriedadeDaInstância.Name);

                if(propriedadeDaClasseEstática != null && propriedadeDaClasseEstática.PropertyType == propriedadeDaInstância.PropertyType)
                {
                    object value = propriedadeDaClasseEstática.GetValue(null);
                    propriedadeDaInstância.SetValue(instância, value);
                }
            }
            string caminhoArquivo = Path.Combine(GerenciadorDeArquivos.PastaConfigurações, ArquivoConfiguraçãoNome);
            string dadosFormatados = JsonConvert.SerializeObject(instância);
            File.WriteAllText(caminhoArquivo, dadosFormatados);
        }
    }
}
