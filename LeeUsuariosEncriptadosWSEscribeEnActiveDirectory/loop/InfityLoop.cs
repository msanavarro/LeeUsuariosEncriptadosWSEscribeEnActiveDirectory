using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.core;

namespace LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.loop
{
    class InfityLoop
    {
        public static bool IsRunning { get; set; }
        private long loopWnTime;

        public InfityLoop()
        {
            InfityLoop.IsRunning = false;
            loopWnTime = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.LoopTime;
        }

        public void Start()
        {
            if (!InfityLoop.IsRunning)
            {
                Task t = new Task(new Action(Run));
            }

        }

        public void Stop()
        { }

        public void Run()
        {
            InfityLoop.IsRunning = true;
            LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.Info("Se empezó a ejecutar el programa");

            long minutes = loopWnTime * 60;
            //TODO: Logs de inciado, indiar el tiempo de espera configurado, en minutos.

            while (InfityLoop.IsRunning)
            {
                //1.- Realizar primera importacion

                AddUser core = new AddUser();
                core.Execute();
                LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.Info("Se empezó a ejecutar el programa");

                //2.- Esperar la ventana de tiempo
                int counterMs = 0;
                int counterSeconds = 0;

                while (InfityLoop.IsRunning)
                {
                    System.Threading.Thread.Sleep(100);

                    counterMs++;
                    if (counterMs == 10)
                    {
                        counterMs = 0;
                        counterSeconds++;
                    }

                    if (minutes == counterSeconds) continue;
                } //While Wait
            } //While Running

            //TODO: Logs de finalizado
            LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.Info("Se terminó de ejecutar el programa");
        }

    }
}
