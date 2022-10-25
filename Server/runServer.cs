using server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server
{
    public static class runServer
    {

        public static void runServerNow()
        {
            var categories = new List<Category>();

            var category1 = new Category();
            category1.Cid = 1;
            category1.Name = "Beverages";
            categories.Add(category1);

            var category2 = new Category();
            category2.Cid = 2;
            category2.Name = "Condiments";
            categories.Add(category2);

            var category3 = new Category();
            category3.Cid = 3;
            category3.Name = "Confections";
            categories.Add(category3);

            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                var client = server.AcceptTcpClient();
                Console.WriteLine("Client connected...");

                try
                {
                    HandleClient(client, categories);
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to communicate with client...");
                }

            }

        }

        static void HandleClient(TcpClient client, List<Category>? categories)
        {
            var stream = client.GetStream();

            var buffer = new byte[1024];

            var rcnt = stream.Read(buffer);

            var requestText = Encoding.UTF8.GetString(buffer, 0, rcnt);
            //Console.WriteLine("writing requestText:");
            //Console.WriteLine(requestText);
            //Console.WriteLine("writing requestText done.");

            var lines = requestText.Split("\n");
            //Console.WriteLine(lines[10]);
            //Console.WriteLine(lines[11]);
            //Console.WriteLine(lines[12]);
            //Console.WriteLine(lines[13]);
            //Console.WriteLine(lines[14]);
            //Console.WriteLine(lines[15]);
            var requestString = lines[10] + lines[11] + lines[12] + lines[13] + lines[14] + lines[15];

            var request = JsonSerializer.Deserialize<Request>(requestString);

            if (request != null)
            {
                VerifyRequest(stream, request);
                ServeRequest(stream, request, categories);
            }


            stream.Close();
        }


        static void SendResponse(NetworkStream stream, Response response)
        {
            var responseText = JsonSerializer.Serialize<Response>(response);
            //Console.WriteLine("reponse:");
            //Console.WriteLine(responseText);
            var resFinal = "HTTP/1.1 200 Ok\nContent-Type: text/plain\n\n" + responseText;
            var responseBuffer = Encoding.UTF8.GetBytes(resFinal);
            stream.Write(responseBuffer);
        }

        static Response CreateReponse(string status, string body = "")
        {
            return new Response
            {
                Status = status,
                Body = body
            };
        }

        static void VerifyRequest(NetworkStream stream, Request request)
        {
            if (string.IsNullOrEmpty(request?.Method) && string.IsNullOrEmpty(request?.Path) && request?.Date == 0)
            {
                Response response = CreateReponse("4 missing date, missing path, missing method");
                SendResponse(stream, response);
            }
            if (string.IsNullOrEmpty(request?.Method))
            {
                Response response = CreateReponse("4 Missing method");
                SendResponse(stream, response);
            }
            if (string.IsNullOrEmpty(request?.Path))
            {
                Response response = CreateReponse("4 Missing path");
                SendResponse(stream, response);
            }
            if (request?.Date == null || request?.Date == 0)
            {
                Response response = CreateReponse("4", "4 Missing date");
                SendResponse(stream, response);
            }
            if (request?.Method == "update" && string.IsNullOrEmpty(request?.Body))
            {
                Response response = CreateReponse("4 body is required for update");
                SendResponse(stream, response);
            }
            if (request?.Method == "create" && string.IsNullOrEmpty(request?.Body))
            {
                Response response = CreateReponse("4 body is required for create");
                SendResponse(stream, response);
            }
        }

        static void ServeRequest(NetworkStream stream, Request request, List<Category>? categories)
        {
            if (request?.Method == "read")
            {
                if (request.Path.Split("/").Length == 4)
                {
                    var pathCid = request.Path.Split("/").Last();
                    var category = categories.Where(c => c.Cid == int.Parse(pathCid));

                    Response response = CreateReponse("1 Ok", JsonSerializer.Serialize<Category>(category.First()));
                    SendResponse(stream, response);
                }
                if (request.Path.Split("/").Length == 3)
                {
                    Response response = CreateReponse("1 Ok", JsonSerializer.Serialize<List<Category>>(categories));
                    SendResponse(stream, response);
                }

            }
            if (request?.Method == "update")
            {
                if (request.Path.Split("/").Length == 4)
                {
                    var requestObject = JsonSerializer.Deserialize<Category>(request.Body);
                    // delete element for categories

                    Response response = CreateReponse("3 Updated");
                    SendResponse(stream, response);
                }
            }
            if (request.Path.Split("/").Length == 3)
            {
                Response response = CreateReponse("4 Bad request");
                SendResponse(stream, response);
            }
        }

    }
}
