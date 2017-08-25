using System;
using System.IO;

namespace SyncSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var configuration = new Sync.Configuration();
                configuration.GenerateScopeName();

                var provision = new Sync.Provision(configuration);
                provision.Run();

                var synchronize = new Sync.Synchronize(configuration);
                synchronize.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + '\n' + ex.ToString());
                File.WriteAllText("Errors.txt", DateTime.Now.ToString() + '\n' + ex.ToString());
            }
        }
    }
}


