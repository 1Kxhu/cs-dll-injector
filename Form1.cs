using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cs_dll_injector
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 0x04;

        public Form1()
        {
            InitializeComponent();
        }

        private void PopulateProcessList()
        {
            Process[] processes = Process.GetProcesses();
            List<string> processNames = new List<string>();
            foreach (Process process in processes)
            {
                processNames.Add(process.ProcessName);
            }
            processNames.Sort();
            listBox1.DataSource = processNames;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            PopulateProcessList();

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void injectDll(string dllPath)
        {
            Process targetProcess = null;

            try
            {
                // Get target process.
                string processName = listBox1.SelectedItem.ToString();
                targetProcess = Process.GetProcessesByName(processName)[0];

                // Open target process for writing.
                IntPtr processHandle = OpenProcess(
                    PROCESS_CREATE_THREAD |
                    PROCESS_QUERY_INFORMATION |
                    PROCESS_VM_OPERATION |
                    PROCESS_VM_WRITE |
                    PROCESS_VM_READ,
                    false,
                    targetProcess.Id
                );

                // Allocate memory in target process.
                IntPtr memoryAddress = VirtualAllocEx(
                    processHandle,
                    IntPtr.Zero,
                    (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))),
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_READWRITE
                );

                // Write DLL path to allocated memory in target process.
                IntPtr bytesWritten;
                byte[] buffer = Encoding.Unicode.GetBytes(dllPath + "\0");
                WriteProcessMemory(
                    processHandle,
                    memoryAddress,
                    buffer,
                    (uint)buffer.Length,
                    out bytesWritten
                );

                // Get LoadLibraryW address.
                IntPtr loadLibraryAddress = GetProcAddress(
                    GetModuleHandle("kernel32.dll"),
                    "LoadLibraryW"
                );

                // Create remote thread in target process to load DLL.
                CreateRemoteThread(
                    processHandle,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddress,
                    memoryAddress,
                    0,
                    IntPtr.Zero
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Injection failed: " + ex.Message);
            }
            finally
            {
                if (targetProcess != null)
                {
                    targetProcess.Dispose();
                }
            }
            return;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dynamic Link Libs|*.dll";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedDllPath = openFileDialog.FileName;
                injectDll(selectedDllPath);
            }
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            PopulateProcessList();
            guna2VScrollBar1.Maximum = listBox1.Items.Count;
        }
    }
}
