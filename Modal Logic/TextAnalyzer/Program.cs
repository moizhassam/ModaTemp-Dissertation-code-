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
        static bool _automated = false;
        static string _origEquation = "";
        static Stack<Classifier> _stack = new Stack<Classifier>();
        public static int _index = 0;
        public static int _worldCount = 0;
        public static List<Branch> _worldBranches = new List<Branch>();
        public static World W0 = new World(0, 0);

        private  static List<Branch> _branchesToProcess = new List<Branch>();
        public static EquationType EquationType { get; set; }
        public static bool CheckValidity { get; set; }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            IEnumerable<string> lines;
            ConsoleKeyInfo key;
            char keyChar = '1';
            string preEq = "";

            if (args.Length < 2)
            {
                if (File.Exists(@"./Equations.txt"))
                {
                    lines = File.ReadLines(@"./Equations.txt");

                    Console.WriteLine(lines.First());

                    Console.WriteLine("Type of system?");
                    Console.WriteLine("1. K");
                    Console.WriteLine("2. S4");
                    Console.WriteLine("3. S5");
                    Console.WriteLine("4. K4");

                    Console.WriteLine();
                    key = Console.ReadKey();
                    Console.WriteLine();

                    keyChar = key.KeyChar;

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
                }
                else
                {
                    lines = new List<string> { "((p Λ q) → r) → (p → (~q V r))" };
                }
            }
            else
            {
                _automated = true;
                keyChar = args[1][0];
                lines = new List<string> { args[0].Trim() };
                if (args.Length == 3)
                {
                    preEq = args[2];
                    CheckValidity = true;
                }
            }


            foreach (var line in lines)
            {
                switch (keyChar)
                {
                    case '1':
                        EquationType = EquationType.K;
                        break;
                    case '2':
                        EquationType = EquationType.S4;
                        break;
                    case '3':
                        EquationType = EquationType.S5;
                        break;
                    case '4':
                        EquationType = EquationType.K4;
                        break;
                    default:
                        return;
                }

                _equation = line;

                if (_equation.Trim().Length == 0)
                    continue;

                _origEquation = _equation;
                _equation = preEq + "(" + _equation + ")";
                Console.WriteLine();

                // Step 1
                var rootClassifier = Classifier.CreateClassifier(_equation[_index]);
                _index = -1;
                rootClassifier = ReadAndClassifyEquation(null); // skip first index which is ~
                rootClassifier.PrintPretty("", false, true, true);

                // Step 2
                Branch mainBranch = new Branch(0, null, null, W0);
                mainBranch.Classifiers.Add(rootClassifier);
                _worldBranches.Add(mainBranch);
                ProcessEquation(mainBranch);


                // Step 3
                CheckAnswer(mainBranch);

                if(!_automated)
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
                else if (_equation[_index] == '◊' || _equation[_index] == '□' || _equation[_index] == '~')
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
                        && (classifier.ClassifieType == TypeEnum.Diamond || classifier.ClassifieType == TypeEnum.Square))
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
                //Variable
                else
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
                    branch.Process(_branchesToProcess);
                }
            }
        }


        private static void CheckAnswer(Branch mainBranch)
        {
            Console.WriteLine();
            Console.WriteLine();

            var closed = ProcessLiterals(mainBranch, null);



            if (!_automated)
            {
                if (closed)
                {
                    if (CheckValidity) Console.WriteLine("The formula: " + _origEquation + " is Valid");
                    else Console.WriteLine("The formula: " + _origEquation + " is Unsatisfiable");
                }
                else
                {
                    if (CheckValidity) Console.WriteLine("The formula: " + _origEquation + " is In-Valid");
                    else Console.WriteLine("The formula: " + _origEquation + " is Satisfiable");
                } 
            }
            else
            {
                if (closed)
                {
                    if (CheckValidity) Console.WriteLine("A: Valid");
                    else Console.WriteLine("A: Unsatisfiable");
                }
                else
                {
                    if (CheckValidity) Console.WriteLine("A: In-Valid");
                    else Console.WriteLine("A: Satisfiable");
                }
            }


            Console.WriteLine();

        }

        private static bool ProcessLiterals(Branch branch, Dictionary<char, int> variableList)
        {
            if(variableList == null)
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

            if (branch.WorldBranches.Count == 0)
            {
                var closed = true;
                if (branch.Branches.Count == 0)
                    return false; //Previous check failed
                foreach (var b in branch.Branches)
                {
                    closed = ProcessLiterals(b, new Dictionary<char, int>(variableList));
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
                    closed = ProcessLiterals(world, null);
                    if (closed)
                        return true;
                }
                return false;
            }

        }
    }
}
