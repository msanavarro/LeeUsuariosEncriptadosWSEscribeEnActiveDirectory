using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LeeUsuariosEncriptadosWSEscribeEnActiveDirectory
{
    public partial class MainService : ServiceBase
    {
        private loop.InfityLoop service;

        public MainService()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            System.Console.WriteLine("OnStart");
            service = new loop.InfityLoop();
            service.Start();
            System.Console.WriteLine("Service Started");
        }

        protected override void OnStop()
        {
            System.Console.WriteLine("OnStop");
            service.Stop();
            System.Console.WriteLine("Service Stoped");
        }

        public void _Start()
        {
            OnStart(null);
        }

        public void _Stop()
        {
            OnStop();
        }
    }
}

