using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextAnalyzer
{
    class World
    {
        private int _number;
        private int _bOwner;


        public List<World> Worlds { get; set; }
        public World Parent { get; set; }

        public World(int number, int branchOwner)
        {
            _number = number;
            _bOwner = branchOwner;
            Worlds = new List<World>();
        }
        
        public int Number {  get { return _number; } }

        public int BOnwer { get { return _bOwner; } }

        public bool Closed { get; set; }
        public bool BranchCloses { get; internal set; }

        public void PrintPretty(string indent, bool last, bool withID, bool subID)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(indent);
            if (last)
            {
                Console.Write("└─");
                indent += "  ";
            }
            else
            {
                Console.Write("├─");
                indent += "| ";
            }

            string id = "";
            if (withID)
                id = " " + _number;

            Console.ForegroundColor = ConsoleColor.Yellow;

            string open = Closed ? "Closed" : "Open";
            Console.WriteLine("W: " + _number + " B: " + _bOwner + " Status: " + open);

            Console.ForegroundColor = ConsoleColor.Gray;

            var children = Worlds;

            for (int i = 0; i < children.Count; i++)
                children[i].PrintPretty(indent, i == children.Count - 1, withID & subID, subID);

        }

        public World FindWorld(int num)
        {
            if (this._number == num)
                return this;
            else
            {
                World ans = null;
                foreach (var item in Worlds)
                {
                    ans = item.FindWorld(num);
                    if (ans != null)
                        return ans;
                }
            }

            return null;
        }

        internal bool IsClosed()
        {
            if (Worlds.Count == 0 || BranchCloses)
                return Closed;

            Dictionary<int, bool> branchWorlds = new Dictionary<int, bool>();

            foreach (var w in Worlds)
            {
                bool closed = false;
                if(branchWorlds.TryGetValue(w.BOnwer, out closed))
                {
                    if (closed)
                        continue;
                }

                closed = w.IsClosed();
                branchWorlds[w._bOwner] = closed   ;
            }


            bool allClosed = true;
            bool allOpen = true;
            foreach (var bw in branchWorlds)
            {
                if (!bw.Value)
                    allClosed = false;

                if (bw.Value)
                    allOpen = false;
            }

            if(!allOpen && !allClosed)
            {
                Console.WriteLine("World " + _number + " Partially Closed");
                Closed = false;
                return true;
            }
            else if(allClosed)
            {
                Console.WriteLine("World " + _number + " All Closed");
                Closed = true;
                return true;
            }
            else
            {
                Console.WriteLine("World " + _number + " All Open");

                Closed = false;
                return false;
            }
        }
    }
}