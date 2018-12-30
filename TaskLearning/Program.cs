using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskLearning
{
    class Program
    {
        private const string url = "http://www.cninnovation.com";
        public static void Main(string[] args)
        {
            ShowAggregatedException();
            Console.ReadLine();
        }

        public static void SynchronizedAPI()
        {
            Console.WriteLine(nameof(SynchronizedAPI));
            using (var client = new WebClient())
            {
                string content = client.DownloadString(url);
                Console.WriteLine(content.Substring(0, 100));
            }
            Console.WriteLine();
        }

        public static void AsynchronousPattern()
        {
            Console.WriteLine(nameof(AsynchronousPattern));
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            IAsyncResult result = request.BeginGetResponse(ReadResponse, null);

            void ReadResponse(IAsyncResult ar)
            {
                using(var response = request.EndGetResponse(ar))
                {
                    Stream stream = response.GetResponseStream();
                    var reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    Console.WriteLine(content.Substring(0, 100));
                }
                Console.WriteLine();
            }
            Console.ReadKey();
        }

        public static void EventBasedAsyncPattern()
        {
            Console.WriteLine(nameof(EventBasedAsyncPattern));
            using(WebClient client = new WebClient())
            {
                client.DownloadStringCompleted += (sender, e) =>
                {
                    Console.WriteLine(e.Result.Substring(0, 100));
                };
                client.DownloadStringAsync(new Uri(url));
            }
            Console.ReadKey();
        }

        public static async Task TaskBasedAsyncPatternAsync()
        {
            Console.WriteLine(nameof(TaskBasedAsyncPatternAsync));
            using(WebClient client = new WebClient())
            {
                string content = await client.DownloadStringTaskAsync(url);
                Console.WriteLine(content.Substring(0, 100));
                Console.WriteLine();
            }
        }

        public static void TraceThreadAndTask(string info)
        {
            string taskInfo = Task.CurrentId == null ? "no task" : "task " +
            Task.CurrentId;
            Console.WriteLine($"{info} in thread {Thread.CurrentThread.ManagedThreadId}" +
            $" and {taskInfo}");
        }

        public static string Greeting(string name)
        {
            TraceThreadAndTask($"running {nameof(Greeting)}");
            Task.Delay(3000).Wait();
            return $"Hello, {name}";
        }

        public static Task<string> GreetingAsync(string name) =>
            Task.Run<string>(() =>
            {
                TraceThreadAndTask($"running {nameof(GreetingAsync)}");
                return Greeting(name);
            });

        public static async void CallerWithAsync()
        {
            TraceThreadAndTask($"started {nameof(CallerWithAsync)}");
            string result = await GreetingAsync("Roka");
            Console.WriteLine(result);
            TraceThreadAndTask($"ended {nameof(CallerWithAsync)}");
        }

        public static void CallerWithAwaiter()
        {
            TraceThreadAndTask($"started {nameof(CallerWithAwaiter)}");
            TaskAwaiter<string> awaiter = GreetingAsync("Roka").GetAwaiter();
            awaiter.OnCompleted(OnCompletedAwaiter);

            void OnCompletedAwaiter()
            {
                Console.WriteLine(awaiter.GetResult());
                TraceThreadAndTask($"ended {nameof(CallerWithAwaiter)}");
            }
        }

        public static void CallerWithContinuationTask()
        {
            TraceThreadAndTask($"started {nameof(CallerWithContinuationTask)}");

            var t1 = GreetingAsync("Roka");

            t1.ContinueWith(t =>
            {
                Console.WriteLine(t.Result);
                TraceThreadAndTask($"ended {nameof(CallerWithContinuationTask)}");
            });
        }

        public async static void MultipleAsyncMethods()
        {
            string s1 = await GreetingAsync("Roka");
            string s2 = await GreetingAsync("Toch");
            Console.WriteLine($"Finished both methods.{Environment.NewLine} " +
                $"Result 1: {s1}{Environment.NewLine} Result 2: {s2}");
        }

        public async static void MultipleAsyncMethodsWithCombinators()
        {
            Task<string> t1 = GreetingAsync("Roka");
            Task<string> t2 = GreetingAsync("Toch");
            await Task.WhenAll(t1, t2);
            Console.WriteLine($"Finished both methods.{Environment.NewLine} " +
                $"Result 1: {t1.Result}{Environment.NewLine} Result 2: {t2.Result}");
        }

        public static async void ConvertingAsyncPattern()
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            using (WebResponse response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse(null, null), request.EndGetResponse))
            {
                Stream stream = response.GetResponseStream();
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    Console.WriteLine(content.Substring(0, 100));
                }
            }
        }

        public static async Task ThrowAfter(int ms, string message)
        {
            await Task.Delay(ms);
            throw new Exception(message);
        }

        public static void DontHandle()
        {
            try
            {
                ThrowAfter(200, "first");
                // exception is not caught
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async void HandleOneException()
        {
            try
            {
                await ThrowAfter(200, "first");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async void StartTwoTasks()
        {
            try
            {
                await ThrowAfter(2000, "first");
                await ThrowAfter(1000, "second");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async void StartTwoTasksParallel()
        {
            try
            {
                Task t1 = ThrowAfter(2000, "first");
                Task t2 = ThrowAfter(1000, "second");
                await Task.WhenAll(t1, t2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async void ShowAggregatedException()
        {
            Task taskResult = null;
            try
            {
                Task t1 = ThrowAfter(2000, "first");
                Task t2 = ThrowAfter(1000, "second");
                await (taskResult = Task.WhenAll(t1, t2));
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Handled {ex.Message}");
                foreach(var ex1 in taskResult.Exception.InnerExceptions)
                {
                    Console.WriteLine($"Inner exception {ex1.Message}");
                }
            }
        }
    }
}
