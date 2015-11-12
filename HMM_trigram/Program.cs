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
            string trainingPath = @"E:\CompLing\CompLing570\hw6_dir\examples\wsj_sec0.word_pos";

            string outputPath = args[0];
            double l1 = Convert.ToDouble(args[1]);
            double l2 = Convert.ToDouble(args[2]);
            double l3 = Convert.ToDouble(args[3]);
            String UnkProbFile = args[4];
            string line;
            Dictionary<String, Dictionary<String, double>> Emission = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionBigram = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionTrigram = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, bool> symbolList = new Dictionary<string, bool>();
            symbolList.Add(@"<unk>", true);
            Dictionary<String, int> TagCount = new Dictionary<string, int>();
            Dictionary<String,double> UnkTagProb = new Dictionary<string,double>();
            int TotalEmissionArc = 0;
            int TotalTransmissionArc = 0;
            double POStagsetSize = 0;
            //read UnkProb into dictionary
            ReadUnkProb(UnkTagProb, UnkProbFile);

            //read the training data and populate all the dictionaries
            using (StreamReader SR = new StreamReader(trainingPath))
            {
                while ((line = SR.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    POStagsetSize += ConvertTrigramToCounts(Emission, TransitionBigram, TransitionTrigram, TagCount, symbolList, line);
                }

            }

            TotalTransmissionArc = ConvertCountToProbTrigram(TransitionTrigram, TransitionBigram, TagCount, POStagsetSize, l1, l2, l3);
            Console.ReadLine();
        }
        public static int HandleEmission (Dictionary<String, Dictionary<String, double>> Emission,
            Dictionary<String, Dictionary<String, double>> TransitionTrigram,
            Dictionary<String, int> TagCount, Dictionary<String,double> UnkTagProb)
        {
            int EmissionArc = 0;
            double prob = 0;
            Dictionary<String, Dictionary<String, double>> TempDict = new Dictionary<string, Dictionary<string, double>>();
            foreach (var tagset in TransitionTrigram)
            {

                int totalTagCount;
                

                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    // we are taking t3 tag's count
                    if (TagCount.ContainsKey(item.Key))
                        totalTagCount = TagCount[tagset.Key];
                    else
                        throw new Exception("The dictionary was not built corectly");
                    
                    double tagCount = item.Value;
                    prob = (tagCount / totalTagCount);
                    //recheck this function
                    if (TempDict.ContainsKey(tagset.Key))
                        TempDict[tagset.Key].Add(item.Key, tagCount);
                    else

                        TempDict.Add(tagset.Key, new Dictionary<string, double> { { item.Key, (Emission[tagset.Key])[item.Key] / totalTagCount } });
                    EmissionArc++;
                }

            }

            foreach (var tagset in TempDict)
            {
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    (Emission[tagset.Key])[item.Key] = (TempDict[tagset.Key])[item.Key];
                }
            }

            return EmissionArc;
        }
        public static int ConvertCountToProbTrigram (Dictionary<String, Dictionary<String, double>> TrigramDictionary,
            Dictionary<String, Dictionary<String, double>> BigramDictionary,
            Dictionary<String, int> TagCount,
            double POStagsetSize,
            double l1,
            double l2,
            double l3)
        {
            
            double bigramCount = 0;
            double tagCount = 0;
            int TotalTriGramCount = 0;
            double prob3;
            double prob2;
            double prob1;
            double pIntero = 0;
            Dictionary<String, double> TempDictUni = new Dictionary<string, double>();
            Dictionary<String, Dictionary<String, double>> TempDictBi = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TempDictTri = new Dictionary<string, Dictionary<string, double>>();
            
            //Calculate Unigram prob and store in temp Dictionary because we cannot modify the dictioanry we iterate through
            foreach (var item in TagCount)
            {
                tagCount = item.Value;
                //here the key must be unique because parent is a dictioanry 
                TempDictUni.Add(item.Key, tagCount / POStagsetSize);
               

            }

            //Calculate Bigram prob and store in a temp Dictionary
            foreach (var tagset in BigramDictionary)
            {

                int totalTagCount;
                if (TagCount.ContainsKey(tagset.Key))
                    totalTagCount = TagCount[tagset.Key];
                else
                    throw new Exception("The dictionary was not built correctly");
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    tagCount = item.Value;
                    if (TempDictBi.ContainsKey(tagset.Key))
                        TempDictBi[tagset.Key].Add(item.Key, tagCount / totalTagCount);
                    else

                        TempDictBi.Add(tagset.Key, new Dictionary<string, double> { { item.Key, tagCount/ totalTagCount } });
                }

            }


            foreach (var tags1 in TempDictUni)
            {
                string t1 = tags1.Key;
                foreach (var tags2 in TempDictUni)
                {
                    string t2 = tags2.Key;

                    foreach (var tag3 in TempDictUni)
                    {
                        string t3 = tag3.Key;
                        string t3App = t2+"_" + tag3.Key;
                        string t1t2 = t1 + "_" + t2;
                        if (BigramDictionary.ContainsKey(t2) && (BigramDictionary[t2]).ContainsKey(t3))
                            bigramCount = BigramDictionary[t2][t3];
                        else
                            bigramCount = 0;


                        if (TrigramDictionary.ContainsKey(t1t2) &&
                            (TrigramDictionary[t1t2]).ContainsKey(t3App))
                            tagCount = TrigramDictionary[t1t2][t3App];
                        else
                            tagCount = 0;
                        
                        if (t3 == "BOS")
                            prob3 = 0;
                        else if ((tagCount == 0) || (bigramCount == 0))
                            prob3 = 1 / (POStagsetSize + 1);
                        else
                            prob3 = tagCount / bigramCount;

                        if (bigramCount != 0)
                            prob2 = TempDictBi[t2][t3];
                        else
                            prob2 = 0;

                        prob1 = TempDictUni[t3];

                        pIntero = (l3 * prob3) + (l2 * prob2) + (l1 * prob1);
                        if (TempDictTri.ContainsKey(t1t2))
                            TempDictTri[t1t2].Add(t3App, pIntero);
                        else
                            TempDictTri.Add(t1t2, new Dictionary<string, double> { { t3App, pIntero } });
                        TotalTriGramCount++;

                    }
                }
            }


            foreach (var tagset in TempDictTri)
            {
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    if (TrigramDictionary.ContainsKey(tagset.Key) && (TrigramDictionary[tagset.Key]).ContainsKey(item.Key))
                        (TrigramDictionary[tagset.Key])[item.Key] = (TempDictTri[tagset.Key])[item.Key];
                    else if (TrigramDictionary.ContainsKey(tagset.Key))
                        (TrigramDictionary[tagset.Key]).Add(item.Key, (TempDictTri[tagset.Key])[item.Key]);
                    else
                        TrigramDictionary.Add(tagset.Key, new Dictionary<string, double> {{ item.Key, (TempDictTri[tagset.Key])[item.Key] } });

                }
            }
            //copy all values to bigram Dict
            foreach (var tagset in TempDictBi)
            {
                foreach (KeyValuePair<String, double> item in tagset.Value)
                {
                    (BigramDictionary[tagset.Key])[item.Key] = (TempDictBi[tagset.Key])[item.Key];
                }
            } 
            return TotalTriGramCount;
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
        public static int ConvertTrigramToCounts(Dictionary<String, Dictionary<String, double>> Emission,
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
            //initialize with 2 to account for 2 BOS we aded to the line
            int sumOfTags = 0;
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
                string toState = tag2 + "_" + tag3;
                if (TransitionTrigram.ContainsKey(fromState) && TransitionTrigram[fromState].ContainsKey(toState))
                    (TransitionTrigram[fromState])[toState]++;
                else
                {
                    if (TransitionTrigram.ContainsKey(fromState))
                        TransitionTrigram[fromState].Add(toState, 1);
                    else
                        TransitionTrigram.Add(fromState, new Dictionary<string, double> { { toState, 1 } });
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
                sumOfTags++;
            }
            
            // doing a -- to eliminate the EOS count
            return --sumOfTags;
        }
    }
}
;