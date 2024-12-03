using System;
using System.IO;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

[Serializable]
class LeaderboardData
{
    public int ID;
    public string? Name;
    public int Score;
}
class Program
{
    static void Main(string[] args)
    {
        //connecting to the MongoDB
        // {
        //     // Replace with your MongoDB connection string
        //     string connectionString = "mongodb://localhost:27017";

        //     // Connect to MongoDB
        //     var client = new MongoClient(connectionString);
        //     var database = client.GetDatabase("my_database"); // Replace with your database name
        //     var collection = database.GetCollection<BsonDocument>("my_collection"); // Replace with your collection name

        //     // Create a sample document to insert
        //     var document = new BsonDocument
        //     {
        //         { "name", "John Doe" },
        //         { "age", 30 },
        //         { "email", "johndoe@example.com" },
        //         { "created_at", DateTime.UtcNow }
        //     };

        //     // Insert the document
        //     collection.InsertOne(document);

        //     Console.WriteLine("Document inserted successfully!");
        // }




        // Define the URL and port to listen on
        string url = "http://localhost:8080/";

        // Create an HttpListener
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);

        try
        {
            // Start the listener
            listener.Start();
            Console.WriteLine($"Server started at {url}. Press Ctrl+C to stop.");

            while (true)
            {
                // Wait for a client request
                HttpListenerContext context = listener.GetContext();
                string responseString = string.Empty;
                Console.WriteLine($"URLLOCALPATH:{context?.Request?.Url?.LocalPath}");
                if(context?.Request?.Url?.LocalPath == "/update")
                {
                    responseString = "UpdateResponse";

                }
                else if(context?.Request?.Url?.LocalPath == "/get")
                {
                    responseString = "getResponse";
                }
                // Get the response object
                HttpListenerResponse response = context.Response;

                // Create the response string
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                // Set the response headers and content
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";

                // Write the response and close the connection
                using (Stream output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                }

                Console.WriteLine($"Request served at {DateTime.Now}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Stop the listener when done
            listener.Stop();
        }
    }
}
