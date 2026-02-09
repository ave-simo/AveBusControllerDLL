using System;
using System.IO.Ports;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace AveBusControllerDLL
{
    public class AveBusController
    {
        private static SerialPort serialPort = new SerialPort();

        private static bool read = false;
        private static Thread readBusThread;

        // questo non è una funzione, è il tipo della funzione
        public delegate void BusGuiCallback(string key, string value);
        private static BusGuiCallback guiCallback = null; // questa è la funzione di callback richiamabile. E' messa a null e impostabile tramite un setter per permettere a programmi esterni di interagirci.

        // light 1
        private static byte[] CHANGE_LIGHT_STATUS_FRAME_COMMAND    = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x02, 0xFA, 0xEA };
        private static byte[] TURN_ON_LIGHT_1_FRAME_COMMAND        = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x01, 0xFA, 0xE9 };
        private static byte[] TURN_OFF_LIGHT_1_FRAME_COMMAND       = new byte[] { 0x40, 0x07, 0x27, 0x27, 0x4E, 0x03, 0xFA, 0xEB };
        private static byte[] LIGHT_1_STATUS_REQUEST_FRAME_COMMAND = new byte[] { 0x20, 0x06, 0x26, 0x01, 0x40, 0xFB, /*0x92*/ 0x91 }; // TODO
        private static byte[] LIGHT_1_STATUS_RESPONSE_FRAME_ON     = new byte[] { 0x20, 0x09, 0x01, 0x26, 0x04, 0x10, 0x00, 0x00, 0xF8, 0x6C }; // TODO
        private static byte[] LIGHT_1_STATUS_RESPONSE_FRAME_OFF    = new byte[] { 0x20, 0x09, 0x01, 0x26, 0x04, 0x10, 0x00, 0x00, 0xF8, 0x6C }; // TODO 
        private static bool light1statusRequestSent = false;
        private static bool light1statusResponseReceived = false;

        // light 2
        private static byte[] TURN_ON_LIGHT_2_FRAME_COMMAND        = new byte[] { 0x40, 0x07, 0x26, 0x26, 0x4E, 0x01, 0xFA, 0xE7 };
        private static byte[] TURN_OFF_LIGHT_2_FRAME_COMMAND       = new byte[] { 0x40, 0x07, 0x26, 0x26, 0x4E, 0x03, 0xFA, 0xE9 };
        private static byte[] LIGHT_2_STATUS_REQUEST_FRAME_COMMAND = new byte[] { 0x20, 0x06, 0x26, 0x01, 0x40, 0xFB, 0x91 }; // frame 07-03 status req (basic) 0x92
        private static byte[] LIGHT_2_STATUS_RESPONSE_FRAME_ON     = new byte[] { 0x20, 0x09, 0x01, 0x26, 0x04, 0x10, 0x00, 0x00, 0xF8, 0x6C};
        private static byte[] LIGHT_2_STATUS_RESPONSE_FRAME_OFF    = new byte[] { 0x20, 0x09, 0x01, 0x26, 0x04, 0x10, 0x00, 0x00, 0xF8, 0x6C}; //TODO SNIFFA SU BUS
        private static bool light2statusRequestSent = false;
        private static bool light2statusResponseReceived = false;



        // ==============================================================
        // callback setter
        public static void registerEventHandler(BusGuiCallback eventHandler)
        {
            guiCallback = eventHandler;
        }


        // ==============================================================
        // getters and setters
        public static SerialPort getSerialPort()
        {
            return serialPort;
        }



        // ==============================================================
        // methods to interact with ports
        public static string[] getAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
        public static void configureSerialPort(string portName, int baudRate, Parity parity, sbyte databits, StopBits stopBits, Handshake handShake)
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
        public static void openSerialPort()
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
        public static void changeLight1Status()
        {
            sendCommand(CHANGE_LIGHT_STATUS_FRAME_COMMAND);
            Console.WriteLine("command [CHANGE_LIGHT_STATUS_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[CHANGE_LIGHT_STATUS_FRAME_COMMAND]" + Environment.NewLine);

        }
        public static void turnOnLight_1()
        {
            sendCommand(TURN_ON_LIGHT_1_FRAME_COMMAND);
            Console.WriteLine("command [TURN_ON_LIGHT_1_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "TURN_ON_LIGHT_1_FRAME_COMMAND" + Environment.NewLine);
        }
        public static void turnOffLight_1()
        {
            sendCommand(TURN_OFF_LIGHT_1_FRAME_COMMAND);
            Console.WriteLine("command [TURN_OFF_LIGHT_1_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[TURN_OFF_LIGHT_1_FRAME_COMMAND]" + Environment.NewLine);
        }
        public static void sendLight1StatusRequest()
        {
            sendCommand(LIGHT_1_STATUS_REQUEST_FRAME_COMMAND);
            Console.WriteLine("command [LIGHT_1_STATUS_REQUEST_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[LIGHT_1_STATUS_REQUEST_FRAME_COMMAND]" + Environment.NewLine);
        }
        public static void turnOnLight_2()
        {
            sendCommand(TURN_ON_LIGHT_2_FRAME_COMMAND);
            Console.WriteLine("command [TURN_ON_LIGHT_2_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[TURN_ON_LIGHT_2_FRAME_COMMAND]" + Environment.NewLine);
        }
        public static void turnOffLight_2()
        {
            sendCommand(TURN_OFF_LIGHT_2_FRAME_COMMAND);
            Console.WriteLine("command [TURN_OFF_LIGHT_2_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[TURN_OFF_LIGHT_2_FRAME_COMMAND]" + Environment.NewLine);
        }
        public static void sendLight2StatusRequest()
        {
            sendCommand(LIGHT_2_STATUS_REQUEST_FRAME_COMMAND);
            Console.WriteLine("command [LIGHT_2_STATUS_REQUEST_FRAME_COMMAND] sent.");
            propagateEvent("COMMAND_SENT", "[LIGHT_2_STATUS_REQUEST_FRAME_COMMAND]" + Environment.NewLine);
        }
        private static byte[] bitwiseNot(byte[] command)
        {
            byte[] bitwiseInvertedCommand = new byte[command.Length];

            for (int i = 0; i < command.Length; i++)
            {
                bitwiseInvertedCommand[i] = (byte)~command[i];
            }
            return bitwiseInvertedCommand;
        }

        // actually send frame in AveBus
        private static void sendCommand(byte[] command)
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
        public static void startReadingBus()
        {
            // evita doppio start
            if (readBusThread != null && readBusThread.IsAlive)
                return;

            read = true;
            readBusThread = new Thread(readBusLoop);
            readBusThread.Start();
            Console.WriteLine("started reading AveBus interface");
        }
        public static void stopReadingBus()
        {
            read = false;
            Console.WriteLine("stopped reading AveBus interface");
        }
        private static void readBusLoop()
        {
            //serialPort.DiscardInBuffer();

            byte[] firstTwoBytesBuf = new byte[2];
            byte[] msgBuf;
            int len;


            if (!light2statusRequestSent)
            {
                sendLight2StatusRequest(); // asks for light 2 status
                light2statusRequestSent = true;
            }

            while (read)
            {
                // PEEK
                if (serialPort.BytesToRead > 2)
                {
                    serialPort.Read(firstTwoBytesBuf, 0, 2);         // leggo i primi due byte
                    len = (byte)((~firstTwoBytesBuf[1] & 0x1F) + 1); // calcolo lunghezza frame

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
                else
                {
                    Thread.Sleep(50);
                    continue;
                }

            }
        }
        private static void updateLightStatusIndicators(string message)
        {
            message = message.Trim();

            // light 2 status response
            if (!light2statusResponseReceived)
            {
                if (message.Equals("20 09 01 26 04 10 01 00 F8 6C"))  // light 2 is on
                {
                    propagateEvent("LIGHT_STATUS", "TURN_ON_LIGHT_2_FRAME_COMMAND");
                    light2statusResponseReceived = true;
                }
                else if (message.Equals("20 09 01 26 04 10 00 00 F8 6C")) // light 2 is off. controlla se giusto
                {
                    propagateEvent("LIGHT_STATUS", "TURN_OFF_LIGHT_2_FRAME_COMMAND");
                    light2statusResponseReceived = true;
                }
            }

            // light status update
                 if (message.Equals("40 07 27 27 4E 01 FA E9".Trim())) propagateEvent("LIGHT_STATUS", "TURN_ON_LIGHT_1_FRAME_COMMAND");
            else if (message.Equals("40 07 27 27 4E 03 FA EB".Trim())) propagateEvent("LIGHT_STATUS", "TURN_OFF_LIGHT_1_FRAME_COMMAND");
            else if (message.Equals("40 07 26 26 4E 01 FA E7".Trim())) propagateEvent("LIGHT_STATUS", "TURN_ON_LIGHT_2_FRAME_COMMAND");
            else if (message.Equals("40 07 26 26 4E 03 FA E9".Trim())) propagateEvent("LIGHT_STATUS", "TURN_OFF_LIGHT_2_FRAME_COMMAND");
            else if (message.Equals("40 07 27 27 4E 02 FA EA".Trim())) { }


        }



        // ==============================================================
        // callback 
        static void propagateEvent(string eventKey, string eventValue)
        {
            if (guiCallback != null)
            {
                guiCallback(eventKey, eventValue);
            }

        }
    }

}
