using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TextAnalyzer
{
    class Branch
    {
        private static int _branchNumber;
        private bool _hasChildren;
        public Branch(int worldNumber)
        {
            Classifiers = new List<Classifier>();
            ChildBranches = new List<Branch>();
            GHClassifiers = new List<Classifier>();
            ProcessedClassifiers = new List<Classifier>();
            VariableList = new Dictionary<char, int>();
            WorldNumber = worldNumber;
            BranchNumber = _branchNumber++;
            ParentBranch = new List<Branch>();
            WorldBranches = new List<Branch>();
            FPClassifiers = new List<Classifier>();

            if (ProcessedFPClassifiers == null)
                ProcessedFPClassifiers = new Dictionary<Classifier, bool>();

        }

        private List<Classifier> _processedClassifiers = new List<Classifier>();

        public int BranchNumber { get; private set; }

        public List<Branch> ParentBranch { get; set; }

        public List<Branch> WorldBranches { get; set; }

        public List<Classifier> Classifiers { get; set; }


        /// <summary>
        /// These classifiers are to be visited
        /// </summary>
        public List<Classifier> GHClassifiers { get; set; }

        /// <summary>
        /// These classifiers (square) are visited
        /// </summary>
        public List<Classifier> ProcessedClassifiers { get; set; }

        /// <summary>
        /// These diamond classifiers are already visiteds
        /// </summary>
        public static Dictionary<Classifier, bool> ProcessedFPClassifiers { get; set; }

        /// <summary>
        /// Rule is the same as Squares. Do not process Diamonds untill the branch closes.
        /// By close we mean does not have any more place to go. Then expand diamonds first
        /// Afterwards copy the squares to it.
        /// </summary>
        public List<Classifier> FPClassifiers { get; set; }
        //public World World { get; private set; }

        public List<Branch> ChildBranches { get; set; }

        public int WorldNumber { get; set; }

        public bool Closed { get; set; }
        public Dictionary<char, int> VariableList { get; private set; }

        internal void Process()
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Branch " + BranchNumber + " World " + WorldNumber);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("=================================================");
            while (Classifiers.Count > 0)
            {
                var classifiersArray = Classifiers.ToArray();
                Classifiers.Clear(); //Make space for new items
                Console.WriteLine("-------------------------------------------");
                bool breakLoop = false;

                foreach (var classifier in classifiersArray)
                {
                    _processedClassifiers.Add(classifier);
                    if (breakLoop)
                    {
                        if (classifier.ClassifieType == TypeEnum.Variable || (classifier.ClassifieType == TypeEnum.Not && classifier.LeftOperand.ClassifieType == TypeEnum.Variable))
                            Classifiers.Add(classifier);
                        continue;
                    }
                    Console.WriteLine("Classifier picked =");
                    classifier.PrintPretty("", false, true, false);
                    switch (classifier.ClassifieType)
                    {
                        case TypeEnum.Not:
                            if (classifier.LeftOperand.ClassifieType == TypeEnum.Bracket)
                            {
                                if(classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Bracket
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.P
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.G
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.H
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.F
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Not)
                                {
                                    classifier.LeftOperand = classifier.LeftOperand.LeftOperand; // skip the extra bracket
                                    this.AddClassifier(classifier);
                                }
                                else if (classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.And) // B1 ~A  + B2 ~B
                                {
                                    //Create Branch1
                                    Classifier not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.LeftOperand; //not brancket and -> branchA
                                    var tempBranch = new Branch(WorldNumber);
                                    tempBranch.AddClassifier(not);
                                    this.AddBranch(tempBranch, classifiersArray, classifier, false);

                                    //Create Branch2
                                    not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.RightOperand; //not brancket and -> branchB
                                    tempBranch = new Branch(WorldNumber);
                                    tempBranch.AddClassifier(not);
                                    this.AddBranch(tempBranch, classifiersArray, classifier, false);
                                    breakLoop = true;
                                }
                                else if (classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Or) //~A + ~B
                                {
                                    Classifier not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.LeftOperand; //not brancket and -> branchA
                                    this.AddClassifier(not);

                                    not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.RightOperand; //not brancket and -> branchA
                                    this.AddClassifier(not);
                                }
                                else if (classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Arrow) // A + ~B
                                {
                                    this.AddClassifier(classifier.LeftOperand.LeftOperand.LeftOperand); //Simply add branchA

                                    Classifier not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.RightOperand; //not brancket and -> branchA
                                    this.AddClassifier(not);
                                }
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.Not)
                            {
                                this.AddClassifier(classifier.LeftOperand.LeftOperand); //Simply add branchA
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.F)
                            {
                                var gClass = Classifier.CreateClassifier(TypeEnum.G);
                                var nClass = Classifier.CreateClassifier(TypeEnum.Not);
                                gClass.LeftOperand = nClass;
                                nClass.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.AddClassifier(gClass);
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.G)
                            {
                                var fClass = Classifier.CreateClassifier(TypeEnum.F);
                                var nClass = Classifier.CreateClassifier(TypeEnum.Not);
                                fClass.LeftOperand = nClass;
                                nClass.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.AddClassifier(fClass);
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.H)
                            {
                                var pClass = Classifier.CreateClassifier(TypeEnum.P);
                                var nClass = Classifier.CreateClassifier(TypeEnum.Not);
                                pClass.LeftOperand = nClass;
                                nClass.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.AddClassifier(pClass);
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.P)
                            {
                                var hClass = Classifier.CreateClassifier(TypeEnum.H);
                                var nClass = Classifier.CreateClassifier(TypeEnum.Not);
                                hClass.LeftOperand = nClass;
                                nClass.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.AddClassifier(hClass);

                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.Variable)
                            {
                                AddVariable(classifier.LeftOperand.Character, false);
                            }
                            break;
                        case TypeEnum.Bracket:

                            var a = classifier.LeftOperand.LeftOperand; //Can be null -> () - X - LeftOperand
                            var b = classifier.LeftOperand.RightOperand; //Can be null -> () - X - BranchB

                            switch (classifier.LeftOperand.ClassifieType)
                            {
                                case TypeEnum.And: // A + B
                                    this.AddClassifier(a);
                                    this.AddClassifier(b);
                                    break;
                                case TypeEnum.Or: // B1 A + B2 B
                                    var branch = new Branch(WorldNumber);
                                    branch.AddClassifier(a);
                                    this.AddBranch(branch, classifiersArray, classifier, false);

                                    branch = new Branch(WorldNumber);
                                    branch.AddClassifier(b);
                                    this.AddBranch(branch, classifiersArray, classifier, false);
                                    breakLoop = true;
                                    break;
                                case TypeEnum.Arrow: // B1 A + B2 ~B
                                    branch = new Branch(WorldNumber);
                                    var not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = a;
                                    branch.AddClassifier(not);
                                    this.AddBranch(branch, classifiersArray, classifier, false);

                                    branch = new Branch(WorldNumber);
                                    branch.AddClassifier(b);
                                    this.AddBranch(branch, classifiersArray, classifier, false);
                                    breakLoop = true;
                                    break;
                                case TypeEnum.Bracket:
                                case TypeEnum.Not:
                                case TypeEnum.F:
                                case TypeEnum.G:
                                    this.AddClassifier(classifier.LeftOperand);
                                    break;
                                case TypeEnum.Variable:
                                    this.AddClassifier(a);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case TypeEnum.F:
                        case TypeEnum.P:
                            if (ProcessedFPClassifiers.ContainsKey(classifier))
                            {
                                if (!ProcessedFPClassifiers[classifier])
                                    ProcessedFPClassifiers[classifier] = true;
                                else
                                {
                                    Console.WriteLine("Not exapnding due to possible infinity loop");
                                    break;
                                }
                            }
                            else
                                ProcessedFPClassifiers[classifier] = false;

                            this.FPClassifiers.Add(classifier);
                            break;
                        case TypeEnum.H:
                        case TypeEnum.G:
                            this.GHClassifiers.Add(classifier);
                            if (Program.EquationType == EquationType.K) // K stops here
                                continue;

                            else if (Program.EquationType == EquationType.M)
                            {
                                Console.WriteLine("Reflective Property");

                                this.AddClassifier(classifier.LeftOperand);
                            }
                            break;
                        case TypeEnum.And:
                        case TypeEnum.Or:
                        case TypeEnum.Arrow:
                            throw new ArgumentException("TypeEnum un expeced = " + classifier.Character);
                        case TypeEnum.Variable:
                            AddVariable(classifier.Character, true);
                            break;
                        default:
                            break;
                    }
                }
            }
            _processedClassifiers.Clear();
            Console.WriteLine("Branch Expansion Completed");

            if (!_hasChildren && FPClassifiers.Count > 0)
            {
                foreach (var item in FPClassifiers)
                {
                    //World w = new World(++Program._worldCount, this.BranchNumber);
                    var tbranch = new Branch(++Program._worldCount);
                    Program._worldBranches.Add(tbranch);

                    //this.World.Worlds.Add(w);
                    string tpe = "F";
                    if (item.ClassifieType == TypeEnum.P)
                        tpe = "P";
                    Console.WriteLine();
                    Console.WriteLine("Processing " + tpe + " from " + this.BranchNumber + " World " + this.WorldNumber);

                    Console.WriteLine("Classifier picked =");
                    item.PrintPretty("", false, true, false);
                    tbranch.AddClassifier(item.LeftOperand);
                    this.AddBranch(tbranch, this.Classifiers.ToArray(), null, item.ClassifieType == TypeEnum.P);
                    this.WorldBranches.Add(tbranch);

                    foreach (var sq in GHClassifiers)
                    {
                        if((item.ClassifieType == TypeEnum.F && sq.ClassifieType== TypeEnum.G) || (item.ClassifieType == TypeEnum.P && sq.ClassifieType == TypeEnum.H))
                            ProcSq(tbranch, sq);
                    }
                }
            }
            else
            {
                // Copy squared equation forward to branches
                foreach (var item in GHClassifiers)
                {
                    ProcessedClassifiers.Add(item);

                    var branches = ChildBranches;
                    if (item.ClassifieType == TypeEnum.H)
                        branches = ParentBranch;

                    foreach (var branch in branches)
                        ProcSq(branch, item);
                        
                }

                foreach (var b in ChildBranches)
                {
                    b.FPClassifiers.AddRange(FPClassifiers);
                    //Console.WriteLine("Copying Dimaond forward");
                }
            }

            _hasChildren = false;
            GHClassifiers.Clear();
            FPClassifiers.Clear();
        }

        private void ProcSq(Branch branch, Classifier item)
        {
            var classifier = item;
            if (branch.WorldNumber != WorldNumber) // Open the equation if new world
            {
                Console.WriteLine();
                Console.WriteLine("Expanding " + item.Character + " classifier from Branch " + this.BranchNumber + " World " + this.WorldNumber);
                Console.WriteLine("Classifier picked =");
                item.PrintPretty("", false, false, false);
                Console.WriteLine("Converted to ->");
                item.LeftOperand.PrintPretty("", false, false, false);
                Console.WriteLine("Expanded to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);

                branch.AddClassifier(item.LeftOperand, false);
                if (Program.EquationType == EquationType.K4T)
                {
                    if (!item.ExistsEqual(branch.ProcessedClassifiers) && !item.ExistsEqual(branch.GHClassifiers))
                        branch.GHClassifiers.Add(item);
                }
                Program._branchesToProcess.Add(branch);

                return;
            }

            //Console.WriteLine("Carried the following classifier to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);
            //classifier.PrintPretty("", false, false, false);
            if (!item.ExistsEqual(branch.ProcessedClassifiers) && !item.ExistsEqual(branch.GHClassifiers))
                branch.GHClassifiers.Add(classifier);
        }

        private void AddClassifier(Classifier classifier, bool print = true)
        {
            if (print)
            {
                Console.WriteLine("Converted to ->");
                classifier.PrintPretty("", false, false, false); 
            }
            if (classifier.ClassifieType == TypeEnum.F || classifier.ClassifieType == TypeEnum.P)
                this.Classifiers.Insert(0, classifier);
            else
                this.Classifiers.Add(classifier);
        }


        private void AddBranch(Branch branch, Classifier[] classifiers, Classifier toExclude, bool isParent)
        {
            Console.WriteLine("Adding in new Branch = " + branch.BranchNumber + " World " + branch.WorldNumber);

            if (classifiers.Length > 0 && toExclude != null)
            {
                List<Classifier> cl = new List<Classifier>();
                foreach (var item in classifiers)
                {
                    if (item != toExclude 
                        && item.ClassifieType != TypeEnum.Variable 
                        && !(item.ClassifieType == TypeEnum.Not 
                        && item.LeftOperand.ClassifieType == TypeEnum.Variable)
                        && !_processedClassifiers.Contains(item))
                        cl.Add(item);
                }

                branch.Classifiers.AddRange(cl.ToArray()); 
            }


            if (!isParent) //Except P
            {
                _hasChildren = true;
                this.ChildBranches.Add(branch);
                branch.ParentBranch.Add(this);
            }
            else
            {
                this.ParentBranch.Add(branch);
                branch.ChildBranches.Add(this);
            }
            Program._branchesToProcess.Add(branch);

            Console.WriteLine();
        }

        private void AddVariable(char variable, bool status)
        {
            var value = 0;
            if (status)
                value = 1;
            else
                value = -1;

            Program._globalVariablList[variable] = 0;

            if (VariableList.ContainsKey(variable))
            {
                if (VariableList[variable] == 0)
                    return;
                else if (VariableList[variable] + value == 0)
                    VariableList[variable] = 0;
                else
                    return;
            }
            else
                VariableList[variable] = value;
            Console.WriteLine("Variable stored");

        }
    }
}
