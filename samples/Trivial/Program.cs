using System;
using System.Diagnostics;
using Divan;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Trivial
{
    /// <summary>
    /// A trivial example of using Divan. Requires a running CouchDB on localhost!
    /// 
    /// Run using:
    ///
    ///     Trivial.exe <host> <port>
    ///
    /// </summary>
    class Program
    {
        static void Main(string[] args) {
            string host = "localhost";
            int port = 5984;

            // Lets you see all HTTP requests made by Divan
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Trivial parse of args to get host and port
            switch (args.Length) {
                case 0:
                    Console.WriteLine("Using localhost:5984");
                    break;
                case 1:
                    Console.WriteLine("Using " + args[0] + ":5984");
                    host = args[0];
                    break;
                case 2:
                    Console.WriteLine("Using " + args[0] + ":" + args[1]);
                    host = args[0];
                    port = int.Parse(args[1]);
                    break;
            }

            // Get a server for default couch port 5984 on localhost
            var server = new CouchServer(host, port);
            Console.WriteLine("Created a CouchServer");

            // Get (creates it if needed) a CouchDB database.
            var db = server.GetDatabase("trivial");
            Console.WriteLine("Created database 'trivial'");

            // Create and save 10 Cars with automatically allocated Ids by Couch
            Car car = null;
            for (int i = 0; i < 10; i++)
            {
                car = new Car("Saab", "93", 170);
                db.SaveDocument(car);
            }
            Console.WriteLine("Saved 10 Cars with 170 hps each.");

            // Modify the last Car we saved...
            car.HorsePowers = 400;

            // ...and save the change.
            // We could also have used WriteDocument if we knew it was an existing doc
            db.SaveDocument(car);
            Console.WriteLine("Modified last Car with id " + car.Id);

            // Load a Car by known id (we just pick it from car), the class to instantiate is given using generics (Car) 
            var sameCar = db.GetDocument<Car>(car.Id);
            Console.WriteLine("Loaded last Car " + sameCar.Make + " " + sameCar.Model + " now with " + sameCar.HorsePowers + "hps.");

            // Load all Cars. QueryAllDocuments() gives us a CouchQuery which we can configure. We tell it to IncludeDocuments()
            // which means that we will get back not only ids but the actual documents too. GetResult() will perform the
            // HTTP request to CouchDB and return a CouchGenericViewResult which we in turn can ask to produce objects from JSON,
            // in this case we pick out the actual documents and instantiate them as instances of the class Car.
            var cars = db.QueryAllDocuments().IncludeDocuments().GetResult().Documents<Car>();
            Console.WriteLine("Loaded all Cars: " + cars.Count);

            // Delete all Cars one by one
            foreach (var eachCar in cars)
            {
                db.DeleteDocument(eachCar);
                Console.WriteLine("Deleted car with id " + eachCar.Id);
            }

            // Delete the db itself
            db.Delete();
            Console.WriteLine("Deleted database");
        }

        /// <summary>
        /// The simplest way to deal with domain objects is to subclass CouchDocument
        /// and inherit members Id and Rev. You will need to implement WriteJson/ReadJson.
        /// </summary>
        private class Car : CouchDocument
        {
            public string Make;
            public string Model;
            public int HorsePowers;

            public Car()
            {
                // This constructor is needed by Divan
            }

            public Car(string make, string model, int hps)
            {
                Make = make;
                Model = model;
                HorsePowers = hps;
            }
            #region CouchDocument Members

            public override void WriteJson(JsonWriter writer)
            {
                // This will write id and rev
                base.WriteJson(writer);

                writer.WritePropertyName("Make");
                writer.WriteValue(Make);
                writer.WritePropertyName("Model");
                writer.WriteValue(Model);
                writer.WritePropertyName("Hps");
                writer.WriteValue(HorsePowers);
            }

            public override void ReadJson(JObject obj)
            {
                // This will read id and rev
                base.ReadJson(obj);

                Make = obj["Make"].Value<string>();
                Model = obj["Model"].Value<string>();
                HorsePowers = obj["Hps"].Value<int>();
            }

            #endregion
        }
    }
}
