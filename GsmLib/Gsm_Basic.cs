using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GsmLib
{
    public partial class Gsm
    {
        #region Events
        public event EventHandler<GsmEventArgs> RaiseGsmEvent;
        #endregion

        #region Fields
        public SerialPort Port;
        public AutoResetEvent receiveNow;
        #endregion

        #region Properties
        /// <summary>
        /// Alle angeschlossenen COM-Ports
        /// </summary>
        public string[] AvailableComPorts { get; } = System.IO.Ports.SerialPort.GetPortNames();

        public string CurrentComPortName { get; set; } = Properties.Settings.Default.ComPort;

        #endregion

        #region Methods

        #region Basic COM
        public Gsm()
        {
            int n = 0;
            while(!CheckComPort())
            {
                OnRaiseGsmEvent(new GsmEventArgs(11011901, string.Format("Verbindungsversuch {0} an {1}", ++n, Port.PortName) ) );
                Thread.Sleep(3000);
            }
          

        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnRaiseGsmEvent(GsmEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<GsmEventArgs> raiseEvent = RaiseGsmEvent;

            // Event will be null if there are no subscribers
            if (raiseEvent != null)
            {
                // Format the string to send inside the CustomEventArgs parameter
                e.Message += DateTime.Now + ":\t" + e.Message;

                // Call to raise the event.
                raiseEvent(this, e);
            }
        }

        //Prüft, ob der ComPort bereit ist 
        public bool CheckComPort()
        {
            if (Port == null || !Port.IsOpen)
            {
                ConnectPort();
            }

            if (Port != null && Port.IsOpen)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Verbindet den COM-Port
        /// </summary>
        private void ConnectPort()
        {
            #region richtigen COM-Port ermitteln
            if (AvailableComPorts.Length < 1)
            {
                OnRaiseGsmEvent(new GsmEventArgs(11011512, "Es sind keine COM-Ports vorhanden"));
            }

            if (!AvailableComPorts.Contains(CurrentComPortName))
            {
                CurrentComPortName = AvailableComPorts.LastOrDefault();
            }
            #endregion

            #region Wenn Port bereits vebunden ist, trennen
            if (Port != null && Port.IsOpen)
            {
                ClosePort();
            }
            #endregion

            #region Verbinde ComPort
            receiveNow = new AutoResetEvent(false);
            SerialPort port = new SerialPort();

            try
            {
                port.PortName = CurrentComPortName;                     //COM1
                port.BaudRate = Properties.Settings.Default.Baudrate;   //9600
                port.DataBits = Properties.Settings.Default.DataBits;   //8
                port.StopBits = StopBits.One;                           //1
                port.Parity = Parity.None;                              //None
                port.ReadTimeout = 300;                                 //300
                port.WriteTimeout = 300;                                //300
                port.Encoding = Encoding.GetEncoding("iso-8859-1");
                port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
                port.Open();
                port.DtrEnable = true;
                port.RtsEnable = true;
            }
            catch (Exception ex)
            {
                OnRaiseGsmEvent(new GsmEventArgs(11011514, string.Format("COM-Port {0} konnte nicht verbunden werden. \r\n{1}\r\n{2}", CurrentComPortName, ex.GetType(), ex.Message)));
            }

            Port = port;
            #endregion
        }

        //Close Port
        public void ClosePort()
        {
            if (Port == null) return;

            OnRaiseGsmEvent(new GsmEventArgs(11011917, "Port " + Port.PortName + " wird geschlossen.\r\n"));
            try
            {
                Port.Close();
                Port.DataReceived -= new SerialDataReceivedEventHandler(Port_DataReceived);
                Port.Dispose();
                Port = null;                
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Send AT Command
        public string SendATCommand(string command)
        {
            if (!CheckComPort())
            {
                OnRaiseGsmEvent(new GsmEventArgs(11011708, "Port nicht bereit für SendATCommand()"));
                return null;
            }

            try
            {
                Port.DiscardOutBuffer();
                Port.DiscardInBuffer();
                receiveNow.Reset();
                Port.Write(command + "\r");

                string input = ReadResponse();
                if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                {
                    RaiseGsmEvent(this, new GsmEventArgs(11021909, "Fehlerhaft Empfangen:\n\r" + input));
                    //throw new ApplicationException("No success message was received.");
                }
                else
                {
                    RaiseGsmEvent(this, new GsmEventArgs(11021751, "Empfangen:\n\r" + input));
                }
                return input;
            }
            catch (ApplicationException)
            {
                //"No Data recieved from phone."
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Receive data from port
        public void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                {
                    receiveNow.Set();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //lese Antwort auf SendATCommand()
        public string ReadResponse()
        {
            string serialPortData = string.Empty;
            try
            {
                do
                {
                    if (receiveNow.WaitOne(300, false))
                    {
                        string data = Port.ReadExisting();
                        serialPortData += data;
                    }
                    else
                    {
                        if (serialPortData.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!serialPortData.EndsWith("\r\nOK\r\n") && !serialPortData.EndsWith("\r\n> ") && !serialPortData.EndsWith("\r\nERROR\r\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return serialPortData;
        }

        #endregion



        #endregion
    }

    // Define a class to hold custom event info
    public class GsmEventArgs : EventArgs
    {

        public GsmEventArgs(uint id, string message)
        {
            Id = id;
            Message = message;
        }

        public uint Id { get; set; }
        public string Message { get; set; }
    }

}
