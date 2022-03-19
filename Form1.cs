using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using System.Security.Policy;
using HtmlAgilityPack;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;
namespace LianjiaWebWorm
{
    public partial class Form1 : Form
    {
        List<HOUSEINFO> list_house = new List<HOUSEINFO>();
        List<QUYU> list_quyu = new List<QUYU>();
        //string[] quyu = new string[17];
        private static SqLiteHelper sql;
        private  List<EventWaitHandle> list_ewh = new List<EventWaitHandle>();
        private List<EventWaitHandle> list_ewh_sell = new List<EventWaitHandle>();
        CookieContainer cookiecontainer = new CookieContainer();
        public Form1()
        {
            InitializeComponent();
            webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(HideWebBrower);
        }
        private void HideWebBrower(object sender,
            WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Document.Body.InnerHtml.ToString().Contains("wrapper-window-login"))
                return;
            tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
            tableLayoutPanel1.RowStyles[0].Height = 100;
            tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent;
            tableLayoutPanel1.RowStyles[1].Height = 0;
            webBrowser1.Visible = false;
            textBox1.Visible = true;
            if (webBrowser1.Document != null)
            {
                foreach (string cookie in webBrowser1.Document.Cookie.Split(';'))
                {
                    string name = cookie.Split('=')[0];
                    string value = cookie.Substring(name.Length + 1);
                    string path = "/";
                    string domain = ".lianjia.com"; //change to your domain name
                    try
                    {
                        cookiecontainer.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
                    }
                    catch (System.Net.CookieException ex)
                    {
                        continue;
                    }
                }
            }
            if (!webBrowser1.Document.Body.InnerHtml.ToString().Contains("wrapper-window-login"))
                new Thread(new ThreadStart(GetData)).Start();
            
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void GetData()
        {
            sql = new SqLiteHelper("data source=house_chengjiao.db");
            //创建名为table1的数据表

            string response = GetResponse("https://sh.lianjia.com/chengjiao/");
            //string response = GetResponse("https://sh.lianjia.com/ershoufang/");
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(response);
            var quyuNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[3]/div[1]/dl[2]/dd[1]/div[1]/div[1]");
            //var quyuNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[3]/div[1]/div[1]/dl[2]/dd[1]/div[1]/div[1]");
            //Task[] tasks = new Task[14];
            for (int i = 1; i < 35; i+=2)
            {
                list_ewh.Add( new EventWaitHandle(false, EventResetMode.AutoReset));
                Thread t = new Thread(new ParameterizedThreadStart(GetQuyudata));
                t.IsBackground = true;
                t.Start(new Tuple<HtmlNode, EventWaitHandle>(quyuNodes.ChildNodes[i], list_ewh.Last()));
                //Debug.WriteLine(i);
                //tasks[(i - 1) / 2] = Task.Factory.StartNew(() => GetQuyudata(quyuNodes.ChildNodes[i]));
                //Thread.Sleep(1000);
                //GetQuyudata(quyuNodes.ChildNodes[i]);
                list_ewh.Last().WaitOne();
            }
            //Task.WaitAll(tasks);
            //WaitHandle.WaitAll(list_ewh.ToArray(),14);
            foreach (var item in list_ewh)
            {
                item.WaitOne();
            }
            textBox1.AppendText("完成"+"\r\n");
        }
        private void GetSellData()
        {
            sql = new SqLiteHelper("data source=house_zaishou.db");
            //创建名为table1的数据表

            //string response = GetResponse("https://sh.lianjia.com/chengjiao/");
            string response = GetResponse("https://sh.lianjia.com/ershoufang/");
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(response);
            //var quyuNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[3]/div[1]/dl[2]/dd[1]/div[1]/div[1]");
            var quyuNodes = htmlDoc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[3]/div[1]/div[1]/dl[2]/dd[1]/div[1]/div[1]");
            //Task[] tasks = new Task[14];
            for (int i = 1; i < 35; i += 2)
            {
                list_ewh_sell.Add(new EventWaitHandle(false, EventResetMode.AutoReset));
                Thread t = new Thread(new ParameterizedThreadStart(GetSellQuyudata));
                t.IsBackground = true;
                t.Start(new Tuple<HtmlNode, EventWaitHandle>(quyuNodes.ChildNodes[i], list_ewh_sell.Last()));
                //Debug.WriteLine(i);
                //tasks[(i - 1) / 2] = Task.Factory.StartNew(() => GetQuyudata(quyuNodes.ChildNodes[i]));
                //Thread.Sleep(1000);
                //GetQuyudata(quyuNodes.ChildNodes[i]);
                //list_ewh_sell.Last().WaitOne();
            }
            //Task.WaitAll(tasks);
            //WaitHandle.WaitAll(list_ewh.ToArray(),14);
            foreach (var item in list_ewh_sell)
            {
                item.WaitOne();
            }
            textBox1.AppendText("完成" + "\r\n");
        }
        public static string GetResponseText(HttpWebResponse response)
        {
            //Int32 Max_length_stream= 10*2048;
            Stream receiveStream = response.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            // Pipes the stream to a higher level stream reader with the required encoding format.
            StreamReader readStream = new StreamReader(receiveStream, encode);
            string text = readStream.ReadToEnd();
            //readStream.Close();
            //response.Close();
            

            return text;
        }

        private string GetResponse(string url)
        {
            try
            {
                lock (this)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "UserAgent=" + useragent[new Random().Next(0, useragent.Length - 1)];//从列表中随机选择一个
                    request.Timeout = 5000;
                    request.CookieContainer = cookiecontainer;
                    request.Proxy = WebRequest.DefaultWebProxy;
                    string response = GetResponseText((HttpWebResponse)request.GetResponse());
                    return response;
                }
            }
            catch(WebException ex)
            {
                listbox1_invoke(ex.Message+"\r\n");
                //Application.Exit();
            }
            return string.Empty;
        }
        private void GetHouse(string url0, ref List<HOUSEINFO> list_house)
        {
            try
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                string response = GetResponse(url0);
                if (string.IsNullOrEmpty(response)) { listbox1_invoke(url0 + "返回错误" + "\r\n"); return; }
                htmlDoc.LoadHtml(response);
                string pageInfo = htmlDoc.DocumentNode.SelectSingleNode("//@page-data").Attributes["page-data"].Value;
                
                PAGEINFO ob = JsonConvert.DeserializeObject<PAGEINFO>(pageInfo);
                for (int i = 1; i < ob.totalPage + 1; i++)
                {
                    listbox1_invoke(url0 + "page" +i+ "\r\n");
                    string url = string.Empty;
                    if (i == 1) url = url0;
                    else url = url0 + "pg" + i + "/";
                    
                    htmlDoc.LoadHtml(GetResponse(url));
                    var nodes = htmlDoc.DocumentNode.SelectNodes("//ul/li/div");
                    if (nodes == null) { listbox1_invoke(url + "无法获取" + "\r\n"); return; }
                    for (int j = 1; j < nodes.Count - 1; j++)
                    {
                        HOUSEINFO houseInfo = new HOUSEINFO();
                        string info = nodes[j].ChildNodes[0].ChildNodes[0].InnerHtml;
                        if (!info.Contains("平米")) continue;
                        houseInfo.housetitle = info.Split(' ')[0];
                        houseInfo.room = info.Split(' ')[1];
                        houseInfo.area = info.Split(' ')[2];
                        houseInfo.info = nodes[j].ChildNodes[1].ChildNodes[0].LastChild.InnerHtml;
                        houseInfo.price = nodes[j].ChildNodes[1].ChildNodes[2].FirstChild.InnerHtml;
                        houseInfo.date = nodes[j].ChildNodes[1].ChildNodes[1].InnerHtml;
                        houseInfo.info += nodes[j].ChildNodes[2].ChildNodes[0].LastChild.InnerHtml;
                        houseInfo.unitprice = nodes[j].ChildNodes[2].ChildNodes[2].FirstChild.InnerHtml;
                        list_house.Add(houseInfo);
                    }

                }
            }
            catch (Exception ex)
            {
                listbox1_invoke(ex.Message);
            }
        }
        private void GetSellHouse(string url0, ref List<HOUSEINFO> list_house)
        {
            try
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                string response = GetResponse(url0);
                if (string.IsNullOrEmpty(response)) { listbox1_invoke(url0 + "返回错误" + "\r\n"); return; }
                htmlDoc.LoadHtml(response);
                if(response.Contains("共找到<span> 0 </span>套")) { listbox1_invoke(url0 + "共找到 0 套" + "\r\n"); return; }
                string pageInfo = htmlDoc.DocumentNode.SelectSingleNode("//@page-data").Attributes["page-data"].Value;

                PAGEINFO ob = JsonConvert.DeserializeObject<PAGEINFO>(pageInfo);
                for (int i = 1; i < ob.totalPage + 1; i++)
                {
                    listbox1_invoke(url0 + "page" + i + "\r\n");
                    string url = string.Empty;
                    if (i == 1) url = url0;
                    else url = url0 + "pg" + i + "/";

                    htmlDoc.LoadHtml(GetResponse(url));
                    var nodes = htmlDoc.DocumentNode.SelectNodes("//ul[@class='sellListContent']/li/div[1]");
                    if (nodes == null) { listbox1_invoke(url + "无法获取" + "\r\n");  continue; }
                    
                    for (int j = 1; j < nodes.Count - 1; j++)
                    {
                        HOUSEINFO houseInfo = new HOUSEINFO();
                        string info = nodes[j].ChildNodes[2].ChildNodes[0].ChildNodes[1].InnerHtml;
                        if (!info.Contains("平米")) continue;
                        houseInfo.housetitle = nodes[j].ChildNodes[1].ChildNodes[0].ChildNodes[1].InnerText;//小区名称
                        houseInfo.room = info.Split('|')[0];
                        houseInfo.area = info.Split('|')[1];
                        houseInfo.info = info.Substring(info.IndexOfAny("平米".ToCharArray()) + 4, info.Length - info.IndexOfAny("平米".ToCharArray()) - 4);
                        houseInfo.price = nodes[j].ChildNodes[5].ChildNodes[0].ChildNodes[0].InnerHtml;
                        houseInfo.date = nodes[j].ChildNodes[3].ChildNodes[1].InnerHtml + " | 当前时间" + DateTime.Now.ToString();
                        houseInfo.unitprice = nodes[j].ChildNodes[5].ChildNodes[1].Attributes["data-price"].Value;
                        list_house.Add(houseInfo);
                    }

                }
            }
            catch (Exception ex)
            {
                listbox1_invoke(ex.Message);
            }
        }
        private void GetQuyudata(object data/*HtmlNode quyuNodes*/)
        {
            //Debug.WriteLine(i);
            //return;
            Tuple<HtmlNode, EventWaitHandle> t = (Tuple<HtmlNode, EventWaitHandle>)data;
            HtmlNode quyuNodes = t.Item1;
            QUYU quyu = new QUYU();
            quyu.name = quyuNodes.InnerHtml;
            listbox1_invoke(quyu.name+ "\r\n");
            
            quyu.url = "https://sh.lianjia.com" + quyuNodes.Attributes["href"].Value;

            sql.CreateTable(quyu.name, new string[] { "ADDR", "housetitle","room","Area", "info", "price", "unitprice", "date" }, new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "INTEGER", "TEXT" });
            //插入两条数据
            //sql.InsertValues(quyu.name, new string[] { "1", "张三", "22", "Zhang@163.com" });

            var htmlDocTmp = new HtmlAgilityPack.HtmlDocument();
            htmlDocTmp.LoadHtml(GetResponse(quyu.url));
            //HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("/html[1]/body[1]/div[3]/div[1]/dl[2]/dd[1]/div[1]/div[2]/a");
            HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("//div[@data-role='ershoufang']/div[2]/a");
            //HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("/html[1]/body[1]/div[3]/div[1]/div[1]/dl[2]/dd[1]/div[1]/div[2]/a");
            if (quyuNodesTmp == null) { listbox1_invoke(quyu.name +"无法获取"+ "\r\n");return; }
            quyu.num = quyuNodesTmp.Count;
            for (int j = 0; j < quyuNodesTmp.Count; j++)
            {
                DIDUAN diduan = new DIDUAN();
                listbox1_invoke(quyuNodesTmp[j].InnerHtml + "\r\n");
                Debug.WriteLine(quyuNodesTmp[j].InnerHtml);
                diduan.name = quyuNodesTmp[j].InnerHtml;
                diduan.url = "https://sh.lianjia.com" + quyuNodesTmp[j].Attributes["href"].Value;
                var htmlDocTmp1 = new HtmlAgilityPack.HtmlDocument();
                htmlDocTmp1.LoadHtml(GetResponse(diduan.url));
                HtmlNodeCollection quyuNodesTmp1 = htmlDocTmp1.DocumentNode.SelectNodes("/html[1]/body[1]/div[5]/div[1]/div[2]/div[1]/span");
                if (quyuNodesTmp1 == null) { listbox1_invoke(quyuNodesTmp[j].InnerHtml +"网络错误"); continue; }
                diduan.num = Convert.ToInt32(quyuNodesTmp1[0].InnerHtml);
                if (diduan.num < 3000)
                    GetHouse(diduan.url, ref diduan.list_house);
                else
                {
                    string[] l = { "l1", "l2", "l3", "l4", "l5", "l6" };//一室、二室等分选
                    for (int k = 0; k < l.Length; k++)
                    {
                        GetHouse(diduan.url + l[k] + "/", ref diduan.list_house);
                    }
                }
                diduan.num = diduan.list_house.Count;
                quyu.list_diduan.Add(diduan);
                lock (this)
                {
                    sql.BeginTrans();
                    for (int m = 0; m < diduan.list_house.Count; m++)
                    {
                        sql.InsertValues(quyu.name, new string[] { diduan.name, diduan.list_house[m].housetitle, diduan.list_house[m].room, diduan.list_house[m].area, diduan.list_house[m].info, diduan.list_house[m].price, diduan.list_house[m].unitprice, diduan.list_house[m].date });
                    }
                    sql.Commit();
                }
            }
            t.Item2.Set();
            list_quyu.Add(quyu);
        }

        private void GetSellQuyudata(object data/*HtmlNode quyuNodes*/)
        {
            //Debug.WriteLine(i);
            //return;
            Tuple<HtmlNode, EventWaitHandle> t = (Tuple<HtmlNode, EventWaitHandle>)data;
            HtmlNode quyuNodes = t.Item1;
            QUYU quyu = new QUYU();
            quyu.name = quyuNodes.InnerHtml;
            listbox1_invoke(quyu.name + "\r\n");

            quyu.url = "https://sh.lianjia.com" + quyuNodes.Attributes["href"].Value;

            sql.CreateTable(quyu.name, new string[] { "ADDR", "housetitle", "room", "Area", "info", "price", "unitprice", "date" }, new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "INTEGER", "TEXT" });
            //插入两条数据
            //sql.InsertValues(quyu.name, new string[] { "1", "张三", "22", "Zhang@163.com" });

            var htmlDocTmp = new HtmlAgilityPack.HtmlDocument();
            htmlDocTmp.LoadHtml(GetResponse(quyu.url));
            //HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("/html[1]/body[1]/div[3]/div[1]/dl[2]/dd[1]/div[1]/div[2]/a");
            //HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("//div[@data-role='ershoufang']/div[2]/a");
            HtmlNodeCollection quyuNodesTmp = htmlDocTmp.DocumentNode.SelectNodes("//div[@data-role='ershoufang']/div[2]/a");
            if (quyuNodesTmp == null) { listbox1_invoke(quyu.name + "无法获取" + "\r\n"); return; }
            quyu.num = quyuNodesTmp.Count;
            for (int j = 0; j < quyuNodesTmp.Count; j++)
            {
                DIDUAN diduan = new DIDUAN();
                listbox1_invoke(quyuNodesTmp[j].InnerHtml + "\r\n");
                Debug.WriteLine(quyuNodesTmp[j].InnerHtml);
                diduan.name = quyuNodesTmp[j].InnerHtml;
                diduan.url = "https://sh.lianjia.com" + quyuNodesTmp[j].Attributes["href"].Value;
                var htmlDocTmp1 = new HtmlAgilityPack.HtmlDocument();
                htmlDocTmp1.LoadHtml(GetResponse(diduan.url));
                HtmlNode quyuNodesTmp1 = htmlDocTmp1.DocumentNode.SelectSingleNode("//div[@class='resultDes clear']/h2/span");
                if (quyuNodesTmp1 == null) { listbox1_invoke(quyuNodesTmp[j].InnerHtml + "网络错误"); continue; }
                diduan.num = Convert.ToInt32(quyuNodesTmp1.InnerHtml);
                if (diduan.num < 3000)
                    GetSellHouse(diduan.url, ref diduan.list_house);
                else
                {
                    string[] l = { "l1", "l2", "l3", "l4", "l5", "l6" };//一室、二室等分选
                    for (int k = 0; k < l.Length; k++)
                    {
                        GetSellHouse(diduan.url + l[k] + "/", ref diduan.list_house);
                    }
                }
                diduan.num = diduan.list_house.Count;
                quyu.list_diduan.Add(diduan);
                lock (this)
                {
                    sql.BeginTrans();
                    for (int m = 0; m < diduan.list_house.Count; m++)
                    {
                        sql.InsertValues(quyu.name, new string[] { diduan.name, diduan.list_house[m].housetitle, diduan.list_house[m].room, diduan.list_house[m].area, diduan.list_house[m].info, diduan.list_house[m].price, diduan.list_house[m].unitprice, diduan.list_house[m].date });
                    }
                    sql.Commit();
                }
            }
            t.Item2.Set();
            list_quyu.Add(quyu);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string response = GetResponse("https://sh.lianjia.com/chengjiao/");
            if (response.Contains("loginHolder"))
            {
                webBrowser1.Navigate("https://sh.lianjia.com/chengjiao/");
            }
            else
            {
                tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
                tableLayoutPanel1.RowStyles[0].Height = 0;
                tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent;
                tableLayoutPanel1.RowStyles[1].Height = 100;
                new Thread(new ThreadStart(
                GetData)).Start();
            }

        }

        private void listbox1_invoke(string msg)
        {

            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(delegate
                {
                    textBox1.AppendText(msg );
                }));
            else
            {
                textBox1.AppendText(msg );
            }
        }
        private static string[] useragent = {
        "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; AcooBrowser; .NET CLR 1.1.4322; .NET CLR 2.0.50727)",
        "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; Acoo Browser; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506)",
        "Mozilla/4.0 (compatible; MSIE 7.0; AOL 9.5; AOLBuild 4337.35; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)",
        "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)",
        "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Win64; x64; Trident/5.0; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET CLR 2.0.50727; Media Center PC 6.0)",
        "Mozilla/5.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET CLR 1.0.3705; .NET CLR 1.1.4322)",
        "Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727; InfoPath.2; .NET CLR 3.0.04506.30)",
        "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN) AppleWebKit/523.15 (KHTML, like Gecko, Safari/419.3) Arora/0.3 (Change: 287 c9dfb30)",
        "Mozilla/5.0 (X11; U; Linux; en-US) AppleWebKit/527+ (KHTML, like Gecko, Safari/419.3) Arora/0.6",
        "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.2pre) Gecko/20070215 K-Ninja/2.1.1",
        "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9) Gecko/20080705 Firefox/3.0 Kapiko/3.0",
        "Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5",
        "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.8) Gecko Fedora/1.9.0.8-1.fc10 Kazehakase/0.5.6",
        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.11 (KHTML, like Gecko) Chrome/17.0.963.56 Safari/535.11",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_3) AppleWebKit/535.20 (KHTML, like Gecko) Chrome/19.0.1036.7 Safari/535.20",
        "Opera/9.80 (Macintosh; Intel Mac OS X 10.6.8; U; fr) Presto/2.9.168 Version/11.52",
        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36"
        };

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
            tableLayoutPanel1.RowStyles[0].Height = 100;
            tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent;
            tableLayoutPanel1.RowStyles[1].Height = 0;
            webBrowser1.Visible = false;
            textBox1.Visible = true;
            new Thread(new ThreadStart(
            GetSellData)).Start();
        }
    }
    class PAGEINFO
    {
       public int totalPage;
        public int curPage;

    }
    class HOUSEINFO
    {
        public string housetitle;
        public string room;
        public string area;
        public string unitprice;
        public string info;
        public string price;
        public string date;
    }
    class QUYU
    {
        public string name;
        public List<DIDUAN> list_diduan = new List<DIDUAN>();
        public string url;
        public int num;//地段数
    }
    class DIDUAN
    {
        public string name;
        public List<HOUSEINFO> list_house = new List<HOUSEINFO>();
        public string url;
        public int num;//房子数
    }
}
