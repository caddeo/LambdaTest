using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{
    class Program
    {
        static void Main(string[] args)
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

                if(input.ToLower().Equals("clear"))
                {
                    Console.Clear();
                    continue;
                }

                if(string.IsNullOrEmpty(input))
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

                if (persons == null)
                {
                    Console.WriteLine("Found 0");
                    continue;
                }

                Console.WriteLine($"Found ({persons.Count}):");

                foreach (Person person in persons)
                {
                    var text = $"Person: {person.Name}, Birthday: {person.Birthday.Value.ToString("dd-MM-yyyy")}";
                    Console.WriteLine(text);
                }
            } while (input != "q");


            Console.WriteLine("Enter any key to close program");
            Console.ReadKey();
        }

        // This method should be split up
        public static Tuple<string, char> GetSearchQuery(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return null;
            }

            var inputSplit = input.Split(' ');

            char[] wildCards = { '>', '<', '=' };

            // index of wildcard and wildcard
            var wildcardsFound = new Dictionary<int, char>();

            for(int i=0; i<inputSplit.Length; i++)
            {
                var current = inputSplit[i];

                if(current.Length > 1)
                {
                    continue;
                }

                var wildcard = ' ';
                char.TryParse(current, out wildcard);

                if(wildCards.Contains(wildcard))
                {
                    wildcardsFound.Add(i, wildcard);
                }
            }

            if(wildcardsFound.Count == 0)
            {
                return null;
            };

            var firstWildcard = wildcardsFound.First();

            if(inputSplit.Length-1 >= firstWildcard.Key + 1)
            {
                var searchParamter = inputSplit[firstWildcard.Key + 1];
                var wildcardSearch = firstWildcard.Value;

                return new Tuple<string, char>(searchParamter, wildcardSearch);
            }
            else if(inputSplit.Length-1 > 0)
            {
                var searchParamter = inputSplit[firstWildcard.Key - 1];
                var wildcardSearch = firstWildcard.Value;

                return new Tuple<string, char>(searchParamter, wildcardSearch);
            }

            return null;
        }

        public static List<Person> GetPersonsFromRepository(char wildcard, string search, PersonRepository repo)
        {
            var ageSearch = -1;
            int.TryParse(search, out ageSearch);

            if(ageSearch < 0)
            {
                return null;
            }

            switch(wildcard)
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

        public PersonRepository(params Person[] persons)
        {
            Persons = new List<Person>();

            foreach(Person person in persons)
            {
                this.AddPerson(person);
            }
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

        public void SetList(List<Person> persons)
        {
            Persons = persons;
        }
    }
}
