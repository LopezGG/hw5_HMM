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
            string trainingPath = @"E:\CompLing\CompLing570\hw6_dir\examples\output_hmm";
            List<String> AllInputString = new List<string>();
            Dictionary<String, double> initBlock = new Dictionary<string, double>();
            Dictionary<String, double> TransitionBlock = new Dictionary<string, double>();
            Dictionary<String, double> EmissionBlock = new Dictionary<string, double>();
            Dictionary<String, bool> symbolList = new Dictionary<string, bool>();
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
            int state_num, sym_num, init_line_num, trans_line_num, emiss_line_num;
            int initBLockCount = 0, TransmissionBlockCount = 0, EmmissionBlockCount = 0;
            string t1, t2,key;

            double prob;
            line = AllInputString[linecount++];
            string temp = line.Substring(line.IndexOf("=")+1);
            state_num = Convert.ToInt32(temp);
            
            line = AllInputString[linecount++];
            temp = line.Substring(line.IndexOf("=") + 1);
            sym_num = Convert.ToInt32(temp);

            line = AllInputString[linecount++];
            temp = line.Substring(line.IndexOf("=") + 1);
            init_line_num = Convert.ToInt32(temp);

            line = AllInputString[linecount++];
            temp = line.Substring(line.IndexOf("=") + 1);
            trans_line_num = Convert.ToInt32(temp);

            line = AllInputString[linecount++];
            temp = line.Substring(line.IndexOf("=") + 1);
            emiss_line_num = Convert.ToInt32(temp);

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
                string[] tempwords = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (tempwords.Length != 4)
                {
                    Console.WriteLine("warning: initBLock not properly formed with 4 columns and hence reading this block is aborted");
                    break;
                }
                t1 = tempwords[0];
                t2 = tempwords[1];
                prob = Convert.ToDouble(tempwords[3]);
                key = t1 + "_" + t2;
                initBLockCount++;
                if (!initBlock.ContainsKey(key))
                    initBlock.Add(key, prob);
                else
                    Console.WriteLine("warning: init block has duplicate entries");

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
                string[] tempwords = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (tempwords.Length != 4)
                {
                    Console.WriteLine("warning: TransitionBlock not properly formed with 4 columns and hence reading this block is aborted");
                    break;
                }
                t1 = tempwords[0];
                t2 = tempwords[1];
                prob = Convert.ToDouble(tempwords[3]);
                key = t1 + "_" + t2;
                TransmissionBlockCount++;
                if (!TransitionBlock.ContainsKey(key))
                    TransitionBlock.Add(key, prob);
                else
                    Console.WriteLine("warning: TransitionBlock has duplicate entries");
            }

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
                string[] tempwords = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (tempwords.Length != 4)
                {
                    Console.WriteLine("warning: EmissionBlock not properly formed with 4 columns and hence reading this block is aborted");
                    break;
                }
                t1 = tempwords[0];
                t2 = tempwords[1];
                prob = Convert.ToDouble(tempwords[3]);
                key = t1 + "_" + t2;
                EmmissionBlockCount++;
                if (!EmissionBlock.ContainsKey(key))
                    EmissionBlock.Add(key, prob);
                else
                    Console.WriteLine("warning: EmissionBlock has duplicate entries");

                
                if (!symbolList.ContainsKey(t2))
                    symbolList.Add(t1, true);
                else
                    Console.WriteLine("warning: symbolList has duplicate entries");

            }

        }

    }
}
