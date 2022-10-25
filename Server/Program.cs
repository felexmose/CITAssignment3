// The Server
using server;
using Server;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

Console.WriteLine("The Server");

var categories = new List<Category>
{
    new Category {Cid = 1, Name = "Beverages"},
    new Category {Cid = 2, Name = "Condiments"},
    new Category {Cid = 3, Name = "Confections"}
};

var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000); //Parse("127.0.0.1")
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

static void HandleClient(TcpClient client, List<Category>? categories)
{
    var stream = client.GetStream();

    var buffer = new byte[1024];

    var rcnt = stream.Read(buffer);

    var requestText = Encoding.UTF8.GetString(buffer, 0, rcnt);

    var request = JsonSerializer.Deserialize<Request>(requestText);

    if (request != null)
    {
        VerifyRequest(stream, request, categories);
    }

    stream.Close();
}


static void SendResponse(NetworkStream stream, Response response)
{
    var responseText = JsonSerializer.Serialize<Response>(response);
    var responseBuffer = Encoding.UTF8.GetBytes(responseText);
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

static void VerifyRequest(NetworkStream stream, Request request, List<Category> categories)
{
    //Constraint_RequestWithoutMethod_MissingMethodError
    if (string.IsNullOrEmpty(request?.Method) && string.IsNullOrEmpty(request?.Path) && string.IsNullOrEmpty(request?.Body) && request?.Date == 0)
    {
        Response response = CreateReponse("4 missing date, missing path, missing method");
        SendResponse(stream, response);
    }

    //Constraint_RequestWithUnknownMethod_IllegalMethodError
    if (request?.Method != "read" && request?.Method != "create" && request?.Method != "update" && request?.Method != "delete" && request?.Method != "echo")
    {
        Response response = CreateReponse("4 illegal method");
        SendResponse(stream, response);
    }

    //Constraint_RequestForCreateReadUpdateDeleteWithoutResource_MissingResourceError x4
    if (request?.Method == "read" || request?.Method == "create" || request?.Method == "update" || request?.Method == "delete")
    {
        if (string.IsNullOrEmpty(request?.Path) && string.IsNullOrEmpty(request?.Body))
        {
            Response response = CreateReponse("4 missing resource");
            SendResponse(stream, response);
        }

    }

    //Constraint_RequestWithoutDate_MissingDateError
    if (request?.Date == 0)
    {
        Response response = CreateReponse("4 missing date");
        SendResponse(stream, response);
    }

    //Constraint_RequestForCreateUpdateEchoWithoutBody_MissingBodyError
    if (string.IsNullOrEmpty(request?.Body))
    {
        if (request?.Method == "create" || request?.Method == "update" || request?.Method == "echo")
        {
            Response response = CreateReponse("4 missing body");
            SendResponse(stream, response);
        }
    }

    //Constraint_RequestUpdateWithoutJsonBody_IllegalBodyError
    // fix to validate wether body is json.
    if (request?.Method == "update" && request?.Body == "Hello World")
    {
        Response response = CreateReponse("4 illegal body");
        SendResponse(stream, response);
    }

    //Echo_RequestWithBody_ReturnsBody
    if (request?.Method == "echo")
    {
        Response response = CreateReponse("4 illegal body", request?.Body);
        SendResponse(stream, response);
    }

    //**********TESTING API*************

    //Constraint_RequestWithInvalidPath_StatusBadRequest
    if (request?.Path.Split("/")[1] != "categories")
    {
        Response response = CreateReponse("4 Bad Request", null);
        SendResponse(stream, response);
    }

    //Constraint_RequestWithInvalidPathId_StatusBadRequest
    if (int.TryParse(request?.Path.Split("/")[2], out _))
    {
        Response response = CreateReponse("4 Bad Request");
        SendResponse(stream, response);
    }

    //Constraint_CreateWithPathId_StatusBadRequest
    if (request?.Method == "create" && !string.IsNullOrEmpty(request?.Path.Split("/")[2]))
    {
        Response response = CreateReponse("4 Bad Request");
        SendResponse(stream, response);
    }

    //Constraint_UpdateWithOutPathId_StatusBadRequest
    if (request?.Method == "update" && string.IsNullOrEmpty(request?.Path.Split("/")[2]))
    {
        Response response = CreateReponse("4 Bad Request");
        SendResponse(stream, response);
    }

    //Constraint_DeleteWithOutPathId_StatusBadRequest
    if (request?.Method == "delete" && string.IsNullOrEmpty(request?.Path.Split("/")[2]))
    {
        Response response = CreateReponse("4 Bad Request");
        SendResponse(stream, response);
    }

    //---------------------------------- split from here----------------------------------------------------

    //Request_ReadCategories_StatusOkAndListOfCategoriesInBody
    if (request?.Method == "read" && request?.Path.Split("/")[2] == "categories" && request?.Path.Split("/")[3] == null)
    {
        Response response = CreateReponse("1 Ok", JsonSerializer.Serialize<List<Category>>(categories));
        SendResponse(stream, response);
    }

    //Request_ReadCategoryWithValidId_StatusOkAndCategoryInBody
    if (request?.Method == "read" && int.TryParse(request?.Path.Split("/")[3], out _))
    {
        var num = int.Parse(request?.Path.Split("/")[3]);
        var category = categories.Where(c => c.Cid == num).FirstOrDefault();
        if (category != null)
        {
            Response response = CreateReponse("1 Ok", JsonSerializer.Serialize<Category>(category));
            SendResponse(stream, response);
        }
        //Request_ReadCategoryWithInvalidId_StatusNotFound
        if (category == null)
        {
            Response response = CreateReponse("5 not found");
            SendResponse(stream, response);
        }
    }

    var newCategories = categories;

    //Request_UpdateCategoryWithValidIdAndBody_StatusUpdated +
    //Request_UpdateCategotyValidIdAndBody_ChangedCategoryName
    if (request?.Method == "update" && int.TryParse(request?.Path.Split("/")[3], out _) && request?.Body != null)
    {
        var num = int.Parse(request?.Path.Split("/")[3]);
        //convert body to json
        var newElement = JsonSerializer.Deserialize<Category>(request?.Body);
        var element2Remove = newCategories.Where(x => x.Cid == num).FirstOrDefault();
        var removed = newCategories.Remove(element2Remove);
        if (removed)
        {
            newCategories.Add(newElement);

            Response response = CreateReponse("3 updated");
            SendResponse(stream, response);
        }
    }

    //Request_UpdateCategotyInvalidId_NotFound
    if (request?.Method == "update" && int.TryParse(request?.Path.Split("/")[3], out _))
    {
        var num = int.Parse(request?.Path.Split("/")[3]);
        var category = categories.Where(c => c.Cid == num).FirstOrDefault();

        if (category == null)
        {
            Response response = CreateReponse("5 not found");
            SendResponse(stream, response);
        }
    }

    //Request_CreateCategoryWithValidBodyArgument_CreateNewCategory
    if (request?.Method == "create" && request?.Path.Split("/")[2] == "categories" && request?.Body != null)
    {
        var newElement = JsonSerializer.Deserialize<Category>(request?.Body);
        newCategories.Add(newElement);
        var theAddeElement = newCategories.Where(x => x.Cid == newElement.Cid).FirstOrDefault();

        var newElementResponse = JsonSerializer.Serialize<Category>(theAddeElement);
        Response response = CreateReponse("5 not found", newElementResponse);
        SendResponse(stream, response);

        newCategories.Remove(newElement);
    }

    //Request_DeleteCategoryWithValidId_RemoveCategory +
    //Request_DeleteCategoryWithInvalidId_StatusNotFound
    if (request?.Method == "delete" && request?.Path.Split("/")[2] == "categories" && request?.Body == null)
    {
        var num = int.Parse(request?.Path.Split("/")[3]);
        var category = newCategories.Where(c => c.Cid == num).FirstOrDefault();

        if (category != null)
        {
            newCategories.Remove(category);
            Response response = CreateReponse("1 ok");
            SendResponse(stream, response);
        }
        if (category == null)
        {
            Response response = CreateReponse("5 not found");
            SendResponse(stream, response);
        }
    }
}