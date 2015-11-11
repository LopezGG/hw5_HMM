using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HMM
{
    class Program
    {
        static void Main(string[] args)
        {
            string trainingPath = @"E:\CompLing\CompLing570\hw6_dir\examples\wsj_sec0.word_pos";
            string outputPath = @"E:\CompLing\CompLing570\hw6_dir\examples\output_hmm";
            string line;
            Dictionary<String, Dictionary<String, double>> Emission = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> Transition = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, bool> symbolList = new Dictionary<string, bool>();
            Dictionary<String, int> TagCount = new Dictionary<string, int>();
            int TotalEmissionArc = 0;
            int TotalTransmissionArc = 0;
            using (StreamReader SR = new StreamReader (trainingPath))
            {
                while ( (line = SR.ReadLine ()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    ProcessLineBigram(Emission, Transition, TagCount, line, symbolList);
                }

            }

            TotalEmissionArc=ConvertCountToProbBigram(Emission, TagCount);
            TotalTransmissionArc=ConvertCountToProbBigram(Transition, TagCount);
            int stateCount = TagCount.Keys.Distinct().ToList().Count;
            int symbolCount = symbolList.Keys.Distinct().ToList().Count;
            //Write Hmm File
            using(StreamWriter Sw = new StreamWriter (outputPath))
            {
                Sw.WriteLine("state_num=" + stateCount);
                Sw.WriteLine("sym_num=" + symbolCount);
                Sw.WriteLine("init_line_num=1");
                Sw.WriteLine("trans_line_num=" + TotalTransmissionArc);
                Sw.WriteLine("emiss_line_num=" + TotalEmissionArc);
                Sw.WriteLine();
                Sw.WriteLine(@"\init");
                Sw.WriteLine("BOS" + "\t" + "1.0");
                Sw.WriteLine();
                Sw.WriteLine(@"\transition");
                WriteDictionary(Transition, Sw);
                Sw.WriteLine();
                Sw.WriteLine(@"\emission");
                WriteDictionary(Emission, Sw);


            }
            Console.ReadLine();
        }

        public static void WriteDictionary(Dictionary<String, Dictionary<String, double>> Target2DDictionary, StreamWriter Sw)
        {
            foreach (KeyValuePair<String, Dictionary<String, double>> tagset in Target2DDictionary)
            {
                string fromState = tagset.Key;
                string toState;
                double prob;
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    toState = item.Key;
                    prob = item.Value;
                    if (toState == "<BSs>")
                        toState = @"<\s>";
                    Sw.WriteLine(fromState + "\t" + toState + "\t" + prob + "\t" + Math.Log10(prob));
                }
            }
        }
        public static int ConvertCountToProbBigram(Dictionary<String, Dictionary<String, double>> Target2DDictionary, Dictionary<String, int> TagCount)
        {
            int TotalEntryCount = 0;
            Dictionary<String, Dictionary<String, double>> TempDict=new Dictionary<string,Dictionary<string,double>>();
            foreach (var tagset in Target2DDictionary)
            {

                int totalTagCount;
                if (TagCount.ContainsKey(tagset.Key))
                    totalTagCount = TagCount[tagset.Key];
                else
                    throw new Exception("The dictionary was not built correctly");
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    if (TempDict.ContainsKey(tagset.Key))
                        TempDict[tagset.Key].Add(item.Key, (Target2DDictionary[tagset.Key])[item.Key] / totalTagCount);
                    else

                        TempDict.Add(tagset.Key, new Dictionary<string, double> { { item.Key, (Target2DDictionary[tagset.Key])[item.Key] / totalTagCount } });
                    TotalEntryCount++;
                }

            }

            foreach (var tagset in TempDict)
            {
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    (Target2DDictionary[tagset.Key])[item.Key] = (TempDict[tagset.Key])[item.Key];
                }
            } 
            return TotalEntryCount;
        }

        public static void ProcessLineBigram(Dictionary<String, Dictionary<String, double>> Emission, Dictionary<String, Dictionary<String, double>> Transition,
           Dictionary<String, int> TagCount,string line,Dictionary<String, bool> symbolList)
        {
            
            //Todo : Check how EOS is handled
            line = "<s>/BOS " + line + " <BSs>/EOS";
            String fromState = "BOS";
            string tag;
            string observation;
            string[] wordList = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in wordList)
            {
                int lastSepIndex = item.ToString().LastIndexOf("/");
                tag = item.ToString().Substring(lastSepIndex + 1);
                observation = item.ToString().Substring(0,lastSepIndex);
                // if transition has the from State and to State increment the counter else add. Note : this is only count we have not 
                //handled prob as yet
                if(tag == "BOS")
                {
                    if (TagCount.ContainsKey(tag))
                        TagCount[tag]++;
                    else
                        TagCount.Add(tag, 1);
                    continue;
                }
                if (Transition.ContainsKey(fromState) && Transition[fromState].ContainsKey(tag))
                    (Transition[fromState])[tag]++;
                else
                {
                    if (Transition.ContainsKey(fromState))
                        Transition[fromState].Add(tag, 1);
                    else
                        Transition.Add(fromState, new Dictionary<string, double> { { tag, 1 } });
                }


                if (Emission.ContainsKey(tag) && (Emission[tag]).ContainsKey(observation))
                    (Emission[tag])[observation]++;
                else
                {
                    if (Emission.ContainsKey(tag))
                        Emission[tag].Add(observation, 1);
                    else
                        Emission.Add(tag, new Dictionary<string, double> { { observation, 1 } });
                }

                if (TagCount.ContainsKey(tag))
                    TagCount[tag]++;
                else
                    TagCount.Add(tag, 1);
                fromState = tag;
                if (!symbolList.ContainsKey(observation))
                    symbolList.Add(observation, true);
            }
        }
    }
}
