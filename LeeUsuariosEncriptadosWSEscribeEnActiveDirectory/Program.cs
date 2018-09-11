using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LeeUsuariosEncriptadosWSEscribeEnActiveDirectory
{
    static class Program
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /*
        static void Main()
        {
            
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MainService()
            };
            ServiceBase.Run(ServicesToRun);
            
        }
        */
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main()
        {
            log.Info("Se empezó a ejecutar el programa wololo");
            string[] args = Environment.GetCommandLineArgs();

            if (args == null || args.Length > 2)
            {
                Console.WriteLine("------------------------------------------");

                System.Reflection.Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ass.Location);
                string pv = fvi.ProductVersion;

                Console.WriteLine(fvi.FileDescription + " - " + fvi.CompanyName);
                Console.WriteLine("Version: " + fvi.ProductVersion);

                Console.WriteLine();
                Console.WriteLine("CLI syntaxis: ");

                Console.WriteLine();
                Console.WriteLine("\tRun in Console Mode:");
                Console.WriteLine("\t..>LeeUsuariosEncriptadosWSEscribeEnActiveDirectory /CONSOLE");
                Console.WriteLine("------------------------------------------");
            }
            else if (args.Length == 2 && args[1].ToUpper().Equals("/CONSOLE"))
            {
                RunInConsole app = new RunInConsole(args);
            }
            else
            { //Run in Windows Service Mode.
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new MainService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private class RunInConsole : IDisposable
        {
            private MainService service;

            public RunInConsole(string[] args)
            {
                Console.WriteLine("------------ CONSOLE MODE ------------");

                service = new MainService();
                Task task1 = new Task(new Action(service._Start));
                task1.Start();

                while (true)
                {
                    string line = Console.ReadLine();
                    if (line == null)
                    {
                        Console.WriteLine("This is not a console application.");
                        break;
                    }
                    if (line.ToLower().Equals("q"))
                        break;
                }

                Console.WriteLine("STOPPING...");

                Task task2 = new Task(new Action(service._Stop));
                task2.Start();

                try
                {
                    task2.Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Console.WriteLine("STOPPED");
                Console.WriteLine("Press any key...");
                Console.ReadLine();
            }

            public void Dispose()
            {
                service.Dispose();
                service = null;
            }
        }//RunInConsole
    }

}
