using System;
using System.IO.Ports;
using System.Threading;

namespace AveBusControllerDLL
{
    internal class AveBusController
    {
        private SerialPort serialPort = new SerialPort();

        private bool read = false;
        private Thread readBusThread;

        // questo non è una funzione, è il tipo della funzione
        public delegate void BusGuiCallback(string key, string value);
        private BusGuiCallback guiCallback = null; // questa è la funzione di callback richiamabile. E' messa a null e impostabile tramite un setter per permettere a programmi esterni di interagirci.

        private static byte[] CHANGE_LIGHT_STATUS_FRAME_COMMAND = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x02, 0xFA, 0xEA };
        private static byte[] TURN_ON_LIGHT_1_FRAME_COMMAND = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x01, 0xFA, 0xE9 };
        private static byte[] TURN_OFF_LIGHT_1_FRAME_COMMAND = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x03, 0xFA, 0xEB };
        private static byte[] TURN_ON_LIGHT_2_FRAME_COMMAND = new byte[] { 0x40, 0x07, 0x26, 0x26, 0x4E, 0x01, 0xFA, 0xE7 };
        private static byte[] TURN_OFF_LIGHT_2_FRAME_COMMAND = new byte[] { 0x40, 0x07, 0x26, 0x26, 0x4E, 0x03, 0xFA, 0xE9 };



        // ==============================================================
        // callback setter
        public void registerEventHandler(BusGuiCallback eventHandler)
        {
            guiCallback = eventHandler;
        }


        // ==============================================================
        // getters and setters
        public SerialPort getSerialPort()
        {
            return serialPort;
        }



        // ==============================================================
        // methods to interact with ports
        public string[] getAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
        public void configureSerialPort(string portName, int baudRate, Parity parity, sbyte databits, StopBits stopBits, Handshake handShake)
        {
            try
            {
                serialPort.PortName = portName;
                serialPort.BaudRate = baudRate;
                serialPort.Parity = parity;
                serialPort.DataBits = databits;
                serialPort.StopBits = stopBits;
                serialPort.Handshake = handShake;

                Console.WriteLine("port " + serialPort.PortName + " configured successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong. Error: ");
                Console.WriteLine(ex.Message);
                return;
            }
        }
        public void openSerialPort()
        {

            try
            {
                serialPort.Open();
                Console.WriteLine("port " + serialPort.PortName + " opened successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong. Error: ");
                Console.WriteLine(ex.Message);
                return;
            }

        }



        // ==============================================================
        // methods to write in avebus
        public void changeLight1Status()
        {
            sendCommand(CHANGE_LIGHT_STATUS_FRAME_COMMAND);
            Console.WriteLine("command [CHANGE_LIGHT_STATUS_FRAME_COMMAND] sent.");

        }
        public void turnOnLight_1()
        {
            sendCommand(TURN_ON_LIGHT_1_FRAME_COMMAND);
            Console.WriteLine("command [TURN_ON_LIGHT_1_FRAME_COMMAND] sent.");
        }
        public void turnOffLight_1()
        {
            sendCommand(TURN_OFF_LIGHT_1_FRAME_COMMAND);
            Console.WriteLine("command [TURN_OFF_LIGHT_1_FRAME_COMMAND] sent.");
        }
        public void turnOnLight_2()
        {
            sendCommand(TURN_ON_LIGHT_2_FRAME_COMMAND);
            Console.WriteLine("command [TURN_ON_LIGHT_2_FRAME_COMMAND] sent.");
        }
        public void turnOffLight_2()
        {
            sendCommand(TURN_OFF_LIGHT_2_FRAME_COMMAND);
            Console.WriteLine("command [TURN_OFF_LIGHT_2_FRAME_COMMAND] sent.");
        }
        private byte[] bitwiseNot(byte[] command)
        {
            byte[] bitwiseInvertedCommand = new byte[command.Length];

            for (int i = 0; i < command.Length; i++)
            {
                bitwiseInvertedCommand[i] = (byte)~command[i];
            }
            return bitwiseInvertedCommand;
        }

        // actually send frame in AveBus
        private void sendCommand(byte[] command)
        {
            try
            {
                serialPort.Write(bitwiseNot(command), 0, command.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong. Error: ");
                Console.WriteLine(ex.Message);
                return;
            }

        }



        // ==============================================================
        // methods to read from avebus
        public void startReadingBus()
        {
            // evita doppio start
            if (readBusThread != null && readBusThread.IsAlive)
                return;

            read = true;
            readBusThread = new Thread(readBusLoop);
            readBusThread.Start();
            Console.WriteLine("started reading AveBus interface");
        }
        public void stopReadingBus()
        {
            this.read = false;
            Console.WriteLine("stopped reading AveBus interface");
        }
        private void readBusLoop()
        {
            serialPort.DiscardInBuffer();

            byte[] firstTwoBytesBuf = new byte[2];
            byte[] msgBuf;
            int len;

            while (read)
            {
                // PEEK
                if (serialPort.BytesToRead < 2)
                {
                    Thread.Sleep(50);
                    continue;
                }

                serialPort.Read(firstTwoBytesBuf, 0, 2);         // leggo i primi due byte
                len = (byte)((~firstTwoBytesBuf[1] & 0x1F) + 1); // calcolo lunghezza pacchetto

                // frame sporco
                if (len < 7 || len > 32)
                {
                    serialPort.DiscardInBuffer();
                    continue;
                }

                msgBuf = new byte[len];

                // copio i primi due byte
                msgBuf[0] = firstTwoBytesBuf[0];
                msgBuf[1] = firstTwoBytesBuf[1];

                int remaining = len - 2;   // byte ancora da leggere
                int offset = 2;            // dove scrivere nel buffer

                // leggo bytes rimanenti
                while (remaining > 0)
                {
                    int readNow = serialPort.Read(msgBuf, offset, remaining);
                    offset += readNow;
                    remaining -= readNow;
                    Thread.Sleep(50);
                }

                // messaggio completo
                string message = "";
                for (int i = 0; i < msgBuf.Length; i++)
                {
                    msgBuf[i] = (byte)~msgBuf[i];
                    message += msgBuf[i].ToString("X2") + " ";
                }

                propagateEvent("PRINT_LOG", "[ " + message + "]" + Environment.NewLine);
                updateLightStatusIndicators(message);
            }
        }
        private void updateLightStatusIndicators(string message)
        {
            message = message.Trim();

            if (message.Equals("40 07 27 27 4E 01 FA E9".Trim())) propagateEvent("LIGHT_STATUS", "TURN_ON_LIGHT_1_FRAME_COMMAND");
            if (message.Equals("40 07 27 27 4E 03 FA EB".Trim())) propagateEvent("LIGHT_STATUS", "TURN_OFF_LIGHT_1_FRAME_COMMAND");
            if (message.Equals("40 07 26 26 4E 01 FA E7".Trim())) propagateEvent("LIGHT_STATUS", "TURN_ON_LIGHT_2_FRAME_COMMAND");
            if (message.Equals("40 07 26 26 4E 03 FA E9".Trim())) propagateEvent("LIGHT_STATUS", "TURN_OFF_LIGHT_2_FRAME_COMMAND");
            if (message.Equals("40 07 27 27 4E 02 FA EA".Trim())) { }
        }



        // ==============================================================
        // callback 
        void propagateEvent(string eventKey, string eventValue)
        {
            if (guiCallback != null)
            {
                guiCallback(eventKey, eventValue);
            }

        }
    }

}
