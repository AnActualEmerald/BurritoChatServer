using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Threading;

namespace ChatServer
{
	class Server
	{
		private static List<Socket> clients = new List<Socket> ();
		private static List<Client> seen = new List<Client>();
		private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		private static byte[] _buffer = new byte[1024];
		private static TcpListener _server;
		private static FileManager fileSaver;

		public static void Main (string[] args)
		{
			Console.Title = "Burrito Time Server";
			fileSaver = new FileManager (true);
			initSeen ();
			StartServer ();
			Console.ReadLine ();
		}

		public static void initSeen(){
			string[] oldSeen = fileSaver.loadFromMain ();
			foreach (string s in oldSeen) {
				if (s.StartsWith (" "))
					continue;
				string[] tmp = s.Split (new char[]{'|'});
				seen.Add(new Client(tmp[0], tmp[1]));
			}
		}

		public static void StartServer(){
			Console.WriteLine ("Starting server...");
			_server = new TcpListener (new IPEndPoint (IPAddress.Any, 25565));
			_server.AllowNatTraversal (true);
			Console.WriteLine ("Server started, accepting connections");
			_server.Start (1);
			_server.BeginAcceptSocket (new AsyncCallback (AcceptCall), null);
		}

		private static void AcceptCall(IAsyncResult r){
			Socket socket = _server.EndAcceptSocket (r);
			clients.Add (socket);

			Console.WriteLine ("Client Connected: " + socket.RemoteEndPoint);
			onConnect (socket);
			socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback (RecieveCall), socket);

			_server.BeginAcceptSocket (new AsyncCallback (AcceptCall), null);
		}

		private static void onConnect(Socket socket){
			try{
				SendText("/get login", socket);
				byte[] rec = new byte[500];
				socket.Receive (rec);

				string login = Encoding.ASCII.GetString (rec);

				string[] tmp = login.Split(Character.ENDVARCHAR);

				string name = tmp[0];
				string ip = tmp[1];

				Client c = new Client (name, ip, socket); 
				if (hasBeenSeen (c, true)) {
					SendToAll ("Welcome back, " + c.name);
				}else{
					SendToAll("Welcome, " + c.name);
					fileSaver.saveToMain(c.name + "|" + c.remoteEnd + ";");
					seen.Add(c);
				}

			}catch(SocketException s){
				LogError (s);
			}
		}

		private static bool hasBeenSeen(Client clnt, bool shouldUpdate){
			foreach (Client c in seen) {
				if ((clnt.name == c.name) || (clnt.remoteEnd == c.remoteEnd)) {
					if(shouldUpdate){
						c.name = clnt.name;
						c.remoteEnd = clnt.remoteEnd;
						c.socket = clnt.socket;
					}
					return true;
				}
			}

			return false;
		}

		private static void RecieveCall(IAsyncResult r){
			try{
				Socket socket = (Socket)r.AsyncState;
				int rec = socket.EndReceive (r);
				byte[] data = new byte[rec];
				Array.Copy (_buffer, data, rec);

				string text = Encoding.ASCII.GetString(data);
				Console.WriteLine ("Client sent: " + text);
				fileSaver.saveToLog(DateTime.Today.ToLongDateString() + " " + DateTime.Now.ToLongTimeString () + " Client sent: " + text);
				parseRequest(text, socket);


				socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback (RecieveCall), socket);
			}catch(SocketException){
				Console.WriteLine ("Client lost connection!");
				SendToAll ("Server: A Client has disconnected!");
				_server.BeginAcceptSocket (new AsyncCallback (AcceptCall), null);
			}
		}

		private static void parseRequest (string req, Socket client){
			string text = string.Empty;
			string[] tmp = req.Split (Character.ENDVARCHAR);
			if (tmp [0].Equals (Character.NAMECHAR)) {
				text = tmp [1] + ": " + tmp [2];
			} else if (tmp [0].Equals (Character.PRIVATECHAR)) {
				text = tmp [1];
			}

			if(text.StartsWith("/")) DoCommand(text, client);
			else{
				SendToAll(text);
				fileSaver.saveToLog (DateTime.Today.ToLongDateString () +" " + DateTime.Now.ToLongTimeString () + " Sent to clients: " + text);
				Console.WriteLine ("Sent to client: " + text);
			}
		}

		private static void SendText(string text, Socket target){
			byte[] data = Encoding.ASCII.GetBytes (text);
			target.BeginSend (data, 0, data.Length, SocketFlags.None, new AsyncCallback (SendCall), target);
		}

		private static void SendToAll(string text){
			byte[] data = Encoding.ASCII.GetBytes (text);
			foreach (Socket s in clients) {
				try{
					s.BeginSend (data, 0 , data.Length, SocketFlags.None, new AsyncCallback (SendCall), s);
				} catch (SocketException e){
					clients.Remove (s);
					Console.WriteLine ("Unable to send message to client, removing from list");
					LogError (e);
					break;
				}
			}
		}

		private static void SendToAll(string text, Socket except){
			byte[] data = Encoding.ASCII.GetBytes (text);
			foreach (Socket s in clients) {
				if (s.Equals (except))
					continue;
				s.BeginSend (data, 0 , data.Length, SocketFlags.None, new AsyncCallback (SendCall), s);
			}
		}

		private static void SendCall(IAsyncResult r){
			Socket s = (Socket)r.AsyncState;
			s.EndSend (r);
		}

		private static void DoCommand(string text, Socket socket){
			string mes = String.Empty;
			if (text.ToLower () == Request.TIME) {
				mes = DateTime.Now.ToLongTimeString ();
				SendText ("Server: " + mes, socket); 

			} else if (text.ToLower () == Request.DATE) {
				mes = DateTime.Today.ToLongDateString ();
				SendText ("Server: " + mes, socket);

			} else if (text.ToLower () == Request.ONLINE) {
				mes = clients.Count.ToString ();
				SendText ("Server: " + mes + " people are online", socket);

			} else if (text.ToLower () == Request.END) {
				mes = "Disconnecting...";
				SendText ("Server: " + mes, socket); 
				clients.Remove (socket);
				socket.Disconnect (false);
				throw new SocketException ();

			}else if(text.ToLower() == Request.ONLINEWHO){
				foreach (Socket s in clients) {
					foreach (Client c in seen) {
						if(c.socket == s)
							mes += c.name + ", ";
					}
				}
				mes = mes.Substring (0, mes.Length - 2);
				SendText ("Server: " + mes , socket);
			}else{
				mes = "Invalid request";
				SendText ("Server: " + mes, socket);
			}

			fileSaver.saveToLog (DateTime.Today.ToLongDateString () + " " + DateTime.Now.ToLongTimeString () + " Sent to client: " + mes);
			Console.WriteLine ("Sent to client: " + mes);
		}

		private static void LogError(Exception e){
			fileSaver.saveToLog (DateTime.Today.ToLongDateString () + " " + DateTime.Now.ToLongTimeString () + " Error Occered: " + e.Message);
			fileSaver.saveToLog (e.StackTrace);
		}
	}

	struct Request{
			
		public static string TIME = "/get time";
		public static string DATE = "/get date";
		public static string ONLINE = "/online";
		public static string ONLINEWHO = "/who";
		public static string END = "/stop";

	}

	struct Character{

		public static string NAMECHAR = "/n";
		public static string PRIVATECHAR = "/p"; 
		public static char[] ENDVARCHAR = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes("|"));

	}

	class Client{

		public string name;
		public string remoteEnd;
		public Socket socket;

		public Client(string name, string remoteEnd){
			this.name = name;
			this.remoteEnd = remoteEnd;
		}

		public Client(string name, string remoteEnd, Socket s){
			this.name = name;
			this.remoteEnd = remoteEnd;
			this.socket = s;
		}

		public EndPoint getEndpoint(){
			char[] c = Encoding.ASCII.GetChars (Encoding.ASCII.GetBytes (":"));
			string[] s = remoteEnd.Split (c, 2);
			IPAddress i = IPAddress.Parse(s[0]);
			return new IPEndPoint (i, int.Parse (s [1]));
		}
	}
}
