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

            if (args.Length < 6)
                throw new Exception("incorrect number of arguments");
            string outputPath = args[0];
            double l1 = Convert.ToDouble(args[1]);
            double l2 = Convert.ToDouble(args[2]);
            double l3 = Convert.ToDouble(args[3]);
            String UnkProbFile = args[4];
            string trainingPath = args[5];
            string line;
            Dictionary<String, Dictionary<String, double>> Emission = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> NewEmission = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionBigram = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> TransitionTrigram = new Dictionary<string, Dictionary<string, double>>();
            List<String> symbolList = new List<String> ();
            symbolList.Add(@"<unk>");
            List<string> statesList = new List<string>();
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
                     ConvertTrigramToCounts(Emission, TransitionBigram, TransitionTrigram, TagCount, symbolList, line);
                }

            }
            POStagsetSize = TagCount.Keys.ToList().Count;
            symbolList = symbolList.Distinct().ToList();

            TotalTransmissionArc = ConvertCountToProbTrigram(TransitionTrigram, TransitionBigram, TagCount, POStagsetSize, l1, l2, l3,statesList);
            statesList = statesList.Distinct().ToList();
            TotalEmissionArc = HandleEmission(Emission, NewEmission, TagCount, UnkTagProb);
            using (StreamWriter Sw = new StreamWriter(outputPath))
            {
                Sw.WriteLine("state_num=" + statesList.Count);
                Sw.WriteLine("sym_num=" + symbolList.Count);
                Sw.WriteLine("init_line_num=1");
                Sw.WriteLine("trans_line_num=" + TotalTransmissionArc);
                Sw.WriteLine("emiss_line_num=" + TotalEmissionArc);
                Sw.WriteLine();
                Sw.WriteLine(@"\init");
                Sw.WriteLine("BOS" + "\t" + "1.0" + "\t" + "0.000");
                Sw.WriteLine();
                Sw.WriteLine(@"\transition");
                WriteDictionary(TransitionTrigram, Sw);
                Sw.WriteLine();
                Sw.WriteLine(@"\emission");
                WriteDictionary(NewEmission, Sw);
            }
            Dictionary<String, Double> test = TransitionTrigram["BOS_BOS"];
            double t = 0;
            foreach (var item in test)
            {
                t += item.Value;
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

        public static int HandleEmission (Dictionary<String, Dictionary<String, double>> Emission,
            Dictionary<String, Dictionary<String, double>> NewEmission,
            Dictionary<String, int> TagCount, Dictionary<String,double> UnkTagProb)
        {
            int EmissionArc = 0;
            double prob = 0;
            
            string t2, t3, observation,key;
            double countT3,countObsT3,probUnk=0;
            foreach (var tag  in TagCount)
            {
                t2 = tag.Key;
                foreach (var tagset in Emission)
                {
                    t3 = tagset.Key;
                    if (TagCount.ContainsKey(t3))
                        countT3 = TagCount[t3];
                    else
                        throw new Exception("tag not found in unigrams");
                    if (UnkTagProb.ContainsKey(t3))
                        probUnk = UnkTagProb[t3];
                    else
                        probUnk = 0;
                    foreach (var obs in tagset.Value)
                    {
                        observation = obs.Key;
                        countObsT3 = obs.Value;
                        prob = (countObsT3 / countT3) * (1 - probUnk);
                        key = t2 + "_" + t3;
                        if (NewEmission.ContainsKey(key) &&
                            (NewEmission[key]).ContainsKey(observation))
                            throw new Exception("duplicate values");
                        else if (NewEmission.ContainsKey(key))
                            NewEmission[key].Add(observation, prob);
                        else
                            NewEmission.Add(key, new Dictionary<string, double> { { observation, prob } });
                        EmissionArc++;
                    }
                }
                //now we need to add the unk prob in emission lines so 
                observation = @"<unk>";
                foreach (var item in UnkTagProb)
                {
                    t3 = item.Key;
                    prob = item.Value;
                    key = t2 + "_" + t3;
                    if (NewEmission.ContainsKey(key) &&
                        (NewEmission[key]).ContainsKey(observation))
                        Console.WriteLine("duplicate values in unk");
                    else if (NewEmission.ContainsKey(key))
                        NewEmission[key].Add(observation, prob);
                    else
                        NewEmission.Add(key, new Dictionary<string, double> { { observation, prob } });
                    EmissionArc++;
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
            double l3,
            List<string> statesList)
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
            double totaltags=0;
            foreach (var item in TagCount)
            {
                tagCount = item.Value;
                totaltags += tagCount;
                            

            }
            foreach (var item in TagCount)
            {
                //here the key must be unique because parent is a dictioanry
                double pro = tagCount / totaltags;
                if (pro > 1)
                    Console.WriteLine("prob>1");
                TempDictUni.Add(item.Key, tagCount / totaltags);
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
                if (t1 == "EOS")
                    continue;
                foreach (var tags2 in TempDictUni)
                {
                    string t2 = tags2.Key;
                    if (t1 != "BOS" && t2 == "BOS")
                        continue;
                    foreach (var tag3 in TempDictUni)
                    {
                        
                        string t3 = tag3.Key;
                        if (t2 == "EOS" && t3 != "EOS")
                            continue;
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
                            prob3 = 1 / ( (POStagsetSize-2) + 1); // to remove EOS and BOS
                        else
                            prob3 = tagCount / bigramCount;

                        if (bigramCount != 0)
                            prob2 = TempDictBi[t2][t3];
                        else
                            prob2 = 0;

                        prob1 = TempDictUni[t3];

                        pIntero = (l3 * prob3) + (l2 * prob2) + (l1 * prob1);
                        if(pIntero>1)
                            Console.WriteLine("Pintro greater than 1");
                        if (TempDictTri.ContainsKey(t1t2))
                            TempDictTri[t1t2].Add(t3App, pIntero);
                        else
                            TempDictTri.Add(t1t2, new Dictionary<string, double> { { t3App, pIntero } });
                        TotalTriGramCount++;
                        statesList.Add(t1t2);
                        statesList.Add(t3App);
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
        public static void ConvertTrigramToCounts(Dictionary<String, Dictionary<String, double>> Emission,
        Dictionary<String, Dictionary<String, double>> TransitionBigram,
        Dictionary<String, Dictionary<String, double>> TransitionTrigram,
        Dictionary<String, int> TagCount,
        List<String> symbolList,
            string line)

        {
            line = "<s>/BOS <s>/BOS " + line + " <BSs>/EOS";
            string tag1;
            string tag2;
            string tag3;
            string observation;
            //initialize with 2 to account for 2 BOS we aded to the line
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

                    symbolList.Add(observation);

            }
        }
    }
}
;