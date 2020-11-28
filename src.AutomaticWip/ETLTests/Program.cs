using AutomaticWip.ETL.Business;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETLTests
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var w = new Worker();
            w.OnWorking();

            Console.ReadKey();
        }
    }
}
