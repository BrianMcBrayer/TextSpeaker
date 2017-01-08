using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace TextSpeaker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dlgSave.ShowDialog() == DialogResult.OK)
            {
                var synth = new SpeechSynthesizer();

                synth.SetOutputToWaveFile(dlgSave.FileName);

                var verses = BibleUtils.GetVersesFromReference(txtText.Text).ToList();

                var partLengths = new[]{ 6, 12 };

                foreach (var partLength in partLengths)
                {
                    var speakableParts = BibleUtils.SplitPassageIntoSpeakableParts(verses, partLength);

                    foreach (var speakablePart in speakableParts)
                    {
                        synth.Volume = 100;
                        synth.Speak(speakablePart);
                        synth.Volume = 0;
                        synth.Speak(speakablePart);
                    }

                    var pausePrompt = new PromptBuilder();
                    pausePrompt.AppendBreak(TimeSpan.FromSeconds(4));
                    synth.Speak(pausePrompt);
                }

                synth.Speak(BibleUtils.BuildPassageFromVerses(verses));

                synth.SetOutputToNull();

                MessageBox.Show("Completed!");
            }
        }
    }

    public class BibleUtils
    {
        private const string Url = @"http://labs.bible.org/api/?passage={0}&type=json";

        public static IEnumerable<VerseInformation> GetVersesFromReference(string reference) // TODO Change reference from a string to a reference object
        {
            var urlRaw = string.Format(Url, reference);
            var uri = new Uri(urlRaw);

            var http = WebRequest.Create(uri);
            using (var response = http.GetResponse())
            {
                var responseStream = response.GetResponseStream();

                if (null == responseStream)
                {
                    throw new Exception();
                }

                using (var responseStreamReader = new StreamReader(response.GetResponseStream()))
                {


                    return JsonConvert.DeserializeObject<VerseInformation[]>(responseStreamReader.ReadToEnd());
                }
            }
        }

        public static string BuildPassageFromVerses(IEnumerable<VerseInformation> verses)
        {
            return string.Join(" ", verses.Select(v => v.Text));
        }

        public static IEnumerable<string> SplitPassageIntoSpeakableParts(IEnumerable<VerseInformation> verses, int partLength)
        {
            // For now, just split it into 10-word sections. In the future, do some grammar stuff and split it into phrases less 10-words or less
            var allVerseText = BuildPassageFromVerses(verses);

            var wordMatcher = new Regex(@"([^\s+]+)");
            var wordMatches = wordMatcher.Matches(allVerseText);

            return wordMatches.Cast<Match>()
                .Select((match, matchIndex) => new { match, matchIndex })
                .GroupBy(m => Math.Floor((double)m.matchIndex / partLength), m => m.match, (d, matches) => string.Join(" ", matches));
        }
    }

    public class VerseInformation
    {
        public string Bookname { get; set; }
        public string Chapter { get; set; }
        public string Verse { get; set; }
        public string Text { get; set; }
    }
}
