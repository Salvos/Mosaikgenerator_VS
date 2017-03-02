using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ImageHandler
{
    class Service
    {
        static void Main(string[] args)
        {

            String basicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\VS16_MosaikGenerator\\";

            if (!System.IO.Directory.Exists(basicPath))
            {
                Console.WriteLine("VS16_MosaikGenerator existiert noch nicht - Wird erstellt");
                System.IO.Directory.CreateDirectory(basicPath);
            }

            ServiceHost host = null;

            try
            {

                host = new ServiceHost(typeof(Handler));
                host.AddServiceEndpoint(typeof(IHandler), new BasicHttpBinding(), new Uri("http://localhost:8080/mosaikgenerator/imagehandler"));
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
