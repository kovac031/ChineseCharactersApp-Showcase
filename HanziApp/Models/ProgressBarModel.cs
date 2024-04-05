using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ProgressBarModel : EventArgs
    {
        public int ProgressValue { get; set; }
        public string ProgressMessage { get; set; }
    }
}
