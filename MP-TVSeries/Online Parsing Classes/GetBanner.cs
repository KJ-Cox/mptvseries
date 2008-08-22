#region GNU license
// MP-TVSeries - Plugin for Mediaportal
// http://www.team-mediaportal.com
// Copyright (C) 2006-2007
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Net;
using System.IO;

namespace WindowPlugins.GUITVSeries
{
    class BannerSeries
    {
        public bool bGraphical = false;
        public bool bHighestRated = false;
        public bool bPoster = false;
        public String sSeriesName = String.Empty;
        public String sOnlineBannerPath = String.Empty;
        public String sBannerFileName = String.Empty;
        public String sBannerLang = String.Empty;
    };

    class BannerSeason
    {
        public int nIndex = 0;
        public bool bIsNeeded = false;
        public bool bHighestRated = false;
        public String sSeriesName = String.Empty;
        public String sOnlineBannerPath = String.Empty;
        public String sBannerFileName = String.Empty;
        public String sBannerLang = String.Empty;
        public String sSeason = string.Empty;
    };

    class seriesBannersMap : System.IEquatable<seriesBannersMap>    
    {
        public string seriesID = string.Empty;
        public List<BannerSeries> seriesBanners = new List<BannerSeries>();
        public List<BannerSeason> seasonBanners = new List<BannerSeason>();

        public seriesBannersMap()
        {
        }

        public seriesBannersMap(string seriesID)
        {
            this.seriesID = seriesID;
        }

        #region IEquatable<seriesBannersMap> Members

        bool IEquatable<seriesBannersMap>.Equals(seriesBannersMap other)
        {
            return seriesID.Equals(other.seriesID);
        }

        #endregion
    }

    class GetBanner
    {
        //private long m_nServerTimeStamp = 0;
        public List<seriesBannersMap> seriesBanners = new List<seriesBannersMap>();

        static String sBannersBasePath = Settings.GetPath(Settings.Path.banners) + @"\";
        String localizedSeriesName = string.Empty;
        //static bool getBlankBanners = false;
        //public long ServerTimeStamp
        //{
        //    get { return m_nServerTimeStamp; }
        //}

        #region old stuff - safe to remove
        /*
        /// <summary>
        /// This constructor automatically get's relevant seasons
        /// </summary>
        /// <param name="nSeriesID"></param>
        /// <param name="nUpdateBannersTimeStamp"></param>
        public GetBanner(int nSeriesID, long nUpdateBannersTimeStamp, string localSeriesName)
        {
            localizedSeriesName = localSeriesName;
            List<int> relevantSeasons = new List<int>();
            foreach(DBSeason season in DBSeason.Get(nSeriesID))
                relevantSeasons.Add(season[DBSeason.cIndex]);
            work(nSeriesID, nUpdateBannersTimeStamp, relevantSeasons);
        }

        static GetBanner()
        {
            if (!Settings.isConfig)
                getBlankBanners = DBOption.GetOptions(DBOption.cGetBlankBanners);
        } 
                 /*
        private void work(int nSeriesID, long nUpdateBannersTimeStamp, List<int> SeasonsToDownload)
        {
            work(ZsoriParser.GetBanners(nSeriesID, nUpdateBannersTimeStamp, null), SeasonsToDownload, false, ZsoriParser.SelLanguageAsString);
            // also always get the english ones
            if(ZsoriParser.SelLanguageAsString != "7")
                work(ZsoriParser.GetBanners(nSeriesID, nUpdateBannersTimeStamp, "7"), SeasonsToDownload, false, "7");
            // also get blank banners?
            if(getBlankBanners || (Settings.isConfig && DBOption.GetOptions(DBOption.cGetBlankBanners)))
                work(ZsoriParser.GetBanners(nSeriesID, nUpdateBannersTimeStamp, "blank"), SeasonsToDownload, false, "blank");

        }

        private void work(XmlNodeList nodeList, List<int> SeasonsToDownload, bool allSeasons, string bannerLang)
        {
            if (nodeList != null)
            {
                foreach (XmlNode itemNode in nodeList)
                {
                    // first return item SHOULD ALWAYS be the sync time (hope so at least!)
                    if (itemNode.ChildNodes[0].Name == "SyncTime")
                    {
                        m_nServerTimeStamp = Convert.ToInt64(itemNode.ChildNodes[0].InnerText);
                    }
                    else
                    {
                        String sType = String.Empty;
                        //string seriesID = String.Empty;
                        seriesBannersMap map = new seriesBannersMap();
                        List<BannerSeries> m_bannerSeriesList = new List<BannerSeries>();
                        List<BannerSeason> m_bannerSeasonList = new List<BannerSeason>();
                        foreach (XmlNode propertyNode in itemNode.ChildNodes)
                        {
                            if (propertyNode.Name == "Type")
                            {
                                sType = propertyNode.InnerText;
                                if (map.seriesID.Length > 0)
                                    break;
                            }
                            else if (propertyNode.Name == "SeriesID")
                            {
                                map.seriesID = propertyNode.InnerText;
                                if (sType.Length > 0)
                                    break;
                            }
                        }

                        switch (sType)
                        {
                            case "series":
                                BannerSeries bannerSeries = new BannerSeries();
                                foreach (XmlNode propertyNode in itemNode.ChildNodes)
                                {
                                    switch (propertyNode.Name)
                                    {
                                        //case "SeriesName":
                                        //    bannerSeries.sSeriesName = propertyNode.InnerText;
                                        //    break;

                                        case "BannerType":
                                            switch (propertyNode.InnerText)
                                            {
                                                case "text":
                                                    bannerSeries.bGraphical = false;
                                                    break;
                                                case "graphical":
                                                    bannerSeries.bGraphical = true;
                                                    break;
                                                default:
                                                    // everything else we don't really like
                                                    bannerSeries.bGraphical = false;
                                                    break;
                                            }
                                            break;

                                        case "BannerPath":
                                            {
                                                bannerSeries.sOnlineBannerPath = propertyNode.InnerText;
                                            }
                                            break;
                                    }
                                }
                                bannerSeries.sSeriesName = localizedSeriesName;
                                bannerSeries.sBannerLang = bannerLang;
                                m_bannerSeriesList.Add(bannerSeries);
                                break;

                            case "season":
                                if (!allSeasons && SeasonsToDownload.Count == 0) break;
                                BannerSeason bannerSeason = new BannerSeason();
                                foreach (XmlNode propertyNode in itemNode.ChildNodes)
                                {
                                    switch (propertyNode.Name)
                                    {
                                        //case "SeriesName":
                                        //    bannerSeason.sSeriesName = propertyNode.InnerText;
                                        //    break;

                                        case "Season":
                                            bannerSeason.nIndex = Convert.ToInt32(propertyNode.InnerText);
                                            break;

                                        case "BannerType":
                                            switch (propertyNode.InnerText)
                                            {
                                                case "season":
                                                    // only season type we support
                                                    bannerSeason.bIsNeeded = true;
                                                    break;

                                                default:
                                                    bannerSeason.bIsNeeded = false;
                                                    // ignore unknown types
                                                    break;
                                            }
                                            break;

                                        case "BannerPath":
                                            {
                                                bannerSeason.sOnlineBannerPath = propertyNode.InnerText;
                                            }
                                            break;
                                    }
                                }
                                if (bannerSeason.bIsNeeded && (allSeasons || (null != SeasonsToDownload && SeasonsToDownload.Contains(bannerSeason.nIndex))))
                                {
                                    bannerSeason.sSeriesName = localizedSeriesName;
                                    bannerSeason.sBannerLang = bannerLang;
                                    m_bannerSeasonList.Add(bannerSeason);
                                }
                                break;
                        }
                        map.seasonBanners = m_bannerSeasonList;
                        map.seriesBanners = m_bannerSeriesList;
                        // series already in?
                        if(seriesBanners.Contains(map))
                        {
                           seriesBannersMap seriesMap =  seriesBanners[seriesBanners.IndexOf(map)];
                           seriesMap.seasonBanners.AddRange(map.seasonBanners);
                           seriesMap.seriesBanners.AddRange(map.seriesBanners);
                        }
                        else seriesBanners.Add(map);
                    }
                }

                DownloadBanners(bannerLang);
            }
        }
        */
        #endregion

        public GetBanner(string seriesID)
        {
            doWork(seriesID);
        }

        private void doWork(string seriesID)
        {
            XmlNodeList nodeList = Online_Parsing_Classes.OnlineAPI.getBannerList(Int32.Parse(seriesID));
            List<BannerSeries> m_bannerSeriesList = new List<BannerSeries>();
            List<BannerSeason> m_bannerSeasonList = new List<BannerSeason>();
            seriesBannersMap map = new seriesBannersMap();

            string sSeriesBannerType = "Graphical";
            bool bGetSeriesPoster = false;

            if (DBOption.GetOptions(DBOption.cGetSeriesPosters))
            {
                bGetSeriesPoster = true;
                sSeriesBannerType = "680x1000";
            }
        
            if (nodeList != null)
            {                
                foreach (XmlNode topNode in nodeList)
                {
                    bool bHighestRatedSeriesIsSet = false;
                    bool bHighestRatedSeasonIsSet = false;
                    string sCurrentSeason = string.Empty;
                    string[] sPreviousSeasons = { "" };

                    foreach (XmlNode itemNode in topNode.ChildNodes)
                    {                        
                        if(itemNode.Name == "Banner")
                        {
                            BannerSeason bs = new BannerSeason();
                            BannerSeries b = new BannerSeries();
                            bool isSeries = false;
                            bool isGood = true;
                            foreach (XmlNode propertyNode in itemNode.ChildNodes)
                            {
                                switch (propertyNode.Name)
                                {
                                    case "BannerPath":
                                        bs.sOnlineBannerPath = propertyNode.InnerText;
                                        b.sOnlineBannerPath = propertyNode.InnerText;
                                        break;
                                    case "BannerType":
                                        if (!bGetSeriesPoster)
                                        {
                                            if (propertyNode.InnerText.Equals("series", StringComparison.CurrentCultureIgnoreCase))
                                                isSeries = true;
                                        }
                                        else
                                        {
                                            if (propertyNode.InnerText.Equals("poster", StringComparison.CurrentCultureIgnoreCase))
                                                isSeries = true;
                                        }                                            
                                        break;
                                    case "BannerType2":
                                        if (isSeries)
                                        {
                                            if (propertyNode.InnerText.Equals(sSeriesBannerType, StringComparison.CurrentCultureIgnoreCase))
                                            {
                                                // The first Banner is the Highest Rated Banner                                                
                                                if (!bHighestRatedSeriesIsSet)
                                                {
                                                    bHighestRatedSeriesIsSet = true;
                                                    b.bHighestRated = true;
                                                }
                                                b.bGraphical = true;
                                            }
                                            //if (propertyNode.InnerText.Equals("680x1000", StringComparison.CurrentCultureIgnoreCase))
                                            //    b.bPoster = true;
                                        }
                                        else
                                        {
                                            if (!propertyNode.InnerText.Equals("season", StringComparison.CurrentCultureIgnoreCase))                                            
                                                 isGood = false;
                                        }
                                        break;
                                    case "Language":
                                        bs.sBannerLang = propertyNode.InnerText;
                                        b.sBannerLang = propertyNode.InnerText;
                                        break;
                                    case "Season":
                                        sCurrentSeason = propertyNode.InnerText;
                                        bs.sSeason = sCurrentSeason;
                                        
                                        // Each season can have a highest rated banner
                                        // Search through all previous season banners                                        
                                        bHighestRatedSeasonIsSet = false;
                                        for (int i = 0; i < sPreviousSeasons.Length; i++)
                                        {   
                                            // If season has already been identified as highest rated
                                            // then set flag
                                            if (sPreviousSeasons[i].Equals(sCurrentSeason))
                                            {
                                                bHighestRatedSeasonIsSet = true;
                                                break;
                                            }
                                        }

                                        if (!bHighestRatedSeasonIsSet)
                                        {
                                            bHighestRatedSeasonIsSet = true;
                                            bs.bHighestRated = true;
                                            Array.Resize(ref sPreviousSeasons, sPreviousSeasons.Length + 1);
                                            sPreviousSeasons[sPreviousSeasons.Length - 1] = sCurrentSeason;                                            
                                        }                                                                                                                        
                                        break;
                                }                                
                            }

                            try
                            {
                                b.sSeriesName = Helper.getCorrespondingSeries(Int32.Parse(seriesID)).ToString();
                            }
                            catch { return; }
                            bs.sSeriesName = b.sSeriesName;

                            if (isSeries)
                                m_bannerSeriesList.Add(b);
                            else if(isGood)
                                m_bannerSeasonList.Add(bs);
                        }                        
                    }
                }
                map.seasonBanners = m_bannerSeasonList;
                map.seriesBanners = m_bannerSeriesList;
                // series already in?
                if (seriesBanners.Contains(map))
                {
                    seriesBannersMap seriesMap = seriesBanners[seriesBanners.IndexOf(map)];
                    seriesMap.seasonBanners.AddRange(map.seasonBanners);
                    seriesMap.seriesBanners.AddRange(map.seriesBanners);
                }
                else seriesBanners.Add(map);
                DownloadBanners(Online_Parsing_Classes.OnlineAPI.SelLanguageAsString);

            }
        }

        private void DownloadBanners(string bannerLang)
        {
            // now that we have all the paths, download all the files
            foreach (seriesBannersMap map in seriesBanners)
            {
                foreach (BannerSeries bannerSeries in map.seriesBanners)
                {
                    if (bannerLang == bannerSeries.sBannerLang || "en" == bannerSeries.sBannerLang || "" == bannerSeries.sBannerLang) //also always english ones
                    {
                        // mark the filename with the language                        
                        bannerSeries.sBannerFileName = Helper.cleanLocalPath(bannerSeries.sSeriesName) + @"\-lang" + bannerSeries.sBannerLang + "-" + bannerSeries.sOnlineBannerPath;                        
                        Online_Parsing_Classes.OnlineAPI.DownloadBanner(bannerSeries.sOnlineBannerPath, Settings.Path.banners, bannerSeries.sBannerFileName);

                    }
                }

                foreach (BannerSeason bannerSeason in map.seasonBanners)
                {
                    if (bannerLang == bannerSeason.sBannerLang || "en" == bannerSeason.sBannerLang || "" == bannerSeason.sBannerLang)
                    {
                        bannerSeason.sBannerFileName = Helper.cleanLocalPath(bannerSeason.sSeriesName) + @"\-lang" + bannerSeason.sBannerLang + "-" + bannerSeason.sOnlineBannerPath;
                        Online_Parsing_Classes.OnlineAPI.DownloadBanner(bannerSeason.sOnlineBannerPath, Settings.Path.banners, bannerSeason.sBannerFileName);

                    }
                }
            }
        }
    }

    class GetFanart
    {
        List<DBFanart> _fanart = new List<DBFanart>();
        public List<DBFanart> Fanart { get { return _fanart; } }
        
        public GetFanart(int SeriesID)
        {
            XmlNodeList nodeList = Online_Parsing_Classes.OnlineAPI.getBannerList(SeriesID);
            if (nodeList != null)
            {
                foreach (XmlNode topNode in nodeList)
                {
                    foreach (XmlNode itemNode in topNode.ChildNodes)
                    {                        
                        if (itemNode.Name == "Banner")
                        {
                            bool isFanart = false;
                            DBFanart dbf = new DBFanart();
                            foreach (XmlNode propertyNode in itemNode.ChildNodes)
                            {
                                try
                                {
                                    dbf[propertyNode.Name] = propertyNode.InnerText;
                                }
                                catch (Exception ex)
                                { MPTVSeriesLog.Write("Error adding Fanart Property to DBEntry: " + propertyNode.Name + " - " + ex.Message); }

                                if (propertyNode.Name == "BannerType"
                                    && propertyNode.InnerText.Equals("fanart", StringComparison.InvariantCultureIgnoreCase))
                                    isFanart = true;
                            }
                            if (isFanart)
                            {
                                dbf[DBFanart.cSeriesID] = SeriesID;
                                _fanart.Add(dbf);
                            }
                        }
                    }
                }
            }
        }

        public static bool DownloadFanart(DBFanart fanart)
        {
            if (fanart != null && !fanart.isAvailableLocally)
            {
                string filename;
                bool sucess = download(fanart[DBFanart.cBannerPath], out filename);
                fanart[DBFanart.cLocalPath] = filename.Replace(Settings.GetPath(Settings.Path.fanart), string.Empty);
                fanart.Commit();
                return sucess;
            }
            return false;
        }

        public static string GetThumbnail(DBFanart fanart)
        {
            if (fanart != null)
            {
                string filename;
                string onlinePath = fanart[DBFanart.cThumbnailPath];
                download(fanart[DBFanart.cThumbnailPath], out filename);
                filename = Helper.PathCombine(Settings.GetPath(Settings.Path.fanart), filename);

                return ImageAllocator.GetOtherImage(filename, new System.Drawing.Size(0, 0), false);
            }
            return string.Empty;
        }

        static bool download(string onlinePath, out string filename)
        {
            filename = onlinePath.Replace("/", @"\");
            return Online_Parsing_Classes.OnlineAPI.DownloadBanner(onlinePath, Settings.Path.fanart, filename);
        }
    }

    
}
