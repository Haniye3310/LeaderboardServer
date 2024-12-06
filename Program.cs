using System;
using System.IO;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

[Serializable]
class LeaderboardData 
{
    [BsonId] // Indicates this is the primary key
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public int ID { get; set; }
    public string? Name { get; set; }
    public int Score { get; set; }
}
class Program
{
    static void Main(string[] args)
    {
       

        IMongoDatabase database;
        //connecting to the MongoDB
        {
            // Replace with your MongoDB connection string
            string connectionString = "mongodb://adminUser:strongPassword@localhost:27017/?authSource=admin&authMechanism=SCRAM-SHA-256";

            // Connect to MongoDB
            var client = new MongoClient(connectionString);
            database = client.GetDatabase("LeaderboardProject"); // Replace with your database name

        }

        List<LeaderboardData> leaderboardDatas = new List<LeaderboardData>();
        {
            var collection = database.GetCollection<LeaderboardData>("Leaderboard");

            // Fetch all documents from MongoDB
            leaderboardDatas = collection.Find<LeaderboardData>(new BsonDocument()).ToList();
        }



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
                if (context?.Request?.Url?.LocalPath == "/update")
                {
                    // Read the JSON payload
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();
                        Console.WriteLine($"Received JSON: {json}");
                        LeaderboardData leaderboardData;

                        try
                        {
                            // Deserialize JSON into LeaderboardData
                            leaderboardData = System.Text.Json.JsonSerializer.Deserialize<LeaderboardData>(json);

                            if (leaderboardData != null)
                            {
                                var collection = database.GetCollection<LeaderboardData>("Leaderboard");

                                // Check if the ID exists and perform upsert
                                var filter = Builders<LeaderboardData>.Filter.Eq(ld => ld.ID, leaderboardData.ID);
                                var update = Builders<LeaderboardData>.Update
                                    .Set(ld => ld.Name, leaderboardData.Name)
                                    .Set(ld => ld.Score, leaderboardData.Score);

                                var updateOptions = new UpdateOptions { IsUpsert = true };

                                // Perform the upsert operation
                                var result = collection.UpdateOne(filter, update, updateOptions);

                                // Update in memory database
                                {
                                     int index = leaderboardDatas.FindIndex(X=>X.Id== leaderboardData.Id);
                                     if(index!= -1)
                                     {
                                        leaderboardDatas[index].Name = leaderboardData.Name;
                                        leaderboardDatas[index].Score = leaderboardData.Score;
                                     }
                                     leaderboardDatas.Sort((l1,l2)=> l1.Score.CompareTo(l2.Score));
                                }

                                if (result.UpsertedId != null)
                                {
                                    Console.WriteLine($"Inserted new document with ID: {result.UpsertedId}");
                                }
                                else
                                {
                                    Console.WriteLine($"Updated existing document with ID: {leaderboardData.ID}");
                                }

                                responseString = "Data received and processed successfully!";
                            }
                            else
                            {
                                responseString = "Invalid JSON format!";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Deserialization failed: {ex.Message}");
                            responseString = "Failed to process the data.";
                        }
                    }
                }


                else if (context?.Request?.Url?.LocalPath == "/get")
                {

                    // Fetch all documents from MongoDB
                    var allDocuments =leaderboardDatas.ToList();

                    // Convert MongoDB documents to JSON
                    string jsonResponse = allDocuments.ToJson();

                    // Send response to the client
                    context.Response.ContentType = "application/json";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                    context.Response.ContentLength64 = buffer.Length;
                    using (Stream output = context.Response.OutputStream)
                    {
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                    }

                    Console.WriteLine("Response sent to client.");
                }
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
