using System;
using System.Diagnostics;
using System.Linq;
using Divan;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Trivial
{
    /// <summary>
    /// A trivial example of using Divan. Requires a running CouchDB on localhost or some other server.
    /// 
    /// Run using:
    ///
    ///     Trivial.exe <host> <port>
    ///
    /// If you leave out arguments it will use localhost:5984.
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
                    Console.WriteLine("Using " + host + ":" + port);
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

            // Get a server instance. It only holds host, port and a string database prefix.
            // For non trivial usage of Divan you typically create your own subclass of CouchServer.
            var server = new CouchServer(host, port);

            /* This has issues with the windows build of couch db - something about file locking
            // a little bit of cleanup
            if (server.HasDatabase("trivial"))
                server.DeleteDatabase("trivial");
             */

            // Get (creates it if needed) a CouchDB database. This call will create the db in CouchDB
            // if it does not exist, create a CouchDatabase instance and then send Initialize() to it
            // before returning it. The base class CouchDatabase also has very little state, it knows
            // only the server that it belongs to and its own name.
            var db = server.GetDatabase("trivial");
            Console.WriteLine("Created database 'trivial'");

            // Create and save 10 Cars with automatically allocated Ids by CouchDB.
            // Divan stores ICouchDocuments and there are several ways you can go:
            //   - Create a subclass of CouchDocument (like Car is here).
            //   - Create a class that implements ICouchDocument.
            //   - Create a subclass of CouchJsonDocument.
            Car car = null;
            for (int i = 0; i < 10; i++)
            {
                car = new Car("Saab", "93", 170+i);
                db.SaveDocument(car);
            }
            Console.WriteLine("Saved 10 Cars with 170-180 hps each.");

            // Load all Cars as CouchJsonDocuments and print one of them
            Console.WriteLine("Here is the first of the Cars: \n\n" + db.GetAllDocuments().First().ToString());

            // Modify the last Car we saved...
            car.HorsePowers = 400;

            // ...and save the change.
            // We could also have used WriteDocument if we knew it was an existing doc
            db.SaveDocument(car);
            Console.WriteLine("Modified last Car with id " + car.Id);

            // Load a Car by known id (we just pick it from car), the class to instantiate is given using generics (Car) 
            var sameCar = db.GetDocument<Car>(car.Id);
            Console.WriteLine("Loaded last Car " + sameCar.Make + " " + sameCar.Model + " now with " + sameCar.HorsePowers + "hps.");

            // Load all Cars. If we dwelve into the GetAllDocuments() method we will see that
            // QueryAllDocuments() gives us a CouchQuery which we can configure. We tell it to IncludeDocuments()
            // which means that we will get back not only ids but the actual documents too. GetResult() will perform the
            // HTTP request to CouchDB and return a CouchGenericViewResult which we in turn can ask to produce objects from JSON,
            // in this case we pick out the actual documents and instantiate them as instances of the class Car.
            var cars = db.GetAllDocuments<Car>();
            Console.WriteLine("Loaded all Cars: " + cars.Count());

            // Now try some linq
            var tempView = db.NewTempView("test", "test", "if (doc.docType && doc.docType == 'car') emit(doc.Hps, doc);");
            var linqCars = tempView.LinqQuery<Car>();

            var fastCars = from c in linqCars where c.HorsePowers >= 175 select c;//.Make + " " + c.Model;
            foreach (var fastCar in fastCars)
                Console.WriteLine(fastCar);

            var twoCars = from c in linqCars where c.HorsePowers == 175 || c.HorsePowers == 176 select c;//.Make + " " + c.Model;
            foreach (var twoCar in twoCars)
                Console.WriteLine(twoCar);

            var hps = new int[] {176, 177};
            var twoMoreCars = from c in linqCars where hps.Contains(c.HorsePowers) select c.Make + " " + c.Model + " with " + c.HorsePowers + "HPs";
            foreach (var twoCar in twoMoreCars)
                Console.WriteLine(twoCar);

            // cleanup for later
            db.DeleteDocument(tempView.Doc);

            // Delete some Cars one by one. CouchDB is an MVCC database which means that for every operation that modifies a document
            // we need to supply not only its document id, but also the revision that we are aware of. This means that we must supply id/rev
            // for each document we want to delete.
            foreach (var eachCar in cars.Where(x => x.HorsePowers > 175))
            {
                db.DeleteDocument(eachCar);
                Console.WriteLine("Deleted car with id " + eachCar.Id);
            }

            // Get all cars again and see how many are left.
            Console.WriteLine("Cars left: " + db.GetAllDocuments<Car>().Count());

            //  We can actually also delete in a single bulk call using DeleteDocuments().
            db.DeleteDocuments(cars.ToArray());
            Console.WriteLine("Deleted the rest of the cars");

            // test out the arbitrary conventions
            Console.WriteLine("Trying arbitrary documents");
            var littleCar = new LittleCar() { docType = "car", Make = "Yugo", Model = "Hell if i know" };
            littleCar = db.SaveArbitraryDocument(littleCar);
            
            Console.Write("\r\nDelete database (y/n)? ");
            if (Console.ReadLine().Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                // Delete the db itself
                db.Delete();
                Console.WriteLine("Deleted database.");
            }

            Console.WriteLine("\r\nPress enter to close. ");

            Console.ReadLine();
        }

        private class LittleCar
        {
            private string Id, Rev;
            public string Make, Model, docType;
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

                writer.WritePropertyName("docType");
                writer.WriteValue("car");
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
