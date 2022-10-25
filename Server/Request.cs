using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Request
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public int Date { get; set; }
        public string? Body { get; set; }
    }

}
