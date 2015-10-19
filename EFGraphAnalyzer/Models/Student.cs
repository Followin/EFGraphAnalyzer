using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGraphAnalyzer.Models
{
    public class Student
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public virtual ICollection<Course> Courses { get; set; } 
    }
}
