using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFacotry;
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFacotry += sessionFactory;

            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptcompleted);
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);
            if (!pending)
                OnAcceptcompleted(null, args);
        }
        void OnAcceptcompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Session sess = _sessionFacotry.Invoke();
                sess.Start(args.AcceptSocket);
                sess.OnConnected(args.AcceptSocket.RemoteEndPoint);
            }
            else
                LOG_W(args.SocketError.ToString());

            RegisterAccept(args);
        }
    }
}
