using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GsmLib
{
	public partial class Gsm
	{

		#region Gsm Operationen
		/// <summary>
		/// Gibt dieSignalstärke in Prozent aus.
		/// </summary>
		/// <returns></returns>
		public int GetSignalQuality()
		{
			int sig_qual = 0;
			string strResp1 = SendATCommand("AT+CSQ");
			if (strResp1 == null)
				return sig_qual;

			string pattern = @"\+CSQ: \d+,";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(strResp1, pattern).Groups[0].Value;
			if (strResp2 == null)
				return sig_qual;

			int.TryParse(strResp2.Substring(6, 2), out sig_qual);

			return sig_qual * 100 / 31;
		}

		/// <summary>
		/// Prüft, ob die SIM-Karte im Mobilfunknetz registriert ist 
		/// </summary>
		public bool IsSimRegiserd()
        {
			string strResp1 = SendATCommand("AT+CREG?");
			if (strResp1 == null)
				return false;

			string pattern = @"\+CREG: \d,\d";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(strResp1, pattern).Groups[0].Value;
			if (strResp2 == null)
				return false;

			int.TryParse(strResp2.Substring(7, 1), out int RegisterStatus);
			int.TryParse(strResp2.Substring(9, 1), out int AccessStatus);

			Console.WriteLine("Status >" + RegisterStatus + "<");
			Console.WriteLine("Access >" + AccessStatus + "<");

			return (strResp2 != "+CREG: 0,0");
		}
		#endregion

		#region SMS empfangen
		// Declare the event using EventHandler<T>
		public event EventHandler<ShortMessageArgs> RaiseSmsRecievedEvent;

		public void SubscribeNewSms()
        {

        }

		public void ReadMessage(string filter = "ALL")
		{
			string command = string.Format("AT+CMGL=\"{0}\"", filter);
			
			try
			{
				//Textmode
				SendATCommand("AT+CMGF=1");
				// Use character set "PCCP437"
				//ExecCommand(port,"AT+CSCS=\"PCCP437\"", 300, "Failed to set character set.");
				// Select SIM storage
				SendATCommand("AT+CPMS=\"SM\"");
				// Read the messages
				string input = SendATCommand(command);

				Console.WriteLine("#INPUT: " + input);
				//Rohantwort Interpretieren
				ParseMessages(input);
			}
			catch
			{
				throw new Exception("Fehler SMS lesen");
			}
		}

		private void ParseMessages(string input)
		{
			try
			{
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
				Match m = r.Match(input);
				while (m.Success)
				{
                    ShortMessageArgs msg = new ShortMessageArgs
                    {
                        Index = m.Groups[1].Value,
                        Status = m.Groups[2].Value,
                        Sender = m.Groups[3].Value,
                        Alphabet = m.Groups[4].Value,
                        Sent = m.Groups[5].Value,
                        Message = m.Groups[6].Value
                    };

                    OnRaiseSmsRecievedEvent(msg);
					m = m.NextMatch();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		// Wrap event invocations inside a protected virtual method
		// to allow derived classes to override the event invocation behavior
		protected virtual void OnRaiseSmsRecievedEvent(ShortMessageArgs e)
		{
            RaiseSmsRecievedEvent?.Invoke(this, e);
        }
		#endregion
	}

    public class ShortMessageArgs : EventArgs
	{

		#region Private Variables
		private string index;
		private string status;
		private string sender;
		private string alphabet;
		private string sent;
		private string message;
		#endregion

		#region Public Properties
		public string Index
		{
			get { return index; }
			set { index = value; }
		}
		public string Status
		{
			get { return status; }
			set { status = value; }
		}
		public string Sender
		{
			get { return sender; }
			set { sender = value; }
		}
		public string Alphabet
		{
			get { return alphabet; }
			set { alphabet = value; }
		}
		public string Sent
		{
			get { return sent; }
			set { sent = value; }
		}
		public string Message
		{
			get { return message; }
			set { message = value; }
		}
		#endregion

	}




}
