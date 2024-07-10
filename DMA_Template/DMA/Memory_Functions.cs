using DMA_Template.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DMA_Template.DMA
{
    public class Memory_Functions
    {
        //Essentials
        public static Vmm.MAP_MODULEENTRY Module;
        public static uint _pid;


        //init Device
        public static Vmm vmm = new Vmm("-printf", "-v", "-device", "fpga");

        public bool ObtainProcessID(string ProcessName)
        {
            return vmm.PidGetFromName(ProcessName, out _pid);
        }




        //Patch CR3
        #region CR3
        public static bool FixCr3(string ModuleName)
        {
            Module = vmm.Map_GetModuleFromName(_pid, ModuleName);
            Console.WriteLine("Attempting to fix CR3 Current ID: 0x" + Module.vaBase.ToString("X"));
            if (Module.vaBase != 0)
            {
                Console.WriteLine("GameAssembly = " + Module.vaBase.ToString("X"));

                return true;
            }
            vmm.InitializePlugins();

            Thread.Sleep(5000);

            while (true)
            {
                byte[] bytes = new byte[4];
                uint i = 0;

                ulong nt = vmm.VfsRead("\\misc\\procinfo\\progress_percent.txt", 3, i, out bytes);

                string fileContent = System.Text.Encoding.Default.GetString(bytes);
                if (int.TryParse(fileContent, out int result) && result == 100)
                {
                    Console.WriteLine("Equals 100");
                    break;
                }




            }

            List<string> possibleDtbs = new List<string>();
            List<string> allParts = new List<string>();

            try
            {
                byte[] bytes;
                ulong dtbDataBytes = vmm.VfsRead("\\misc\\procinfo\\dtb.txt", 32768, 0, out bytes);
                string dtbData = System.Text.Encoding.UTF8.GetString(bytes);


                string[] lines = dtbData.Split('\n');
                foreach (string line in lines)
                {

                    string[] parts = line.Split();
                    if (parts.Length >= 5)
                    {
                        string a = string.Join(",", parts);
                        allParts.Add(a);
                    }

                }
                for (int i = 0; i < allParts.Count; i++)
                {
                    allParts[i] = Regex.Replace(allParts[i], ",+", ",");
                }

                foreach (string part in allParts)
                {
                    string[] items = part.Split(',');
                    string index = items[0];
                    string pid = items[1];
                    string dtb = items[2];
                    string kerneladdr = items[3];
                    string name = items[4];
                    int pidd = int.Parse(pid);
                    if (pidd == 0 | pidd == _pid)
                    {
                        possibleDtbs.Add(dtb);
                        Console.WriteLine("DTBVALUE: " + items);
                        Console.WriteLine("DTBVALUE: " + name);

                    }
                }
                foreach (string dtb in possibleDtbs)
                {
                    ulong dtbValue;
                    ulong.TryParse(dtb, System.Globalization.NumberStyles.HexNumber, null, out dtbValue);
                    Console.WriteLine("DTBVALUE: " + dtbValue);
                    vmm.ConfigSet(Vmm.OPT_PROCESS_DTB | _pid, dtbValue);
                    try
                    {
                        Module = vmm.Map_GetModuleFromName(_pid, ModuleName);
                        Console.WriteLine("Attempting to fix2 CR3 Current ID: 0x" + Module.vaBase.ToString("X"));
                        Console.WriteLine("Returned True.?");
                        if (Module.vaBase != 0)
                        {
                            return true;
                        }
                        //return true;


                    }
                    catch { }
                }

            }
            catch { }
            Console.WriteLine("Failed.");
            return false;
        }
        #endregion








        //ReadProcessMemory
        #region ReadMemory
        public static T ReadMemory<T>(ulong address)
        {
            if (address != 0)
            {
                uint size = (uint)Marshal.SizeOf(typeof(T));
                byte[] buffer = vmm.MemRead(_pid, address, size, Vmm.FLAG_NOCACHE);
                T result = default(T);
                result = BytesTS<T>(buffer);
                return result;

            }
            else
            {
                return default(T);
            }
        }
        public static T BytesTS<T>(byte[] buffer)
        {
            T result = default(T);
            int size = buffer.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            result = (T)Marshal.PtrToStructure(ptr, result.GetType());

            Marshal.FreeHGlobal(ptr);
            return result;
        }
        #endregion



    }
}
