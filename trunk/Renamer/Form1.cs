﻿#region SVN Info
/***************************************************************
 * $Author$
 * $Revision$
 * $Date$
 * $LastChangedBy$
 * $LastChangedDate$
 * $URL$
 * 
 * License: GPLv3
 * 
****************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections;
using System.Diagnostics;
using Schematrix;
using ICSharpCode.SharpZipLib.Zip;
using Renamer.Classes;
using Renamer.Dialogs;
using Renamer.Classes.Configuration.Keywords;
using Renamer.Classes.Configuration;
using System.Runtime.InteropServices;
using Renamer.Logging;
using Renamer.Classes.Provider;
using System.Threading;
using BrightIdeasSoftware;
namespace Renamer
{
    /// <summary>
    /// Main Form
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// Settings class contains some stuff which can't be stored in config file for one reason or another
        /// </summary>
        private Settings settings;

        /// <summary>
        /// Custom sorting class to sort list view for 2 columns
        /// </summary>
        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        /// <summary>
        /// Column width ratios needed to keep them during resize
        /// </summary>
        private float[] columnsizes;

        /// <summary>
        /// Temp variable to store a previously focused control
        /// </summary>
        private Control focused = null;

        /// <summary>
        /// Program arguments
        /// </summary>
        private List<string> args;

        protected static Form1 instance;
        private static object m_lock = new object();

        public static Form1 Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (m_lock) { if (instance == null) instance = new Form1(null); }
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }
        /// <summary>
        /// GUI constructor
        /// </summary>
        /// <param name="args">program arguments</param>
        public Form1(string[] args) {
            this.args = new List<string>(args);
            InitializeComponent();
        }

        #region processing
        #region Name Creation




        /// <summary>
        /// Sets an InfoEntry as movie and generates name
        /// </summary>
        private void MarkAsMovie() {
            //Tags should be preceeded by . or _ or ( or [ or - or ]
            string[] tags = Helper.ReadProperties(Config.Tags);
            List<string> regexes = new List<string>();
            foreach (string s in tags) {
                regexes.Add("[\\._\\(\\[-]" + s);
            }
            for(int i=0;i<lstEntries.SelectedIndices.Count;i++){
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)((OLVListItem)lvi).RowObject;
                //Go through all selected files and remove tags and clean them up
                ie.Showname = "";
                ie.Destination = "";
                ie.NewFilename = "";
                ie.RemoveVideoTags();
            }
            lstEntries.Refresh();
        }

        /// <summary>
        /// Sets an InfoEntry as TV Show and generates name
        /// </summary>
        private void MarkAsTVShow()
        {            
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)((OLVListItem)lvi).RowObject;
                //Go through all selected files and remove tags and clean them up
                ie.Movie = false;
                ie.Showname=SeriesNameExtractor.Instance.ExtractSeriesName(ie);
                ie.Destination = "";
                ie.NewFilename = "";
                DataGenerator.ExtractSeasonAndEpisode(ie);
            }
            lstEntries.Refresh();
        }
        #endregion

        /// <summary>
        /// Main Rename function
        /// </summary>
        private void Rename() {
            InfoEntryManager.Instance.Rename();
            //Get a list of all involved folders
            FillListView();
        }

        #endregion
        #region Subtitles
        #region Parsing


       


        /// <summary>
        /// This function is needed if Subtitle links are located on a series page, not implemented yet
        /// </summary>
        /// <param name="url">URL of the page to get subtitle links from</param>
        private void GetSubtitleFromSeriesPage(string url) {
            //Don't forget the cropping
            //
            //Source cropping
            //source = source.Substring(Math.Max(source.IndexOf(provider.SearchStart),0));
            //source = source.Substring(0, Math.Max(source.LastIndexOf(provider.SearchEnd),0));
            /*
            //if episode infos are stored on a new page for each season, this should be marked with %S in url, so we can iterate through all those pages
            int season = 1;
            string url2 = url;
            while (true)
            {
                    if (url2.Contains("%S"))
                    {
                            url = url2.Replace("%S", season.ToString());
                    }
                    if (url == null || url == "") return;
                    // request
                    url = System.Web.HttpUtility.UrlPathEncode(url);
                    HttpWebRequest requestHtml = null;
                    try
                    {
                            requestHtml = (HttpWebRequest)(HttpWebRequest.Create(url));
                    }
                    catch (Exception ex)
                    {
                            Logger.Instance.LogMessage(ex.Message, LogLevel.ERROR);
                            return;
                    }
                    requestHtml.Timeout = 5000;
                    // get response
                    HttpWebResponse responseHtml;
                    try
                    {
                            responseHtml = (HttpWebResponse)(requestHtml.GetResponse());
                    }
                    catch (WebException e)
                    {
                            Logger.Instance.LogMessage(e.Message, LogLevel.ERROR);
                            return;
                    }
                    //if we get redirected, lets assume this page does not exist
                    if (responseHtml.ResponseUri.AbsoluteUri != url)
                    {
                            break;
                    }
                    // and download
                    //Logger.Instance.LogMessage("charset=" + responseHtml.CharacterSet, LogLevel.INFO);

                    StreamReader r = new StreamReader(responseHtml.GetResponseStream(), Encoding.GetEncoding(responseHtml.CharacterSet));
                    string source = r.ReadToEnd();
                    r.Close();
                    responseHtml.Close();
                    string pattern = Settings.subprovider.RelationsRegExp;
                    MatchCollection mc = Regex.Matches(source, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match m in mc)
                    {
                            //if we are iterating through season pages, take season from page url directly
                            if (url != url2)
                            {
                                    Info.AddRelationCollection(new Relation(season.ToString(), m.Groups["Episode"].Value, System.Web.HttpUtility.HtmlDecode(m.Groups["Title"].Value)));
                                    //Logger.Instance.LogMessage("Found Relation: " + "S" + season.ToString() + "E" + m.Groups["Episode"].Value + " - " + System.Web.HttpUtility.HtmlDecode(m.Groups["Title"].Value), LogLevel.INFO);
                            }
                            else
                            {
                                    Info.AddRelationCollection(new Relation(m.Groups["Season"].Value, m.Groups["Episode"].Value, System.Web.HttpUtility.HtmlDecode(m.Groups["Title"].Value)));
                                    //Logger.Instance.LogMessage("Found Relation: " + "S" + m.Groups["Season"].Value + "E" + m.Groups["Episode"].Value + " - " + System.Web.HttpUtility.HtmlDecode(m.Groups["Title"].Value), LogLevel.INFO);
                            }
                    }
                    // THOU SHALL NOT FORGET THE BREAK
                    if (!url2.Contains("%S")) break;
                    season++;
            }*/
        }
        #endregion

 

       
        #endregion
        
        //Since sorting after the last two selected columns is supported, we need some event handling here
        private void lstFiles_ColumnClick(object sender, ColumnClickEventArgs e) {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn) {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending) {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else {
                // Set the column number that is to be sorted; default to ascending.

                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            //this.lstFiles.Sort();
        }

        //End editing with combo box data types
        /*private void cbEdit_SelectedIndexChanged(object sender, EventArgs e) {
            lstFiles.EndEditing(true);
        }

        //Start editing single values
        private void lstFiles_SubItemClicked(object sender, ListViewEx.SubItemEventArgs e) {
            if (e.SubItem != 0 && e.SubItem != 1) {
                if (settings.IsMonoCompatibilityMode) {
                    Logger.Instance.LogMessage("Editing Entries dynamically is not supported in Mono unfortunately :(", LogLevel.WARNING);
                    return;
                }
                RelationCollection rc = RelationManager.Instance.GetRelationCollection(InfoEntryManager.Instance[(int)e.Item.Tag].Showname);
                if (e.SubItem == 4) {
                    //if season is valid and there are relations at all, show combobox. Otherwise, just show edit box
                    if (rc != null && RelationManager.Instance.Count > 0 && Convert.ToInt32(e.Item.SubItems[2].Text) >= rc.FindMinSeason() && Convert.ToInt32(e.Item.SubItems[2].Text) <= rc.FindMaxSeason()) {
                        comEdit.Items.Clear();
                        foreach (Relation rel in rc) {
                            if (rel.Season == InfoEntryManager.Instance[(int)e.Item.Tag].Season) {
                                comEdit.Items.Add(rel.Name);
                            }
                        }
                        lstFiles.StartEditing(comEdit, e.Item, e.SubItem);
                    }
                    else {
                        lstFiles.StartEditing(txtEdit, e.Item, e.SubItem);
                    }
                }
                else if (e.SubItem == 5 || e.SubItem == 6 || e.SubItem == 7) {
                    lstFiles.StartEditing(txtEdit, e.Item, e.SubItem);
                }
                else {
                    //clamp season and episode values to allowed values
                    if (rc != null && rc.Count > 0) {
                        if (e.SubItem == 2) {
                            numEdit.Minimum = rc.FindMinSeason();
                            numEdit.Maximum = rc.FindMaxSeason();
                        }
                        else if (e.SubItem == 3) {
                            numEdit.Minimum = rc.FindMinEpisode(Convert.ToInt32(InfoEntryManager.Instance[(int)e.Item.Tag].Season));
                            numEdit.Maximum = rc.FindMaxEpisode(Convert.ToInt32(InfoEntryManager.Instance[(int)e.Item.Tag].Season));
                        }
                    }
                    else {
                        numEdit.Minimum = 0;
                        numEdit.Maximum = 10000;
                    }
                    lstFiles.StartEditing(numEdit, e.Item, e.SubItem);
                }
            }
        }

        //End editing a value, apply possible changes and process them
        private void lstFiles_SubItemEndEditing(object sender, ListViewEx.SubItemEndEditingEventArgs e) {
            string dir = Helper.ReadProperty(Config.LastDirectory);
            bool CreateDirectoryStructure = Helper.ReadInt(Config.CreateDirectoryStructure) == 1;
            bool UseSeasonSubdirs = Helper.ReadInt(Config.UseSeasonSubDir) == 1;
            int tmp = -1;
            InfoEntry ie = InfoEntryManager.Instance.GetByListViewItem(e.Item);
            //add lots of stuff here
            switch (e.SubItem) {
                //season
                case 2:
                    try{
                        tmp = Int32.Parse(e.DisplayText);
                    }
                    catch (Exception ex){
                        Logger.Instance.LogMessage("Cannot parse '" + e.DisplayText + "' to an integer", LogLevel.WARNING);
                    }
                    ie.Season = tmp;
                    if (e.DisplayText == "") {
                        ie.Movie = true;
                    }
                    else {
                        ie.Movie = false;
                    }
                    //SetupRelation((int)e.Item.Tag);
                    //foreach (InfoEntry ie in InfoEntryManager.Episodes)
                    //{
                    //    SetDestinationPath(ie, dir, CreateDirectoryStructure, UseSeasonSubdirs);
                    //}
                    break;
                //Episode
                case 3:
                    try {
                        tmp = Int32.Parse(e.DisplayText);
                    }
                    catch (Exception ex) {
                        Logger.Instance.LogMessage("Cannot parse '" + e.DisplayText + "' to an integer", LogLevel.WARNING);
                    }
                    ie.Episode = tmp;
                    if (e.DisplayText == "") {
                        ie.Movie = true;
                    }
                    else {
                        ie.Movie = false;
                    }
                    //SetupRelation((int)e.Item.Tag);                    
                    //SetDestinationPath(ie, dir, CreateDirectoryStructure, UseSeasonSubdirs);
                    break;
                //name
                case 4:
                    //backtrack to see if entered text matches a season/episode
                    RelationCollection rc = RelationManager.Instance.GetRelationCollection(ie.Showname);
                    if (rc != null) {
                        foreach (Relation rel in rc) {
                            //if found, set season and episode in gui and sync back to data
                            if (e.DisplayText == rel.Name) {
                                e.Item.SubItems[2].Text = rel.Season.ToString();
                                e.Item.SubItems[3].Text = rel.Episode.ToString();
                            }
                        }
                    }
                    ie.Name = e.DisplayText;
                    break;
                //Filename
                case 5:
                    ie.NewFileName = e.DisplayText;
                    if (ie.NewFileName != e.DisplayText)
                    {
                        e.Cancel = true;
                        Logger.Instance.LogMessage("Changed entered Text from " + e.DisplayText + " to " + ie.NewFileName + " because of invalid filename characters.", LogLevel.INFO);
                    }
                    break;
                //Destination
                case 6:
                    try {
                        Path.GetDirectoryName(e.DisplayText);
                        ie.Destination = e.DisplayText;
                        if (ie.Destination != e.DisplayText)
                        {
                            e.Cancel = true;
                            Logger.Instance.LogMessage("Changed entered Text from " + e.DisplayText + " to " + ie.Destination + " because of invalid path characters.", LogLevel.INFO);
                        }
                    }
                    catch (Exception) {
                        e.Cancel = true;
                    }
                    break;
                case 7:                   
                    ie.Showname = e.DisplayText;
                    //Cancel editing since we want to set the subitem manually in SyncItem to a different value
                    if (ie.Showname == "")
                    {
                        e.Cancel = true;
                    }
                    break;
                default:
                    throw new Exception("Unreachable code");
            }
            SyncItem((int)e.Item.Tag, false);
        }

        //Double click = Invert process flag
        private void lstFiles_DoubleClick(object sender, EventArgs e) {
            if (lstFiles.SelectedIndices.Count > 0)
            {
                lstFiles.Items[lstFiles.SelectedIndices[0]].Checked = !lstFiles.Items[lstFiles.SelectedIndices[0]].Checked;
            }
        }*/

        #region GUI-Events
        //Main Initialization
        private void Form1_Load(object sender, EventArgs e) {
            settings = Settings.Instance;
            this.initMyLoggers();

            // Init logging here:

            


            //and read a value to make sure it is loaded into memory
            Helper.ReadProperty(Config.Case);

            //lstFiles.ListViewItemSorter = lvwColumnSorter;
            txtTarget.Text = Helper.ReadProperty(Config.TargetPattern);

            

            //subtitle provider combo box
            cbSubs.Items.AddRange(SubtitleProvider.ProviderNames);
            string LastSubProvider = Helper.ReadProperty(Config.LastSubProvider);
            if (LastSubProvider == null)
                LastSubProvider = "";
            cbSubs.SelectedIndex = Math.Max(-1, cbSubs.Items.IndexOf(LastSubProvider));
            Helper.WriteProperty(Config.LastSubProvider, cbSubs.Text);

            //Last directory
            string lastdir = Helper.ReadProperty(Config.LastDirectory);

            //First argument=folder
            if (args.Count > 0) {
                string dir = args[0].Replace("\"", "");
                if (Directory.Exists(args[0])) {
                    lastdir = dir;
                    Helper.WriteProperty(Config.LastDirectory, lastdir);
                }
            }
            InitListView();
            if (lastdir != null && lastdir != "" && Directory.Exists(lastdir)) {
                txtPath.Text = lastdir;
                Environment.CurrentDirectory = lastdir;
                UpdateList(true);
            }


            string[] ColumnWidths = Helper.ReadProperties(Config.ColumnWidths);
            string[] ColumnOrder = Helper.ReadProperties(Config.ColumnOrder);
            for (int i = 0; i < lstEntries.Columns.Count; i++) {
                try {
                    int width = lstEntries.Columns[i].Width;
                    Int32.TryParse(ColumnWidths[i], out width);
                    lstEntries.Columns[i].Width = width;
                }
                catch (Exception) {
                    Logger.Instance.LogMessage("Invalid Value for ColumnWidths[" + i + "]", LogLevel.ERROR);
                }
                try {
                    int order = lstEntries.Columns[i].DisplayIndex;
                    Int32.TryParse(ColumnOrder[i], out order);
                    lstEntries.Columns[i].DisplayIndex = order;
                }
                catch (Exception) {
                    Logger.Instance.LogMessage("Invalid Value for ColumnOrder[" + i + "]", LogLevel.ERROR);
                }
            }
            string[] Windowsize = Helper.ReadProperties(Config.WindowSize);
            if (Windowsize.GetLength(0) >= 2) {
                try {
                    int w, h;
                    Int32.TryParse(Windowsize[0], out w);
                    Int32.TryParse(Windowsize[1], out h);
                    this.Width = w;
                    this.Height = h;
                }
                catch (Exception) {
                    Logger.Instance.LogMessage("Couldn't process WindowSize property: " + Helper.ReadProperty(Config.WindowSize), LogLevel.ERROR);
                }
                //focus fix
                txtPath.Focus();
                txtPath.Select(txtPath.Text.Length, 0);
            }
        }

        //Auto column resize by storing column width ratios at resize start
        private void Form1_ResizeBegin(object sender, EventArgs e) {
            if (Helper.ReadBool(Config.ResizeColumns)) {
                columnsizes = new float[]{
                (float)(lstEntries.Columns[0].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[1].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[2].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[3].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[4].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[5].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[6].Width)/(float)(lstEntries.ClientRectangle.Width),
                (float)(lstEntries.Columns[7].Width)/(float)(lstEntries.ClientRectangle.Width)};
                float sum = 0;
                for (int i = 0; i < lstEntries.Columns.Count; i++) {
                    sum += columnsizes[i];
                }
                //some numeric correction to make ratios:
                for (int i = 0; i < lstEntries.Columns.Count; i++)
                {
                    columnsizes[i] *= (float)1 / sum;
                }
            }
        }

        //Auto column resize, restore Column width ratios at resize end (to make sure!)
        private void Form1_ResizeEnd(object sender, EventArgs e) {
            if (Helper.ReadBool(Config.ResizeColumns)) {
                if (lstEntries != null && lstEntries.Columns.Count > 0 && columnsizes != null)
                {
                    lstEntries.Columns[0].Width = (int)(columnsizes[0] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[1].Width = (int)(columnsizes[1] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[2].Width = (int)(columnsizes[2] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[3].Width = (int)(columnsizes[3] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[4].Width = (int)(columnsizes[4] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[5].Width = (int)(columnsizes[5] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[6].Width = (int)(columnsizes[6] * (float)(lstEntries.ClientRectangle.Width));
                    lstEntries.Columns[7].Width = (int)(columnsizes[7] * (float)(lstEntries.ClientRectangle.Width));
                }
            }
        }

        //Auto column resize, restore Column width ratios during resize
        private void Form1_Resize(object sender, EventArgs e) {
            if (this.Visible) {
                if (Helper.ReadBool(Config.ResizeColumns)) {
                    if (lstEntries != null && lstEntries.Columns.Count >0 && columnsizes != null) {
                        lstEntries.Columns[0].Width = (int)(columnsizes[0] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[1].Width = (int)(columnsizes[1] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[2].Width = (int)(columnsizes[2] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[3].Width = (int)(columnsizes[3] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[4].Width = (int)(columnsizes[4] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[5].Width = (int)(columnsizes[5] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[6].Width = (int)(columnsizes[6] * (float)(lstEntries.ClientRectangle.Width));
                        lstEntries.Columns[7].Width = (int)(columnsizes[7] * (float)(lstEntries.ClientRectangle.Width));
                    }
                }
            }
        }

        //Save last focussed control so it can be restored after splitter between file and log window is moved
        private void scContainer_MouseDown(object sender, MouseEventArgs e) {
            // Get the focused control before the splitter is focused
            focused = getFocused(this.Controls);
        }

        //restore last focussed control so splitter doesn't keep focus
        private void scContainer_MouseUp(object sender, MouseEventArgs e) {
            // If a previous control had focus
            if (focused != null) {
                // Return focus and clear the temp variable for
                // garbage collection (is this needed?)
                focused.Focus();
                focused = null;
            }
        }

        //Update Current Provider setting
        private void cbProviders_SelectedIndexChanged(object sender, EventArgs e) {
            Helper.WriteProperty(Config.LastProvider, cbProviders.SelectedItem.ToString());
        }

        //Update Current Subtitle Provider setting
        private void cbSubs_SelectedIndexChanged(object sender, EventArgs e) {
            Helper.WriteProperty(Config.LastSubProvider, cbSubs.SelectedItem.ToString());
        }

        //Show About box dialog
        private void btnAbout_Click(object sender, EventArgs e) {
            AboutBox ab = new AboutBox();
            ab.AppMoreInfo += Environment.NewLine;
            ab.AppMoreInfo += "Using:" + Environment.NewLine;
            ab.AppMoreInfo += "ObjectListView " + "http://www.codeproject.com/KB/list/ObjectListView.aspx" + Environment.NewLine;
            ab.AppMoreInfo += "About Box " + "http://www.codeproject.com/KB/vb/aboutbox.aspx" + Environment.NewLine;
            ab.AppMoreInfo += "Unrar.dll " + "http://www.rarlab.com" + Environment.NewLine;
            ab.AppMoreInfo += "SharpZipLib " + "http://www.icsharpcode.net/OpenSource/SharpZipLib/" + Environment.NewLine;
            ab.ShowDialog(this);
        }

        //Open link in browser when clicked in log
        private void rtbLog_LinkClicked(object sender, LinkClickedEventArgs e) {
            try {
                Process myProc = Process.Start(e.LinkText);
            }
            catch (Exception ex) {
                Logger.Instance.LogMessage("Couldn't open " + e.LinkText + ":" + ex.Message, LogLevel.ERROR);
            }
        }

        /*//Update Destination paths when title of show is changed
        private void cbTitle_TextChanged(object sender, EventArgs e)
        {
                //when nothing is selected, assume there is no autocompletion taking place right now
                if (cbTitle.SelectedText == "" && cbTitle.Text != "")
                {
                        for (int i = 0; i < cbTitle.Items.Count; i++)
                        {
                                if (((string)cbTitle.Items[i]).ToLower() == cbTitle.Text.ToLower())
                                {
                                        bool found = false;
                                        foreach (object o in cbTitle.Items)
                                        {
                                                if ((string)o == cbTitle.Text)
                                                {
                                                        found = true;
                                                        break;
                                                }
                                        }
                                        if (!found)
                                        {
                                                cbTitle.Items.Add(cbTitle.Text);
                                        }
                                        break;
                                }
                        }
                }
        }*/


        //Show File Open dialog and update file list
        private void btnPath_Click(object sender, EventArgs e) {
            //weird mono hackfix
            if (settings.IsMonoCompatibilityMode) {
                fbdPath.SelectedPath = Environment.CurrentDirectory;
            }
            string lastdir = Helper.ReadProperty(Config.LastDirectory);
            if (!settings.IsMonoCompatibilityMode) {
                if (lastdir != null && lastdir != "" && Directory.Exists(lastdir)) {
                    fbdPath.SelectedPath = lastdir;
                }
                else {
                    fbdPath.SelectedPath = Environment.CurrentDirectory;
                }
            }

            DialogResult dr = fbdPath.ShowDialog();
            if (dr == DialogResult.OK) {
                string path = fbdPath.SelectedPath;
                InfoEntryManager.Instance.SetPath(ref path);
                UpdateList(true);
            }
        }

        public void initMyLoggers()
        {
            Logger logger = Logger.Instance;
            logger.removeAllLoggers();
            logger.addLogger(new FileLogger(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Renamer.log", true, Helper.ReadEnum<LogLevel>(Config.LogFileLevel)));
            logger.addLogger(new MessageBoxLogger(Helper.ReadEnum<LogLevel>(Config.LogMessageBoxLevel)));

            //mono compatibility fixes
            if (Type.GetType("Mono.Runtime") != null)
            {
                logger.addLogger(new TextBoxLogger(txtLog, Helper.ReadEnum<LogLevel>(Config.LogTextBoxLevel)));
                rtbLog.Visible = false;
                txtLog.Visible = true;
                Logger.Instance.LogMessage("Running on Mono", LogLevel.INFO);
            }
            else
            {
                rtbLog.Text = "";
                logger.addLogger(new RichTextBoxLogger(rtbLog, Helper.ReadEnum<LogLevel>(Config.LogTextBoxLevel)));
            }
        }

        //Show configuration dialog
        private void btnConfig_Click(object sender, EventArgs e) {
            Configuration cfg = new Configuration();
            if (cfg.ShowDialog() == DialogResult.OK) {
                UpdateList(true);                
            }
        }

        //Fetch all title information etc yada yada yada blalblabla
        private void btnTitles_Click(object sender, EventArgs e) {
            DataGenerator.GetAllTitles();
            FillListView();
        }



        /*//Enter = Click "Get Titles" button
        private void cbTitle_KeyDown(object sender, KeyEventArgs e)
        {
                if (e.KeyCode == Keys.Enter)
                {
                        SetNewTitle(cbTitle.Text);
                        btnTitles.PerformClick();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                        cbTitle.Text = Helper.ReadProperties(Config.LastTitles)[0];
                        cbTitle.SelectionStart = cbTitle.Text.Length;
                }
        }*/

        //Enter = Change current directory
        private void txtPath_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                //need to update the displayed path because it might be changed (tailing backlashes are removed, and added for drives ("C:\" instead of "C:"))
                string path = txtPath.Text;
                InfoEntryManager.Instance.SetPath(ref path);
                txtPath.Text = path;
                txtPath.SelectionStart = txtPath.Text.Length;
                UpdateList(true);
            }
            else if (e.KeyCode == Keys.Escape) {
                txtPath.Text = Helper.ReadProperty(Config.LastDirectory);
                txtPath.SelectionStart = txtPath.Text.Length;
            }
        }

        //Focus lost = change current directory
        private void txtPath_Leave(object sender, EventArgs e) {
            InfoEntryManager.Instance.SetPath(txtPath.Text);
        }

        //Start renaming
        private void btnRename_Click(object sender, EventArgs e) {
            Rename();
        }

        //Focus lost = store desired pattern and update names
        private void txtTarget_Leave(object sender, EventArgs e) {
            if (txtTarget.Text != Helper.ReadProperty(Config.TargetPattern)) {
                Helper.WriteProperty(Config.TargetPattern, txtTarget.Text);
                InfoEntryManager.Instance.CreateNewNames();
                FillListView();
            }
        }

        //Enter = store desired pattern and update names
        private void txtTarget_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                if (txtTarget.Text != Helper.ReadProperty(Config.TargetPattern)) {
                    Helper.WriteProperty(Config.TargetPattern, txtTarget.Text);
                    InfoEntryManager.Instance.CreateNewNames();
                    FillListView();
                }
            }
            else if (e.KeyCode == Keys.Escape) {
                txtTarget.Text = Helper.ReadProperty(Config.TargetPattern);
                txtTarget.SelectionStart = txtTarget.Text.Length;
            }
        }

        //start fetching subtitles
        private void btnSubs_Click(object sender, EventArgs e) {
            DataGenerator.GetSubtitles();
        }

        //Cleanup, save some stuff etc
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            Helper.WriteProperty(Config.LastSubProvider, cbSubs.SelectedItem.ToString());

            //Save column order and sizes
            string[] ColumnWidths = new string[lstEntries.Columns.Count];
            string[] ColumnOrder = new string[lstEntries.Columns.Count];
            for (int i = 0; i < lstEntries.Columns.Count; i++) {
                ColumnOrder[i] = lstEntries.Columns[i].DisplayIndex.ToString();
                ColumnWidths[i] = lstEntries.Columns[i].Width.ToString();
            }
            Helper.WriteProperties(Config.ColumnOrder, ColumnOrder);
            Helper.WriteProperties(Config.ColumnWidths, ColumnWidths);

            //save window size
            string[] WindowSize = new string[2];
            if (this.WindowState == FormWindowState.Normal) {
                WindowSize[0] = this.Size.Width.ToString();
                WindowSize[1] = this.Size.Height.ToString();
            }
            else {
                WindowSize[0] = this.RestoreBounds.Width.ToString();
                WindowSize[1] = this.RestoreBounds.Height.ToString();
            }
            Helper.WriteProperties(Config.WindowSize, WindowSize);

            //Also flush values stored directly in RelationProvider classd
            RelationProvider.Flush();
            foreach (DictionaryEntry dict in settings) {
                ((ConfigFile)dict.Value).Flush();
            }
        }
        #endregion
        #region Contextmenu
        //Set which context menu options should be avaiable
        private void contextFiles_Opening(object sender, CancelEventArgs e) {
            editSubtitleToolStripMenuItem.Visible = false;
            viewToolStripMenuItem.Visible = false;
            renamingToolStripMenuItem.Visible = false;
            markAsMovieToolStripMenuItem.Visible = true;
            markAsTVSeriesToolStripMenuItem.Visible = true;
            selectToolStripMenuItem.Visible = true;
            checkToolStripMenuItem.Visible = true;
            renamingToolStripMenuItem.Visible = true;
            setDestinationToolStripMenuItem.Visible = true;
            setEpisodesFromtoToolStripMenuItem.Visible = true;
            setSeasonToolStripMenuItem.Visible = true;
            setShownameToolStripMenuItem.Visible = true;
            deleteToolStripMenuItem.Visible = true;
            toolStripSeparator2.Visible = true;
            toolStripSeparator3.Visible = true;
            toolStripSeparator4.Visible = true;
            if (lstEntries.SelectedIndices.Count == 0)
            {
                markAsMovieToolStripMenuItem.Visible = false;
                markAsTVSeriesToolStripMenuItem.Visible = false;
                selectToolStripMenuItem.Visible = false;
                checkToolStripMenuItem.Visible = false;
                renamingToolStripMenuItem.Visible = false;
                setDestinationToolStripMenuItem.Visible = false;
                setEpisodesFromtoToolStripMenuItem.Visible = false;
                setSeasonToolStripMenuItem.Visible = false;
                setShownameToolStripMenuItem.Visible = false;
                deleteToolStripMenuItem.Visible = false;
                toolStripSeparator2.Visible = false;
                toolStripSeparator3.Visible = false;
                toolStripSeparator4.Visible = false;
            }
            if (lstEntries.SelectedIndices.Count == 1)
            {
                //if selected file is a subtitle
                List<string> subext = new List<string>(Helper.ReadProperties(Config.SubtitleExtensions));
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[0]];
                InfoEntry ie=(InfoEntry)lvi.RowObject;
                if (subext.Contains(ie.Extension.ToLower()))
                {
                    editSubtitleToolStripMenuItem.Visible = true;
                }

                //if selected file is a video and there is a matching subtitle
                if (InfoEntryManager.Instance.GetSubtitle(ie) != null)
                {
                    editSubtitleToolStripMenuItem.Visible = true;
                }

                //if there is a matching video
                if (InfoEntryManager.Instance.GetVideo(ie) != null)
                {
                    viewToolStripMenuItem.Visible = true;
                }
            }
            if (lstEntries.SelectedIndices.Count > 0)
            {
                renamingToolStripMenuItem.Visible = true;
                createDirectoryStructureToolStripMenuItem1.Checked = false;
                dontCreateDirectoryStructureToolStripMenuItem.Checked = false;
                largeToolStripMenuItem.Checked = false;
                smallToolStripMenuItem.Checked = false;
                igNorEToolStripMenuItem.Checked = false;
                cAPSLOCKToolStripMenuItem.Checked = false;
                useUmlautsToolStripMenuItem.Checked = false;
                dontUseUmlautsToolStripMenuItem.Checked = false;
                useProvidedNamesToolStripMenuItem.Checked = false;
                copyToolStripMenuItem.Visible = true;
                bool OldPath = false;
                bool OldFilename = false;
                bool Name = false;
                bool Destination = false;
                bool NewFilename = false;
                bool MoviesOnly = true;
                bool TVShowsOnly = true;
                InfoEntry.DirectoryStructure CreateDirectoryStructure = InfoEntry.DirectoryStructure.Unset;
                InfoEntry.Case Case = InfoEntry.Case.Unset;
                InfoEntry.UmlautAction Umlaute = InfoEntry.UmlautAction.Unset;
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    if (ie.Movie) TVShowsOnly = false;
                    else MoviesOnly = false;
                    if (i == 0) {
                        CreateDirectoryStructure = ie.CreateDirectoryStructure;
                        Case = ie.Casing;
                        Umlaute = ie.UmlautUsage;
                    }
                    else {
                        if (CreateDirectoryStructure != ie.CreateDirectoryStructure)
                        {
                            CreateDirectoryStructure = InfoEntry.DirectoryStructure.Unset;
                        }
                        if (Case != ie.Casing)
                        {
                            Case = InfoEntry.Case.Unset;
                        }
                        if (Umlaute != ie.UmlautUsage)
                        {
                            Umlaute = InfoEntry.UmlautAction.Unset;
                        }
                    }
                }
                if (TVShowsOnly)
                {
                    markAsMovieToolStripMenuItem.Visible = true;
                    markAsTVSeriesToolStripMenuItem.Visible = false;
                }
                else if (MoviesOnly)
                {
                    markAsTVSeriesToolStripMenuItem.Visible = true;
                    markAsMovieToolStripMenuItem.Visible = false;
                }
                else
                {
                    markAsMovieToolStripMenuItem.Visible = true;
                    markAsTVSeriesToolStripMenuItem.Visible = true;
                }
                if (CreateDirectoryStructure == InfoEntry.DirectoryStructure.CreateDirectoryStructure) {
                    createDirectoryStructureToolStripMenuItem1.Checked = true;
                }
                else if (CreateDirectoryStructure == InfoEntry.DirectoryStructure.NoDirectoryStructure) {
                    dontCreateDirectoryStructureToolStripMenuItem.Checked = true;
                }
                if (Case == InfoEntry.Case.UpperFirst) {
                    largeToolStripMenuItem.Checked = true;
                }
                else if (Case == InfoEntry.Case.small) {
                    smallToolStripMenuItem.Checked = true;
                }
                else if (Case == InfoEntry.Case.Ignore) {
                    igNorEToolStripMenuItem.Checked = true;
                }
                else if (Case == InfoEntry.Case.CAPSLOCK) {
                    cAPSLOCKToolStripMenuItem.Checked = true;
                }
                if (Umlaute == InfoEntry.UmlautAction.Use) {
                    useUmlautsToolStripMenuItem.Checked = true;
                }
                else if (Umlaute == InfoEntry.UmlautAction.Dont_Use) {
                    dontUseUmlautsToolStripMenuItem.Checked = true;
                }
                else if (Umlaute == InfoEntry.UmlautAction.Ignore) {
                    useProvidedNamesToolStripMenuItem.Checked = true;
                }
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    if (ie.Filename != "")
                        OldFilename = true;
                    if (ie.FilePath.Path != "")
                        OldPath = true;
                    if (ie.Name != "")
                        Name = true;
                    if (ie.Destination != "")
                        Destination = true;
                    if (ie.NewFilename != "")
                        NewFilename = true;
                }
                originalNameToolStripMenuItem.Visible = OldFilename;
                pathOrigNameToolStripMenuItem.Visible = OldPath && OldFilename;
                titleToolStripMenuItem.Visible = Name;
                newFileNameToolStripMenuItem.Visible = NewFilename;
                destinationNewFileNameToolStripMenuItem.Visible = Destination && NewFilename;
            }
            else {
                copyToolStripMenuItem.Visible = false;
            }
        }

        //Select all list items
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e) {
            lstEntries.SelectedIndices.Clear();
            for (int i = 0; i < lstEntries.Items.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[i];
                lstEntries.SelectedIndices.Add(lvi.Index);
            }
        }

        //Invert file list selection
        private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.Items.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[i];
                if (lstEntries.SelectedIndices.Contains(lvi.Index))
                {
                    lstEntries.SelectedIndices.Remove(lvi.Index);
                }
                else {
                    lstEntries.SelectedIndices.Add(lvi.Index);
                }
            }
        }

        //Check all list boxes
        private void checkAllToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (InfoEntry ie in InfoEntryManager.Instance) {
                ie.ProcessingRequested = true;
            }
            lstEntries.Refresh();
        }

        //Uncheck all list boxes
        private void uncheckAllToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (InfoEntry ie in InfoEntryManager.Instance) {
                ie.ProcessingRequested = false;
            }
            lstEntries.Refresh();
        }

        //Invert check status of Selected list boxes
        private void invertCheckToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (InfoEntry ie in InfoEntryManager.Instance) {
                ie.ProcessingRequested = !ie.ProcessingRequested;
            }
            lstEntries.Refresh();
        }

        //Filter function to select files by keyword
        private void selectByKeywordToolStripMenuItem_Click(object sender, EventArgs e) {
            Filter f = new Filter("");
            if (f.ShowDialog() == DialogResult.OK) {
                lstEntries.SelectedIndices.Clear();
                for (int i = 0; i < lstEntries.Items.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[i];
                    if (lvi.Text.ToLower().Contains(f.result.ToLower())) {
                        lstEntries.SelectedIndices.Add(lvi.Index);
                    }
                }
            }

        }

        //Set season property for selected items
        private void setSeasonToolStripMenuItem_Click(object sender, EventArgs e) {
            //yes we are smart and guess the season from existing ones
            int sum = 0;
            int count = 0;
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                if (ie.Season != -1) {
                    sum += ie.Season;
                    count++;
                }
            }
            int EstimatedSeason =1;
            if (count > 0)
            {
                EstimatedSeason = (int)Math.Round(((float)sum / (float)count));
            }
            EnterSeason es = new EnterSeason(EstimatedSeason);
            if (es.ShowDialog() == DialogResult.OK) {
                string basepath = Helper.ReadProperty(Config.LastDirectory);
                bool createdirectorystructure = (Helper.ReadInt(Config.CreateDirectoryStructure) > 0);
                bool UseSeasonDir = (Helper.ReadInt(Config.UseSeasonSubDir) > 0);
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    ie.Season = es.season;                    
                    if (ie.Destination != "") {
                        ie.ProcessingRequested = true;
                    }
                    lstEntries.RefreshItem(lvi);
                }
            }

        }

        //Set episodes for selected items to a range
        private void setEpisodesFromtoToolStripMenuItem_Click(object sender, EventArgs e) {
            SetEpisodes se = new SetEpisodes(lstEntries.SelectedIndices.Count);
            if (se.ShowDialog() == DialogResult.OK) {
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    ie.Episode = (i + se.From);
                    lstEntries.RefreshItem(lvi);
                }
            }



        }

        //Refresh file list
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e) {
            UpdateList(true);
        }

        //Delete file
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Delete selected files?", "Delete selected files?", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            List<InfoEntry> lie = new List<InfoEntry>();
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                lie.Add(ie);
            }
            foreach (InfoEntry ie in lie) {
                try {
                    File.Delete(ie.FilePath.Path + Path.DirectorySeparatorChar + ie.Filename);
                    InfoEntryManager.Instance.Remove(ie);
                    lstEntries.RemoveObject(ie);
                }
                catch (Exception ex) {
                    Logger.Instance.LogMessage("Error deleting file: " + ex.Message, LogLevel.ERROR);
                }
            }
            lstEntries.Refresh();
        }

        //Open file
        private void viewToolStripMenuItem_Click(object sender, EventArgs e) {
            InfoEntry ie = InfoEntryManager.Instance.GetVideo((InfoEntry)((OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[0]]).RowObject);
            string VideoPath = ie.FilePath.Path + Path.DirectorySeparatorChar + ie.Filename;
            try {
                Process myProc = Process.Start(VideoPath);
            }
            catch (Exception ex) {
                Logger.Instance.LogMessage("Couldn't open " + VideoPath + ":" + ex.Message, LogLevel.ERROR);
            }
        }

        //Edit subtitle
        private void editSubtitleToolStripMenuItem_Click(object sender, EventArgs e) {
            InfoEntry sub = InfoEntryManager.Instance.GetSubtitle((InfoEntry)((OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[0]]).RowObject);
            InfoEntry video = InfoEntryManager.Instance.GetVideo((InfoEntry)((OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[0]]).RowObject);
            if (sub != null) {
                string path = sub.FilePath.Path + Path.DirectorySeparatorChar + sub.Filename;
                string videopath = "";
                if (video != null) {
                    videopath = video.FilePath.Path + Path.DirectorySeparatorChar + video.Filename;
                }
                EditSubtitles es = new EditSubtitles(path, videopath);
                es.ShowDialog();
            }
        }

        //Set Destination
        private void setDestinationToolStripMenuItem_Click(object sender, EventArgs e) {
            InputBox ib = new InputBox("Set Destination", "Set Destination directory for selected files", Helper.ReadProperty(Config.LastDirectory), InputBox.BrowseType.Folder, true);
            if (ib.ShowDialog(this) == DialogResult.OK) {
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    string destination = ib.input;
                    ie.Destination = destination;
                    lstEntries.RefreshItem(lvi);
                }
            }
        }
        #endregion
        #region misc
        
        /// <summary>
        /// Fills list view control with info data
        /// </summary>
        private void FillListView() {
            // TODO: show at least a progressbar while adding items, user can't see anything but processor utilization will be very high
            lstEntries.Items.Clear();
            lstEntries.VirtualListSize = InfoEntryManager.Instance.Count;
            lstEntries.SetObjects(InfoEntryManager.Instance);
            lstEntries.Sort();
            lstEntries.Refresh();
        }





        private void InitListView()
        { 
            lstEntries.RowFormatter =  delegate(OLVListItem olvi)
            {
                //reset colors to make sure they are set properly
                olvi.BackColor = Color.White;
                olvi.ForeColor = Color.Black;
                InfoEntry ie = (InfoEntry)olvi.RowObject;
                if ((ie.NewFilename == "" && (ie.Destination == "" || ie.Destination == ie.FilePath.Path)) || !ie.ProcessingRequested)
                {
                    olvi.ForeColor = Color.Gray;
                }
                if (!ie.MarkedForDeletion)
                {
                    foreach(InfoEntry ie2 in InfoEntryManager.Instance)
                    {
                        if (ie != ie2)
                        {
                            if (InfoEntryManager.Instance.IsSameTarget(ie, ie2))
                            {
                                olvi.BackColor = Color.IndianRed;
                                break;
                            }
                            else if (olvi.BackColor != Color.Yellow)
                            {
                                olvi.BackColor = Color.White;
                            }
                        }
                    }
                }
            };
            //Processing
            this.lstEntries.BooleanCheckStateGetter = delegate(object x)
            {
                return ((InfoEntry)x).ProcessingRequested;
            };
            this.lstEntries.BooleanCheckStatePutter = delegate(object x, bool newValue)
            {
                ((InfoEntry)x).ProcessingRequested = newValue;
                bool shouldbeenabled = false;
                foreach (InfoEntry ie in InfoEntryManager.Instance)
                {
                    if (ie.ProcessingRequested)
                    {
                        shouldbeenabled = true;
                        break;
                    }
                }
                btnTitles.Enabled = shouldbeenabled;
                btnSubs.Enabled = shouldbeenabled;
                lstEntries.Refresh();
                return newValue;
            };

            //source filename
            this.ColumnSource.AspectGetter = delegate(object x) {
                return ((InfoEntry)x).Filename;
            };

            //Source path
            this.ColumnFilepath.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).FilePath.Path;
            };

            //Showname
            this.ColumnShowname.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).Showname;
            };
            this.ColumnShowname.AspectPutter = delegate(object x, object newValue) {
                ((InfoEntry)x).Showname = (string)newValue;
                lstEntries.Refresh();
            };

            //Season
            this.ColumnSeason.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).Season;
            };
            this.ColumnSeason.AspectPutter = delegate(object x, object newValue) {
                ((InfoEntry)x).Season = (int)newValue;
                lstEntries.Refresh();            
            };

            //Episode
            this.ColumnEpisode.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).Episode;
            };
            this.ColumnEpisode.AspectPutter = delegate(object x, object newValue) {
                ((InfoEntry)x).Episode = (int)newValue;
                lstEntries.Refresh();            
            };

            //Episode Name
            this.ColumnEpisodeName.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).Name;
            };
            this.ColumnEpisodeName.AspectPutter = delegate(object x, object newValue) { 
                ((InfoEntry)x).Name=(string)newValue; 
                //backtrack to see if entered text matches a season/episode
                RelationCollection rc = RelationManager.Instance.GetRelationCollection(((InfoEntry)x).Showname);
                    if (rc != null) {
                        foreach (Relation rel in rc) {
                            //if found, set season and episode in gui and sync back to data
                            if ((string)newValue == rel.Name) {
                                ((InfoEntry)x).Season=rel.Season;
                                ((InfoEntry)x).Episode=rel.Episode;
                                break;
                            }
                        }
                    }
            };

            //Destination
            this.ColumnDestination.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).Destination;
            };
            this.ColumnDestination.AspectPutter = delegate(object x, object newValue) { 
                ((InfoEntry)x).Destination=(string)newValue;
                lstEntries.Refresh();
            };

            //Filename
            this.ColumnNewFilename.AspectGetter = delegate(object x)
            {
                return ((InfoEntry)x).NewFilename;
            };
            this.ColumnNewFilename.AspectPutter = delegate(object x, object newValue) {
                ((InfoEntry)x).NewFilename=(string)newValue;
                lstEntries.Refresh();
            };
        }

        
        /// <summary>
        /// Gets focussed control
        /// </summary>
        /// <param name="controls">Array of controls in which to search for</param>
        /// <returns>focussed control or null</returns>
        private Control getFocused(Control.ControlCollection controls) {
            foreach (Control c in controls) {
                if (c.Focused) {
                    // Return the focused control
                    return c;
                }
                else if (c.ContainsFocus) {
                    // If the focus is contained inside a control's children
                    // return the child
                    return getFocused(c.Controls);
                }
            }
            // No control on the form has focus
            return null;
        }
        #endregion


        /*bool UsedDropDown = false;
        private void cbTitle_DropDownClosed(object sender, EventArgs e)
        {
            UsedDropDown = true;
        }

        private void cbTitle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UsedDropDown)
            {
                UsedDropDown = !UsedDropDown;
                SetNewTitle(cbTitle.Text);
            }
        }*/

        private void originalNameToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                clipboard += ie.Filename + Environment.NewLine;
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            Clipboard.SetText(clipboard);
        }

        private void pathOrigNameToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                clipboard += ie.FilePath.Path + Path.DirectorySeparatorChar + ie.Filename + Environment.NewLine;
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            Clipboard.SetText(clipboard);
        }

        private void titleToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                clipboard += ie.Name + Environment.NewLine;
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            Clipboard.SetText(clipboard);
        }

        private void newFileNameToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                clipboard += ie.NewFilename + Environment.NewLine;
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            Clipboard.SetText(clipboard);
        }

        private void destinationNewFileNameToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
           for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                if(ie.Destination != "" && ie.NewFilename != "") {
                    clipboard += ie.Destination + Path.DirectorySeparatorChar + ie.NewFilename + Environment.NewLine;
                }
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            Clipboard.SetText(clipboard);
        }

        private void operationToolStripMenuItem_Click(object sender, EventArgs e) {
            string clipboard = "";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                bool DestinationDifferent = ie.Destination != "" && ie.Destination != ie.FilePath.Path;
                bool FilenameDifferent = ie.NewFilename != "" && ie.NewFilename != ie.Filename;
                if (DestinationDifferent&&FilenameDifferent) {
                    clipboard += ie.Filename + " --> " + ie.Destination + Path.DirectorySeparatorChar + ie.NewFilename + Environment.NewLine;
                }
                else if (DestinationDifferent)
                {
                    clipboard += ie.Filename + " --> " + ie.Destination + Environment.NewLine;
                }
                else if (FilenameDifferent)
                {
                    clipboard += ie.Filename + " --> " + ie.NewFilename + Environment.NewLine;
                }
            }
            clipboard = clipboard.Substring(0, Math.Max(clipboard.Length - Environment.NewLine.Length, 0));
            clipboard = clipboard.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            if (!string.IsNullOrEmpty(clipboard))
            {
                Clipboard.SetText(clipboard);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e) {
            if (Directory.Exists(txtPath.Text)) {
                Process myProc = Process.Start(txtPath.Text);
            }
        }

        private void replaceInPathToolStripMenuItem_Click(object sender, EventArgs e) {
            ReplaceWindow rw = new ReplaceWindow(this);
            rw.Show();
            rw.TopMost = true;
        }

        /// <summary>
        /// Replaces strings from various fields in selected files
        /// </summary>
        /// <param name="SearchString">String to look for, may contain parameters</param>
        /// <param name="ReplaceString">Replace string, may contain parameters</param>
        /// <param name="Source">Field from which the source string is taken</param>
        public void Replace(string SearchString, string ReplaceString, string Source) {
            int count = 0;
            
            string destination = "Filename";
            if (Source.Contains("Path"))
                destination = "Path";
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                string title=ie.Showname;
                string source = "";

                string LocalSearchString = SearchString;
                string LocalReplaceString = ReplaceString;
                //aquire source string
                switch (Source) {
                    case "Original Filename":
                        source = ie.Filename;
                        break;
                    case "Original Path":
                        source = ie.FilePath.Path;
                        break;
                    case "Destination Filename":
                        source = ie.NewFilename;
                        break;
                    case "Destination Path":
                        source = ie.Destination;
                        break;
                }
                //Insert parameter values
                LocalSearchString = LocalSearchString.Replace("%OF", ie.Filename);
                LocalSearchString = LocalSearchString.Replace("%DF", ie.NewFilename);
                LocalSearchString = LocalSearchString.Replace("%OP", ie.FilePath.Path);
                LocalSearchString = LocalSearchString.Replace("%DP", ie.Destination);
                LocalSearchString = LocalSearchString.Replace("%T", title);
                LocalSearchString = LocalSearchString.Replace("%N", ie.Name);
                LocalSearchString = LocalSearchString.Replace("%E", ie.Episode.ToString());
                LocalSearchString = LocalSearchString.Replace("%s", ie.Season.ToString());
                LocalSearchString = LocalSearchString.Replace("%S", ie.Season.ToString("00"));
                LocalReplaceString = LocalReplaceString.Replace("%OF", ie.Filename);
                LocalReplaceString = LocalReplaceString.Replace("%DF", ie.NewFilename);
                LocalReplaceString = LocalReplaceString.Replace("%OP", ie.FilePath.Path);
                LocalReplaceString = LocalReplaceString.Replace("%DP", ie.Destination);
                LocalReplaceString = LocalReplaceString.Replace("%T", title);
                LocalReplaceString = LocalReplaceString.Replace("%N", ie.Name);
                LocalReplaceString = LocalReplaceString.Replace("%E", ie.Episode.ToString());
                LocalReplaceString = LocalReplaceString.Replace("%s", ie.Season.ToString());
                LocalReplaceString = LocalReplaceString.Replace("%S", ie.Season.ToString("00"));

                //see if replace will be done for count var
                if (source.Contains(SearchString))
                    count++;
                //do the replace
                source = source.Replace(LocalSearchString, LocalReplaceString);
                if (destination == "Filename") {
                    ie.NewFilename = source;
                }
                else if (destination == "Path") {
                    ie.Destination = source;
                }

                //mark files for processing
                ie.ProcessingRequested = true;
                lstEntries.Refresh();
            }
            if (count > 0) {
                Logger.Instance.LogMessage(SearchString + " was replaced with " + ReplaceString + " in " + count + " fields.", LogLevel.INFO);
            }
            else {
                Logger.Instance.LogMessage(SearchString + " was not found in any of the selected files.", LogLevel.INFO);
            }
        }

        private void byNameToolStripMenuItem_Click(object sender, EventArgs e) {
            List<string> names = new List<string>();
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                string Showname = ie.Showname;
                if (!names.Contains(Showname)) {
                    names.Add(Showname);
                }
            }
            List<InfoEntry> similar = new List<InfoEntry>();
            foreach (string str in names) {
                similar.AddRange(InfoEntryManager.Instance.FindSimilarByName(str));
            }
            for (int i = 0; i < lstEntries.Items.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[i];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                if (similar.Contains(ie)) {
                    lvi.Selected = true;
                }
                else {
                    lvi.Selected = false;
                }
            }
            lstEntries.Refresh();
        }

        private void byPathToolStripMenuItem_Click(object sender, EventArgs e) {
            List<string> paths = new List<string>();
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                string path = ie.FilePath.Path;
                if (!paths.Contains(path)) {
                    paths.Add(path);
                }
            }
            List<InfoEntry> similar = new List<InfoEntry>();
            foreach (string str in paths) {
                similar.AddRange(InfoEntryManager.Instance.FindSimilarByName(str));
            }
            for (int i = 0; i < lstEntries.Items.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[i];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                if (similar.Contains(ie)) {
                    lvi.Selected = true;
                }
                else {
                    lvi.Selected = false;
                }
            }
            lstEntries.Refresh();
        }

        private void createDirectoryStructureToolStripMenuItem1_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.CreateDirectoryStructure = InfoEntry.DirectoryStructure.CreateDirectoryStructure;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            dontCreateDirectoryStructureToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void dontCreateDirectoryStructureToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.CreateDirectoryStructure = InfoEntry.DirectoryStructure.NoDirectoryStructure;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            createDirectoryStructureToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void useUmlautsToolStripMenuItem1_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.UmlautUsage = InfoEntry.UmlautAction.Use;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            dontUseUmlautsToolStripMenuItem.Checked = false;
            useProvidedNamesToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void dontUseUmlautsToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.UmlautUsage = InfoEntry.UmlautAction.Dont_Use;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            useUmlautsToolStripMenuItem.Checked = false;
            useProvidedNamesToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void useProvidedNamesToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.UmlautUsage = InfoEntry.UmlautAction.Ignore;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            useUmlautsToolStripMenuItem.Checked = false;
            dontUseUmlautsToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void largeToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.Casing = InfoEntry.Case.UpperFirst;            
            }
            ((ToolStripMenuItem)sender).Checked = true;
            smallToolStripMenuItem.Checked = false;
            igNorEToolStripMenuItem.Checked = false;
            cAPSLOCKToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void smallToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.Casing = InfoEntry.Case.small;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            largeToolStripMenuItem.Checked = false;
            igNorEToolStripMenuItem.Checked = false;
            cAPSLOCKToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void igNorEToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.Casing = InfoEntry.Case.Ignore;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            smallToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = false;
            cAPSLOCKToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void cAPSLOCKToolStripMenuItem_Click(object sender, EventArgs e) {
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                ie.Casing = InfoEntry.Case.CAPSLOCK;
            }
            ((ToolStripMenuItem)sender).Checked = true;
            smallToolStripMenuItem.Checked = false;
            igNorEToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = false;
            lstEntries.Refresh();
        }

        private void setShownameToolStripMenuItem_Click(object sender, EventArgs e) {
            Dictionary<string, int> ht = new Dictionary<string, int>();
            for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
            {
                OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                InfoEntry ie = (InfoEntry)lvi.RowObject;
                if (!ht.ContainsKey(ie.Showname)) {
                    ht.Add(ie.Showname, 1);
                }
                else {
                    ht[ie.Showname] += 1;
                }
            }
            int max = 0;
            string Showname = "";
            foreach (KeyValuePair<string, int> pair in ht) {
                if (pair.Value > max) {
                    Showname = pair.Key;
                }
            }
            EnterShowname es = new EnterShowname(Showname);
            if (es.ShowDialog() == DialogResult.OK) {
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    ((InfoEntry)lvi.RowObject).Showname = es.SelectedName;
                    lstEntries.RefreshItem(lvi);
                }
            }
        }

        private void regexTesterToolStripMenuItem_Click(object sender, EventArgs e) {
            RegexTester rt = new RegexTester();
            rt.Show();
        }



        #region Functions remaining in Form1
        bool working = false;
        private void UpdateList(bool clear)
        {
            lstEntries.ClearObjects();
            progressBar1.Visible = false;
            lblFileListingProgress.Visible = true;
            txtPath.Visible = false;
            btnCancel.Visible = true;
            btnCancel.Enabled = true;
            btnPath.Enabled = false;
            btnOpen.Enabled = false;
            btnPath.Visible = false;
            btnOpen.Visible = false;
            btnConfig.Enabled = false;
            btnRename.Enabled = false;
            txtTarget.Enabled = false;
            backgroundWorker1.RunWorkerAsync(clear);
            //DataGenerator.UpdateList(clear);

            
        }
        #endregion        

        private void lstEntries_CellEditStarting(object sender, BrightIdeasSoftware.CellEditEventArgs e)
        {
            InfoEntry ie = (InfoEntry)e.RowObject;
            if(e.Column==ColumnEpisode){
                ((NumericUpDown)e.Control).Minimum = 1;
                RelationCollection rc = RelationManager.Instance.GetRelationCollection(ie.Showname);
                if (rc != null)
                {
                    ((NumericUpDown)e.Control).Maximum = rc.FindMaxEpisode(ie.Season);    
                }
            }
            else if (e.Column == ColumnSeason)
            {
                ((NumericUpDown)e.Control).Minimum = 1;
                RelationCollection rc = RelationManager.Instance.GetRelationCollection(ie.Showname);
                if (rc != null)
                {
                    ((NumericUpDown)e.Control).Maximum = rc.FindMaxSeason();
                }
            }
            else if (e.Column == ColumnEpisodeName)
            {
                RelationCollection rc = RelationManager.Instance.GetRelationCollection(ie.Showname);
                if (rc != null)
                {
                    ComboBox cb = new ComboBox();
                    cb.Bounds = e.CellBounds;
                    cb.Font = ((FastObjectListView)sender).Font;
                    cb.DropDownStyle = ComboBoxStyle.DropDown;
                    foreach (Relation r in rc)
                    {
                        if (r.Season == ie.Season)
                        {
                            cb.Items.Add(r.Name);
                        }
                        if (ie.Name == r.Name)
                        {
                            cb.SelectedItem = r.Name;
                        }
                    }
                    if (cb.SelectedIndex < 0 && cb.Items.Count>0)
                    {
                        cb.SelectedIndex = 0;
                    }
                    cb.SelectedIndexChanged += new EventHandler(cb_SelectedIndexChanged);
                    cb.Tag = e.RowObject; // remember which person we are editing
                    e.Control = cb;
                }
            }
        }
        private void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)sender).Tag.GetType() == typeof(InfoEntry))
            {
                InfoEntry ie = (InfoEntry)((ComboBox)sender).Tag;
                ie.Name = ((ComboBox)sender).Text;
            }
        }

        private void lstEntries_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            InfoEntry ie=(InfoEntry)e.RowObject;
            if (e.Control.GetType() == typeof(ComboBox))
            {
                ComboBox cb = (ComboBox)e.Control;
            }else if(e.Column==ColumnSeason){
                int newValue=(int)((NumericUpDown)e.Control).Value;
                RelationCollection rc= RelationManager.Instance.GetRelationCollection(((InfoEntry)e.RowObject).Showname);
                if (rc != null)
                {
                    if(ie.Episode>rc.FindMaxEpisode(newValue)){
                        ie.Episode=rc.FindMaxEpisode(newValue);
                    }
                }
                
            }
            lstEntries.RefreshItem(e.ListViewItem);
        }

        private void lstEntries_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    if (ie.IsSubtitle) ie = InfoEntryManager.Instance.GetVideo(ie);
                    Process myProc = Process.Start(ie.FilePath.Path + Path.DirectorySeparatorChar + ie.Filename);
                }
            }
            else if (e.KeyCode == Keys.Space)
            {
                for (int i = 0; i < lstEntries.SelectedIndices.Count; i++)
                {
                    OLVListItem lvi = (OLVListItem)lstEntries.Items[lstEntries.SelectedIndices[i]];
                    InfoEntry ie = (InfoEntry)lvi.RowObject;
                    ie.ProcessingRequested = !ie.ProcessingRequested;
                    lstEntries.RefreshObject(ie);
                }
            }
        }
        private void lstEntries_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (s.Length == 1 && Directory.Exists(s[0]))
            {
                InfoEntryManager.Instance.SetPath(s[0]);
            }
        }

        private void lstEntries_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (s.Length == 1 && Directory.Exists(s[0]))
                {
                    e.Effect = DragDropEffects.Move;
                }
            }
        }

        private void markAsMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {             
            MarkAsMovie();
        }

        private void markAsTVSeriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MarkAsTVShow();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DataGenerator.UpdateList((bool)e.Argument, worker,e);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = this.progressBar1.Maximum * e.ProgressPercentage/100;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Visible = false;
            btnCancel.Visible = false;
            btnCancel.Enabled = false;
            btnPath.Enabled = true;
            btnOpen.Enabled = true;
            btnPath.Visible = true;
            btnOpen.Visible = true;
            lblFileListingProgress.Visible = false;
            txtPath.Visible = true;
            btnConfig.Enabled = true;
            btnRename.Enabled = true;
            txtTarget.Enabled = true;
            if (!e.Cancelled)
            {
                FillListView();
            }
            else
            {
                InfoEntryManager.Instance.Clear();
            }
            //also update some gui elements for the sake of it
            txtTarget.Text = Helper.ReadProperty(Config.TargetPattern);
            txtPath.Text = Helper.ReadProperty(Config.LastDirectory);
            string LastSubProvider = Helper.ReadProperty(Config.LastSubProvider);
            if (LastSubProvider == null)
                LastSubProvider = "";
            cbSubs.SelectedIndex = Math.Max(0, cbSubs.Items.IndexOf(LastSubProvider));
            btnTitles.Enabled = InfoEntryManager.Instance.Count > 0;
            btnRename.Enabled = InfoEntryManager.Instance.Count > 0;
            //make sure get titles and rename button are only enabled if there are recognized shownames of series
            if (btnTitles.Enabled)
            {
                btnTitles.Enabled = false;
                btnRename.Enabled = false;
                foreach (InfoEntry ie in InfoEntryManager.Instance)
                {
                    if (!string.IsNullOrEmpty(ie.Showname))
                    {

                        btnRename.Enabled = true;
                    }
                    if (!ie.Movie && !string.IsNullOrEmpty(ie.Showname))
                    {
                        btnTitles.Enabled = true;
                        break;
                    }
                }
            }
            btnSubs.Enabled = InfoEntryManager.Instance.Count > 0;
            //make sure subtitle button is only enabled if there are video files to get subtitles for
            if (btnSubs.Enabled)
            {
                btnSubs.Enabled = false;
                foreach (InfoEntry ie in InfoEntryManager.Instance)
                {
                    if (ie.IsVideofile && !string.IsNullOrEmpty(ie.Showname))
                    {
                        btnSubs.Enabled = true;
                        break;
                    }
                }
            }
            lstEntries.Refresh();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

    }
}

