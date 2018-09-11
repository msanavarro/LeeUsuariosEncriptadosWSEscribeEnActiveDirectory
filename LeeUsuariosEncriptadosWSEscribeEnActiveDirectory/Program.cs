using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LeeUsuariosEncriptadosWSEscribeEnActiveDirectory
{
    static class Program
    {
        /// <summary>
        /// Servicio de Windows que lee unusarios encriptados de un servicio web, los desencripta y los da
        /// de alta en un dominio de un directorio activo.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
