# ApirSample
A working sample using ApirIO nuget package
# Why Apir
If you have a SQL Server Database and want mobile or modern web apps 
to work with your data, you need an API.

With Microsoft tooling that means writing a WebApi application.
Typically you develop your API in C# writing code using Linq and Entity Framework.

In this article I want to give you an alternative. 
Develop a WebApi backend using Stored Procedures. 

I will use my open source nuget package [apirIO](https://www.nuget.org/packages?q=apirio)


# REST, Resources and HTTP

REST is about resources. A resource is an object such as a product or a list of products.

Http is used to manipulate resources with words such as GET, PUT, POST, DELETE.

|    Verb	|  URI |	Description
----------  | ---- |   ------------
GET	| http://myserver/products	| Retrieve an array of products
GET	| http://myserver/products/1 |	Retrieve product with ID=1
PUT	| http://myserver/products/1 |	Update product 1 with values in body
POST |	http://myserver/products |	Add product
DELETE	| http://myserver/products/99 |	Remove product

# Resources and SQL

Using SQL we develop a procedures to Create, Read, Update and DELETE

The HTTP verbs we need are POST, GET, PUT, DELETE

And the stored procedures for the Products Resource are defined as:

~~~~
    CREATE PROCEDURE API_Products_POST(@ID int, @Name VARCHAR(100))
    CREATE PROCEDURE API_Products_GET(@ID int = NULL)
    CREATE PROCEDURE API_Products_PUT(@ID)
    CREATE PROCEDURE API_Products_DELETE(@ID int)

~~~~

## The sample table
You need a test database where we can create the products sample table.
~~~~
CREATE TABLE Products
(
	ProductID int IDENTITY(1,1) PRIMARY KEY,
	ProductName varchar(100) NOT NULL
)
GO
INSERT INTO Products(ProductName) VALUES ('Widget 1')
GO
INSERT INTO Products(ProductName) VALUES ('Widget 2')
GO
~~~~
## Reading and defining a resource.

In the GET procedure we return one or a list of resources. 
It also defines the structure of the resurce. 

~~~~
CREATE PROCEDURE API_Products_Get (@ID int = NULL) 
AS
    SELECT ProductId, ProductName 
        FROM Products 
        WHERE ProductID = @ID OR  @ID IS NULL
~~~~

## Updating a resource
An update procedure will respond to the Put HTTP verb. In this example we want to be able to change the name of the product.
~~~~
CREATE PROCEDURE API_Products_Put(@ID int, @ProductName VARCHAR(100))
AS
    UPDATE Products SET ProductName = @ProductName 
    WHERE ProductID = @ID
~~~~

This may be a good place to introduce som error handling. We see that the two parameters have no default values. A runtime error will occur if this is no value is set.

We could check for a valid @ID 

~~~~
ALTER PROCEDURE API_Products_Put(@ID int, @ProductName VARCHAR(100))
AS
    IF NOT EXISTS(SELECT ProductID FROM Products 
    WHERE ProductID = @ID) 
    BEGIN
            RAISERROR('Unknown Product',1,1)
            RETURN 400
    END
    UPDATE Products SET ProductName = @ProductName 
    WHERE ProductID = @ID
    RETURN 200 –- OK
~~~~

The RAISERROR uses a severity level of 1 which means a warning in TSQL. The execution continues and returns 400 which will be the HTTP return code. The message “Unknown Product” is returned to the caller as a message.
If the UPDATE was successful a 200 is returned.

Note: If you leave out the RETURN 200 a zero will be returned  which will be translated to 200 before returning to the caller.


## Creating a resource 

The POST procedure may be simply

~~~~
CREATE PROCEDURE API_Products_Post(@ProductName VARCHAR(100))
AS
	INSERT INTO Products(ProductName) VALUES(@ProductName)
~~~~



It is useful to be able to return the ID of the newly created row. 
We can do this like:
 
~~~~
CREATE PROCEDURE API_Products_Post(
@ProductName VARCHAR(100), @NewId int OUTPUT)
AS
	INSERT INTO Products(ProductName) VALUES(@ProductName)
	SET @NewId  = @@IDENTITY
	RETURN 200
~~~~
Apir constructs the URI of the new Product and returns it in the HTTP header to the client. 

## Deleting a resource

Finally the delete procedure may be simple:
~~~~
CREATE PROCEDURE API_Products_Delete(@ID int)
AS
	DELETE FROM Products WHERE ProductID = @ID
~~~~

#  Creating the WebApi site
With the four Stored Procedures we have all the code needed for the Product resource.

Lets use Visual Studio 2015 to create the Site.
 
Using the standard ASP.NET Web Application template we create MySqlApi
   
 ![SelectTemplate](img/SelectTemplate.png)

 Start a simple WebApi project

 ![SelectWebApi](img/SelectWebApi.png)

When the project is ready, right click on the project in the Solution Manager 
and select Manage Nuget Packages.

Add the ApirIO package from Nuget.org.

 ![Add Nuget](img/AddNuget.png)

 Add a connection to you database with  connection string in web.config

~~~~
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" connectionString="Data Source=localhost\sqlexpress;Initial Catalog=myDatabase;Integrated Security=True" />
  </connectionStrings>
~~~~


 
#  Start you WebApi

Just hit F5 to start a debug run of the project.

You will get the standard HomePage. If you click on API in the heading, 
the ApiControllers are shown. The example ValuesController are there **and** 
the new products.

Try running the API_Products_Get procedure by entering the URL
~~~~
http://localhost:50608/api/products
~~~~

You will get a list of products in XML or JSON depending on your browser.


# Swagger test and documentation harness

Swashbuckle is a great nuget package that will give you a GUI for testing the API

From the tools menu start package manager console

~~~~
Install-Package Swashbuckle -Version 5.5.3
~~~~

Restart you project and open the URL

~~~~
http://localhost:50608/swagger
~~~~


You have added the Swagger GUI for the API.


# How ApirIO works

When the app starts controllers are generated from the Stored Procedures.
The generated controllers inherit from ASP.NET ApiControllers.

The code is compiled and loaded into the project at runtime. The c# source file is
located in the App_Data folder by default. 

If ApirIO is unable to compile the generated c# code an error log, swaError.txt, is written 
to the same folder. 

ApirIO uses ADO.NET for database access. The connection named "DefaultConnection" is used.

The code is generated each time you start the project. If you add stored procedures or 
changes parameters, you will need to restart the app.


# Documenting the API 

c# developers have documented code with XML comments for years. 
These comments are used by tools such as Visual Studio and also Swashbuckle.
ApirIO lets you comment the stored procedures and moves any comments to the generated c#
code. It is a great for API documentation since it can be used in Swagger and lives with the database.

Lets change the procedure for inserting new products to make it more production ready.

~~~~
--- <summary> 
--- Add a new product to the database
--- </summary>  
--- <remarks> A link to the new product is returned  </remarks> 
--- <response code="201">OK</response>
--- <response code="521">Bad productname</response>
--- <response code="522">Product with name exists</response>
CREATE PROCEDURE API_Products_Post(
        @ProductName VARCHAR(100), 
        @NewId int OUTPUT)
AS
	IF (@ProductName IS NULL OR LEN(@ProductName) < 2) 
    BEGIN
            RAISERROR('Bad product name',1,1)
            RETURN 521
    END
	IF EXISTS (SELECT ProductId FROM Products WHERE ProductName = @ProductName)
    BEGIN
            RAISERROR('Product with that name already exists',1,1)
            RETURN 522
    END

	INSERT INTO Products(ProductName) VALUES(@ProductName)
	SET @NewId  = @@IDENTITY
	RETURN 201
~~~~

One code change is needed for adding the XML comments to Swagger. 
In the App_Start\SwaggerConfig.cs file at line 100 insert the line:

~~~~
    //c.IncludeXmlComments(GetXmlCommentsPath());
    c.IncludeXmlComments(AppDomain.CurrentDomain.GetData("DataDirectory").ToString() + "\\xmlComments.xml");
~~~~


You will also need using "System" for AppDomain.

Try adding products. You will get a 201 when you have successfully added a product.
Watch the Response Headers. It will have 

~~~~
    "location": "http://localhost:50608/api/products/2", 
~~~~

which is the URI of the created resource.



