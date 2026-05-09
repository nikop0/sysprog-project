using System;
using System.Net;
using System.Threading;

namespace zadatak2
{
    internal class RequestBuffer
    {
        private readonly int N;
        private readonly HttpListenerContext[] buffer;
        private int count = 0;
        private int next_read = 0;
        private int next_write = 0;

        private readonly object mutex = new();
        private readonly object isEmpty = new();
        private readonly object isFull = new();

        public RequestBuffer(int n)
        {
            this.N = n;
            this.buffer = new HttpListenerContext[this.N];
        }

        public void Add(HttpListenerContext context)
        {
            lock (this.isEmpty)
            {
                while (this.count >= this.N)
                {
                    Monitor.Wait(this.isEmpty);
                }
            }

            lock (this.mutex)
            {
                this.buffer[this.next_write] = context;
                this.next_write = (this.next_write + 1) % this.N;
                this.count++;
            }

            lock (this.isFull)
            {
                Monitor.Pulse(this.isFull);
            }
        }

        public HttpListenerContext Remove()
        {
            HttpListenerContext context;

            lock (this.isFull)
            {
                while (this.count == 0)
                {
                    Monitor.Wait(this.isFull);
                }
            }

            lock (this.mutex)
            {
                context = this.buffer[this.next_read];
                this.next_read = (this.next_read + 1) % this.N;
                this.count--;
            }

            lock (this.isEmpty)
            {
                Monitor.Pulse(this.isEmpty);
            }

            return context;
        }
    }
}