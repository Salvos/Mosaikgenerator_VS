using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Mosaikgenerator
{
    class Service
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(Generator));
                host.AddServiceEndpoint(typeof(Generator), new BasicHttpBinding(), new Uri("http://localhost:8080/mosaikgenerator/mosaikgenerator"));
                host.Open();

                Console.WriteLine();
                Console.WriteLine("Enter to Exit");
                Console.ReadKey();

            }
            catch (Exception)
            {
                host.Close();
            }
        }
    }
}
