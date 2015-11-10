using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMM_trigram
{
    class Program
    {
        static void Main(string[] args)
        {
            string trainingPath = @"C:\compling570\hw5_HMM\examples\wsj_sec0.word_pos";

            string outputPath = args[0];
            int l1 = Convert.ToInt32(args[1]);
            int l2 = Convert.ToInt32(args[2]);
            int l3 = Convert.ToInt32(args[3]);
            String UnkProbFile = args[4];
            string line;
            Dictionary<String, Dictionary<String, double>> Emission = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionBigram = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionTrigram = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, bool> symbolList = new Dictionary<string, bool>();
            Dictionary<String, int> TagCount = new Dictionary<string, int>();
            Dictionary<String,double> UnkTagProb = new Dictionary<string,double>();
            int TotalEmissionArc = 0;
            int TotalTransmissionArc = 0;

            //read UnkProb into dictionary
            ReadUnkProb(UnkTagProb, UnkProbFile);

            using (StreamReader SR = new StreamReader(trainingPath))
            {
                while ((line = SR.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    //ProcessLineTrigram(Emission, Transition, TagCount, line, symbolList);
                }

            }
        }
        public static void ReadUnkProb(Dictionary<String,double> UnkTagProb,String UnkProbPath)
        {
            string line;
            using (StreamReader Sr = new StreamReader(UnkProbPath))
            {
                while ((line = Sr.ReadLine())!=null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] wordset = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (!UnkTagProb.ContainsKey(wordset[0]))
                        UnkTagProb.Add(wordset[0], Convert.ToDouble(wordset[1]));

                }
            }
        }
        public static void ConvertTrigramToCounts(Dictionary<String, Dictionary<String, double>> Emission,
        Dictionary<String, Dictionary<String, double>> TransitionBigram,
        Dictionary<String, Dictionary<String, double>> TransitionTrigram,
        Dictionary<String, int> TagCount,
        Dictionary<String, bool> symbolList,
            string line)

        {
            line = "<s>/BOS <s>/BOS " + line + " <BSs>/EOS";
            string tag1;
            string tag2;
            string tag3;
            string observation;
            string[] wordList = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            // add BOS once for each sentence 
            if (TagCount.ContainsKey("BOS"))
                TagCount["BOS"]++;
            else
                TagCount.Add("BOS", 1);

            //add BOS to transitionBigram dictionary
            if (TransitionBigram.ContainsKey("BOS") && (TransitionBigram["BOS"]).ContainsKey("BOS"))
                TransitionBigram["BOS"]["BOS"]++;
            else if (TransitionBigram.ContainsKey("BOS"))
                TransitionBigram["BOS"].Add("BOS", 1);

            //starting with i=2 because we dont want to count 
            for (int i = 2; i < wordList.Length; i++)
            {
                int lastSepIndex = wordList[i].LastIndexOf("/");
                //get 3 tags and observation for the current word/tag
                tag3 = wordList[i].Substring(lastSepIndex + 1);
                observation = wordList[i].Substring(0,lastSepIndex);
                lastSepIndex = wordList[i-1].LastIndexOf("/");
                tag2 = wordList[i-1].Substring(lastSepIndex + 1);
                lastSepIndex = wordList[i - 2].LastIndexOf("/");
                tag1 = wordList[i - 2].Substring(lastSepIndex + 1);

                //for unigram update the count
                if (TagCount.ContainsKey(tag3))
                    TagCount[tag3]++;
                else
                    TagCount.Add(tag3, 1);
                
                //for bigram transition update the count
                string fromState = tag2;
                if (TransitionBigram.ContainsKey(fromState) && TransitionBigram[fromState].ContainsKey(tag3))
                    (TransitionBigram[fromState])[tag3]++;
                else
                {
                    if (TransitionBigram.ContainsKey(fromState))
                        TransitionBigram[fromState].Add(tag3, 1);
                    else
                        TransitionBigram.Add(fromState, new Dictionary<string, double> { { tag3, 1 } });
                }

                ////for trigram transition update the count
                fromState = tag1+"_"+tag2;
                if (TransitionTrigram.ContainsKey(fromState) && TransitionTrigram[fromState].ContainsKey(tag3))
                    (TransitionTrigram[fromState])[tag3]++;
                else
                {
                    if (TransitionTrigram.ContainsKey(fromState))
                        TransitionTrigram[fromState].Add(tag3, 1);
                    else
                        TransitionTrigram.Add(fromState, new Dictionary<string, double> { { tag3, 1 } });
                }
                //Coming to Emission Prob table
                if (Emission.ContainsKey(tag3) && (Emission[tag3]).ContainsKey(observation))
                    (Emission[tag3])[observation]++;
                else
                {
                    if (Emission.ContainsKey(tag3))
                        Emission[tag3].Add(observation, 1);
                    else
                        Emission.Add(tag3, new Dictionary<string, double> { { observation, 1 } });
                }


                if (!symbolList.ContainsKey(observation))
                    symbolList.Add(observation, true);
            }
        }
    }
}
;