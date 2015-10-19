using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGraphAnalyzer.Models
{
    public class Course
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public ICollection<Student> Studnets { get; set; } 
    }
}
