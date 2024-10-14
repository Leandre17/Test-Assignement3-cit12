
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Text.Json;


public class Server
{
    private readonly int _port;
    public List<Category> categories = new List<Category>
    {
        new Category { cid = 1, name = "Beverages" },
        new Category { cid = 2, name = "Condiments" },
        new Category { cid = 3, name = "Confections" }
};


    public Server(int port)
    {
        _port = port;
        
        
    }


    public void Run() { 
 
        var server = new TcpListener(IPAddress.Loopback, _port); // IPv4 127.0.0.1 IPv6 ::1
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!!!");

            Task.Run(() => HandleClient(client));

            HandleClient(client);

        }

    }
    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);

            Console.WriteLine("Message from client: " + msg);
            if (msg == "{}")
            {
                var response = new Response
                {
                    Status = "missing Method"
                };

                var json = ToJson(response);
                WriteToStream(stream, json);
            }
            else
            {
                var request = FromJson(msg);
                if(request == null)
                {

                }

               string[] validMethods = ["create", "read", "update", "delete", "echo"];

                if (!validMethods.Contains(request.Method))
                {
                    var response = new Response
                    {
                        Status = "illegal Method"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else
                {
                    var response = HandleRequest(request);
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }

            }

        }
        catch { }
    }

    private Response HandleRequest(Request request) {
        if (request == null || request.Method == null || request.Method == "{}") return new Response { Status = "4 missing Method", Body = "" };
        string error = "";
        string[] PathParts;
        try {
            PathParts = request.Path.Split('/');
        } catch {
            error = "invalid Path";
        }
        int cid = -1;
        switch (request.Method) {
            case "read":
                if (string.IsNullOrEmpty(request.Path))
                    return new Response { Status = "4 Bad Request", Body = "missing or invalid Path" };
                else if (request.Path == "/api/categories")
                    return new Response { Status = "1 Ok", Body = JsonSerializer.Serialize(categories) };
                else if (request.Path.StartsWith("/api/categories/")) {
                    PathParts = request.Path.Split('/');
                    if (PathParts.Length == 3 && int.TryParse(PathParts[2], out cid)) {
                        var category = categories.Find(c => c.cid == cid);
                        if (category != null)
                            return new Response { Status = "1 Ok", Body = JsonSerializer.Serialize(category) };
                        else
                            return new Response { Status = "5 Not Found", Body = "" };
                        
                    }
                }
                return new Response { Status = "4 Bad Request", Body = "invalid Path" };
            case "create":
                if (string.IsNullOrEmpty(request.Body))
                    return new Response { Status = "4 missing Body | missing resource", Body = "" };
                else if (request.Path != "/api/categories") {
                    return new Response { Status = "4 Bad Request", Body = "" };
                }
                var newCategory = JsonSerializer.Deserialize<Category>(request.Body);
                if (newCategory == null || string.IsNullOrEmpty(newCategory.name)) {
                    return new Response { Status = "4 Bad Request", Body = "invalid Body" };
                }
                newCategory.cid = categories.Count + 1;
                categories.Add(newCategory);
                return new Response { Status = "2 Created", Body = JsonSerializer.Serialize(newCategory)};
            case "update":
                if (string.IsNullOrEmpty(request.Body))
                    return new Response { Status = "4 missing body | missing resource", Body = "" };
                else if (string.IsNullOrEmpty(request.Path) || !request.Path.StartsWith("/api/categories/"))
                    return new Response { Status = "4 Bad Request", Body = "missing or invalid Path" };
                PathParts = request.Path.Split('/');
                if (PathParts.Length == 3 && int.TryParse(PathParts[2], out cid)) {
                    var category = categories.Find(c => c.cid == cid);
                    if (category != null) {
                        var upDatedCategory = JsonSerializer.Deserialize<Category>(request.Body);
                        if (upDatedCategory != null && !string.IsNullOrEmpty(upDatedCategory.name)) {
                            category.name = upDatedCategory.name;
                            return new Response { Status = "3 UpDated", Body = "" };
                        }
                    } else
                        return new Response { Status = "5 Not Found", Body = "" };
                }
                return new Response { Status = "4 Bad Request", Body = "invalid Path or Body" };
            case "delete":
                if (string.IsNullOrEmpty(request.Body))
                    return new Response { Status = "4 missing resource", Body = "" };
                else if (string.IsNullOrEmpty(request.Path) || !request.Path.StartsWith("/api/categories/"))
                    return new Response { Status = "4 Bad Request", Body = "missing or invalid Path" };
                PathParts = request.Path.Split('/');
                if (PathParts.Length == 3 && int.TryParse(PathParts[2], out cid)) {
                    var category = categories.Find(c => c.cid == cid);
                    if (category != null) {
                        categories.Remove(category);
                        return new Response { Status = "1 Ok", Body = "" };
                    } else
                        return new Response { Status = "5 Not Found", Body = "" };
                }
                return new Response { Status = "4 Bad Request", Body = "invalid Path" };
            case "echo":
                if (string.IsNullOrEmpty(request.Body))
                    return new Response { Status = "4 missing Body", Body = "" };
                return new Response { Status = "1 OK", Body = request.Body };
            default:
                return new Response { Status = "4 illegal Method", Body = "" };
        }
        return new Response { Status = "4 illegal Method", Body = "" };
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    private void WriteToStream(NetworkStream stream, string msg)
    {
        Console.WriteLine("Message to the client: " + msg);

        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }
    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
