using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBTracker.Plugin.Contracts
{
    internal interface IPluginTaskProvider
    {
        public object GetConfiguraitonView(IPluginConfigRepository configRepository);
        public IEnumerable<string> GetTasks(IPluginConfigRepository configRepository);
    }
}
