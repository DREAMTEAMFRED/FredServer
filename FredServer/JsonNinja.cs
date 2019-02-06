using System;
using System.Collections.Generic;
using System.Text;

namespace FredServer
{
    public class JsonNinja
    {
        List<string> names = new List<string>();
        List<string> vals = new List<string>();

        public JsonNinja(string data)
        {
            names.Clear();
            vals.Clear();
            data = data.Replace("}", "");
            data = data.Replace("{", "");
            int startPos = 0;
            int endPos = 0;
            int endPosVal = 0;
            bool startFound = true;
            bool endFound = true;
            bool collection = false;
            int collectionCounter = 0;

            int k = 0;
            while (k < data.Length)
            {
                char test = data[k];
                if (data[k] == '"' && startFound == true)
                {
                    startFound = false;
                    startPos = k;
                }
                if (data[k] == ':' && endFound == true)
                {
                    if (data[k + 1] == '[')
                    {
                        collection = true;
                    }
                    endPos = k;
                    endFound = false;

                    string nameCut = data.Substring(startPos, endPos - startPos);
                    names.Add(nameCut);
                }

                if (collection == true)
                {
                    if (data[k] == '[')
                        collectionCounter++;

                    if (data[k] == ']')
                    {
                        if (collectionCounter == 1)
                        {
                            endPosVal = k;
                            string valCut = data.Substring(endPos + 1, endPosVal - (endPos));
                            vals.Add(valCut);
                            startFound = true;
                            endFound = true;
                            collectionCounter = 0;
                            collection = false;
                        }
                        else
                        {
                            collectionCounter--;
                        }
                    }
                }
                else if ((data[k] == ',' && data[k + 1] == '"' && startFound == false) || k == data.Length - 1)
                {
                    endPosVal = k;
                    string valCut = data.Substring(endPos + 1, endPosVal - (endPos + 1));
                    vals.Add(valCut);
                    startFound = true;
                    endFound = true;
                }
                k++;
            }
        } // end constructor

        public List<string> GetNames()
        {
            return names;
        }

        public List<string> GetVals()
        {
            return vals;
        }

        public List<string> GetInfoList(string term)
        {
            List<string> values = new List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                if (term == names[i])
                {
                    values.Add(vals[i].Substring(1, vals[i].Length - 2));
                }
            }
            return values;
        }

        public string GetInfo(string name)
        {

            string value = "";
            for (int i = 0; i < names.Count; i++)
            {
                if (name == names[i])
                {
                    value = vals[i];
                }
            }
            return value;
        }

        public List<string> GetIds(string name)
        {
            List<string> value = new List<string>();
            for (int i = 1; i < names.Count; i++)
            {
                if (name == names[i])
                {
                    value.Add(vals[i]);
                }
            }
            return value;
        }
    }// end class
}
