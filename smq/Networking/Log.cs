using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smq.Networking {
    public static class Log {
        public static void Write(object? msg) {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} {msg}");
        }
    }
}
