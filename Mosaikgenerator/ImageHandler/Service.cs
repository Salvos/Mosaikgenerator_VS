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

            checkPath();

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

        static void checkPath()
        {
            String basicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\VS16_MosaikGenerator\\";

            if (!System.IO.Directory.Exists(basicPath))
            {
                Console.WriteLine("VS16_MosaikGenerator existiert noch nicht - Wird erstellt");
                System.IO.Directory.CreateDirectory(basicPath);
            }

            if (!System.IO.Directory.Exists(basicPath + "Motive\\"))
            {
                Console.WriteLine("VS16_MosaikGenerator\\Motive\\ existiert noch nicht - Wird erstellt");
                System.IO.Directory.CreateDirectory(basicPath + "Motive\\");
            }

            if (!System.IO.Directory.Exists(basicPath + "Kacheln\\"))
            {
                Console.WriteLine("VS16_MosaikGenerator\\Kacheln\\ existiert noch nicht - Wird erstellt");
                System.IO.Directory.CreateDirectory(basicPath + "Kacheln\\");
            }
        }
    }
}
