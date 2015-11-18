using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValidateHMM
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                throw new Exception("give the hmm file name");
            string trainingPath = args[0];
            List<String> AllInputString = new List<string>();
            Dictionary<String, double> initBlock = new Dictionary<String, double>();
            Dictionary<String, Dictionary<String, double>> TransitionBlock = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<String, Dictionary<String, double>> EmissionBlock = new Dictionary<string, Dictionary<string, double>>();
            List<string> symbolList = new List<string>();
            List<string> statesList = new List<string>();
            string line;
            using (StreamReader SR = new StreamReader(trainingPath))
            {
                while((line = SR.ReadLine() ) !=null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    AllInputString.Add(line);

                }
            }
            int linecount = 0;
            int state_num=0, sym_num=0, init_line_num=0, trans_line_num=0, emiss_line_num=0;
            int initBLockCount = 0, TransmissionBlockCount = 0, EmmissionBlockCount = 0;
            string t1, t2, temp;

            double prob;
            line = AllInputString[linecount++];
            if (line.Contains("state_num"))
            {
                temp = line.Substring(line.IndexOf("=") + 1);
                state_num = Convert.ToInt32(temp);
            }
            line = AllInputString[linecount++];

            if(line.Contains("sym_num"))
            {
                temp = line.Substring(line.IndexOf("=") + 1);
                sym_num = Convert.ToInt32(temp);
            }

            line = AllInputString[linecount++];
            if (line.Contains("init_line_num"))
            {
                temp = line.Substring(line.IndexOf("=") + 1);
                init_line_num = Convert.ToInt32(temp);
            }

            line = AllInputString[linecount++];
            if (line.Contains("trans_line_num"))
            {
                temp = line.Substring(line.IndexOf("=") + 1);
                trans_line_num = Convert.ToInt32(temp);
            }
            line = AllInputString[linecount++];
            if(line.Contains("emiss_line_num"))
            {
                temp = line.Substring(line.IndexOf("=") + 1);
                emiss_line_num = Convert.ToInt32(temp);
            }

            line = AllInputString[linecount++];
            if(!line.Contains("init"))
                Console.WriteLine("warning: init block missing");
            while(true)
            {
                if (linecount >= AllInputString.Count)
                    break;
                line = AllInputString[linecount++];
                if (line.Contains(@"\transition"))
                    break;
                string[] tempwords = line.Split(new string[] { "\t"," " }, StringSplitOptions.RemoveEmptyEntries);

                t1 = tempwords[0];
                prob = Convert.ToDouble(tempwords[1]);
                initBLockCount++;
                if (initBlock.ContainsKey(t1) )
                    Console.WriteLine("warning: init block has duplicate entries");
                else
                    initBlock.Add(t1, prob);

            }

            while (!line.Contains(@"\transition"))
            {
                if (linecount >= AllInputString.Count)
                    break;
                line = AllInputString[linecount++];
            }

            while (true)
            {
                if (linecount >= AllInputString.Count)
                    break;
                line = AllInputString[linecount++];
                if (line.Contains(@"\emission"))
                    break;
                string[] tempwords = line.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (tempwords.Length != 4)
                {
                    Console.WriteLine("warning: TransitionBlock not properly formed with 4 columns and hence reading this block is aborted");
                    break;
                }
                t1 = tempwords[0];
                t2 = tempwords[1];
                prob = Convert.ToDouble(tempwords[2]);
                TransmissionBlockCount++;
                if (TransitionBlock.ContainsKey(t1) && TransitionBlock[t1].ContainsKey(t2))
                    Console.WriteLine("warning: TransitionBlock  block has duplicate entries");
                else if (TransitionBlock.ContainsKey(t1))
                    TransitionBlock[t1].Add(t2, prob);
                else
                    TransitionBlock.Add(t1, new Dictionary<string, double> { { t2, prob } });
                statesList.Add(t1);
                statesList.Add(t2);

            }
            statesList = statesList.Distinct().ToList();
            while (!line.Contains(@"\emission"))
            {
                if (linecount >= AllInputString.Count)
                    break;
                line = AllInputString[linecount++];
            }

            while (true)
            {
                if (linecount >= AllInputString.Count)
                    break;
                line = AllInputString[linecount++];
                string[] tempwords = line.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (tempwords.Length != 4)
                {
                    Console.WriteLine("warning: EmissionBlock not properly formed with 4 columns and hence reading this block is aborted");
                    break;
                }
                t1 = tempwords[0];
                string observation = tempwords[1];
                prob = Convert.ToDouble(tempwords[2]);
                EmmissionBlockCount++;
                if (EmissionBlock.ContainsKey(t1) && EmissionBlock[t1].ContainsKey(observation))
                    Console.WriteLine("warning: TransitionBlock  block has duplicate entries");
                else if (EmissionBlock.ContainsKey(t1))
                    EmissionBlock[t1].Add(observation, prob);
                else
                    EmissionBlock.Add(t1, new Dictionary<string, double> { { observation, prob } });
               //here t2 is hte observation
               symbolList.Add(observation);

            }
            symbolList = symbolList.Distinct().ToList();
            double totalProb = 0;
            foreach (var items in initBlock)
            {
                totalProb += items.Value;

            }
            if (totalProb != 1)
            {
                Console.WriteLine("Warning: Total Prob of init out of state  is not equal to one");
            }
            foreach (var tagset in TransitionBlock)
            {
                if(tagset.Key == "BOS_BOS")
                    Console.WriteLine("Wait");
                totalProb = 0;
                foreach (var items in tagset.Value)
                {
                    totalProb += items.Value;
                }
                if (Math.Round(totalProb, 2) != 1.00)
                {
                    Console.WriteLine("warning: the trans_prob_sum for state " + tagset.Key + " is " + totalProb);
                }
            }

            foreach (var tagset in EmissionBlock)
            {
                totalProb = 0;
                foreach (var items in tagset.Value)
                {
                    totalProb += items.Value;
                }
                if (Math.Round(totalProb, 2) != 1.00)
                {
                    Console.WriteLine("warning: the emiss_prob_sum for state " + tagset.Key + " is " + totalProb);
                }
            }
            if (state_num != statesList.Count)
                Console.WriteLine("Actual number of states is " + statesList.Count + " but the number of states declared is " + state_num);

            if (sym_num != symbolList.Count)
                Console.WriteLine("Actual number of symbols is " + symbolList.Count + " but the number of states declared is " + sym_num);

            if (init_line_num != initBLockCount)
                Console.WriteLine("warning: different numbers of init_line_num: claimed=" + init_line_num + ", real=" + initBLockCount);


            if (trans_line_num != TransmissionBlockCount)
                Console.WriteLine("warning: different numbers of trans_line_num: claimed=" + trans_line_num + ", real=" + TransmissionBlockCount);


            if (emiss_line_num != EmmissionBlockCount)
                Console.WriteLine("warning: different numbers of trans_line_num: claimed=" + emiss_line_num + ", real=" + EmmissionBlockCount);

            Console.ReadLine();
        }

    }
}
