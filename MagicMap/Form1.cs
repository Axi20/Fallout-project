using System;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace MagicMap
{   
    public partial class Form1 : Form
    {    
        string Owner = "";
        public string date;
        string usernameSR = "";
        string hostSR = "";
        string buildSR = "";
        string modelSR = "";
        public Form1()
        {
            InitializeComponent();
        }

    //Dont edit
    public class Drive
    {
     [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetVolumeInformation(
            string rootPathName,
            StringBuilder volumeNameBuffer,
            int volumeNameSize,
            ref uint volumeSerialNumber,
            ref uint maximumComponentLength,
            ref uint fileSystemFlags,
            StringBuilder fileSystemNameBuffer,
            int nFileSystemNameSize);

            public string VolumeName { get; private set; }

            public string FileSystemName { get; private set; }

            public uint SerialNumber { get; private set; }

            public string DriveLetter { get; private set; }

            public static Drive GetDrive(string driveLetter)
            {
                const int VolumeNameSize = 255;
                const int FileSystemNameBufferSize = 255;
                StringBuilder volumeNameBuffer = new StringBuilder(VolumeNameSize);
                uint volumeSerialNumber = 0;
                uint maximumComponentLength = 0;
                uint fileSystemFeatures = 0;
                StringBuilder fileSystemNameBuffer = new StringBuilder(FileSystemNameBufferSize);

                if (GetVolumeInformation(
                    string.Format("{0}:\\", driveLetter),
                    volumeNameBuffer,
                    VolumeNameSize,
                    ref volumeSerialNumber,
                    ref maximumComponentLength,
                    ref fileSystemFeatures,
                    fileSystemNameBuffer,
                    FileSystemNameBufferSize))
                {
                    return new Drive
                    {
                        DriveLetter = driveLetter,
                        FileSystemName = fileSystemNameBuffer.ToString(),
                        VolumeName = volumeNameBuffer.ToString(),
                        SerialNumber = volumeSerialNumber
                    };
                }

                // Something failed, returns null
                return null;
            }
        }
    public class NetworkAPI
        {
            // USER_INFO_1 - Strucutre to hold obtained user information  
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct USER_INFO_1
            {
                public string usri1_name;
                public string usri1_password;
                public int usri1_password_age;
                public int usri1_priv;
                public string usri1_home_dir;
                public string comment;
                public int usri1_flags;
                public string usri1_script_path;
            }
            // USER_INFO_0 - Structure to hold Just Usernames  
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct USER_INFO_0
            {
                public String Username;
            }
            // NetUserAdd - To Add Users to a local machine or Network  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserAdd([MarshalAs(UnmanagedType.LPWStr)] string servername, int level, ref USER_INFO_1 buf, int parm_err);
            // NetUserDel - To delete Users from a local machine or Network  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserDel([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string username);
            // NetUserGetInfo - Returns to a struct Information about the specified user  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserGetInfo([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string username, int level, out IntPtr bufptr);
            // NetUserSetInfo - Allows us to modify User information  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserSetInfo([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string username, int level, ref USER_INFO_1 buf, int error);
            // NetUserChangePassword - Allows us to change a users password providing we have it  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserChangePassword([MarshalAs(UnmanagedType.LPWStr)] string domainname, [MarshalAs(UnmanagedType.LPWStr)] string username, [MarshalAs(UnmanagedType.LPWStr)] string oldpassword, [MarshalAs(UnmanagedType.LPWStr)] string newpassword);
            // NetUserEnum - Obtains a list of all users on local machine or network  
            [DllImport("Netapi32.dll")]
            public extern static int NetUserEnum(string servername, int level, int filter, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, out int resume_handle);
            // NetAPIBufferFree - Used to clear the Network buffer after NetUserEnum  
            [DllImport("Netapi32.dll")]
            public extern static int NetApiBufferFree(IntPtr Buffer);
            public NetworkAPI()
            {
                //  
                // TODO: Add constructor logic here  
                //  
            }
        } 
        public void EnumerateUsers()
        {
            int EntriesRead;
            int TotalEntries;
            int Resume;
            IntPtr bufPtr;
            NetworkAPI.NetUserEnum(null, 0, 2, out bufPtr, -1, out EntriesRead, out TotalEntries, out Resume);
            if (EntriesRead > 0)
            {
                listBox2.Items.Add("Other users:");
                NetworkAPI.USER_INFO_0[] Users = new NetworkAPI.USER_INFO_0[EntriesRead];
                IntPtr iter = bufPtr;
                for (int i = 0; i < EntriesRead; i++)
                {
                    Users[i] = (NetworkAPI.USER_INFO_0)Marshal.PtrToStructure(iter, typeof(NetworkAPI.USER_INFO_0));
                    iter = (IntPtr)((int)iter + Marshal.SizeOf(typeof(NetworkAPI.USER_INFO_0)));
                    listBox2.Items.Add(Users[i].Username);
                }
                NetworkAPI.NetApiBufferFree(bufPtr);
            }
        } 
        public void GetRegisteredOwner()
        {
            OperatingSystem osInfo = System.Environment.OSVersion;
            if (osInfo.Platform == PlatformID.Win32Windows)
            {                                               
                Owner = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion", "RegisteredOwner", "Unknown").ToString();
            }
            else if (osInfo.Platform == PlatformID.Win32NT)
            {
                // for NT+                
                RegistryKey localKey;
                if (Environment.Is64BitOperatingSystem)
                    localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                Owner = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion").GetValue("RegisteredOwner", "Unknown").ToString();
            }
        }
        private void GetBiosInformation()
        {
            string relDt = "";
            ManagementObjectSearcher mSearcher = new ManagementObjectSearcher("SELECT SerialNumber, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            ManagementObjectCollection collection = mSearcher.Get();
            foreach (ManagementObject obj in collection)
            {
                relDt = (string)obj["ReleaseDate"];
                DateTime dt = ManagementDateTimeConverter.ToDateTime(relDt);
                label25.Text = "BIOS: " + (string)obj["SMBIOSBIOSVersion"] + " " + (string)obj["SerialNumber"] + " " + "UPDATED: " + dt.ToString("dd-MMM-yyyy");
            }
        }

        //User informations 
        private void button1_Click_1(object sender, EventArgs e)
        {
            EnumerateUsers();
            GetRegisteredOwner();
            tabControl1.SelectedTab = tabPage3;
            string networkName = Environment.UserDomainName;
            string username = System.Windows.Forms.SystemInformation.UserName;
            string userdomain = System.Windows.Forms.SystemInformation.UserDomainName;
            string domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            TimeZoneInfo tzinfo = TimeZoneInfo.Local;
            CultureInfo ci = CultureInfo.InstalledUICulture;
            usernameSR = username;
            label7.Text = "Registered owner: " + Owner;
            label9.Text = "Current user: " + username;
            label10.Text = "Language: " + ci;
            label11.Text = "Timezone: " + tzinfo;
            label13.Text = "User domain: " + userdomain;
            label14.Text = "Network name: " + networkName;
            label15.Text = "Machine name: " + Environment.MachineName;
        }
        //PC informations  
        private void button2_Click_1(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage4;
            string dnsHost = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
            string hostName = System.Net.Dns.GetHostName();
            string bootmode = System.Windows.Forms.SystemInformation.BootMode.ToString();
            string systemType = "";
            int uptime = Environment.TickCount;
            hostSR = hostName;

            if (Environment.Is64BitOperatingSystem == true)
            {
                systemType = "x64-based PC";
            }
            else { systemType = "X32-based PC"; }

            label8.Text = "DNS-based hostname: " + dnsHost;
            label12.Text = "Hostname: " + hostName;
            label16.Text = "BOOT mode: " + bootmode;
            label17.Text = "BOOT time: " + uptime;
            label18.Text = "System type: " + systemType;

            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
            ManagementObjectCollection moc = mc.GetInstances();
            if (moc.Count != 0)
            {
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    label19.Text = "System manufacturer: " + mo["Manufacturer"].ToString();
                    label20.Text = "System model: " + mo["Model"].ToString();
                    modelSR = mo["Model"].ToString();
                }
            }
        }
        //System informations 
        private void button3_Click(object sender, EventArgs e)
        {
            Drive drive = Drive.GetDrive("C");
            GetBiosInformation();
            tabControl1.SelectedTab = tabPage6;
            double totalCapacity = 0;
            ObjectQuery objectQuery = new ObjectQuery("select * from Win32_PhysicalMemory");
            ManagementObjectSearcher searcher = new
            ManagementObjectSearcher(objectQuery);
            ManagementObjectCollection vals = searcher.Get();

            foreach (ManagementObject val in vals)
            {
                totalCapacity += System.Convert.ToDouble(val.GetPropertyValue("Capacity"));
            }

            label21.Text = "Total Machine Memory = " + totalCapacity.ToString() + " Bytes";
            label22.Text = "Total Machine Memory = " + (totalCapacity / 1024) + " KiloBytes";
            label23.Text = "Total Machine Memory = " + (totalCapacity / 1048576) + "    MegaBytes";
            label24.Text = "Total Machine Memory = " + (totalCapacity / 1073741824) + " GigaBytes";
            label26.Text = "System directory: " + Environment.SystemDirectory;
            var slash = @"\WINDOWS";
            label27.Text = "Windows directory: C:" + slash;
            label28.Text = "System volume: " + string.Format(drive.FileSystemName);    
        }
        //OS Details 
        private void button4_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
            OperatingSystem os = Environment.OSVersion;
            label46.Text = "OS Version: " + os.Version.ToString();
            label45.Text = "OS Platform: " + os.Platform.ToString();
            if(os.ServicePack == string.Empty)
                label44.Text = "OS ServicePack: Not found";
            else{ label44.Text = "OS ServicePack: " + os.ServicePack; }

            
            label6.Text = "OS Version string: " + os.VersionString.ToString();
            //Get version details
            Version ver = os.Version;
            label5.Text = "Major Version: " + ver.Major;
            label4.Text = "Major Revision: " + ver.MajorRevision;
            label3.Text = "Minor Version: " + ver.Minor;
            label2.Text = "Minor Revision: " + ver.MinorRevision;
            label1.Text = "Build: " + ver.Build;
            buildSR = ver.Build.ToString();
        }
        //Server room
        private void button5_Click(object sender, EventArgs e)
        {
            int uptime = Environment.TickCount;
            label17.Text = "BOOT time: " + uptime;
        }
        //Refresh
        private void button6_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage7;
        }   
        //Back to map from user info
        private void button7_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }
        //Arrow button
        private void button8_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }
        //Connect
        private void button9_Click(object sender, EventArgs e)
        {
            bool answer = false;
            if(textBox8.Text == usernameSR && textBox7.Text == hostSR && textBox6.Text == buildSR && textBox5.Text == modelSR)
                answer = true;

            if (answer)
            {
                MessageBox.Show("Authenticated as ROOT", "Success", MessageBoxButtons.OK);
                MessageBox.Show("You gain access to the back up server, so you can read the secret file now." +
                    "The secret file location is: " + Environment.CurrentDirectory, "Save the future", MessageBoxButtons.OK);
                StreamWriter sw = new StreamWriter("MessageFromTheFuture_secret_file.txt", false);
                sw.WriteLine("<This is the secret file>");
                sw.WriteLine("");
                sw.WriteLine("We have reached our destination. After overcoming many obstacles, we entered the last room.");
                sw.WriteLine("");
                sw.WriteLine("Here we had to look through more computers than expected, where we found a lot of information left in the past.");
                sw.WriteLine("");
                sw.WriteLine("After a few computers we finally found the folder we were looking for.\r\nAfter reading hundreds of lines, we finally reached the last lines.");
                sw.WriteLine("");
                sw.WriteLine("");
                sw.WriteLine("30.11.2022: \"After a lot of research, I finally found out what the solution to the virus could be. ");
                sw.WriteLine("");
                sw.WriteLine("Unfortunately, I won't be able to share this with the world anymore, but I hope someone will find it in the near future...");
                sw.WriteLine("");
                sw.WriteLine("The antidote to stupidity is none other than...\" ");
                sw.WriteLine("");
                sw.WriteLine("CORRUPT FILE");
                sw.Flush();
                sw.Close();
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show("Access denied!", "Failed", MessageBoxButtons.OK);
                answer = false;
            }        
        }
        //Back to map from pc info
        private void button10_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }
        //Back to map from os info
        private void button11_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }
        //Back to map from sys info
        private void button12_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }
    }
}
