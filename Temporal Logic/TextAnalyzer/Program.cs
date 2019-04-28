using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextAnalyzer
{
    class Program
    {

        static string _equation = "";
        static string _origEquation = "";
        static Stack<Classifier> _stack = new Stack<Classifier>();
        public static int _index = 0;
        public static int _worldCount = 0;
        public static Dictionary<char, int> _globalVariablList = new Dictionary<char, int>();
        public static List<Branch> _worldBranches = new List<Branch>();
        //public static World W0 = new World(0, 0);

        public static List<Branch> _branchesToProcess = new List<Branch>();
        public static EquationType EquationType { get; set; }
        public static bool CheckValidity { get; set; }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            IEnumerable<string> lines;
            if (File.Exists(@"./Equations.txt"))
            {
                lines = File.ReadLines(@"./Equations.txt"); 
            }
            else
            {
                lines = new List<string> { "((p Λ q) → r) → (p → (~q V r))" };
            }

            foreach (var line in lines)
            {
                
                _equation = line;

                if (_equation.Trim().Length == 0)
                    continue;

                Console.WriteLine(_equation);
                Console.WriteLine("Type of System?");
                Console.WriteLine("1. Kt");
                Console.WriteLine("2. Tt");
                Console.WriteLine("3. K4t");
                Console.WriteLine();
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();
                switch (key.KeyChar)
                {
                    case '1':
                        EquationType = EquationType.K;
                        break;
                    case '2':
                        EquationType = EquationType.M;
                        break;
                    case '3':
                        EquationType = EquationType.K4T;
                        break;
                    default:
                        return;
                }

                string preEq = "";

                Console.WriteLine();
                Console.WriteLine("Evaluate formula for");
                Console.WriteLine("1. Validty");
                Console.WriteLine("2. Satisfiable");
                Console.WriteLine();
                key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '1':
                        preEq = "~";
                        CheckValidity = true;
                        break;
                    default:
                        CheckValidity = false;
                        break;
                }
                Console.WriteLine();

                _origEquation = _equation;
                _equation = preEq + "(" + _equation + ")";
                Console.WriteLine();

                // Step 1
                var rootClassifier = Classifier.CreateClassifier(_equation[_index]);
                _index = -1;
                rootClassifier = ReadAndClassifyEquation(null); // skip first index which is ~
                rootClassifier.PrintPretty("", false, false, true);

                // Step 2
                Branch mainBranch = new Branch(0);
                mainBranch.Classifiers.Add(rootClassifier);
                _worldBranches.Add(mainBranch);
                ProcessEquation(mainBranch);


                // Step 3
                Console.WriteLine();
                Console.WriteLine("Checking Answer");
                CheckAnswer(mainBranch);

                Console.ReadKey();
                break;
            }
        }
        
        public static Classifier ReadAndClassifyEquation(Classifier rootClassifier)
        {
            _index++;
            for (int i = _index; i < _equation.Length; i++)
            {
                if (_equation[i] == ' ')
                    continue;

                _index = i;

                if (_equation[_index] == '(')
                {
                    var classifier = Classifier.CreateClassifier(_equation[_index]);
                    _stack.Push(classifier);
                    var returnedClassifier = ReadAndClassifyEquation(classifier);
                    classifier.LeftOperand = returnedClassifier;

                    while (_stack.Count != 0 && _stack.Peek() == classifier)
                    {
                        returnedClassifier = ReadAndClassifyEquation(classifier);
                        if (returnedClassifier == null)
                            break;
                        if (returnedClassifier.ClassifieType == TypeEnum.And
                        || returnedClassifier.ClassifieType == TypeEnum.Or
                        || returnedClassifier.ClassifieType == TypeEnum.Arrow)
                        {
                            returnedClassifier.LeftOperand = classifier.LeftOperand;
                            classifier.LeftOperand = returnedClassifier;
                        }
                    }
                    return classifier;
                }
                else if (_equation[_index] == ')')
                {
                    _stack.Pop();
                    return null;
                }
                else if (_equation[_index] == 'P' || _equation[_index] == 'G' || _equation[_index] == 'H' || _equation[_index] == 'F' || _equation[_index] == '~')
                {
                    var classifier = Classifier.CreateClassifier(_equation[_index]);
                    var returnedClassifier = ReadAndClassifyEquation(classifier);
                    if (returnedClassifier.ClassifieType == TypeEnum.And
                        || returnedClassifier.ClassifieType == TypeEnum.Or
                        || returnedClassifier.ClassifieType == TypeEnum.Arrow)
                    {
                        var tempClassifier = returnedClassifier.LeftOperand;
                        returnedClassifier.LeftOperand = classifier;
                        if (tempClassifier != null)
                            returnedClassifier.LeftOperand.LeftOperand = tempClassifier; //insert
                        return returnedClassifier;
                    }
                    else if(returnedClassifier.ClassifieType == classifier.ClassifieType
                        && (classifier.ClassifieType == TypeEnum.P || classifier.ClassifieType == TypeEnum.G || classifier.ClassifieType == TypeEnum.F || classifier.ClassifieType == TypeEnum.H))
                    {
                        classifier.LeftOperand = returnedClassifier.LeftOperand;
                        return classifier;
                    }
                    else
                    {
                        classifier.LeftOperand = returnedClassifier;
                        return classifier;
                    }
                }
                else if (_equation[_index] == 'V' || _equation[_index] == 'Λ' || _equation[_index] == '→')
                {
                    var classifier = Classifier.CreateClassifier(_equation[_index]);
                    classifier.RightOperand = ReadAndClassifyEquation(classifier);
                    return classifier;

                }
                else //Variable
                {
                    var classifier = Classifier.CreateClassifier(_equation[_index]);
                    var returnedClassifier = ReadAndClassifyEquation(classifier);

                    if (returnedClassifier == null)
                        return classifier;
                    else if (returnedClassifier.ClassifieType == TypeEnum.And
                        || returnedClassifier.ClassifieType == TypeEnum.Or
                        || returnedClassifier.ClassifieType == TypeEnum.Arrow)
                    {
                        var tempClassifier = returnedClassifier.LeftOperand;
                        returnedClassifier.LeftOperand = classifier;
                        if (tempClassifier != null)
                            returnedClassifier.LeftOperand.LeftOperand = tempClassifier; //insert
                        return returnedClassifier;
                    }
                }
            }
            return rootClassifier;
        }


        private static void ProcessEquation(Branch mainBranch)
        {
            _branchesToProcess.Add(mainBranch);

            while (_branchesToProcess.Count > 0)
            {
                var copy_list = _branchesToProcess.ToArray(); //Cannot change the same list iterating over hence creating a copy
                _branchesToProcess.Clear();
                foreach (var branch in copy_list)
                {
                    branch.Process();
                }
            }
        }


        private static void CheckAnswer(Branch mainBranch)
        {
            Console.WriteLine();
            Console.WriteLine();

            var closed = ProcessVariables(mainBranch, null);

         
            if(closed)
            {
                if (CheckValidity) Console.WriteLine("The formula: " + _origEquation + " is Valid");
                else Console.WriteLine("The formula: " + _origEquation + " is Unsatisfiable");
            }
            else
            {
                if (CheckValidity) Console.WriteLine("The formula: " + _origEquation + " is In-Valid");
                else Console.WriteLine("The formula: " + _origEquation + " is Satisfiable");
            }


            Console.WriteLine();

        }

        static Dictionary<Branch, bool> branchesProcessedForAnswer = new Dictionary<Branch, bool>();

        private static bool ProcessVariables(Branch branch, Dictionary<char, int> variableList)
        {
            if (variableList == null)
                variableList = branch.VariableList;
            else
            {
                foreach (var item in branch.VariableList)
                {
                    if (variableList.TryGetValue(item.Key, out int val))
                    {
                        if (val == 0)
                            variableList[item.Key] = 0;
                        else if (variableList[item.Key] + item.Value == 0)
                            variableList[item.Key] = 0;
                        else
                            continue;
                    }
                    else
                        variableList[item.Key] = item.Value;
                }
            }

            if (variableList.Any(x => x.Value == 0))
            {
                return true;
            }

            if (branchesProcessedForAnswer.ContainsKey(branch))
            {
                //Console.WriteLine("Circular dependency detected for branch " + branch.BranchNumber + " and World " + branch.WorldNumber);
                return false;
            }
            else
                branchesProcessedForAnswer.Add(branch, true);

            if (branch.WorldBranches.Count == 0)
            {
                var closed = true;
                if (branch.ChildBranches.Count == 0)
                    return false; //Previous check failed
                foreach (var b in branch.ChildBranches)
                {
                    if (b.WorldNumber != branch.WorldNumber)
                    {
                        closed = false;
                        continue;
                    }
                    closed = ProcessVariables(b, new Dictionary<char, int>(variableList));
                    if (!closed)
                        return false;
                }
                return closed;
            }
            else
            {
                bool closed = false;
                foreach (var world in branch.WorldBranches)
                {
                    closed = ProcessVariables(world, null);
                    if (closed)
                        return true;
                }
                return false;
            }

        }
    }
}
