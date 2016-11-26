using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // The search algorithm could be improved a lot
            // but it was more to try out funcs and lambdas in methods
            Program program = new Program();
            program.Run();
        }

        public void Run()
        {
            var repository = new PersonRepository(new Person("Patrick", new DateTime(1993, 11, 23)), new Person("Tobias", new DateTime(2004, 12, 1)));

            var input = "";

            Console.WriteLine("\nFind a person by property (Age, Birthday, Name)\n" +
                              "ex. Age > 10 23-11-1993 > Birthday\n" +
                              "or even Age > 10 & 23-11-1993 > Birthday\n" +
                              "Valid operators: (>, <, =)\n" +
                              "Valid comparators: (& (AND), | (OR))");

            do
            {
                input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.ToLower().Equals("clear"))
                {
                    Console.Clear();
                    continue;
                }

                var query = GetSearchQuery(input);

                if (query == null)
                {
                    Console.WriteLine("Please write a valid query");
                    continue;
                }

                var persons = GetPersonsFromRepository(query, repository);

                if (persons == null || persons.Count == 0)
                {
                    Console.WriteLine("Found none - try again");
                    continue;
                }

                Console.WriteLine($"Found ({persons.Count}):");
                foreach (var person in persons)
                {
                    Console.WriteLine($"Name: {person.Name}, Birthday: {person.Birthday:dd-MM-yyyy}, Age: {person.Age}");
                }

            } while (input != "q");


            Console.WriteLine("Enter any key to close program");
            Console.ReadKey();
        }

        // This method should be split up
        public List<Tuple<Search, Comparator>> GetSearchQuery(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return null;
            }

            // TODO: Split search query up in groups
            // i.e. age > 10 & age < 30
            // or age > 10 | birthday = 23-11-93 

            var inputSplit = input.Split(' ');

            var groups = new List<Tuple<string, string, string>>();
            var comparators = new Dictionary<int, char>();

            for(int i=0; i<=inputSplit.Length; i+=4)
            {
                if(inputSplit.Length > i+2)
                {
                    groups.Add(new Tuple<string, string, string>(inputSplit[i], inputSplit[i + 1], inputSplit[i + 2]));
                }

                // is there a comperator? (bit operators) 
                if(inputSplit.Length > i+3 && (inputSplit[i + 3] == "&" || inputSplit[i + 3] == "|"))
                {
                    // TODO: locking if multithreading
                    comparators.Add(groups.Count - 1, char.Parse(inputSplit[i + 3]));
                }
            }

            char[] operators = { '>', '<', '=' };

            // because of threading, we can't remove it from groups, so we create a new 
            var validGroups =  new List<Tuple<string, string, string>>();

            foreach (var group in groups)
            {
                if((IsPropertyOfPerson(group.Item1) == true || IsPropertyOfPerson(group.Item3) == true))
                {
                    var operatorChar = ' ';
                    var isChar = char.TryParse(group.Item2, out operatorChar);

                    if (isChar == true && operators.Contains(operatorChar) == true)
                    {
                        validGroups.Add(group);
                        continue;
                    }
                }

                comparators.Remove(groups.IndexOf(group));
            }

            if(validGroups.Count == 0)
            {
                return null;
            }

            // validate the comparator if it's the same size as valid groups
            // then the last comperator is invalid 
            if (validGroups.Count <= comparators.Count)
            {
                comparators.Remove(comparators.Count - 1);
            }

            // List of Searches, and the comperator for the next 
            var searches = new List<Tuple<Search, Comparator>>();
            
            for(int i=0; i<validGroups.Count; i++)
            {
                var group = validGroups[i];
                var comparator = (i >= comparators.Count-1) ? null : new Comparator(comparators[i], i, i + 1);
                Search search = null;

                // Age < 10 
                if (IsPropertyOfPerson(group.Item1))
                {
                    search = new Search(group.Item1, char.Parse(group.Item2), group.Item3);
                }

                // 10 > Age
                else
                {
                    search = new Search(group.Item3, char.Parse(group.Item2),  group.Item1);
                }

                searches.Add(new Tuple<Search, Comparator>(search, comparator));
            }

            if (searches.Count > 0)
            {
                return searches;
            }

            return null;
        }

        private bool IsPropertyOfPerson(string value)
        {
            var personProperties = typeof(Person).GetProperties();

            // evaluates if the value is a property name, (with both to lowercase)
            return personProperties.Any(property => string.Equals(value, property.Name, StringComparison.CurrentCultureIgnoreCase));
        }

        public Person GetPersonFromRepository(string searchProperty, char searchOperator, string searchCondition, PersonRepository repo)
        {
            var search = new Search(searchProperty, searchOperator, searchCondition);
            var personTuple = new Tuple<Search, Comparator>(search, null);

            var personParameter = new List<Tuple<Search, Comparator>>() { personTuple };

            // parse userinput first (int and datetime) 

            return GetPersonsFromRepository(personParameter, repo).FirstOrDefault();
        }

        // Make this mess into multiply methods 
        // mistake to introduce classes search and comparator since they're not based on a interface
        public List<Person> GetPersonsFromRepository(List<Tuple<Search, Comparator>> queries, PersonRepository repo)
        {
            var expressionTree = new List<Tuple<Expression, Comparator>>();

            // this is the parameter like "person => person.Age = 10"
            var parameterExpression = Expression.Parameter(typeof(Person), "person");

            // Create each expression tree 
            foreach (var query in queries)
            {
                // https://msdn.microsoft.com/en-us/library/bb882637(v=vs.110).aspx
                // First convert the values 

                var search = query.Item1;
                
                // Find parameter first (we already filtered this) 
                var parameterType =
                    typeof(Person).GetProperties()
                        .FirstOrDefault(
                            property => string.Equals(search.Property, property.Name, StringComparison.CurrentCultureIgnoreCase));

                // Handle left hand (ex. "Name", "Age", "Birthday")
                Expression left = Expression.Property(parameterExpression, parameterType);
                Expression right = null;

                // Handle right hand (ex. 10, "Patrick", '23-11-1993')
                // person => person.Age == 10 
                switch (parameterType.Name)
                {
                    case "Name":
                        right = Expression.Constant(search.Condition, typeof(string));
                        break;
                    case "Birthday":
                        // TODO: Try and parse this earlier so we don't get an error
                        right = Expression.Constant(DateTime.Parse(search.Condition), typeof(DateTime));
                        break;
                    case "Age":
                        // TODO: Try and parse this earlier so we don't get an error
                        right = Expression.Constant(int.Parse(search.Condition), typeof(int));
                        break;
                }

                Expression expressionOperator = null;

                // Handle operators
                switch (search.Operator)
                {
                    case '>':
                        expressionOperator = Expression.GreaterThan(left, right);
                        break;
                    case '<':
                        expressionOperator = Expression.LessThan(left, right);
                        break;
                    case '=':
                        expressionOperator = Expression.Equal(left, right);
                        break;
                }

                var comparator = query.Item2;
                expressionTree.Add(new Tuple<Expression, Comparator>(expressionOperator, comparator));
            }

           var expressions = new List<Expression<Func<Person, bool>>>();

            // If they're 'all null' then there's only one expression 
            if (expressionTree.All(tree => tree.Item2 == null))
            {
                expressions = expressionTree.Select(
                    expression => Expression.Lambda<Func<Person, bool>>(expression.Item1, parameterExpression)).ToList();

                return repo.GetPersons(expressions.FirstOrDefault().Compile());
            }

            var expressionsTreesCombined = new List<Expression>();

            for (int i=0; i<expressionTree.Count; i++)
            {
                var pair = expressionTree[i];

                var expression = pair.Item1;
                var comparator = pair.Item2;

                // if it's zero, it was the last comparator
                if (comparator == null)
                {
                    continue;
                }

                if (comparator?.Type == '&')
                {
                    var nextExpression = expressionTree[i + 1].Item1;
                    expressionsTreesCombined.Add(Expression.And(expression, nextExpression));
                }
                else if (comparator?.Type == '|')
                {
                    var nextExpression = expressionTree[i + 1].Item1;
                    expressionsTreesCombined.Add(Expression.Or(expression, nextExpression));
                }
            }

            expressions = expressionsTreesCombined.Select(
                expression => Expression.Lambda<Func<Person, bool>>(expression, parameterExpression)).ToList();

            return repo.GetPersons(expressions.FirstOrDefault().Compile());

        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime? Birthday { get; set; }

        public Person(string name, DateTime? birthday)
        {
            this.Name = name;
            this.Birthday = birthday;
            CalculateAndSetAge();
        }

        private void CalculateAndSetAge()
        {
            if(this.Birthday == null)
            {
                return;
            }

            var now = DateTime.Now;

            var ageParsedFromYears = now.Year - this.Birthday.Value.Year;
            var age = (now.Month >= this.Birthday.Value.Month && now.Day >= this.Birthday.Value.Day) 
                ? ageParsedFromYears  : ageParsedFromYears - 1;
            this.Age = age;
        }
    }

    public class PersonRepository
    {
        public static List<Person> Persons { get; protected set; }

        public PersonRepository(List<Person> persons)
        {
            Persons = new List<Person>();

            foreach (Person person in persons)
            {
                this.AddPerson(person);
            }
        }

        public PersonRepository(params Person[] persons) : this(persons.ToList())
        {

        }

        public void AddPerson(Person person)
        {
            if(person != null)
            {
                Persons.Add(person);
            }
        }

        public List<Person> GetPersons(Func<Person, bool> query)
        {
            if(query == null)
            {
                return null;
            }

            return Persons.Where(query).ToList();
        }

        public Person GetPerson(Func<Person, bool> query)
        {
            return GetPersons(query).FirstOrDefault();
        }
    }

    public class Search
    {
        public string Property { get; protected set; }
        public char Operator { get; protected set; }
        public string Condition { get; protected set; }

        public Search(string property, char operatorChar, string condition)
        {
            this.Property = property;
            this.Operator = operatorChar;
            this.Condition = condition;
        }
    }

    public class Comparator
    {
        public char Type { get; protected set; }
        public int CompareFirstIndex { get; protected set; }
        public int CompareSecondIndex { get; protected set; }

        public Comparator(char type, int compareIndex, int compareWithIndex)
        {
            this.Type = type;
            this.CompareFirstIndex = compareIndex;
            this.CompareSecondIndex = compareWithIndex;
        }
    }
}