using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ChatServer
{
	public class FileManager
	{
		private DirectoryInfo mainDir;
		private string mainSavPath = "./ServerDat/sever.sav";
		private string mainLogPath = "./ServerDat/serverLog.sav";
		private FileStream mainFile;
		private FileStream logFile;

		public FileManager (bool isLocal)
		{
			mainDir = Directory.CreateDirectory ("./ServerDat");
			mainFile = File.Open (mainSavPath, FileMode.OpenOrCreate);
			mainFile.Close ();

			logFile = File.Open (mainLogPath, FileMode.OpenOrCreate);
			logFile.Close ();
		}

		public void saveToMain(string data){
			mainFile = File.Open (mainSavPath, FileMode.Append);
			byte[] dat = Encoding.ASCII.GetBytes (data + Environment.NewLine);
			mainFile.Write (dat, 0, dat.Length);
			mainFile.Close ();
		}

		public string[] loadFromMain(){
			mainFile = File.Open (mainSavPath, FileMode.Open);
			byte[] dat = new byte[mainFile.Length];
			mainFile.Read (dat, 0, (int) mainFile.Length);
			List<string> bits = new List<string> ();
			string tmp = string.Empty;
			foreach (byte b in dat) {
				string s = Encoding.ASCII.GetString (new byte[]{b});
				if (s != ";") {
					tmp += s;

				} else {
					tmp = tmp.Replace (Environment.NewLine, "");
					bits.Add (tmp);
					tmp = string.Empty;
					continue;
				}
			}
			mainFile.Close ();
			return bits.ToArray ();
		}

		public void saveToLog(string dat){
			logFile = File.Open (mainLogPath, FileMode.Append);
			byte[] data = Encoding.ASCII.GetBytes (dat + Environment.NewLine);
			logFile.Write (data, 0, data.Length);
			logFile.Close ();
		}
	}
}

