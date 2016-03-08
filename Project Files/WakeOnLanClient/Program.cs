#region Licence
//MIT License(MIT)


/*     WakeOnLAN Version 1.0        */

/*     Copyright(c) 2016 Mike Simpson      */

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
#endregion

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace WakeOnLanClient
{
    class Program
    {

        static void Main(string[] args)
        {
            bool run = true;
            int attempts = 1;
            int sleepTime = 1;
            string targetMacAddress = "";
            string targetIPAddress = "";

            if(args.Length == 4)
            {
                if(!Int32.TryParse(args[0], out attempts))
                {
                    endProgram("Please enter a valid number of attempts");
                }
                if (attempts < 0)
                {
                    endProgram("Please enter a valid number of attempts");
                }
                if (!Int32.TryParse(args[1], out sleepTime))
                {
                    endProgram("Please enter a valid number for sleep time");
                }
                if (sleepTime < 0)
                {
                    endProgram("Please enter a valid number for sleep time");
                }
                targetMacAddress = args[2];
                targetIPAddress = args[3];
            }
            else
            {
                endProgram("Incorrect number of arguments \nFormat = 'Number of attempts, wait time(ms), MAC Address (xx:xx:xx:xx:xx:xx), IP Address");
            }
            
            attempts--;
            while (run)
            {
                Console.Write("Attempting to wake Device with MAC Address: ");
                byte[] macAddress = StringToByteArray(targetMacAddress);
                Console.WriteLine(targetMacAddress);
                WakeOnLan(macAddress);
                Console.WriteLine("Device should now be booting up");
                Console.WriteLine("Waiting 10s for device to boot");
                Thread.Sleep(sleepTime);
                Console.WriteLine("Attempting to ping device");
                string s = PingHost(targetIPAddress);
                if (s.Contains("Reply from"))
                {
                    Console.WriteLine(s);
                    endProgram("Device is now on, you may need to wait 60s for drives to be accessible");
                    run = false;
                }
                else
                {
                    if(attempts == 0)
                    {
                        run = false;
                        endProgram("Cannot wake device, you may need to manually boot the device");
                    }
                    else
                    {
                        attempts--;
                        Console.WriteLine("Attempting to wake device again");
                    }
                }                              
            }
        }

        private static void endProgram(string endReason)
        {
            Console.WriteLine("Program finished - " + endReason);
            Console.WriteLine("Press anykey to end");
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static string PingHost(string host)
        {
            //string to hold our return messge
            string returnMessage = string.Empty;

            //IPAddress instance for holding the returned host
            IPAddress address = IPAddress.Loopback;

            IPAddress.TryParse(host, out address);

            //set the ping options, TTL 128
            PingOptions pingOptions = new PingOptions(128, true);

            //create a new ping instance
            Ping ping = new Ping();

            //32 byte buffer (create empty)
            byte[] buffer = new byte[32];

            //here we will ping the host 4 times (standard)
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    //send the ping 4 times to the host and record the returned data.
                    //The Send() method expects 4 items:
                    //1) The IPAddress we are pinging
                    //2) The timeout value
                    //3) A buffer (our byte array)
                    //4) PingOptions
                    PingReply pingReply = ping.Send(address, 1000, buffer, pingOptions);

                    //make sure we dont have a null reply
                    if (!(pingReply == null))
                    {
                        switch (pingReply.Status)
                        {
                            case IPStatus.Success:
                                returnMessage = string.Format("Reply from {0}: bytes={1} time={2}ms TTL={3}", 
                                    pingReply.Address, pingReply.Buffer.Length, pingReply.RoundtripTime, pingReply.Options.Ttl);                                
                                break;
                            case IPStatus.TimedOut:
                                returnMessage = "Connection has timed out...";
                                break;
                            default:
                                returnMessage = string.Format("Ping failed: {0}", pingReply.Status.ToString());
                                break;
                        }
                    }
                    else
                    {
                        returnMessage = "Connection failed for an unknown reason...";
                    }
                }
                catch (PingException ex)
                {
                    returnMessage = string.Format("Connection Error: {0}", ex.Message);
                }
                catch (SocketException ex)
                {
                    returnMessage = string.Format("Connection Error: {0}", ex.Message);
                }
            }
            //return the message
            return returnMessage;
        }

        /// <summary>
        /// Sends a Wake-On-Lan packet to the specified MAC address.
        /// </summary>
        /// <param name="mac">Physical MAC address to send WOL packet to.</param>
        /// By Michael List, 7/30/2013  @http://dotnet-snippets.com/snippet/wake-on-lan/1698
        private static void WakeOnLan(byte[] mac)
        {
            // WOL packet is sent over UDP 255.255.255.0:40000.
            UdpClient client = new UdpClient();
            client.Connect(IPAddress.Broadcast, 40000);

            // WOL packet contains a 6-bytes trailer and 16 times a 6-bytes sequence containing the MAC address.
            byte[] packet = new byte[17 * 6];

            // Trailer of 6 times 0xFF.
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            // Body of magic packet contains 16 times the MAC address.
            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];

            // Send WOL packet.
            client.Send(packet, packet.Length);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static byte[] StringToByteArray(string hex)
        {
            hex = hex.Replace(":", "");
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            try
            {
                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
            }
            catch(FormatException Ex)
            {
                endProgram(Ex.Message);
            }
            return bytes;
        }
    }
}
