using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace StormiumTeam.GameBase.Systems.Hosting
{
    public unsafe partial class RunNetCoreHostSystem
    {
        private delegate void PrintDelegate(LogType level, ReadOnlySpan<char> chars);
        public delegate void ReceiveExchangeDelegate(ReadOnlySpan<char> property, Span<byte> data);
        private delegate void SendExchangeGetDelegate(IntPtr ptr);

        private PrintDelegate _print;
        private ReceiveExchangeDelegate _receiveExchange;
        private SendExchangeGetDelegate _sendExchange;

        private delegate*<ReadOnlySpan<char>, Span<byte>, void> _hostExchange;

        private void InitCallbacks()
        {
            _print = (level, chars) =>
            {
                Debug.unityLogger.Log(level, chars.ToString());
            };
            _receiveExchange = (property, data) =>
            {
                OnHostExchange?.Invoke(property, data);
            };
            _sendExchange = ptr =>
            {
                _hostExchange = (delegate*<ReadOnlySpan<char>, Span<byte>, void>) ptr;
            };
        }

        public event ReceiveExchangeDelegate OnHostExchange;

        public void SendHostDataRaw(ReadOnlySpan<char> property, Span<byte> data)
        {
            if (_hostExchange == null)
                throw new NullReferenceException(nameof(_hostExchange));

            _hostExchange(property, data);
        }

        public void SendHostData<T>(ReadOnlySpan<char> property, Span<T> array)
            where T : struct
        {
            SendHostDataRaw(property, MemoryMarshal.Cast<T, byte>(array));
        }

        public void SendHostData<T>(ReadOnlySpan<char> property, T data)
            where T : struct
        {
            SendHostData(property, MemoryMarshal.CreateSpan(ref data, 1));
        }
    }
}