using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Console = Colorful.Console;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace killswitch_windows
{
    class Program
    {
        const string assLogo =
    @"
 __   .__.__  .__                  .__  __         .__     
|  | _|__|  | |  |   ________  _  _|__|/  |_  ____ |  |__  
|  |/ /  |  | |  |  /  ___/\ \/ \/ /  \   __\/ ___\|  |  \ 
|    <|  |  |_|  |__\___ \  \     /|  ||  | \  \___|   Y  \
|__|_ \__|____/____/____  >  \/\_/ |__||__|  \___  >___|  /
     \/                 \/                       \/     \/ ";

        static private string vpnNIC;
        static private Int32 nicChoice;
        static private bool isKill;
        static private int Time = 30;
        static void Main(string[] args)
        {

            Console.Write("==============================================================", Color.Blue);
            Console.WriteLine(""+assLogo, Color.Cyan);
            Console.WriteLine("==============================================================", Color.Blue);
            if (args.Length > 0)
            {
                Int32.TryParse(args[0],out Time);
            }
            Start();

        }

        static void Start()
        {


            // get list of interfaces
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();


            // ask the user which interface the vpn runs on
            Console.WriteLine("\nSelect which network interface your vpn is running on.");
            //foreach (NetworkInterface n in adapters)
            foreach (var x in adapters.Select((value, index) => new { value, index }))
            {
                Console.WriteLine("\t{0} - {1}", x.index, x.value.Name);
            }
            Console.Write(">");
            nicChoice = Convert.ToInt32(Console.ReadLine());

            // store the interface id to a member value so we can compate when we detect a network change 
            vpnNIC = adapters[nicChoice].Id;


            while (true) { 
                try
                {
                    string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

                    var externalIp = IPAddress.Parse(externalIpString);

                    Console.WriteLine("Network online with current IP as: " + externalIp.ToString());
                }
                catch
                {
                    Console.WriteLine("Network currently down. Bringing Online");
                    // enable internet connection (it could have been disabled before calling this function)
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "ipconfig";
                    info.Arguments = "/renew"; // or /release if you want to disconnect
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    Process p = Process.Start(info);
                    p.WaitForExit();
                }


                //Wait until VPN is operational first.
                if (adapters[nicChoice].OperationalStatus == OperationalStatus.Down)
                {
                    Console.WriteLine("Waiting for VPN to come online");

                    while (adapters[nicChoice].OperationalStatus == OperationalStatus.Down)
                    {

                        Console.WriteLine("waiting 5 seconds");
                        Console.WriteLine(adapters[nicChoice].OperationalStatus);
                        System.Threading.Thread.Sleep(5000);
                        adapters = NetworkInterface.GetAllNetworkInterfaces();
                    }
                    Console.WriteLine("VPN Online");
                }

                // start listening for a network change
                isKill = false;
                NetworkChange.NetworkAddressChanged += new
                NetworkAddressChangedEventHandler(AddressChangedCallback);
                Console.Write("\n[STARTED]", Color.Green);
                Console.Write(" killswitch is now listening for a change in your supplied interface.");

                // stop the program from terminating so we can keep the listener going
                Console.ReadLine();

                Console.Clear();
            }
        }

        static void AddressChangedCallback(object sender, EventArgs e)
        {
            if (!isKill)
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                Console.Beep(570, 100);
                Console.Beep(570, 100);
                Console.Beep(570, 100);
                Console.Beep(570, 100);
                Console.Write("\r\n[NETWORK CHANGE DETECTED]", Color.Red);
                Console.Write(" Checking if our vpn has been disabled.\n");
                if (adapters[nicChoice].OperationalStatus == OperationalStatus.Down)
                {
                    System.Threading.Thread.Sleep(Time * 1000); //wait x seconds for the chance for a different vpn (default 30)
                    adapters = NetworkInterface.GetAllNetworkInterfaces();
                    if (adapters[nicChoice].OperationalStatus == OperationalStatus.Down) {
                        isKill = true;
                        Console.Beep(570, 100);
                        Console.Beep(570, 100);
                        Console.Beep(570, 100);
                        Console.Beep(570, 100);
                        KillNow();
                        Console.Write("[INTERNET DISABLED]", Color.Red);
                        Console.Write(" your vpn was disconnected. (press enter to restart the kill switch)");
                    }
                    else
                    {
                        Console.WriteLine("Saved for now!");
                    }
                }
                
            }

        }

        public static void KillNow()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "ipconfig";
            info.Arguments = "/release";
            info.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = Process.Start(info);
            p.WaitForExit();
        }


    }
}
