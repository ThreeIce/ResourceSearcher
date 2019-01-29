using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace 资源搜索神器
{
    public struct ResourceAddress
    {
        public string Type { get; set; }
        public string Address { get; set; }
        public string Key { get; set; }
        public string From { get; set; }
        public ResourceAddress(string _type,string _address,string _key,string _from)
        {
            Type = _type;
            Address = _address;
            Key = _key;
            From = _from;
        }
    }
    public class RunningError : Exception
    {
        public RunningError(string pname) : base("正在运行中，请勿更改" + pname) { }
    }
    public class ThreadNumbersError : Exception
    {
        public ThreadNumbersError() : base("线程数量太多了，电脑承受不了") { }
    }
    public class Searcher
    {
        public int threadmax { get; private set; } = 50;
        public int Time { get => time ; set { if (!isrun) time = value; else throw new RunningError("运行时间"); } }
        public List<ResourceAddress> SourceAddresses { get; private set; }
        public Action<List<ResourceAddress>> AddAddress { get => addAddress; set { if(!isrun)addAddress = value; else throw new RunningError("完成后调用函数"); } }
        public Action End { get => end; set { if (!isrun) end = value; else throw new RunningError("查到地址后调用函数"); } }
        public bool isrun { get; private set; }
        private Action end;
        private Action<List<ResourceAddress>> addAddress;
        private int time;
        private Queue<string> addresses;
        private Thread[] threads;
        private const string datapath = @"C:\\ProgramData\ResourceSearcher\";
        
        private const string SearchAddress = "<h3 class=\"t\">" + @"<a[\u0000-\uffff]*?href=" + "\"" + @"https?://([^\s]+)" + "\"";
        private static List<ResourceTypeInfo> ResourceTypes;
        private string search;
        public Searcher(int _time) : this(50, _time) { }
        public Searcher(int _time,int _threadmax)
        {
            int max1, max2;
            ThreadPool.GetMaxThreads(out max1, out max2);
            if (threadmax > max1 / 2)
            {
                throw new ThreadNumbersError();
            }
            else
            {
                threadmax = _threadmax;
                time = _time;
            }
            threads = new Thread[threadmax];
        }
        static Searcher()
        {
            //数据目录不存在，即第一次使用
            if (!Directory.Exists(datapath))
            {
                Init();
            }
            if (ResourceTypes == null)
            {
                using (FileStream fs = File.Open(datapath + "ResourceTypes.dat", FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    ResourceTypes = (List<ResourceTypeInfo>)bf.Deserialize(fs);
                }
            }
        }
        public static void Init()
        {
            Directory.CreateDirectory(datapath);
            //资源搜索类型配置
            using (FileStream fs = File.Create(datapath + "ResourceTypes.dat"))
            {
                ResourceTypes = new List<ResourceTypeInfo>();
                ResourceTypes.Add(new ResourceTypeInfo("百度网盘", "https?://(pan.baidu.com/s/[1-9a-zA-Z_]{6,24})",
                    @"密?提?取?码：?:?\s*([a-zA-Z0-9]{4})","<title>百度网盘-链接不存在</title>"));
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, ResourceTypes);
            }

        }
        public static void AddResourceType(ResourceTypeInfo r)
        {
            ResourceTypes.Add(r);
            using(FileStream fs = File.Open(datapath + "ResourceTypes.dat",FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, ResourceTypes);
            }
        }
        public void Start(string search)
        {
            this.search = search;
            addresses = new Queue<string>();
            SourceAddresses = new List<ResourceAddress>();
            isrun = true;
            threads[0] = new Thread(Baidu);
            threads[0].IsBackground = true;
            threads[0].Start();
            for(int i = 2; i < threadmax; i++)
            {
                threads[i] = new Thread(Search);
                threads[i].IsBackground = true;
                threads[i].Start();
            }
            threads[1] = new Thread(e =>
            {
                Thread.Sleep(time);
                lock (threads)
                {
                    for (int i = 2; i < threadmax; i++)
                    {
                        try
                        {
                            threads[i].Abort();
                        }
                        catch
                        {

                        }
                    }
                    try
                    {
                        threads[0].Abort();
                    }
                    catch { }
                }
                if (End != null)
                    End();
                isrun = false;
            });
            threads[1].IsBackground = true;
            threads[1].Start();
        }
        public void Baidu()
        {
            int num = 0;
            Website web = new Website("http://www.baidu.com/s");
            MatchCollection ms;
            lock (addresses)
            {
                head:
                try
                {
                    web.Get("wd", search, "pn", "1", "rn", threadmax.ToString());
                }
                catch { goto head; }
                ms = Regex.Matches(web.content, SearchAddress);
                for (int i = 0; i < ms.Count; i++)
                {
                    addresses.Enqueue("http://" + ms[i].Groups[1].Value);
                }
                num++;
            }
            while (true)
            {
                Thread.Sleep(500);
                lock (addresses)
                {
                    if (addresses.Count >= threadmax)
                        continue;
                }
                try
                {
                    web.Get("wd", search, "pn", (num * threadmax).ToString(), "rn", threadmax.ToString());
                }
                catch
                {
                    continue;
                }
                ms = Regex.Matches(web.content, SearchAddress);
                lock (addresses)
                {
                    for (int i = 0; i < ms.Count; i++)
                    {
                        addresses.Enqueue("http://" + ms[i].Groups[1].Value);
                    }
                }
                num++;
            }
        }
        private void Search()
        {
            string address,key = "无";
            Website web;
            MatchCollection ms;
            int i;
            ResourceAddress s = new ResourceAddress();
            List<ResourceAddress> ss = new List<ResourceAddress>();
            while (true)
            {
                Thread.Sleep(50);
                //领取任务
                lock (addresses)
                {
                    if (addresses.Count > 0)
                        address = addresses.Dequeue();
                    else
                        continue;
                }
                web = new Website(address);
                try
                {
                    web.Get();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("地址：" + address + "，" + "错误：" + e.Message + "，" + "位置：" + e.TargetSite + "\r\n");
                    continue;
                }
                //检测
                int length = ResourceTypes.Count();
                for (int j = 0; j < length; j++)
                {
                    if (Regex.IsMatch(web.content, ResourceTypes[j].AddressRegex))
                    {
                        s.Type = ResourceTypes[j].name;
                        s.From = address;
                        ms = Regex.Matches(web.content, ResourceTypes[j].AddressRegex);
                        for (i = 0; i < ms.Count; i++)
                        {
                            try
                            {
                                if (ms[i].Groups.Count == 1)
                                    address = ms[i].Groups[0].Value;
                                else
                                    address = ms[i].Groups[1].Value;
                                string[] contents = web.content.Split(new string[] { address }, StringSplitOptions.None);
                                string last;
                                if (contents.Count() == 2)
                                    last = contents[1];
                                else
                                    last = web.content;
                                if (!(ResourceTypes[j].pwRegex == null || ResourceTypes[j].pwRegex == ""))
                                {
                                    Match Key = Regex.Match(last, ResourceTypes[j].pwRegex);
                                    if (Key.Groups.Count == 1)
                                        key = Key.Groups[0].Value;
                                    else
                                        key = Key.Groups[1].Value;
                                    //事实证明，如果没找到key，Groups[1].Value为""
                                    if (key == "") key = "无"; else Debug.WriteLine("找到一个密码：" + key);
                                }
                                if (!(ResourceTypes[j].verifyRegex == null || ResourceTypes[j].verifyRegex == ""))
                                {
                                    web = new Website("http://" + address);
                                    web.Get();
                                    if (web.IsMatch(ResourceTypes[j].verifyRegex))
                                    {
                                        Debug.WriteLine(address + "已失效");
                                        continue;
                                    }
                                }
                                s.Address = address;
                                s.Key = key;
                                if (ss.Contains(s))
                                {
                                    continue;
                                }
                                ss.Add(s);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("地址：" + address + "，" + "错误：" + e.Message + "，" + "位置：" + e.TargetSite + " " + e.Source);
                                continue;
                            }
                        }
                        for (i = 1; i < ss.Count; i++)
                            if (ss[i].Key == ss[i - 1].Key)
                                ss[i - 1] = new ResourceAddress(ss[i - 1].Type, ss[i - 1].Address, "无", ss[i - 1].From);
                        lock (SourceAddresses)
                        {
                            SourceAddresses.AddRange(ss);
                            if (addAddress != null)
                                addAddress(ss);
                        }
                    }
                }
            }
        }
        public void Stop()
        {
            lock (threads)
            {
                if(threads.Count() == 1)
                {
                    return;
                }
                for (int i = 0; i < threadmax; i++)
                {
                    try
                    {
                        threads[i].Abort();
                    }
                    catch
                    {

                    }
                }
                if (end != null) end();
                isrun = false;
            }
        }
    }
}
