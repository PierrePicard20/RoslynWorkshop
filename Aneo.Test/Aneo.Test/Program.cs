using System;

namespace Aneo.Test
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    class A
    {
        int x;

        public void Inc()
        {
            lock (this)
            {
                x++;
            }
        }

        public void HelloWorld()
        {
            Console.WriteLine("Hello world!");
        }
    }
}
