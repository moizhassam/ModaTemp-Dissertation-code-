using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TextAnalyzer
{
    class Branch
    {
        private static int _branchNumber;
        public Branch(int worldNumber, Branch worldParentBranch, Branch immediateParentBranch, World world = null)
        {
            Classifiers = new List<Classifier>();
            Branches = new List<Branch>();
            SquaredClassifiers = new List<Classifier>();
            ProcessedClassifiers = new List<Classifier>();
            VariableList = new Dictionary<char, int>();
            WorldNumber = worldNumber;
            BranchNumber = _branchNumber++;
            WorldParentBranch = worldParentBranch;
            WorldBranches = new List<Branch>();
            DiamondClassifiers = new List<Classifier>();
            _immediateParent = immediateParentBranch;
            //World = world == null ? ParentBranch.World : world;

            if (ProcessedDiamondClassifiers == null)
                ProcessedDiamondClassifiers = new Dictionary<Classifier, bool>();

        }

        private List<Classifier> _processedClassifiers = new List<Classifier>();

        public int BranchNumber { get; private set; }

        public Branch WorldParentBranch { get; set; }

        public List<Branch> WorldBranches { get; set; }

        public List<Classifier> Classifiers { get; set; }


        /// <summary>
        /// These classifiers are to be visited
        /// </summary>
        public List<Classifier> SquaredClassifiers { get; set; }

        /// <summary>
        /// These classifiers (square) are visited
        /// </summary>
        public List<Classifier> ProcessedClassifiers { get; set; }

        /// <summary>
        /// These diamond classifiers are already visiteds
        /// </summary>
        public static Dictionary<Classifier, bool> ProcessedDiamondClassifiers { get; set; }

        /// <summary>
        /// Rule is the same as Squares. Do not process Diamonds untill the branch closes.
        /// By close we mean does not have any more place to go. Then expand diamonds first
        /// Afterwards copy the squares to it.
        /// </summary>
        public List<Classifier> DiamondClassifiers { get; set; }

        private Branch _immediateParent;

        public World World { get; private set; }

        public List<Branch> Branches { get; set; }

        public int WorldNumber { get; set; }

        public bool Closed { get; set; }
        public Dictionary<char, int> VariableList { get; private set; }

        internal void Process(List<Branch> branchesToProcess)
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
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Diamond
                                    || classifier.LeftOperand.LeftOperand.ClassifieType == TypeEnum.Square
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
                                    var tempBranch = new Branch(WorldNumber, WorldParentBranch, this);
                                    tempBranch.AddClassifier(not);
                                    this.AddBranch(tempBranch, classifiersArray, classifier);

                                    //Create Branch2
                                    not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = classifier.LeftOperand.LeftOperand.RightOperand; //not brancket and -> branchB
                                    tempBranch = new Branch(WorldNumber, WorldParentBranch, this);
                                    tempBranch.AddClassifier(not);
                                    this.AddBranch(tempBranch, classifiersArray, classifier);
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
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.Square)
                            {
                                if (ProcessedDiamondClassifiers.ContainsKey(classifier))
                                {
                                    if (!ProcessedDiamondClassifiers[classifier])
                                        ProcessedDiamondClassifiers[classifier] = true;
                                    else
                                    {
                                        Console.WriteLine("Not exapnding due to possible infinity loop");
                                        continue;
                                    }
                                }
                                else
                                    ProcessedDiamondClassifiers[classifier] = false;

                                Classifier diamond = Classifier.CreateClassifier(TypeEnum.Diamond);
                                diamond.LeftOperand = Classifier.CreateClassifier(TypeEnum.Not);
                                diamond.LeftOperand.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.InsertClassifier(diamond); //Add beginning of list (priority)
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.Diamond)
                            {
                                Classifier square = Classifier.CreateClassifier(TypeEnum.Square);
                                square.LeftOperand = Classifier.CreateClassifier(TypeEnum.Not);
                                square.LeftOperand.LeftOperand = classifier.LeftOperand.LeftOperand;
                                this.AddClassifier(square); //Add bottom of list

                                var im = _immediateParent;
                                while(im != null && im.WorldNumber == this.WorldNumber)
                                {
                                    im.ProcessedClassifiers.Add(square);
                                    im = im._immediateParent;
                                }
                            }
                            else if (classifier.LeftOperand.ClassifieType == TypeEnum.Variable)
                            {
                                AddVariable(classifier.LeftOperand.Character, false);
                            }
                            break;
                        case TypeEnum.Bracket:

                            var a = classifier.LeftOperand.LeftOperand; //Can be null -> () - X - BranchA
                            var b = classifier.LeftOperand.RightOperand; //Can be null -> () - X - BranchB

                            switch (classifier.LeftOperand.ClassifieType)
                            {
                                case TypeEnum.And: // A + B
                                    if (a.ClassifieType == TypeEnum.Diamond)
                                        this.InsertClassifier(a);
                                    else
                                        this.AddClassifier(a);

                                    if (b.ClassifieType == TypeEnum.Diamond)
                                        this.InsertClassifier(b);
                                    else
                                        this.AddClassifier(b);
                                    break;
                                case TypeEnum.Or: // B1 A + B2 B
                                    var branch = new Branch(WorldNumber, WorldParentBranch, this);
                                    if (a.ClassifieType == TypeEnum.Diamond)
                                        branch.InsertClassifier(a);
                                    else
                                        branch.AddClassifier(a);
                                    this.AddBranch(branch, classifiersArray, classifier);

                                    branch = new Branch(WorldNumber, WorldParentBranch, this);
                                    if (b.ClassifieType == TypeEnum.Diamond)
                                        branch.InsertClassifier(b);
                                    else
                                        branch.AddClassifier(b);
                                    this.AddBranch(branch, classifiersArray, classifier);
                                    breakLoop = true;
                                    break;
                                case TypeEnum.Arrow: // B1 A + B2 ~B
                                    branch = new Branch(WorldNumber, WorldParentBranch, this);
                                    var not = Classifier.CreateClassifier(TypeEnum.Not);
                                    not.LeftOperand = a;
                                    if (a.ClassifieType == TypeEnum.Diamond)
                                        branch.InsertClassifier(not);
                                    else
                                        branch.AddClassifier(not);
                                    this.AddBranch(branch, classifiersArray, classifier);

                                    branch = new Branch(WorldNumber, WorldParentBranch, this);
                                    if (b.ClassifieType == TypeEnum.Diamond)
                                        branch.InsertClassifier(b);
                                    else
                                        branch.AddClassifier(b);
                                    this.AddBranch(branch, classifiersArray, classifier);
                                    breakLoop = true;
                                    break;
                                case TypeEnum.DoubleArrow:
                                    break;
                                case TypeEnum.Bracket:
                                case TypeEnum.Not:
                                case TypeEnum.Diamond:
                                case TypeEnum.Square:
                                    this.AddClassifier(classifier.LeftOperand);
                                    break;
                                case TypeEnum.Variable:
                                    if (a.ClassifieType == TypeEnum.Diamond)
                                        this.InsertClassifier(a);
                                    else
                                        this.AddClassifier(a);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case TypeEnum.Diamond:
                            if (ProcessedDiamondClassifiers.ContainsKey(classifier))
                            {
                                if (!ProcessedDiamondClassifiers[classifier])
                                    ProcessedDiamondClassifiers[classifier] = true;
                                else
                                {
                                    Console.WriteLine("Not exapnding due to possible infinity loop");
                                    continue;
                                }
                            }
                            else
                                ProcessedDiamondClassifiers[classifier] = false;

                            this.DiamondClassifiers.Add(classifier);
                            break;
                        case TypeEnum.Square:
                            this.SquaredClassifiers.Add(classifier);
                            if (Program.EquationType == EquationType.K || Program.EquationType == EquationType.K4) // K stops here
                                continue;

                            Console.WriteLine("Reflective Property");
                            this.AddClassifier(classifier.LeftOperand); //Reflectivity is is in everycase

                            if (Program.EquationType == EquationType.B || Program.EquationType == EquationType.S5) // Both have Symmetry
                            {
                                var rootBranch = WorldParentBranch;
                                if(rootBranch != null)
                                {
                                    if(rootBranch.WorldNumber != WorldNumber)
                                    {
                                        Console.WriteLine("Symmetry Property: Adding to Branch " + rootBranch.BranchNumber + " World " + rootBranch.WorldNumber);
                                        rootBranch.AddClassifier(classifier.LeftOperand);
                                        var par = rootBranch.WorldParentBranch;
                                        
                                        while (par!= null)
                                        {
                                            Console.WriteLine("Symmetry Property: Adding to Branch " + par.BranchNumber + " World " + par.WorldNumber);
                                            par.AddClassifier(classifier.LeftOperand);
                                            branchesToProcess.Add(par);
                                            par = par.WorldParentBranch;
                                        }
                                        branchesToProcess.AddRange(new List<Branch>() { rootBranch });
                                        if (Program.EquationType == EquationType.S5)
                                        {
                                            if (!classifier.ExistsEqual(rootBranch.ProcessedClassifiers) && !classifier.ExistsEqual(rootBranch.SquaredClassifiers))
                                            {
                                                //Console.WriteLine("Copying square classifier to Branch " + rootBranch.BranchNumber + " World " + rootBranch.WorldNumber);
                                                //classifier.PrintPretty("", false, false, false);
                                                rootBranch.SquaredClassifiers.Add(classifier);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            break;
                        case TypeEnum.And:
                        case TypeEnum.Or:
                        case TypeEnum.Arrow:
                        case TypeEnum.DoubleArrow:
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

            if (this.Branches.Count == 0)
            {
                foreach (var item in DiamondClassifiers)
                {
                    World w = new World(++Program._worldCount, this.BranchNumber);
                    var tbranch = new Branch(w.Number, this, this, w);
                    Program._worldBranches.Add(tbranch);

                    //this.World.Worlds.Add(w);

                    Console.WriteLine();
                    Console.WriteLine("Processing Diamond from " + this.BranchNumber + " World " + this.WorldNumber);

                    Console.WriteLine("Classifier picked =");
                    item.PrintPretty("", false, true, false);
                    tbranch.AddClassifier(item.LeftOperand);
                    this.AddBranch(tbranch, this.Classifiers.ToArray(), null);
                    this.WorldBranches.Add(tbranch);

                    foreach (var sq in SquaredClassifiers)
                    {
                        ProcSq(tbranch, sq);
                    }
                }
            }
            else
            {
                // Copy squared equation forward to branches
                foreach (var item in SquaredClassifiers)
                {
                    foreach (var branch in Branches)
                        ProcSq(branch, item);
                        
                }

                foreach (var b in Branches)
                {
                    b.DiamondClassifiers.AddRange(DiamondClassifiers);
                    //Console.WriteLine("Copying Dimaond forward");
                }
            }
            ProcessedClassifiers.AddRange(SquaredClassifiers);
            SquaredClassifiers.Clear();
            DiamondClassifiers.Clear();
            branchesToProcess.AddRange(this.Branches);
        }

        private void ProcSq(Branch branch, Classifier item)
        {
            var classifier = item;
            if (branch.WorldNumber != WorldNumber) // Open the equation if new world
            {
                if (item.ExistsEqual(branch.ProcessedClassifiers))
                {
                    //Console.WriteLine("Not expanding square classifer to Branch " + branch.BranchNumber + "and World " + branch.WorldNumber);
                    //item.PrintPretty("", false, false, false);
                    return;
                }
                Console.WriteLine();
                Console.WriteLine("Expanding square classifier from Branch " + this.BranchNumber + " World " + this.WorldNumber);
                Console.WriteLine("Classifier picked =");
                item.PrintPretty("", false, false, false);
                Console.WriteLine("Converted to ->");
                item.LeftOperand.PrintPretty("", false, false, false);
                Console.WriteLine("Expanded to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);

                branch.AddClassifier(item.LeftOperand, false);
                if (Program.EquationType == EquationType.S4 || Program.EquationType == EquationType.S5 || Program.EquationType == EquationType.K4)
                {
                    if (!item.ExistsEqual(branch.ProcessedClassifiers) && !item.ExistsEqual(branch.SquaredClassifiers))
                    {
                        //Console.WriteLine("Copying square classifier to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);
                        //classifier.PrintPretty("", false, false, false);
                        branch.SquaredClassifiers.Add(item);
                    }
                }
                return;
            }

            //Console.WriteLine("Carried the following classifier to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);
            //classifier.PrintPretty("", false, false, false);
            if (!item.ExistsEqual(branch.ProcessedClassifiers) &&  !item.ExistsEqual(branch.SquaredClassifiers))
            {
                //Console.WriteLine("Copying square classifier to Branch " + branch.BranchNumber + " World " + branch.WorldNumber);
                //classifier.PrintPretty("", false, false, false);
                branch.SquaredClassifiers.Add(classifier);
            }
        }

        private void AddClassifier(Classifier classifier, bool print = true)
        {
            if (print)
            {
                Console.WriteLine("Converted to ->");
                classifier.PrintPretty("", false, false, false); 
            }
            this.Classifiers.Add(classifier);
        }

        private void InsertClassifier(Classifier classifier)
        {
            Console.WriteLine("Converted to ->");
            classifier.PrintPretty("", false, false, false);

            this.Classifiers.Insert(0, classifier);
        }

        private void AddBranch(Branch branch, Classifier[] classifiers, Classifier toExclude)
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
                    {
                        if (item.ClassifieType == TypeEnum.Square)
                        {
                            var sq = Classifier.CreateClassifier(TypeEnum.Square);
                            sq.LeftOperand = item.LeftOperand;
                            cl.Add(sq);
                            ProcessedClassifiers.Add(sq);
                        }
                        else if(item.ClassifieType == TypeEnum.Not && item.LeftOperand.ClassifieType == TypeEnum.Square)
                        {
                            var not = Classifier.CreateClassifier(TypeEnum.Not);
                            not.LeftOperand = item.LeftOperand;
                            cl.Add(not);
                            ProcessedClassifiers.Add(not);
                        }
                        else
                            cl.Add(item);
                    }
                }

                branch.Classifiers.AddRange(cl.ToArray()); 
            }


            this.Branches.Add(branch);
            Console.WriteLine();
        }

        private void AddVariable(char variable, bool status)
        {
            var value = 0;
            if (status)
                value = 1;
            else
                value = -1;

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

        }
    }
}
