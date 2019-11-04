﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NSmartProxy.Shared;

namespace NSmartProxy.Infrastructure
{
    public static class StreamExtension
    {
        public static async Task WriteAndFlushAsync(this Stream stream, byte[] buffer, int offset = 0, int count = 0)
        {
            //不让赋值默认值，只能暂时给个0
            if (count == 0) count = buffer.Length;
            await stream.WriteAsync(buffer, offset, count);
            await stream.FlushAsync();
        }

        /// <summary>
        /// 带超时的readasync,timeout 毫秒
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="TimeOut"></param>
        /// <returns></returns>
        public static async Task<int> ReadAsync(this NetworkStream stream, byte[] buffer, int offset, int count, int timeOut)
        {
            var receiveCount = 0;
            var receiveTask = Task.Run(async () => { receiveCount = await stream.ReadAsync(buffer, offset, count); });
            var isReceived = await Task.WhenAny(receiveTask, Task.Delay(timeOut)) == receiveTask;
            if (!isReceived) return -1;
            return receiveCount;
        }


        public static async Task<int> ReadAsyncEx(this NetworkStream stream, byte[] buffer)
        {
            return await stream.ReadAsync(buffer, 0, buffer.Length, Global.DefaultConnectTimeout);
        }

        public static Stream ProcessSSL(this Stream clientStream, X509Certificate cert)
        {
            try
            {
                SslStream sslStream = new SslStream(clientStream);
                sslStream.AuthenticateAsServer(cert, false, SslProtocols.Tls, true);
                sslStream.ReadTimeout = 10000;
                sslStream.WriteTimeout = 10000;
                return sslStream;
            }
            catch (Exception ex)
            {
                clientStream.Close();
                throw ex;
            }

            //return null;
        }

        public static async Task WriteAsync(this Stream stream, byte[] bytes)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

    }
}