using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextAnalyzer
{
    class Classifier
    {
        private static int _index;
        private Classifier(TypeEnum classifier, char character)
        {
            this.ClassifieType = classifier;
            this.Character = character;
            ID = _index++;
        }

        public int ID { get; private set; }

        public TypeEnum ClassifieType { get; }

        public Classifier LeftOperand { get; set; }

        public Classifier RightOperand { get; set; }

        public char Character { get; set; }

        public static Classifier CreateClassifier(char character)
        {

            switch (character)
            {
                case '~':
                    return new Classifier(TypeEnum.Not, character);
                case '(':
                    return new Classifier(TypeEnum.Bracket, character);
                case '→':
                    return new Classifier(TypeEnum.Arrow, character);
                case 'Λ':
                    return new Classifier(TypeEnum.And, character);
                case 'V':
                    return new Classifier(TypeEnum.Or, character);
                case 'P':
                    return new Classifier(TypeEnum.P, character);
                case 'G':
                    return new Classifier(TypeEnum.G, character);
                case 'H':
                    return new Classifier(TypeEnum.H, character);
                case 'F':
                    return new Classifier(TypeEnum.F, character);
                default:
                    return new Classifier(TypeEnum.Variable, character);
            }

        }

        public static Classifier CreateClassifier(TypeEnum classifier)
        {
            switch (classifier)
            {
                case TypeEnum.Not:
                    return new Classifier(classifier, '~');
                case TypeEnum.Bracket:
                    return new Classifier(classifier, '(');
                case TypeEnum.And:
                    return new Classifier(classifier, 'Λ');
                case TypeEnum.Or:
                    return new Classifier(classifier, 'V');
                case TypeEnum.Arrow:
                    return new Classifier(classifier, '→');
                case TypeEnum.P:
                    return new Classifier(classifier, 'P');
                case TypeEnum.G:
                    return new Classifier(classifier, 'G');
                case TypeEnum.H:
                    return new Classifier(classifier, 'H');
                case TypeEnum.F:
                    return new Classifier(classifier, 'F');
                case TypeEnum.Variable:
                    throw new ArgumentException("User other method for variables");
                default:
                    throw new ArgumentException("User other method for variables");
            }

        }

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
                id = " " + ID;

            if (this.ClassifieType == TypeEnum.Variable)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (this.ClassifieType == TypeEnum.Bracket)
                Console.ForegroundColor = ConsoleColor.DarkCyan;
            else if (this.ClassifieType == TypeEnum.Arrow || this.ClassifieType == TypeEnum.And || this.ClassifieType == TypeEnum.Or || this.ClassifieType == TypeEnum.Not)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else
                Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(" " + Character + id);

            Console.ForegroundColor = ConsoleColor.Gray;

            var children = new List<Classifier>();
            if (this.LeftOperand != null)
                children.Add(this.LeftOperand);
            if (this.RightOperand != null)
                children.Add(this.RightOperand);

            for (int i = 0; i < children.Count; i++)
                children[i].PrintPretty(indent, i == children.Count - 1, withID & subID, subID);

        }


        internal bool ExistsEqual(List<Classifier> processedClassifiers)
        {
            bool ret = false;
            foreach (var item in processedClassifiers)
            {
                ret = Equate(this, item);
                if (ret)
                    break;
            }
            return ret;
        }

        private bool Equate(Classifier a, Classifier b)
        {
            if (b.ClassifieType == a.ClassifieType)
            {
                if (b.LeftOperand == null)
                    return true;
                else
                {
                    var eq = Equate(a.LeftOperand, b.LeftOperand);
                    if (!eq)
                        return false;
                    else
                    {
                        if (b.RightOperand == null)
                            return true;
                        else
                        {
                            return Equate(a.RightOperand, b.RightOperand);

                        }
                    }
                }
            }
            return false;
        }

    }
}