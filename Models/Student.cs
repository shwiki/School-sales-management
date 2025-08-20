using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gusheshe.Models
{
    public class Student
    {
        public string Name { get; set; }
        public string Class { get; set; }
        public override string ToString() => $"{Name} ({Class})";
    }
}
