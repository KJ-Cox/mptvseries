using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace WindowPlugins.GUITVSeries.Newzbin
{
    class Load
    {
        DBNewzbin m_Search = null;
        DBEpisode m_dbEpisode = null;
        public BackgroundWorker worker = null;
        Feedback.Interface m_feedback = null;
        bool m_bSuccess = false;
        String m_msgOut = String.Empty;

        public delegate void LoadCompletedHandler(bool bOK, String sMsg);
        /// <summary>
        /// This will be triggered once all the SeriesAndEpisodeInfo has been parsed completely.
        /// </summary>
        public event LoadCompletedHandler LoadCompleted;

        public Load(Feedback.Interface feedback)
        {
            m_feedback = feedback;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (LoadCompleted != null) // only if any subscribers exist
            {
                this.LoadCompleted.Invoke(m_bSuccess, m_msgOut);
            }
        }

        public bool Search(DBEpisode dbEpisode)
        {
            m_Search = DBNewzbin.Get()[0];
            if (m_Search[DBNewzbin.cSearchUrl] != String.Empty && System.IO.File.Exists(DBOption.GetOptions(DBOption.cNewsLeecherPath)))
            {
                m_dbEpisode = dbEpisode;
                worker.RunWorkerAsync();
                return true;
            }
            return false;
        }

        public CookieCollection LoadCookies(DBNewzbin db)
        {
            CookieCollection cookies = new CookieCollection();
            // parse the string;
            String sFromStore = db[DBNewzbin.cCookieList];
            while (sFromStore != String.Empty)
            {
                String sCookie = sFromStore.Substring(0, sFromStore.IndexOf(':'));
                sFromStore = sFromStore.Substring(sFromStore.IndexOf(":") + 1);

                byte[] bytCook = Convert.FromBase64String(sCookie);
                MemoryStream inMs = new MemoryStream(bytCook);
                inMs.Seek(0, 0);
                DeflateStream zipStream = new DeflateStream(inMs, CompressionMode.Decompress, true);

                byte[] buffer = new byte[32768];

                MemoryStream ms = new MemoryStream();
                while (true)
                {
                    int read = zipStream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;;
                    ms.Write(buffer, 0, read);
                }
                byte[] outByt = ms.ToArray();
                zipStream.Flush();
                zipStream.Close();
                MemoryStream outMs = new MemoryStream(outByt);
                outMs.Seek(0, 0);
                BinaryFormatter bf = new BinaryFormatter();               
                Cookie cookie = (Cookie)bf.Deserialize(outMs, null);
                cookies.Add(cookie);
            }

            return cookies;
        }

        public void SaveCookies(DBNewzbin db, CookieCollection cookies)
        {
            String sToStore = String.Empty;

            foreach (Cookie cookie in cookies)
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, cookie);
                byte[] inbyt = ms.ToArray();
                MemoryStream objStream = new MemoryStream();
                DeflateStream objZS = new DeflateStream(objStream, CompressionMode.Compress);
                objZS.Write(inbyt, 0, inbyt.Length);
                objZS.Flush();
                objZS.Close();

                byte[] b = objStream.ToArray();
                sToStore += Convert.ToBase64String(b) + ":";
            }

            // and store in the DB
            db[DBNewzbin.cCookieList] = sToStore;
            db.Commit();
        }

        public bool IsLoggedIn(String sPage)
        {
            // check the presence of our login name
            if (sPage.IndexOf("/account/profile/" + m_Search[DBNewzbin.cLogin]) != -1)
                return true;
            else
                return false;
        }

        public void DoLogin()
        {
            CookieCollection loginCookies = null;

            // not logged in, either our cookies were empty or expired.. relog
            HttpWebRequest loginRequest = (HttpWebRequest)WebRequest.Create("http://v3.newzbin.com/account/login/");
            loginRequest.Method = "POST";
            loginRequest.AllowAutoRedirect = false;
            loginRequest.KeepAlive = false;
            loginRequest.CookieContainer = new CookieContainer();

            String sPostData = String.Format("username={0}&password={1}", m_Search[DBNewzbin.cLogin], m_Search[DBNewzbin.cPassword]);

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] bytePostData = encoding.GetBytes(sPostData);
            // Set the content type of the data being posted.
            // Set the content length of the string being posted.
            loginRequest.ContentLength = bytePostData.Length;
            loginRequest.ContentType = "application/x-www-form-urlencoded";

            Stream newStream = loginRequest.GetRequestStream();
            newStream.Write(bytePostData, 0, bytePostData.Length);
            // Close the Stream object.
            newStream.Close();

            HttpWebResponse loginResponse = (HttpWebResponse)loginRequest.GetResponse();

//             Stream data = loginResponse.GetResponseStream();
//             StreamReader reader = new StreamReader(data);
//             String sPage = reader.ReadToEnd();
//             data.Close();
//             reader.Close();

            String redirect = loginResponse.Headers["Location"];
            loginCookies = loginResponse.Cookies;
            loginResponse.Close();

            if (redirect != null)
            {
                loginRequest = (HttpWebRequest)WebRequest.Create(redirect);
                loginRequest.KeepAlive = false;
                loginRequest.Method = "GET";
                loginRequest.ContentType = "application/x-www-form-urlencoded";
                loginRequest.AllowAutoRedirect = true;
                loginRequest.CookieContainer = new CookieContainer();
                loginRequest.CookieContainer.Add(loginCookies);

                loginResponse = (HttpWebResponse)loginRequest.GetResponse();

                Stream data = loginResponse.GetResponseStream();
                StreamReader reader = new StreamReader(data);
                String sPage = reader.ReadToEnd();
                data.Close();
                reader.Close();
                loginCookies = loginResponse.Cookies;
                SaveCookies(m_Search, loginCookies);
                loginResponse.Close();
            }
        }

        public void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            // come up with a valid series name (remove some things basically)
            MPTVSeriesLog.Write("**********************************");
            MPTVSeriesLog.Write("Starting Newzbin Search process");
            MPTVSeriesLog.Write("**********************************");

            try
            {
                DBOnlineSeries series = new DBOnlineSeries(m_dbEpisode[DBEpisode.cSeriesID]);
                String sSeries = series[DBOnlineSeries.cSortName];
                // remove anything between parenthesis
                String RegExp = "(\\([^)]*\\))";
                Regex Engine = new Regex(RegExp, RegexOptions.IgnoreCase);
                sSeries = Engine.Replace(sSeries, "").Trim();

                List<NewzbinResult> sortedMatchList = new List<NewzbinResult>();
                String sSearch = String.Format("{0} {1}x{2:D2}", sSeries, (int)m_dbEpisode[DBEpisode.cSeasonIndex], (int)m_dbEpisode[DBEpisode.cEpisodeIndex]);
                sSearch = sSearch.Replace(' ', '+');
                RegExp = "\\$search\\$";
                Engine = new Regex(RegExp, RegexOptions.IgnoreCase);
                String sUrl = Engine.Replace(m_Search[DBNewzbin.cSearchUrl], sSearch);

                String sPage = String.Empty;
                bool bSuccess = false;
                bool bRetry = false;
                do
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sUrl);
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(LoadCookies(m_Search));

                    WebResponse response = request.GetResponse();
                    Stream data = response.GetResponseStream();
                    StreamReader reader = new StreamReader(data);
                    sPage = reader.ReadToEnd();
                    data.Close();
                    reader.Close();
                    response.Close();

                    if (!IsLoggedIn(sPage))
                    {
                        if (!bRetry)
                        {
                            bRetry = true;
                            DoLogin();
                        }
                        else
                            break;
                    }
                    else
                    {
                        bSuccess = true;
                        break;
                    }
                }
                while (true);

                if (bSuccess)
                {
                    RegExp = m_Search[DBNewzbin.cSearchRegex];
                    Engine = new Regex(RegExp, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    MatchCollection matches = Engine.Matches(sPage);
                    foreach (Match match in matches)
                    {
                        sortedMatchList.Add(new NewzbinResult(match));
                    }

                    // show the user the list and ask for the right one
                    List<Feedback.CItem> Choices = new List<Feedback.CItem>();
                    foreach (NewzbinResult match in sortedMatchList)
                    {
                        String sName = match.m_sName;
                        if (sName.Length > 20)
                            sName = sName.Substring(0, 18) + "..";
                        Choices.Add(new Feedback.CItem(sName + "(" + match.m_sFormat + ") - " + match.m_sSize + ", " + match.m_sPost, String.Empty, match));
                    }
                    Feedback.CDescriptor descriptor = new Feedback.CDescriptor();
                    descriptor.m_sTitle = "Found reports:";
                    descriptor.m_sItemToMatchLabel = "Looking for:";
                    descriptor.m_sItemToMatch = String.Format("{0} {1}x{2:D2}", sSeries, m_dbEpisode[DBEpisode.cSeasonIndex], m_dbEpisode[DBEpisode.cEpisodeIndex]);
                    descriptor.m_sListLabel = "Found reports:";
                    descriptor.m_List = Choices;
                    descriptor.m_sbtnIgnoreLabel = String.Empty;

                    Feedback.CItem Selected = null;
                    if (m_feedback.ChooseFromSelection(descriptor, out Selected) == Feedback.ReturnCode.OK && Selected != null)
                    {
                        NewzbinResult result = Selected.m_Tag as NewzbinResult;
                        // download the NZB somewhere
                        HttpWebRequest requestDownload = (HttpWebRequest)WebRequest.Create("http://v3.newzbin.com/dnzb/");

                        requestDownload.Method = "POST";
                        String sPostData = String.Format("username={0}&password={1}&reportid={2}", m_Search[DBNewzbin.cLogin], m_Search[DBNewzbin.cPassword], result.m_sID);

                        ASCIIEncoding encoding = new ASCIIEncoding();
                        byte[] bytePostData = encoding.GetBytes(sPostData);
                        // Set the content type of the data being posted.
                        requestDownload.ContentType = "application/x-www-form-urlencoded";
                        // Set the content length of the string being posted.
                        requestDownload.ContentLength = bytePostData.Length;
                        //                    requestDownload.Headers.Add("Accept-Encoding", "gzip");
                        //                    requestDownload.Accept = "text/plain";

                        Stream newStream = requestDownload.GetRequestStream();
                        newStream.Write(bytePostData, 0, bytePostData.Length);
                        // Close the Stream object.
                        newStream.Close();

                        WebResponse responseDownload = null;
                        try
                        {
                            responseDownload = requestDownload.GetResponse();
                        }
                        catch (WebException exp)
                        {
                            m_msgOut = exp.Response.Headers.Get("X-DNZB-RCode") + ": " + exp.Response.Headers.Get("X-DNZB-RText");
                        }
                        if (responseDownload != null)
                        {
                            Stream receiveStream = responseDownload.GetResponseStream();
                            // Pipes the stream to a higher level stream reader with the required encoding format. 

                            String sOutputFile = responseDownload.Headers["X-DNZB-Name"];
                            sOutputFile = sOutputFile.Replace(":", "");
                            sOutputFile = sOutputFile.Replace("?", "");
                            sOutputFile = sOutputFile.Replace("\\", "");
                            sOutputFile = sOutputFile.Replace("*", "");
                            sOutputFile = sOutputFile.Replace("<", "");
                            sOutputFile = sOutputFile.Replace(">", "");
                            sOutputFile = sOutputFile.Replace("|", "");
                            sOutputFile = sOutputFile.Replace("/", "");
                            // 
                            sOutputFile = Path.Combine(System.IO.Path.GetTempPath(), sOutputFile + ".nzb");
                            FileStream file = System.IO.File.Create(sOutputFile);

                            byte[] read = new byte[256];
                            // Reads 256 bytes at a time.    
                            int count = receiveStream.Read(read, 0, 256);
                            while (count > 0)
                            {
                                file.Write(read, 0, count);
                                count = receiveStream.Read(read, 0, 256);
                            }

                            file.Close();
                            responseDownload.Close();

                            Download.Monitor.AddPendingDownload(m_dbEpisode);
                            System.Diagnostics.Process.Start(DBOption.GetOptions(DBOption.cNewsLeecherPath), "\"" + sOutputFile + "\"");
                            m_bSuccess = true;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                m_msgOut = exp.Message;
            }
        }
    }

    class NewzbinResult : IComparable<NewzbinResult>
    {
        public String m_sName;
        public String m_sID;
        public String m_sSize;
        public String m_sPost;
        public String m_sReport;
        public String m_sFormat;

        public int CompareTo(NewzbinResult other)
        {
            return other.m_sSize.CompareTo(m_sSize);
        }

        public NewzbinResult(Match match)
        {
            m_sName = match.Groups["name"].Value;
            m_sID = match.Groups["ID"].Value;
            m_sSize = match.Groups["size"].Value;
            m_sPost = match.Groups["post"].Value;
            m_sReport = match.Groups["report"].Value;
            m_sFormat = match.Groups["format"].Value;
        }
    };
}