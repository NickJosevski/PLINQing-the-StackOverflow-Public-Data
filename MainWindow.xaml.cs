using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Diagnostics;

namespace PlinqOnStackOverflow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region RoutedCommands
        public static readonly RoutedCommand CommandSQLPostsR1 = new RoutedCommand();
        public static readonly RoutedCommand CommandSQLPostsR2 = new RoutedCommand();
        public static readonly RoutedCommand CommandSQLPostsR3 = new RoutedCommand();
        #endregion

        private StackOverflowDataDumpDataContext db;
        private Stopwatch sw;
        private SOXMLDataType selectedRadioButton;

        private double seqMS;
        private double parMS;

        private IEnumerable<Post> wPosts;

        public MainWindow()
        {
            InitializeComponent();
            setup();
        }

        private void setup()
        {
            db = new StackOverflowDataDumpDataContext();
            sw = new Stopwatch();
            selectedRadioButton = SOXMLDataType.SQLPostsR1;
            this.useCores.Maximum = Environment.ProcessorCount;
            this.useCores.Minimum = 1;
            seqMS = 0;
            parMS = 0;
        }

        private void RunDataLoadTest(bool parallel)
        {
            switch (selectedRadioButton)
            {
                case SOXMLDataType.SQLPostsR1:
                case SOXMLDataType.SQLPostsR2:
                case SOXMLDataType.SQLPostsR3:
                    if (wPosts != null)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        if (parallel)
                            SQLPostsParallel();
                        else
                            SQLPostsStandard();
                        Mouse.OverrideCursor = null;
                    }
                    else
                        feedback.Content = "No data loaded.";
                    break;
                default:
                    break;

            }
        }

        #region window events

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            feedback.Content = "Pre loading data..."; //need to move to a loction that will update window before action starts
            Mouse.OverrideCursor = Cursors.Wait;

            if (wPosts != null)
            {
                wPosts = null;
            }

            //todo: replace with a switch, but can't native switch() on an ICommand...
            if (e.Command == CommandSQLPostsR1)
            {
                selectedRadioButton = SOXMLDataType.SQLPostsR1;
                wPosts = GetPosts(0, 30);
            }
            else if (e.Command == CommandSQLPostsR2)
            {
                selectedRadioButton = SOXMLDataType.SQLPostsR2;
                wPosts = GetPosts(0, 120);
            }
            else if (e.Command == CommandSQLPostsR3)
            {
                selectedRadioButton = SOXMLDataType.SQLPostsR3;
                wPosts = GetPosts(0, 250);
            }
            Mouse.OverrideCursor = null;
        }

        private void execLINQ_Click(object sender, RoutedEventArgs e)
        {
            Clear(); //a seq call means start a new reporting round //ie clear the factor calc, reset the vars

            RunDataLoadTest(false);
        }

        private void execPLINQ_Click(object sender, RoutedEventArgs e)
        {
            //No Clear(), run the basic LINQ() call first, that way we have results here for the parallel (factor improvement)

            RunDataLoadTest(true);

            return;
            //extra demo tests under audience questions...
            /*
            var query =
                from p in wPosts
                let LetterCount = CalcLetters(p.Body)
                select new { p.Title, LetterCount };

            int sum = 0;
            foreach (var item in query)
            {
                sum += item.LetterCount;
            }*/
            
        }


        //Get the data out of SQL 
        //[this takes some time, but we're not trying to speed up the ability of SQL Server to fetch data faster]
        private IEnumerable<Post> GetPosts(int score, int viewCount)
        {
            var result = from p in db.Posts
                         where (p.Score ?? 0) > score
                         && p.ViewCount > viewCount
                         select p;

            return result.ToList();
        }

        private void SQLUsersStandard()
        {
            KickOffStopWatch();
            var data = (from u in db.Posts
                        where (u.Score ?? 0) > 80
                        //orderby u.Score
                        select new
                        {
                            Title = u.Title,
                            Score = u.Score
                        }).ToList();

            EndStopWatchAndDisplayToScreen();
        }

        private void SQLUsersParallel()
        {
            KickOffStopWatch();
            var data = (from u in db.Posts.AsParallel()
                        where (u.Score ?? 0) > 80
                        && u.ViewCount > 200
                        && u.AcceptedAnswerId != null
                        //orderby u.Score
                        select new
                        {
                            Title = u.Title,
                            Score = u.Score
                        });

            EndStopWatchAndDisplayToScreen();
        }

        // Post Data [Main Performance Benchmarking]
        private void SQLPostsStandard()
        {
            KickOffStopWatch();

            var qty = wPosts.Where(p => p.IsDescriptive()).Count();

            EndStopWatchAndDisplayToScreen(true, "posts", qty);
        }

        private void SQLPostsParallel()
        {
            KickOffStopWatch();

            var qty = wPosts.AsParallel().Where(p => p.IsDescriptive()).Count();

            //var qty2 = wPosts.AsParallel().Where(p => p.TagCount() > 2 && p.TagQuality()).Count();

            EndStopWatchAndDisplayToScreen(false, "posts", qty);
        }

        #endregion

        #region Stopwatch / Display
        private void KickOffStopWatch()
        {
            sw.Reset();
            sw.Start();
        }
        private void EndStopWatchAndDisplayToScreen()
        {
            sw.Stop();
            this.factorDisplay.Content = String.Format("{0:#,##0}ms", sw.ElapsedMilliseconds);
        }
        private void EndStopWatchAndDisplayToScreen(bool seq, string desc, int qty)
        {
            sw.Stop();
            if (seq)
            {
                seqMS = sw.ElapsedMilliseconds;
                seqTime.Content = String.Format("{0:#,##0}ms", sw.ElapsedMilliseconds);
                desc = "sequentially " + desc;
            }
            else
            {
                parMS = sw.ElapsedMilliseconds;
                parTime.Content = String.Format("{0:#,##0}ms", sw.ElapsedMilliseconds);
                desc = "in parallel " + desc;
            }

            WriteToListBox(desc, qty);

            if (seqMS > 0 && parMS > 0)
            {
                factorDisplay.Content = String.Format("{0}x speedup", Math.Round((seqMS / parMS), 2));
            }
        }

        private void WriteToListBox(String desc, int qty)
        {
            listOutputMain.Items.Add(String.Format("{0} items processed {1} took {2}ms", qty, desc, sw.ElapsedMilliseconds));
        }

        private void Clear()
        {
            seqMS = 0;
            parMS = 0;
            seqTime.Content = " ";
            parTime.Content = " ";
            factorDisplay.Content = " ";
            if (listOutputMain.Items.Count > 0)
                listOutputMain.Items.Add(new String('-', 80));
        }
        #endregion
    }

    public enum SOXMLDataType
    {
        SQLPostsR1,
        SQLPostsR2,
        SQLPostsR3
    }
}
