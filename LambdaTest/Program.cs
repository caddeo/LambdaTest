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
            // also i am not sure why i am calling the expressions 'wildcards'... 
            Program program = new Program();
            program.Run();

            // TODO: Only allow left hand expressions for now 
            // TODO: If it's a right hand expression, flip the wildcard
        }

        public void Run()
        {
            var repository = new PersonRepository(new Person("Patrick", new DateTime(1993, 11, 23)), new Person("Tobias", new DateTime(2004, 12, 1)));

            var input = "";

            Console.WriteLine(@"Find a person from age (ex. > 12) supports '>', '<' and '=='
    *Current version only allows one wildcard
    *Future versions will also allow to search on multiply parameters other than age
    *Write 'q' to exit");

            do
            {
                input = Console.ReadLine();

                if (input.ToLower().Equals("clear"))
                {
                    Console.Clear();
                    continue;
                }

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                var query = GetSearchQuery(input);

                if (query == null)
                {
                    Console.WriteLine("Please write a valid query");
                    continue;
                }

                var persons = GetPersonsFromRepository(query.Item2, query.Item1, repository);

                if (persons == null || persons.Count == 0)
                {
                    Console.WriteLine("Found 0");
                    continue;
                }

                Console.WriteLine($"Found ({persons.Count}) (age {query.Item2} {query.Item1}):");

                foreach (Person person in persons)
                {
                    var text = $"Person: {person.Name}, Birthday: {person.Birthday.Value.ToString("dd-MM-yyyy")}, Age: {person.Age}";
                    Console.WriteLine(text);
                }
            } while (input != "q");


            Console.WriteLine("Enter any key to close program");
            Console.ReadKey();
        }

        // This method should be split up
        public Tuple<string, char> GetSearchQuery(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return null;
            }

            // TODO: Split search query up in groups
            // i.e. age > 10 && age < 30 
            // or age > 10 || birthday = 23-11-93 

            var inputSplit = input.Split(' ');

            // See if the first is a string, get the string group then (i.e 'age') 
            // then check if the next is a searchParameter
            // then check if the third is a value

            char[] operators = { '>', '<', '=' };


            // index of wildcard and wildcard
            var searchOperatorsFound = new Dictionary<int, char>();

            for(int i=0; i<inputSplit.Length; i++)
            {
                var current = inputSplit[i];

                if(current.Length > 1)
                {
                    continue;
                }

                var searchOperator = ' ';
                char.TryParse(current, out searchOperator);

                if(operators.Contains(searchOperator))
                {
                    searchOperatorsFound.Add(i, searchOperator);
                }
            }

            if(searchOperatorsFound.Count == 0)
            {
                return null;
            }

            var firstOperator = searchOperatorsFound.First();

            if(inputSplit.Length-1 >= firstOperator.Key + 1)
            {
                var searchParamter = inputSplit[firstOperator.Key + 1];
                var operatorSearch = firstOperator.Value;

                return new Tuple<string, char>(searchParamter, operatorSearch);
            }
            else if(inputSplit.Length-1 > 0)
            {
                var searchParamter = inputSplit[firstOperator.Key - 1];
                var operatorSearch = firstOperator.Value;

                return new Tuple<string, char>(searchParamter, operatorSearch);
            }

            return null;
        }

        public List<Person> GetPersonsFromRepository(char searchOperator, string search, PersonRepository repo)
        {
            var ageSearch = 0;

            bool result = int.TryParse(search, out ageSearch);

            if(result == false || ageSearch < 0)
            {
                return null;
            }

            Expression<Func<Person, bool>> lambda = x => x.Age > ageSearch;

            switch(searchOperator)
            {
                case '>':
                    return repo.GetPersons(x => x.Age > ageSearch);
                case '<':
                    return repo.GetPersons(x => x.Age < ageSearch);
                case '=':
                    return repo.GetPersons(x => x.Age == ageSearch);
            }

            return null;
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
            if(this.Birthday == null && this.Birthday.HasValue)
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
}
