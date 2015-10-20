using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFGraphAnalyzer.Models;

namespace EFGraphAnalyzer
{
    class Program
    {
        private static EFContext db = new EFContext();
        static void Main(string[] args)
        {

            var s = new Student
            {
                Id = 1,
                Name = "First",
                Courses = new[]
                {
                    new Course
                    {
                        Id = 1,
                        Name = "FirstCourse"
                    }
                }
            };

            db.AnalyseGraph(s);
            

            db.SaveChanges();


        }


    }
}
