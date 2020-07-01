using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Permissions;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections;
using System.Net;
using System.Threading;
using System.Diagnostics;


namespace HTTPPOST
{
    public partial class Form1 : Form
    {
        private string path;
        
       
        private bool upload_flag = true;
        Queue queue_file = new Queue();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            path = txtDirectory.Text;
            watch(); //FileSystemWatcher Start!
           
        }

        private void watch()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(OnChanged); //Event occurs to add queue
            watcher.EnableRaisingEvents = true;
        }

        private static string Upload(string actionUrl, string paramString, /*Stream paramFileStream, */byte[] paramFileBytes)
        {
            //HttpContent stringContent = new StringContent(paramString);
            //HttpContent fileStreamContent = new StreamContent(paramFileStream);
            HttpContent bytesContent = new ByteArrayContent(paramFileBytes);
            
             using (var client = new HttpClient())
             using (var formData = new MultipartFormDataContent())
             {
              //   formData.Add(stringContent, "param1", "param1");
              //   formData.Add(fileStreamContent, "ile1", "file1");
                 formData.Add(bytesContent, "fileToUpload", paramString);
                 var response = client.PostAsync(actionUrl, formData).GetAwaiter().GetResult();
                 Debug.WriteLine("HTTP Response:" + response.StatusCode);
                if (!response.IsSuccessStatusCode)
                 {
                     return null;
                 }
                 return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
             }
            
        }
        private static Object lockGuard = new Object();
       

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(e.FullPath);

                queue_file.Enqueue(fileBytes);
                //Although queue is always 1, because it runs OnChanged function one by one sequentially not asynchronously, so don't worry.
                //In fact, no need queue i think. OnChanged runs sequentially.
                Debug.WriteLine("File added to queue.");
                Debug.WriteLine("Current queue count:" + queue_file.Count); while (queue_file.Count > 0)
                {
                    if (upload_flag == true)        //upload allowed?
                    {
                        upload_flag = false;        //upload lock

                       
                          //   var response = Upload("http://localhost/fupload/upload.php", e.Name, fileBytes);
                            var response = Upload("http://dev1.webminus.co.uk/upload.php", e.Name, fileBytes); 
                            if (response != null)
                            {

                                queue_file.Dequeue();
                                upload_flag = true;     //upload lock released
                               
                            }
                        
                    }
                }
           
            }
            catch(Exception ex)
            {
                string sss = ex.ToString();
            }
        }


    }
 
   
    public class TaskQueue
    {
        private SemaphoreSlim semaphore;
        public TaskQueue()
        {
            semaphore = new SemaphoreSlim(1);
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            await semaphore.WaitAsync();
            try
            {
                return await taskGenerator();
            }
            finally
            {
                semaphore.Release();
            }
        }
        public async Task Enqueue(Func<Task> taskGenerator)
        {
            await semaphore.WaitAsync();
            try
            {
                await taskGenerator();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
