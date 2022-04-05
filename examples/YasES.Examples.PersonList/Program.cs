using System;
using System.Linq;
using YasES.Core;
using YasES.Examples.PersonList.Users;
using YasES.Examples.PersonList.Users.Projections.ActiveUsers;

namespace YasES.Examples.PersonList
{
    class Program
    {
        private static UsersContext Context = default!;
        private static UserList UserList = default!;

        static void Main(string[] args)
        {
            using IEventStore eventStore = EventStoreBuilder.Init()
                .UseInMemoryPersistance()
                //.UseSqlitePersistance("DataSource=./users.sqlite")
                .Build();

            Context = new UsersContext(eventStore.Events);
            UserList = new UserList(eventStore.Events);

            InitialState();
            CreateFirstUsers();
            DeleteSimon();
            AddNewUsers();
            UpdateAndPrintProjection();
            Guid sandra = RenameSandra();
            RenameSandraAgain(sandra);
        }

        private static void InitialState()
        {
            Console.WriteLine("Initial State");
            UpdateAndPrintProjection();
        }

        private static void CreateFirstUsers()
        {
            Console.WriteLine("Add initial users");
            Context.CreateUser("Admin");
            Context.CreateUser("Simon");
            Context.CreateUser("Joe");

            UpdateAndPrintProjection();
        }

        private static void DeleteSimon()
        {
            Console.WriteLine("Delete Simon");
            Context.DeleteUser(UserList.CurrentUsers.Single(p => p.Name == "Simon").Id);
            UpdateAndPrintProjection();
        }

        private static void AddNewUsers()
        {
            Console.WriteLine("Add two more users");
            Context.CreateUser("Kelly");
            Context.CreateUser("Sandra");
        }

        private static Guid RenameSandra()
        {
            // the rename cooldown is 5 minutes - so we just cheat now - move the clock
            // 10 minutes into the future for demo purposes.
            SystemClock.ResolveUtcNow = () => DateTime.UtcNow.AddMinutes(10);

            Console.WriteLine("Try rename Sandra to Simon");
            Guid sandra = UserList.CurrentUsers.Single(p => p.Name == "Sandra").Id;
            try
            {
                // lets try to rename sandra to simon - which will fail
                Context.RenameUser(sandra, "Simon");
                Console.WriteLine($"\t\tWait - this is a bug");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\tExpected error: user name in use, message: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Try rename Sandra to Lani");
            Context.RenameUser(sandra, "Lani");
            UpdateAndPrintProjection();
            Console.WriteLine($"\tSandra exists: {UserList.CurrentUsers.Any(p => p.Name == "Sandra")}");
            Console.WriteLine($"\tLani exists: {UserList.CurrentUsers.Any(p => p.Name == "Lani")}");
            Console.WriteLine();
            return sandra;
        }

        private static void UpdateAndPrintProjection()
        {
            int oldUserCount = UserList.CurrentUsers.Count;
            UserList.Update();
            int newUserCount = UserList.CurrentUsers.Count;
            Console.WriteLine($"\tUser Count changed from {oldUserCount} to {newUserCount}");
            Console.WriteLine();
        }

        private static void RenameSandraAgain(Guid sandra)
        {
            Console.WriteLine("Try rename Sandra");
            try
            {
                // lets try to rename sandra to simon - which will fail
                Context.RenameUser(sandra, "Jess");
                Console.WriteLine($"\t\tWait - this is a bug");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\tExpected error: cooldown, message: {ex.Message}");
            }
        }
    }
}
