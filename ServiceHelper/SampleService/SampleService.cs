using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceHelper;
using System.Diagnostics;

namespace SampleService
{
    public class SampleService : WindowsServiceImplementation
    {
        private static int Main(string[] args)
        {
            return WindowsService<SampleService>.Run(args);
        }

        private bool sleepBeforeNextTick;

        protected override bool SleepBetweenTicks
        {
            get { return this.sleepBeforeNextTick; }
        }

        protected override TimeSpan TimeBetweenTicks
        {
            get { return Properties.Settings.Default.TickTimeout; }
        }

        protected override TimeSpan TimeToNextTick
        {
            get { return Properties.Settings.Default.TimeToNextTick; }
        }

        public SampleService()
        {
            this.TickTimeout += SampleService_TickTimeout;
        }

        protected override void Tick()
        {
            // TODO: finish creating an implemenatation example
        }

        private void SampleService_TickTimeout(object sender, TickTimeoutEventArgs e)
        {
            base.LogWarning("Service loop took longer than {0} seconds to complete.", this.TimeBetweenTicks.TotalSeconds);
        }
    }
}
