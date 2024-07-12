using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca
{
    public enum Atenuação
    {
        X1 =      1,
        X10=      10,
        X100 =    100,
        X1000 =   1000
    }

    public enum CanalFonte
    {
        CH1 = 1,
        CH2 = 2,
        CH3 = 3,
        CH4 = 4
    }
    public enum MediçãoTipo
    {
        Admitancia = 1,
        TensãoTransferencia = 2
    }
    public enum EnsaioTipo
    {
        Varredura = 0,
        Impulso = 1,
        RelaçãoDeTransferência = 2
    }
    public enum FaseModulo
    {
        Modulo = 0,
        Fase = 1,
        Detalhes = 2
    }

    public enum AquisiçãoModo 
    {
        AltaResolução = 0,
        Médias = 1    
    }

    public enum Função
    {
        Senoidal    = 0,
        Quadrada    = 1,
        Triangular  = 2,
        Pulso       = 3,
        Ruído       = 4,
        DC          = 5,
        Arbitrária  = 6,
        preset1     = 7,
        preset2     = 8,    
        preset3     = 9,
    }
    public enum Tensão
    {
        Vpp = 0,
        Vrms= 1,
        dBm = 2,
    }

    public enum Gatilho
    {        
        Subida,
        Descida,
        Ambos
    }

    public enum MultiplicadorNegativo
    {
        sem,
        mili,
        micro,
        nano
    }

    public enum MultiplicadorPositivo
    {
        sem,
        kilo,
        Mega
    }

    public enum Formato
    {
        Dispositivo,
        Ensaio,        
        TesteFrequencia,
        TesteTempo
    }



    
}
